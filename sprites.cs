using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.GameConverter;
using RectangleF = System.Drawing.RectangleF;


namespace Cube_Run_C_ {
  public struct CollisionResult {
    public bool Collided;
    public Sprite SpriteA;
    public Sprite SpriteB;

    public CollisionResult(bool collided, Sprite spriteA, Sprite spriteB) {
      this.Collided = collided;
      this.SpriteA = spriteA;
      this.SpriteB = spriteB;
    }
  }

  public struct Speed {
    public ushort X;
    public ushort Y;

    public Speed(ushort x, ushort y) {
      this.X = x;
      this.Y = y;
    }
  }

  public struct Dimensions {
    public int Width;
    public int Height;

    public Dimensions(int w, int h) {
      this.Width = w;
      this.Height = h;
    }
  }


  public class Animation {
    public List<Texture2D> Frames;
    private TimeSpan StartTime;
    public ushort Interval;
    public bool Loop;
    private ushort CurrentFrame = 0;
    private bool Playing = false;


    public Animation(List<Texture2D> frames, ushort interval, bool loop) {
      this.Frames = frames;
      this.Interval = interval;
      this.Loop = loop;
    }


    public void Start() {
      this.StartTime = CurrentGameTime;
      this.CurrentFrame = 0;
      this.Playing = true;
    }

    public void Update() {
      if (!this.Playing)
        return;

      ushort TargetFrame = (ushort)((CurrentGameTime - StartTime).TotalMilliseconds / this.Interval);

      if (TargetFrame >= this.Frames.Count) {
        if (Loop) {
          StartTime = CurrentGameTime;
          CurrentFrame = 0;
        } else {
          CurrentFrame = (ushort)((ushort)this.Frames.Count - 1);
          Playing = false;
        }
      } else {
        this.CurrentFrame = TargetFrame;
      }
    }


    public Texture2D GetCurrentFrame() => Frames[CurrentFrame];
  }

  public class SpriteGroup<T> where T : Sprite {
    public List<T> SpriteList = new List<T>();
    private SpatialGrid Grid = new SpatialGrid();
    private bool GridDirty = false;


    public void Add(T sprite) {
      this.SpriteList.Add(sprite);
      this.GridDirty = true;
    }

    public void Clear() {
      this.SpriteList.Clear();
      this.GridDirty = false;
    }


    private CollisionResult OverlapsWithSimple(SpriteGroup<Sprite> group) {
      foreach (Sprite CheckSprite in group.SpriteList) {
        foreach (Sprite Sprite in this.SpriteList) {
          if (Sprite.Rect.IntersectsWith(CheckSprite.Rect))
            return new(true, Sprite, CheckSprite);
        }
      }

      return new(false, null, null);
    }

    private CollisionResult OverlapsWithGrid(SpriteGroup<Sprite> group) {
      this.UpdateGrid();

      foreach (Sprite CheckSprite in group.SpriteList) {
        foreach (Sprite Sprite in Grid.Query(CheckSprite.Rect)) {
          if (Sprite.Rect.IntersectsWith(CheckSprite.Rect))
            return new(true, Sprite, CheckSprite);
        }
      }

      return new(false, null, null);
    }

    private CollisionResult OverlapsWithSimple(Sprite checkSprite) {
      foreach (Sprite Sprite in this.SpriteList) {
        if (Sprite.Rect.IntersectsWith(checkSprite.Rect))
          return new CollisionResult(true, Sprite, checkSprite);
      }

      return new CollisionResult(false, null, null);
    }

    private CollisionResult OverlapsWithGrid(Sprite checkSprite) {
      this.UpdateGrid();

      foreach (Sprite Sprite in this.Grid.Query(checkSprite.Rect)) {
        if (Sprite.Rect.IntersectsWith(checkSprite.Rect))
          return new(true, Sprite, checkSprite);
      }

      return new(false, null, null);
    }

    public CollisionResult OverlapsWith(Sprite checkSprite) => this.SpriteList.Count <= 100 ? OverlapsWithSimple(checkSprite) : OverlapsWithGrid(checkSprite);
    public CollisionResult OverlapsWith(SpriteGroup<Sprite> group) => this.SpriteList.Count <= 100 && group.SpriteList.Count <= 100 ? OverlapsWithSimple(group) : OverlapsWithGrid(group);


