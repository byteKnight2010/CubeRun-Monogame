using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.Globals;
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


  public class Animation {
    public Texture2D SpriteSheet;
    public Vector2 Size;
    private float FrameTimer = 0f;
    private int CurrentFrame = 0;
    private ushort Interval;
    public ushort FrameCount;
    public ushort Columns;
    private bool Loop;
    private bool Playing = false;

    public Animation(Texture2D spriteSheet, ushort frameCount, ushort columns, ushort interval, Vector2 size, bool loop = false) {
      this.SpriteSheet = spriteSheet;
      this.Size = size;
      this.Interval = interval;
      this.FrameCount = frameCount;
      this.Columns = columns;
      this.Loop = loop;
    }


    public void Play() => this.Playing = true;
    public void Pause() => this.Playing = false;
    public void Stop() {
      this.Playing = false;
      this.FrameTimer = 0f;
      this.CurrentFrame = 0;
    }

    public Rectangle GetFrame() => new((int)(this.CurrentFrame % Columns * this.Size.X), (int)(this.CurrentFrame / Columns * this.Size.Y), (int)(this.Size.X), (int)(this.Size.Y));


    public void Update(float deltaTime) {
      if (!this.Playing) return;

      this.FrameTimer += deltaTime;

      if (this.FrameTimer >= this.Interval) {
        this.FrameTimer -= this.Interval;
        this.CurrentFrame++;

        if (this.CurrentFrame >= this.FrameCount) {
          if (this.Loop) {
            this.CurrentFrame = 0;
          } else {
            this.CurrentFrame = this.FrameCount - 1;
            this.Playing = false;
          }
        }
      }
    }
  }

  public class SpriteGroup<T> where T : Sprite {
    public List<T> SpriteList = new List<T>();
    private SpatialGrid Grid = new SpatialGrid();
    private bool GridDirty = false;


    public virtual void Add(T sprite) {
      this.SpriteList.Add(sprite);
      this.GridDirty = true;
    }

    public virtual bool Remove(T sprite) {
      bool Removed = this.SpriteList.Remove(sprite);

      if (Removed)
        this.GridDirty = true;

      return Removed;
    }

    public virtual void Clear() {
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

    private (Sprite, bool) OverlapsWithSimple(RectangleF checkSpriteRect) {
      foreach (Sprite Sprite in this.SpriteList) {
        if (Sprite.Rect.IntersectsWith(checkSpriteRect)) {
          return (Sprite, true);
        }
      }

      return (null, false);
    }

    private CollisionResult OverlapsWithGrid(Sprite checkSprite) {
      this.UpdateGrid();

      foreach (Sprite Sprite in this.Grid.Query(checkSprite.Rect)) {
        if (Sprite.Rect.IntersectsWith(checkSprite.Rect))
          return new(true, Sprite, checkSprite);
      }

      return new(false, null, null);
    }

    private (Sprite, bool) OverlapsWithGrid(RectangleF checkSpriteRect) {
      this.UpdateGrid();

      foreach (Sprite Sprite in this.Grid.Query(checkSpriteRect)) {
        if (Sprite.Rect.IntersectsWith(checkSpriteRect))
          return (Sprite, true);
      }

      return (null, false);
    }

    public CollisionResult OverlapsWith(Sprite checkSprite) => this.SpriteList.Count <= 100 ? OverlapsWithSimple(checkSprite) : OverlapsWithGrid(checkSprite);
    public (Sprite, bool) OverlapsWith(RectangleF checkSpriteRect) => this.SpriteList.Count <= 100 ? OverlapsWithSimple(checkSpriteRect) : OverlapsWithGrid(checkSpriteRect);
    public CollisionResult OverlapsWith(SpriteGroup<Sprite> group) => this.SpriteList.Count <= 100 && group.SpriteList.Count <= 100 ? OverlapsWithSimple(group) : OverlapsWithGrid(group);


    public void UpdateGrid() {
      if (!this.GridDirty) return;

      this.Grid.Clear();
      this.GridDirty = false;

      foreach (Sprite Sprite in this.SpriteList) {
        this.Grid.Insert(Sprite);
      }
    }

    public void Update(float deltaTime) {
      bool SpritesMoved = false;

      foreach (Sprite Sprite in this.SpriteList) {
        RectangleF OldRect = Sprite.Rect;

        Sprite.Update(deltaTime);

        if (!SpritesMoved && OldRect.X != Sprite.Rect.X || OldRect.Y != Sprite.Rect.Y || OldRect.Width != Sprite.Rect.Width || OldRect.Height != Sprite.Rect.Height) {
          SpritesMoved = true;
        }
      }

      if (SpritesMoved)
        this.GridDirty = true;
    }
  }

  class SpatialGrid {
    public Dictionary<(ushort, ushort), List<Sprite>> Cells = new();
    private HashSet<Sprite> QueryResult = new();
    private (ushort, ushort) TopLeft;
    private (ushort, ushort) BottomRight;


    public void Insert(Sprite sprite) {
      this.TopLeft = SpatialGrid.PointToCell(sprite.Rect.TopLeft());
      this.BottomRight = SpatialGrid.PointToCell(new(sprite.Rect.Right, sprite.Rect.Bottom));

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

    public void Remove(Sprite sprite) {
      this.TopLeft = SpatialGrid.PointToCell(sprite.Rect.TopLeft());
      this.BottomRight = SpatialGrid.PointToCell(new(sprite.Rect.Right, sprite.Rect.Bottom));

      for (ushort X = TopLeft.Item1; X <= BottomRight.Item1; X++) {
        for (ushort Y = TopLeft.Item2; Y <= BottomRight.Item2; Y++) {
          if (this.Cells.TryGetValue((X, Y), out List<Sprite> List)) {
            List.Remove(sprite);

            if (List.Count == 0) {
              this.Cells.Remove((X, Y));
            }
          }
        }
      }
    }

    public void Clear() => this.Cells.Clear();


    private static (ushort, ushort) PointToCell(Vector2 point) => ((ushort)(point.X / CELL_SIZE), (ushort)(point.Y / CELL_SIZE));


    public IEnumerable<Sprite> Query(RectangleF rect) {
      this.TopLeft = SpatialGrid.PointToCell(new(rect.X, rect.Y));
      this.BottomRight = SpatialGrid.PointToCell(new(rect.Right, rect.Bottom));

      for (ushort X = TopLeft.Item1; X <= BottomRight.Item1; X++) {
        for (ushort Y = TopLeft.Item2; Y <= BottomRight.Item2; Y++) {
          if (this.Cells.TryGetValue((X, Y), out List<Sprite> List)) {
            foreach (Sprite Sprite in List) {
              this.QueryResult.Add(Sprite);
            }
          }
        }
      }

      return this.QueryResult;
    }
  }


  public class Sprite {
    public Animation Animation;
    public Texture2D Image;
    public RectangleF Rect;
    public RectangleF OldRect = RectangleF.Empty;
    public byte Z = 0;


    public Sprite(Texture2D texture, Vector2 position, List<string> groups, ZLayers z) {
      this.Animation = null;
      this.Image = texture;
      this.Rect = new RectangleF(position.X, position.Y, texture.Width, texture.Height);
      this.Z = (byte)z;

      foreach (string group in groups) {
        if (group == "All") {
          Camera.Add(this);
          continue;
        }

        SpriteGroups[group].Add(this);
      }
    }

    public Sprite(Animation animation, Vector2 position, List<string> groups, ZLayers z) {
      this.Animation = animation;
      this.Image = null;
      this.Rect = new RectangleF(position.X, position.Y, animation.Size.X, animation.Size.Y);
      this.Z = (byte)z;

      foreach (string group in groups) {
        if (group == "All") {
          Camera.Add(this);
          continue;
        }

        SpriteGroups[group].Add(this);
      }
    }


    public virtual void Update(float deltaTime) => this.Animation?.Update(deltaTime);


    public Texture2D GetImage() => this.Animation?.SpriteSheet ?? this.Image;
    public Rectangle? GetFrame() => this.Animation?.GetFrame() ?? null;
  }

  public class MovingSprite : Sprite {
    public Vector2 Direction;
    public Vector2 Speed;


    public MovingSprite(Texture2D texture, Vector2 position, Vector2 direction, Vector2 speed, List<string> groups, ZLayers z) : base(texture, position, groups, z) {
      this.Direction = direction;
      this.Speed = speed;

      if (this.Direction != Vector2.Zero)
        this.Direction.Normalize();
    }

    public MovingSprite(Animation animation, Vector2 position, Vector2 direction, Vector2 speed, List<string> groups, ZLayers z) : base(animation, position, groups, z) {
      this.Direction = direction;
      this.Speed = speed;

      if (this.Direction != Vector2.Zero)
        this.Direction.Normalize();
    }


    public override void Update(float deltaTime) {
      this.OldRect = this.Rect;

      this.Rect.X += this.Direction.X * this.Speed.X * deltaTime;
      this.Rect.Y += this.Direction.Y * this.Speed.Y * deltaTime;

      base.Update(deltaTime);
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
        Pixel = new Texture2D(Assets.GraphicsDevice, 1, 1);
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
      ["Sprint"] = false,
      ["Telescope"] = false
    };
    private (bool, bool, bool) ActivatedCheckpoints = (false, false, false);
    private Vector2 RespawnPos;
    public char Size = 'N';
    public char Environment = 'A';
    public ushort Deaths = 0;
    public bool FallDamageEnabled = false;
    private bool FallDamageCondition = false;
    // Movement
    public (bool, bool) MovementAbility = (true, false);
    private (sbyte, sbyte) InputVector = (0, 0);
    public Vector2 Direction = Vector2.Zero;
    public Vector2 Acceleration = Vector2.Zero;
    private Vector2 Velocity = Vector2.Zero;
    public ushort Gravity = LevelData.Gravity;
    public ushort JumpHeight = 600;
    public ushort MovementSpeed = 300;
    public bool CanJump = true;
    // Collision
    private bool[] OnSurface = new bool[4];
    private MovingSprite Platform;
    private bool StickingCeiling = false;
    // Timers
    public Timer[] Timers = [new(0), new(0), new(0), new(0), new(0), new(0), new(0), new(200), new(100), new(100), new(200), new(100), new(100)];


    public Player(Vector2 position) : base(Assets.GetTexture("images/playerImages/player"), position, new() { "All" }, ZLayers.Player) => this.RespawnPos = position;


    private void DeathConditions() {
      if (this.Velocity.Y >= 900 && this.FallDamageEnabled)
        this.FallDamageCondition = true;
      if (this.Velocity.Y == 0 && !this.OnSurface[(byte)Directions.Down])
        this.FallDamageCondition = false;

      if (this.OnSurface[(byte)Directions.Down] && this.FallDamageCondition) {
        this.FallDamageCondition = false;

        if (!this.ActivatedPowers["Invincibility"] && !this.ActivatedPowers["Honey"])
          this.Death();
      }
    }

    public void Death() {
      if (this.Timers[(byte)PlayerTimers.RespawnInvincibility].Active || this.Timers[(byte)PlayerTimers.Invincibility].Active) return;

      this.FallDamageCondition = false;
      this.Rect.X = this.RespawnPos.X;
      this.Rect.Y = this.RespawnPos.Y;
      this.Direction = new(0, 0);

      this.Timers[(byte)PlayerTimers.RespawnInvincibility].Activate();
      this.Timers[(byte)PlayerTimers.DeathStun].Activate();

      PlayerData.Lives -= 1;
    }


    private void Input() {
      this.InputVector = (0, 0);

      if (this.MovementAbility.Item1) {
        if (!this.Timers[(byte)PlayerTimers.SpringMove].Active && !this.Timers[(byte)PlayerTimers.ShieldKnockback].Active) {
          if (InputManager.CheckAction(GameAction.MoveRight, false))
            this.InputVector.Item1++;

          if (InputManager.CheckAction(GameAction.MoveLeft, false))
            this.InputVector.Item1--;

          this.Direction.X = this.InputVector.Item1 * (this.ActivatedPowers["Sprint"] && InputManager.IsKeyDown(Keys.LeftShift) ? 2 : 1);
        }
      }

      if (this.MovementAbility.Item2) {
        if (!this.Timers[(byte)PlayerTimers.DeathStun].Active) {
          if (InputManager.CheckAction(GameAction.MoveUp, false))
            this.InputVector.Item2--;

          if (InputManager.CheckAction(GameAction.MoveDown, false))
            this.InputVector.Item2++;
        }

        this.Direction.Y = this.InputVector.Item2;
      }

      if (this.CanJump && InputManager.CheckAction(GameAction.Jump, true))
        this.StartJump();

      if (InputManager.IsKeyDown(Keys.Down) || InputManager.IsKeyDown(Keys.S)) {
        if (this.ActivatedPowers["Honey"] && this.StickingCeiling) {
          this.Rect.Y += 2;
          this.StickingCeiling = false;
        }
      }
    }

    public void ReturnMovement() {
      this.MovementAbility = (true, true);
      this.Gravity = LevelData.Gravity;
    }


    private void Move(float deltaTime) {
      if (this.ActivatedPowers["Auto-Move"]) {
        this.Velocity.X += this.Acceleration.X * deltaTime;
        this.Rect.X += this.Velocity.X;
      } else {
        this.Velocity.X = this.Direction.X * this.MovementSpeed * deltaTime;
        this.Rect.X += this.Velocity.X;
      }

      if (this.Platform != null) {
        this.Rect.X += this.Platform.Direction.X * this.Platform.Speed.X * deltaTime;
        this.Rect.Y += this.Platform.Direction.Y * this.Platform.Speed.Y * deltaTime;
      }

      this.Collision(true);

      if (this.MovementAbility.Item2) {
        this.Velocity.Y = this.Direction.Y * this.MovementSpeed * deltaTime;
        this.Rect.Y += this.Velocity.Y;
        return;
      }

      if (this.ActivatedPowers["Honey"]) {
        if (this.OnSurface[(byte)Directions.Up]) {
          this.StickingCeiling = true;
          this.Direction.Y = 0;
          this.Rect.Y -= 1;
        } else if (!this.Timers[(byte)PlayerTimers.WallJump].Active && !this.OnSurface[(byte)Directions.Down] && (this.OnSurface[(byte)Directions.Left] || this.OnSurface[(byte)Directions.Right])) {
          this.Direction.Y = 0;
        }
      }

      this.Velocity.Y += this.Gravity * deltaTime;
      this.Rect.Y += this.Velocity.Y * deltaTime;

      this.Collision(false);
    }

    private void StartJump() {
      this.Direction.Y = 1;

      if (this.OnSurface[(byte)Directions.Down]) {
        this.Timers[(byte)PlayerTimers.WallJump].Activate();
        this.Velocity.Y = -this.JumpHeight;
        this.Rect.Y -= 2;
      } else {
        if (this.ActivatedPowers["Honey"] && !this.Timers[(byte)PlayerTimers.WallJump].Active && (this.OnSurface[(byte)Directions.Left] || this.OnSurface[(byte)Directions.Right])) {
          this.Timers[(byte)PlayerTimers.WallJumpStun].Activate();
          this.Velocity.Y = -this.JumpHeight;
          this.Direction.X = (this.OnSurface[(byte)Directions.Left] ? 1 : -1) * 1.5f;
        }
      }
    }


    private void CheckContact() {
      RectangleF[] Rectangles = [
        new(this.Rect.X - 2, this.Rect.Y + this.Rect.Height * 0.25f, 2, this.Rect.Height * 0.5f),
        new(this.Rect.X, this.Rect.Y - 2, this.Rect.Width, 2),
        new(this.Rect.Right, this.Rect.Y + this.Rect.Height * 0.25f, 2, this.Rect.Height * 0.5f),
        new(this.Rect.X, this.Rect.Bottom, this.Rect.Width, 2)
      ];
      this.Platform = null;

      for (ushort I = 0; I < Rectangles.Length; I++) {
        (Sprite, bool) WallCollision = SpriteGroups["Collidable"].OverlapsWith(Rectangles[I]);
        bool SemiCollision = false;

        if (!WallCollision.Item2) {
          foreach (Sprite Sprite in SpriteGroups["Semi-Collidable"].SpriteList) {
            SemiCollision = true;
          }
        } else if (I == 3) {
          if (WallCollision.Item1 is MovingSprite MovingSprite && WallCollision.Item1.Rect != WallCollision.Item1.OldRect) this.Platform = MovingSprite;
        }

        this.OnSurface[I] = WallCollision.Item2 || SemiCollision;
      }
    }

    private void HandleCollision(Sprite wall, char direction) {
      RectangleF WallRect = wall.Rect;

      switch (direction) {
        case 'L':
          this.Rect.X = WallRect.X - this.Rect.Width;
          this.Direction.X = 0;
          this.Velocity.X = 0;
          break;
        case 'R':
          this.Rect.X = WallRect.Right;
          this.Direction.X = 0;
          this.Velocity.X = 0;
          break;
        case 'U':
          this.Rect.Y = WallRect.Y - this.Rect.Height;
          this.Direction.Y = 0;
          this.Velocity.Y = 0;
          break;
        case 'D':
          this.Rect.Y = WallRect.Bottom;
          this.Direction.Y = 0;
          this.Velocity.Y = 0;
          break;
        default:
          this.Rect.TopLeft(wall.Rect.TopLeft());
          this.Direction = new(0, 0);
          break;
      }
    }

    private void Collision(bool horizontal) {
      foreach (Sprite Wall in SpriteGroups["Collidable"].SpriteList) {
        if (!Wall.Rect.IntersectsWith(this.Rect)) continue;

        this.HandleCollision(Wall, Engine.CollisionDirection(this, Wall, horizontal ? 'H' : 'V'));
      }

      foreach (Spring SemiWall in SpriteGroups["Semi-Collidable"].SpriteList.Cast<Spring>()) {
        if (!SemiWall.Rect.IntersectsWith(this.Rect)) continue;

        char CollisionDirection = Engine.CollisionDirection(this, SemiWall, horizontal ? 'H' : 'V');

        if (!SemiWall.CollisionDirections.Contains(CollisionDirection)) continue;

        this.HandleCollision(SemiWall, CollisionDirection);
      }
    }


    private void Teleport() {
      foreach (Teleporter Teleporter in SpriteGroups["Teleporter"].SpriteList) {
        if (Teleporter.Active && Teleporter.Rect.IntersectsWith(this.Rect)) {
          this.Rect.TopLeft(Level.TeleportLocations[Teleporter.ID]);
          return;
        }
      }
    }


    private void UpdateTimers() {
      foreach (Timer Timer in this.Timers) {
        Timer.Update();
      }
    }

    public override void Update(float deltaTime) {
      this.OldRect = this.Rect;

      this.UpdateTimers();
      this.Input();
      this.Move(deltaTime);
      this.CheckContact();
      this.Teleport();
      this.DeathConditions();

      base.Update(deltaTime);
    }
  }

  
  public class Teleporter : Sprite {
    public byte ID;
    public bool Active;

    public Teleporter(Vector2 position, byte id, bool active) : base(Assets.GetTexture($"Images/TileImages/TeleporterPortal{(active ? "On" : "Off")}"), position, new() { "All", "Teleporter" }, ZLayers.Placeholders) {
      this.ID = id;
      this.Active = active;
    }


    public void Activate() {
      if (this.Active) return;

      this.Image = Assets.GetTexture("TeleporterPortalOn");
      this.Active = true;
    }
  }

  public class Spring : Sprite {
    public List<char> CollisionDirections = new() { 'L', 'R', 'U', 'D' };
    public char FaceDirection;
    private bool Active = false;
    public bool Horizontal;
    public bool Multi;


    public Spring(Animation animation, Vector2 position, char faceDirection, bool multi) : base(animation, position, new() { "All", "Spring" }, ZLayers.Main) {
      this.CollisionDirections.Remove(faceDirection);
      this.FaceDirection = faceDirection;
      this.Horizontal = faceDirection == 'L' || faceDirection == 'R';
      this.Multi = multi;

      if (multi) this.CollisionDirections.Remove(OppositeDirections[faceDirection]);
    }
  }
}