    public void Draw(SpriteBatch spriteBatch) {
      foreach (Sprite Sprite in this.SpriteList) {
        spriteBatch.Draw(Sprite.GetImage(), Sprite.TopLeft(), Color.White);
      }
    }

    public void UpdateGrid() {
      if (!this.GridDirty)
        return;

      this.Grid.Clear();
      this.GridDirty = false;

      foreach (Sprite Sprite in this.SpriteList) {
        this.Grid.Insert(Sprite);
      }
    }

    public void Update(float deltaTime) {
      foreach (Sprite Sprite in this.SpriteList) {
        Sprite.Update(deltaTime);
      }

      this.GridDirty = true;
    }
  }

  internal class SpatialGrid {
    public Dictionary<(ushort, ushort), List<Sprite>> Cells = new();


    public void Insert(Sprite sprite) {
      (ushort, ushort) TopLeft = PointToCell(new(sprite.Rect.X, sprite.Rect.Y));
      (ushort, ushort) BottomRight = PointToCell(new(sprite.Rect.Right, sprite.Rect.Bottom));

      for (ushort X = TopLeft.Item1; X <= BottomRight.Item1; X++) {
        for (ushort Y = TopLeft.Item2; Y <= BottomRight.Item2; Y++) {
          (ushort, ushort) Cell = (X, Y);

          if (!this.Cells.ContainsKey(Cell)) {
            this.Cells[Cell] = new List<Sprite>();
          }

          this.Cells[Cell].Add(sprite);
        }
      }
    }

    public void Clear() => this.Cells.Clear();

    public IEnumerable<Sprite> Query(RectangleF rect) {
      HashSet<Sprite> Result = new HashSet<Sprite>();
      (ushort, ushort) TopLeft = PointToCell(new(rect.X, rect.Y));
      (ushort, ushort) BottomRight = PointToCell(new(rect.Right, rect.Bottom));

      for (ushort X = TopLeft.Item1; X <= BottomRight.Item1; X++) {
        for (ushort Y = TopLeft.Item2; Y <= BottomRight.Item2; Y++) {
          if (this.Cells.TryGetValue((X, Y), out List<Sprite> List)) {
            foreach (Sprite Sprite in List) {
              Result.Add(Sprite);
            }
          }
        }
      }

      return Result;
    }
  }


  public class Sprite {
    public Animation Animation { get; set; }
    public Texture2D Image;
    public RectangleF Rect;
    public Z_LAYERS z = 0;


    public Sprite(Texture2D texture, Vector2 position, List<string> groups, Z_LAYERS z) {
      this.Animation = null;
      this.Image = texture;
      this.Rect = new RectangleF(position.X, position.Y, texture.Width, texture.Height);
      this.z = z;

      foreach (string group in groups) {
        spriteGroups[group].Add(this);
      }
    }

    public Sprite(Animation animation, Vector2 position, List<string> groups, Z_LAYERS z) {
      this.Animation = animation;
      this.Image = null;
      this.Rect = new RectangleF(position.X, position.Y, animation.Frames[0].Width, animation.Frames[0].Height);
      this.z = z;

      foreach (string group in groups) {
        spriteGroups[group].Add(this);
      }
    }


    public virtual void Update(float deltaTime) {
      this.Animation?.Update();
    }


    public Texture2D GetImage() => this.Animation?.GetCurrentFrame() ?? Image;
    public Vector2 TopLeft() => new(Rect.X, Rect.Y);
  }

  public class MovingSprite : Sprite {
    public Vector2 Direction;
    public Speed Speed;


    public MovingSprite(Texture2D texture, Vector2 position, Vector2 direction, Speed speed, List<string> groups, Z_LAYERS z) : base(texture, position, groups, z) {
      this.Direction = direction;
      this.Speed = speed;

      if (this.Direction != Vector2.Zero)
        this.Direction.Normalize();
    }

    public MovingSprite(Animation animation, Vector2 position, Vector2 direction, Speed speed, List<string> groups, Z_LAYERS z) : base(animation, position, groups, z) {
      this.Direction = direction;
      this.Speed = speed;

      if (this.Direction != Vector2.Zero)
        this.Direction.Normalize();
    }


    public override void Update(float deltaTime) {
      this.Rect.X += this.Direction.X * this.Speed.X * deltaTime;
      this.Rect.Y += this.Direction.Y * this.Speed.Y * deltaTime;
    }
  }


  public class DottedLine {
    public static Texture2D Pixel;
    private Vector2 Direction;
    private Vector2 Start;
    private Color Color;
    private float Distance;
    private float DotSpacing;
    private float Thickness;


    public DottedLine(Vector2 start, Vector2 end, Color color, float dotSpacing, float thickness) {
      if (Pixel == null) {
        Pixel = new Texture2D(Graphics, 1, 1);
        Pixel.SetData(new[] { Color.White });
      }

      this.Direction = end - start;
      this.Start = start;
      this.Color = color;
      this.Distance = this.Direction.Length();
      this.DotSpacing = dotSpacing + thickness;
      this.Thickness = thickness;

      this.Direction.Normalize();
    }


    public void SetPosition(Vector2 start, Vector2 end) {
      this.Direction = end - start;
      this.Start = start;
      this.Distance = this.Direction.Length();
      this.Direction.Normalize();
    }
    public void SetPosition(Vector2 start) => this.Start = start;


    public void Draw(SpriteBatch spriteBatch) {
      for (float i = 0; i < this.Distance; i += this.DotSpacing) {
        spriteBatch.Draw(DottedLine.Pixel, this.Start + this.Direction * i, null, this.Color, 0f, new Vector2(0.5f), this.Thickness, SpriteEffects.None, 0f);
      }
    }
  }


  public class Player : Sprite {
    // Stats
    public Dictionary<string, bool> ActivatedPowers = new() {
      ["Invincibility"] = false,
      ["Auto-Move"] = false,
      ["Flying"] = false,
      ["Frozen"] = false,
      ["Goggles"] = false,
      ["Honey"] = false,
      ["Telescope"] = false
    };
    private bool[] ActivatedCheckpoints = new bool[3];
    private Vector2 RespawnPos;
    public char Size = 'N';
    public char Environment = 'A';
    public ushort Deaths = 0;
    public bool FallDamageEnabled = false;
    private bool FallDamageCondition = false;
    // Movement
    public Dictionary<char, bool> MovementAbility = new() {
      ['X'] = true,
      ['Y'] = false
    };
    public Vector2 Direction = new(0, 0);
    private const byte SPRINT_MULTIPLIER = 2;
    private ushort VY;
    public ushort JumpHeight = 600;
    public ushort MovementSpeed = 300;
    public bool CanJump = true;
    private bool Jumping = false;
    // Collision
    private Dictionary<char, bool> OnSurface = new() {
      ['L'] = false,
      ['R'] = false,
      ['U'] = false,
      ['D'] = false
    };
    private bool Platform;
    private bool StickingCeiling = false;
    // Timers
    public Dictionary<string, Timer> PowerTimers = new() {
      ["Invincibility"] = new(0),
      ["Auto-Move"] = new(0),
      ["Flying"] = new(0),
      ["Frozen"] = new(0),
      ["Goggles"] = new(0),
      ["Honey"] = new(0),
      ["Telescope"] = new(0)
    };
    private Timer WallJumpTimer = new(200);
    private Timer WallJumpStunTimer = new(100);
    private Timer DeathMoveTimer = new(100);
    private Timer InvincibiltyFrames = new(200);


    public Player(Vector2 position) : base(Assets.GetTexture("images/playerImages/player"), position, ["All"], Z_LAYERS.Player) => this.RespawnPos = position;


    public void DeathConditions() {
      if (this.VY >= 900 && this.FallDamageEnabled)
        this.FallDamageCondition = true;
      if (this.VY == 0 && !this.OnSurface['D'])
        this.FallDamageCondition = false;

      if (this.OnSurface['D'] && this.FallDamageCondition) {
        this.FallDamageCondition = false;

        if (!this.ActivatedPowers["Invincibility"] && !this.ActivatedPowers["Honey"])
          this.Death();
      }
    }

    public void Death() {
      if (this.InvincibiltyFrames.Active || this.PowerTimers["Invincibility"].Active)
        return;

      this.FallDamageCondition = false;
      this.Rect.X = this.RespawnPos.X;
      this.Rect.Y = this.RespawnPos.Y;
      this.Direction = new(0, 0);

      this.InvincibiltyFrames.Activate(CurrentGameTime);
      this.DeathMoveTimer.Activate(CurrentGameTime);

      PlayerData.Lives -= 1;
    }

  }
}