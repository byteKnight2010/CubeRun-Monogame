using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.Tools;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Tools.Assets;
using static Cube_Run_C_.Tools.Engine;
using static Cube_Run_C_.Tools.BitMask;
using static Cube_Run_C_.Globals.PlayerData;
using RectangleF = System.Drawing.RectangleF;



namespace Cube_Run_C_ {
  public struct BasicSprite {
    public Texture2D Image;
    public Vector2 Position;


    public BasicSprite(Texture2D image, Vector2 position) {
      this.Image = image;
      this.Position = position;
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
    public List<T> SpriteList = new();
    private readonly SpatialGrid Grid = new();
    private byte Properties = 0b00000000;


    public virtual void Add(T sprite) {
      this.SpriteList.Add(sprite);
      this.Grid.Insert(sprite);

      if (this.SpriteList.Count > 100) {
        Set(ref this.Properties, (byte)SpriteGroupProperties.UseQuery, true);
      } else {
        Set(ref this.Properties, (byte)SpriteGroupProperties.GridDirty, true);
      }
    }

    public virtual void Remove(T sprite) {
      bool Removed = this.SpriteList.Remove(sprite);

      if (Removed) {
        if (this.SpriteList.Count <= 100) {
          Set(ref this.Properties, (byte)SpriteGroupProperties.UseQuery, false);
        } else {
          Set(ref this.Properties, (byte)SpriteGroupProperties.GridDirty, true);
        }
      }
    }

    public bool Contains(T sprite) => this.SpriteList.Contains(sprite);

    public virtual void Clear() {
      this.SpriteList.Clear();
      this.Grid.Clear();

      Set(ref this.Properties, (byte)SpriteGroupProperties.GridDirty, false);
      Set(ref this.Properties, (byte)SpriteGroupProperties.UseQuery, false);
    }


    public CollisionResult OverlapsWith(Sprite checkSprite) {
      if (this.SpriteList.Count == 0)
        return new(false, null, null);

      if (IsSet(this.Properties, (byte)SpriteGroupProperties.UseQuery)) {
        this.UpdateGrid();

        List<Sprite> Query = this.Grid.Query(checkSprite.Rect);

        for (int Index = 0; Index < Query.Count; Index++) {
          if (Query[Index].Rect.IntersectsWith(checkSprite.Rect))
            return new(true, Query[Index], checkSprite);
        }
      } else {
        for (int Index = 0; Index < this.SpriteList.Count; Index++) {
          if (this.SpriteList[Index].Rect.IntersectsWith(checkSprite.Rect)) {
            return new(true, this.SpriteList[Index], checkSprite);
          }
        }
      }

      return new(false, null, null);
    }

    public Sprite OverlapsWith(RectangleF checkSpriteRect) {
      if (this.SpriteList.Count == 0)
        return (null);

      if (IsSet(this.Properties, (byte)SpriteGroupProperties.UseQuery)) {
        this.UpdateGrid();

        List<Sprite> Query = this.Grid.Query(checkSpriteRect);

        for (int Index = 0; Index < Query.Count; Index++) {
          if (Query[Index].Rect.IntersectsWith(checkSpriteRect))
            return Query[Index];
        }
      } else {
        for (int Index = 0; Index < this.SpriteList.Count; Index++) {
          if (this.SpriteList[Index].Rect.IntersectsWith(checkSpriteRect))
            return this.SpriteList[Index];
        }
      }

      return (null);
    }

    public CollisionResult OverlapsWith(SpriteGroup<Sprite> group) {
      if (group.SpriteList.Count == 0)
        return new(false, null, null);

      if (IsSet(this.Properties, (byte)SpriteGroupProperties.UseQuery)) {
        this.UpdateGrid();

        for (int Index = 0; Index < group.SpriteList.Count; Index++) {
          List<Sprite> SpriteQuery = this.Grid.Query(group.SpriteList[Index].Rect);

          for (int QueryIndex = 0; QueryIndex < SpriteQuery.Count; QueryIndex++) {
            if (SpriteQuery[QueryIndex].Rect.IntersectsWith(group.SpriteList[Index].Rect))
              return new(true, SpriteQuery[QueryIndex], group.SpriteList[Index]);
          }
        }
      } else {
        for (int GroupIndex = 0; GroupIndex < group.SpriteList.Count; GroupIndex++) {
          for (int Index = 0; Index < this.SpriteList.Count; Index++) {
            if (this.SpriteList[Index].Rect.IntersectsWith(group.SpriteList[GroupIndex].Rect))
              return new(true, this.SpriteList[Index], group.SpriteList[GroupIndex]);
          }
        }
      }

      return new(false, null, null);
    }


    public void UpdateGrid() {
      if (!IsSet(this.Properties, (byte)SpriteGroupProperties.GridDirty) || this.SpriteList.Count == 0) return;

      this.Grid.Clear();
      Set(ref this.Properties, (byte)SpriteGroupProperties.GridDirty, false);

      for (int Index = 0; Index < this.SpriteList.Count; Index++) {
        this.Grid.Insert(this.SpriteList[Index]);
      }
    }

    public void Update(float deltaTime) {
      for (int Index = 0; Index < this.SpriteList.Count; Index++) {
        Sprite Sprite = this.SpriteList[Index];
        Sprite.Update(deltaTime);

        Set(ref this.Properties, (byte)SpriteGroupProperties.SpritesMoved, IsSet(this.Properties, (byte)SpriteGroupProperties.UseQuery) && !IsSet(this.Properties, (byte)SpriteGroupProperties.SpritesMoved) && (Sprite.OldRect.X != Sprite.Rect.X || Sprite.OldRect.Y != Sprite.Rect.Y));
      }

      if (IsSet(this.Properties, (byte)SpriteGroupProperties.SpritesMoved))
        Set(ref this.Properties, (byte)SpriteGroupProperties.GridDirty, true);
    }
  }

  class SpatialGrid {
    public Dictionary<(ushort, ushort), List<Sprite>> Cells = new();
    private List<Sprite> QueryResult = new();
    private HashSet<Sprite> SeenSprites = new();
    private (ushort, ushort) TopLeft;
    private (ushort, ushort) BottomRight;


    public void Insert(Sprite sprite) {
      this.TopLeft = PointToCell(sprite.Rect.TopLeft());
      this.BottomRight = PointToCell(new(sprite.Rect.Right, sprite.Rect.Bottom));


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
      this.TopLeft = PointToCell(sprite.Rect.TopLeft());
      this.BottomRight = PointToCell(new(sprite.Rect.Right, sprite.Rect.Bottom));


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

    public void Clear() {
      this.Cells.Clear();
      this.QueryResult.Clear();
      this.SeenSprites.Clear();
    }


    private static (ushort, ushort) PointToCell(Vector2 point) => ((ushort)(point.X / CELL_SIZE), (ushort)(point.Y / CELL_SIZE));


    public List<Sprite> Query(RectangleF rect) {
      this.TopLeft = PointToCell(new(rect.X, rect.Y));
      this.BottomRight = PointToCell(new(rect.Right, rect.Bottom));

      for (ushort X = TopLeft.Item1; X <= BottomRight.Item1; X++) {
        for (ushort Y = TopLeft.Item2; Y <= BottomRight.Item2; Y++) {
          if (this.Cells.TryGetValue((X, Y), out List<Sprite> List)) {
            for (int Index = 0; Index < List.Count; Index++) {
              if (this.SeenSprites.Add(List[Index]))
                this.QueryResult.Add(List[Index]);
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
    public SpriteTransform Transformations;
    public RectangleF Rect;
    public RectangleF OldRect;
    public Vector2 RotationOffset = Vector2.Zero;
    public byte Z = 0;


    public Sprite(Texture2D texture, Vector2 position, List<Groups> groups, ZLayers z, Directions faceDirection = Directions.Left) {
      this.Animation = null;
      this.Image = texture;
      this.Transformations = DirectionRotations[(int)faceDirection];
      this.Rect = texture.GetRectangleF(position);
      this.OldRect = this.Rect;
      this.Z = (byte)z;

      if (this.Transformations.Rotation != 0f || this.Transformations.Effect != SpriteEffects.None) {
        this.RotationOffset = new(texture.Width >> 1, texture.Height >> 1);
      }

      for (int Index = 0; Index < groups.Count; Index++) {
        if (groups[Index] == Groups.All) {
          Camera.Add(this);
          continue;
        }

        SpriteGroups[(int)groups[Index]].Add(this);
      }
    }

    public Sprite(Animation animation, Vector2 position, List<Groups> groups, ZLayers z, Directions faceDirection = Directions.Left) {
      this.Animation = animation;
      this.Image = null;
      this.Transformations = DirectionRotations[(int)faceDirection];
      this.Rect = new(position.X, position.Y, animation.Size.X, animation.Size.Y);
      this.OldRect = this.Rect;
      this.Z = (byte)z;

      if (this.Transformations.Rotation != 0f || this.Transformations.Effect != SpriteEffects.None) {
        this.RotationOffset = new(animation.Size.X * 0.5f, animation.Size.Y * 0.5f);
      }

      for (int Index = 0; Index < groups.Count; Index++) {
        if (groups[Index] == Groups.All) {
          Camera.Add(this);
          continue;
        }

        SpriteGroups[(int)groups[Index]].Add(this);
      }
    }


    public virtual void Update(float deltaTime) => this.Animation?.Update(deltaTime);


    public Texture2D GetImage() => this.Animation?.SpriteSheet ?? this.Image;
    public Rectangle? GetFrame() => this.Animation?.GetFrame() ?? null;
  }

  public class MovingSprite : Sprite {
    public Vector2 Direction;
    public ushort Speed;


    public MovingSprite(Texture2D texture, Vector2 position, Vector2 direction, ushort speed, List<Groups> groups, ZLayers z, Directions faceDirection = Directions.Left) : base(texture, position, groups, z, faceDirection) {
      this.Direction = direction;
      this.Speed = speed;

      if (this.Direction != Vector2.Zero)
        this.Direction.Normalize();
    }

    public MovingSprite(Animation animation, Vector2 position, Vector2 direction, ushort speed, List<Groups> groups, ZLayers z, Directions faceDirection = Directions.Left) : base(animation, position, groups, z, faceDirection) {
      this.Direction = direction;
      this.Speed = speed;

      if (this.Direction != Vector2.Zero)
        this.Direction.Normalize();
    }


    public override void Update(float deltaTime) {
      this.OldRect = this.Rect;

      this.Rect.X += this.Direction.X * this.Speed * deltaTime;
      this.Rect.Y += this.Direction.Y * this.Speed * deltaTime;

      if (this.OldRect.X != this.Rect.X || this.OldRect.Y != this.Rect.Y)
        Camera.UpdateSpritePosition(this);

      base.Update(deltaTime);
    }
  }

  public class RotatingSprite : Sprite {
    private Circle Circle;
    private float Theta = 0f;
    public float Speed;
    public bool Clockwise; 


    public RotatingSprite(Texture2D texture, Circle circle, bool clockwise, float speed, List<Groups> groups, ZLayers z) : base(texture, circle.Center, groups, z) {
      this.Circle = circle;
      this.Speed = speed;
      this.Clockwise = clockwise;
    }

    public RotatingSprite(Animation animation, Circle circle, bool clockwise, float speed, List<Groups> groups, ZLayers z) : base(animation, circle.Center, groups, z) {
      this.Circle = circle;
      this.Speed = speed;
      this.Clockwise = clockwise;
    }


    public override void Update(float deltaTime) {
      this.OldRect = this.Rect;
      this.Theta += ToRadians(this.Speed * deltaTime) * (Clockwise ? 1 : -1);

      if (this.Theta >= MathHelper.TwoPi) {
        this.Theta = 0;
      } else if (this.Theta < 0) {
        this.Theta = MathHelper.TwoPi;
      }

      this.Rect.TopLeft(new(this.Circle.Center.X + this.Circle.Radius * MathF.Cos(this.Theta), this.Circle.Center.Y + this.Circle.Radius * MathF.Sin(this.Theta)));
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
    private RectangleF[] CollisionRectangles = [RectangleF.Empty, RectangleF.Empty, RectangleF.Empty, RectangleF.Empty];
    private MovingSprite Platform;
    private Vector2 RespawnPos;
    public Vector2 Direction = Vector2.Zero;
    public Vector2 Acceleration = Vector2.Zero;
    private Vector2 Velocity = Vector2.Zero;
    private IVector2 InputVector = IVector2.Zero;
    public uint Stats = 0x00000000;
    public ushort Deaths = 0;
    public ushort Gravity = LevelData.Gravity;
    public ushort JumpHeight = 600;
    public ushort MovementSpeed = 300;
    public char Size = 'N';
    public char Environment = 'A';
    public Timer[] Timers = [new(250), new(0), new(0), new(0), new(0), new(0), new(200), new(100), new(100), new(200), new(100), new(100)];


   public Player(Vector2 position) : base(GetTexture("Images/PlayerImages/Player"), position, new() { Groups.All }, ZLayers.Player) {
      this.RespawnPos = position;

      Set(ref this.Stats, (uint)PlayerStats.HorizontalMovement, true);
      Set(ref this.Stats, (uint)PlayerStats.CanJump, true);

      this.Timers[(byte)PlayerTimers.RespawnStatus].Activate();
    }


    private void DeathConditions() {
      if (this.Velocity.Y >= 900 && IsSet(this.Stats, (byte)PlayerStats.FallDamageEnabled))
        Set(ref this.Stats, (byte)PlayerStats.FallDamageCondition, true);
      if (this.Velocity.Y == 0 && !IsSet(this.Stats, (uint)PlayerStats.Bottom))
        Set(ref this.Stats, (byte)PlayerStats.FallDamageCondition, false);

      if (IsSet(this.Stats, (uint)PlayerStats.Bottom) && IsSet(this.Stats, (byte)PlayerStats.FallDamageCondition)) {
        Set(ref this.Stats, (byte)PlayerStats.FallDamageCondition, false);

        if (!IsSet(this.Stats, (uint)PlayerStats.Invincibility) && !IsSet(this.Stats, (uint)PlayerStats.Honey))
          this.Death();
      }
    }

    public void Death() {
      if (this.Timers[(byte)PlayerTimers.RespawnStatus].Active || this.Timers[(byte)PlayerTimers.Invincibility].Active)
        return;

      Set(ref this.Stats, (byte)PlayerStats.FallDamageCondition, false);
      this.Rect.X = this.RespawnPos.X;
      this.Rect.Y = this.RespawnPos.Y;
      this.Direction = Vector2.Zero;

      this.Timers[(byte)PlayerTimers.RespawnStatus].Activate();

      Lives--;
    }


    private void Input() {
      this.InputVector = IVector2.Zero;

      if (!this.Timers[(uint)PlayerTimers.RespawnStatus].Active && !this.Timers[(byte)PlayerTimers.SpringMove].Active && !this.Timers[(byte)PlayerTimers.ShieldKnockback].Active) {
        if (IsSet(this.Stats, (uint)PlayerStats.HorizontalMovement)) {
          if (InputManager.CheckAction(GameAction.MoveRight, false))
            this.InputVector.X++;

          if (InputManager.CheckAction(GameAction.MoveLeft, false))
            this.InputVector.X--;

          this.Direction.X = this.InputVector.X * (IsSet(this.Stats, (uint)PlayerStats.Sprint) && InputManager.IsKeyDown(Keys.LeftShift) ? 2 : 1);
        }

        if (IsSet(this.Stats, (uint)PlayerStats.VerticalMovement)) {
          if (InputManager.CheckAction(GameAction.MoveUp, false))
            this.InputVector.Y--;

          if (InputManager.CheckAction(GameAction.MoveDown, false))
            this.InputVector.Y++;

          this.Direction.Y = this.InputVector.Y;
        }
      }

      if (IsSet(this.Stats, (uint)PlayerStats.CanJump) && InputManager.CheckAction(GameAction.Jump, true))
        this.StartJump();

      if (InputManager.IsKeyDown(Keys.Down) || InputManager.IsKeyDown(Keys.S)) {
        if (IsSet(this.Stats, (uint)PlayerStats.Honey) && IsSet(this.Stats, (uint)PlayerStats.StickingCeiling)) {
          this.Rect.Y += 2;
          Set(ref this.Stats, (uint)PlayerStats.StickingCeiling, false);
        }
      }
    }


    public void MovementChange(PlayerStats change) {
      if (IsSet(this.Stats, (uint)change))
        return;

      Set(ref this.Stats, (uint)change, true);

      switch (change) {
        case PlayerStats.Ladder:
          Set(ref this.Stats, (uint)PlayerStats.HorizontalMovement, true);
          Set(ref this.Stats, (uint)PlayerStats.VerticalMovement, true);
          break;
      }
    }

    private void MovementReturn() {
      if (IsSet(this.Stats, (uint)PlayerStats.ReturnedMovement) || !IsSet(this.Stats, (uint)PlayerStats.ReturnMovement) || !Any(this.Stats, 13, 18))
        return;

      Set(ref this.Stats, (uint)PlayerStats.HorizontalMovement, true);
      Set(ref this.Stats, (uint)PlayerStats.VerticalMovement, false);
      this.Gravity = LevelData.Gravity;
      this.JumpHeight = 600;
      this.MovementSpeed = 300;
      Set(ref this.Stats, (uint)PlayerStats.ReturnedMovement, false);
    }


    private void Move(float deltaTime) {
      if (IsSet(this.Stats, (uint)PlayerStats.AutoMove)) {
        this.Velocity.X += this.Acceleration.X * deltaTime;
        this.Rect.X += this.Velocity.X;
      } else {
        this.Velocity.X = this.Direction.X * this.MovementSpeed * deltaTime;
        this.Rect.X += this.Velocity.X;
      }

      if (this.Platform != null) {
        this.Rect.X += this.Platform.Direction.X * this.Platform.Speed * deltaTime;
        this.Rect.Y += this.Platform.Direction.Y * this.Platform.Speed * deltaTime;
      }

      this.Collision(true);

      if (IsSet(this.Stats, (uint)PlayerStats.VerticalMovement)) {
        this.Velocity.Y = this.Direction.Y * this.MovementSpeed * deltaTime;
        this.Rect.Y += this.Velocity.Y;
        return;
      }

      if (IsSet(this.Stats, (uint)PlayerStats.Honey)) {
        if (IsSet(this.Stats, (uint)PlayerStats.Top)) {
          Set(ref this.Stats, (uint)PlayerStats.StickingCeiling, true);
          this.Direction.Y = 0;
          this.Rect.Y -= 1;
        } else if (!this.Timers[(uint)PlayerTimers.WallJump].Active && !IsSet(this.Stats, (uint)PlayerStats.Bottom) && (IsSet(this.Stats, (uint)PlayerStats.Left) || IsSet(this.Stats, (uint)PlayerStats.Right))) {
          this.Direction.Y = 0;
        }
      }

      this.Velocity.Y += this.Gravity * deltaTime;
      this.Rect.Y += this.Velocity.Y * deltaTime;

      this.Collision(false);
    }

    private void StartJump() {
      if (IsSet(this.Stats, (uint)PlayerStats.Bottom) && !IsSet(this.Stats, (uint)PlayerStats.Top)) {
        this.Timers[(uint)PlayerTimers.WallJump].Activate();
        this.Direction.Y = 1;
        this.Velocity.Y = -this.JumpHeight;
        this.Rect.Y -= 2;
      } else {
        if (IsSet(this.Stats, (uint)PlayerStats.Honey) && !this.Timers[(uint)PlayerTimers.WallJump].Active && (IsSet(this.Stats, (uint)PlayerStats.Left) || IsSet(this.Stats, (uint)PlayerStats.Right))) {
          this.Timers[(uint)PlayerTimers.WallJumpStun].Activate();
          this.Direction.Y = 1;
          this.Direction.X = (IsSet(this.Stats, (uint)PlayerStats.Left) ? 1 : -1) * 1.5f;
          this.Velocity.Y = -this.JumpHeight;
        }
      }
    }


    private void CheckContact() {
      Set(ref this.Stats, (uint)PlayerStats.Left, false);
      Set(ref this.Stats, (uint)PlayerStats.Right, false);
      Set(ref this.Stats, (uint)PlayerStats.Top, false);
      Set(ref this.Stats, (uint)PlayerStats.Bottom, false);

      this.CollisionRectangles[0] = new(this.Rect.X - 2, this.Rect.Y + this.Rect.Height * 0.25f, 2, this.Rect.Height);
      this.CollisionRectangles[1] = new(this.Rect.Right, this.Rect.Y + this.Rect.Height * 0.25f, 2, this.Rect.Height * 0.5f);
      this.CollisionRectangles[2] = new(this.Rect.X, this.Rect.Y - this.Rect.Height * 0.25f, this.Rect.Width, this.Rect.Height * 0.25f);
      this.CollisionRectangles[3] = new(this.Rect.X, this.Rect.Bottom, this.Rect.Width, this.Rect.Height * 0.25f);
      this.Platform = null;

      for (int I = 0; I < this.CollisionRectangles.Length; I++) {
        Sprite WallCollision = SpriteGroups[(uint)Groups.Collidable].OverlapsWith(this.CollisionRectangles[I]);
        bool SemiCollision = false;

        if (WallCollision == null) {
          List<Sprite> SemiCollidables = SpriteGroups[(int)Groups.SemiCollidable].SpriteList;

          for (int Index = 0; Index < SemiCollidables.Count; Index++) {

          }
        } else if (I == 3) {
          if (WallCollision is MovingSprite MovingSprite && WallCollision.Rect != WallCollision.OldRect)
            this.Platform = MovingSprite;
        }

        Set(ref this.Stats, (uint)(1 << (I + 8)), WallCollision != null || SemiCollision);
      }
    }

    private void CollisionPosition(float position, bool horizontal) {
      if (horizontal) {
        this.Rect.X = position;
        this.Direction.X = 0;
        this.Velocity.X = 0;
      } else {
        this.Rect.Y = position;
        this.Direction.Y = 0;
        this.Velocity.Y = 0;
      }
    }

    private void HandleWallCollision(Sprite wall, char direction) {
      RectangleF WallRect = wall.Rect;
      bool Horizontal = direction == 'L' || direction == 'R';

      switch (direction) {
        case 'R':
          this.CollisionPosition(WallRect.X - this.Rect.Width, Horizontal);
          break;
        case 'L':
          this.CollisionPosition(WallRect.Right, Horizontal);
          break;
        case 'D':
          this.CollisionPosition(WallRect.Y - this.Rect.Height, Horizontal);
          Set(ref this.Stats, (uint)PlayerStats.Bottom, true);

          if (wall is MovingSprite MovingSprite)
            this.Platform = MovingSprite;
          break;
        case 'U':
          this.CollisionPosition(WallRect.Bottom, Horizontal);
          Set(ref this.Stats, (uint)PlayerStats.Top, true);
          break;
        default:
          this.Rect.TopLeft(wall.Rect.TopLeft());
          this.Direction = Vector2.Zero;
          break;
      }
    }

    private void Collision(bool horizontal) {
      for (int Index = 0; Index < SpriteGroups[(int)Groups.Collidable].SpriteList.Count; Index++) {
        Sprite Wall = SpriteGroups[(int)Groups.Collidable].SpriteList[Index];

        if (!Wall.Rect.IntersectsWith(this.Rect))
          continue;

        this.HandleWallCollision(Wall, CollisionDirection(this, Wall, horizontal ? 'H' : 'V'));
      }
      for (int Index = 0; Index < SpriteGroups[(int)Groups.SemiCollidable].SpriteList.Count; Index++) {
        Spring SemiWall = (Spring)SpriteGroups[(int)Groups.SemiCollidable].SpriteList[Index];

        if (!SemiWall.Rect.IntersectsWith(this.Rect))
          continue;

        char Direction = CollisionDirection(this, SemiWall, horizontal ? 'H' : 'V');

        if (!SemiWall.CollisionDirections.Contains(Direction)) continue;

        this.HandleWallCollision(SemiWall, Direction);
      }
    }


    public void Lantern(bool active) {
      BrightnessEffect.Parameters["LanternEnabled"].SetValue(active);
      Set(ref this.Stats, (uint)PlayerStats.LanternEnabled, active);
    }

    public void ActivateCheckpoint(Vector2 position, byte index) {
      this.RespawnPos = position;
      Set(ref this.Stats, (uint)(1 << (index + 27)), true);
    }

    public void Teleport(Teleporter teleportPortal) {
      this.Rect.TopLeft(Level.TeleportLocations[teleportPortal.ID]);
      this.Direction = Vector2.Zero;
      this.Velocity = Vector2.Zero;
    }


    private void KeepInScreen() {
      Dimensions LevelDimensions = LevelData.Dimensions.Item2;

      if (this.Rect.X < 0) {
        this.CollisionPosition(0, true);
      } else if (this.Rect.Right > LevelDimensions.Width) {
        this.CollisionPosition(LevelDimensions.Width - this.Rect.Width, true);
      }

      if (this.Rect.Y < 0) {
        this.CollisionPosition(0, false);
      } else if (this.Rect.Bottom > LevelDimensions.Height) {
        this.CollisionPosition(LevelDimensions.Height - this.Rect.Height, false);
      }
    }

    private void UpdateTimers() {
      for (int Index = 0; Index < this.Timers.Length; Index++) {
        this.Timers[Index].Update();
      }
    }

    public override void Update(float deltaTime) {
      this.OldRect = this.Rect;

      this.UpdateTimers();
      this.Input();
      this.Move(deltaTime);
      this.KeepInScreen();
      this.CheckContact();
      this.DeathConditions();

      if (this.OldRect.X != this.Rect.X | this.OldRect.Y != this.Rect.Y) 
        Camera.UpdateSpritePosition(this);

      base.Update(deltaTime);
    }
  }


  public class DeathCube : MovingSprite {
    private readonly bool Horizontal;
    private bool Active;


    public DeathCube(Vector2 position, ushort speed, bool horizontal, bool active) : base(GetTexture($"Images/EnemyImages/{(active ? "DeathCube" : "DeadDeathCube")}"), position, new(horizontal ? 1 : 0, horizontal ? 0 : 1), speed, active ? new() { Groups.All, Groups.Damage } : new() { Groups.All, Groups.Damage, Groups.Switch } , ZLayers.Main) {
      this.Horizontal = horizontal;
      this.Active = active;
    }


    public void Activate() {
      if (this.Active)
        return;

      this.Image = GetTexture($"Images/EnemyImages/DeathCube");
      this.Active = true;
    }


    public override void Update(float deltaTime) {
      if (!this.Active)
        return;

      for (int Index = 0; Index < SpriteGroups[(int)Groups.Collidable].SpriteList.Count; Index++) {
        Sprite Wall = SpriteGroups[(int)Groups.Collidable].SpriteList[Index];

        if (!this.Rect.IntersectsWith(Wall.Rect))
          continue;

        switch (CollisionDirection(this, Wall, this.Horizontal ? 'H' : 'V')) {
          case 'L':
          case 'R':
            this.Direction.X *= -1;
            break;
          case 'U':
          case 'D':
            this.Direction.Y *= -1;
            break;
        }
      }


      base.Update(deltaTime);
    }
  }

  public class Prowler : MovingSprite {
    private readonly bool Horizontal;


    public Prowler(Vector2 position, ushort speed, bool horizontal) : base(GetTexture("Images/EnemyImages/Prowler"), position, new(horizontal ? 1 : 0, horizontal ? 0 : 1), speed, new() { Groups.All, Groups.Damage }, ZLayers.Main) {
      this.Horizontal = horizontal;
    }


    public override void Update(float deltaTime) {
      if (this.Horizontal) {
        if (this.Rect.X < Globals.Player.Rect.X) {
          this.Direction.X = 1;
        } else if (this.Rect.X > Globals.Player.Rect.X) {
          this.Direction.X = -1;
        } else {
          this.Direction.X = 0;
        }
      } else {
        if (this.Rect.Y < Globals.Player.Rect.Y) {
          this.Direction.Y = 1;
        } else if (this.Rect.Y > Globals.Player.Rect.Y) {
          this.Direction.Y = -1;
        } else {
          this.Direction.Y = 0;
        }
      }


      for (int Index = 0; Index < SpriteGroups[(int)Groups.Collidable].SpriteList.Count; Index++) {
        Sprite Wall = SpriteGroups[(int)Groups.Collidable].SpriteList[Index];

        if (!this.Rect.IntersectsWith(Wall.Rect))
          continue;

        switch (CollisionDirection(this, Wall, this.Horizontal ? 'H' : 'V')) {
          case 'L':
            this.Rect.X = Wall.Rect.X + this.Rect.Width;
            this.Direction.X = 0;
            break;
          case 'R':
            this.Rect.X = Wall.Rect.X - this.Rect.Width;
            this.Direction.X = 0;
            break;
          case 'U':
            this.Rect.Y = Wall.Rect.Y - this.Rect.Height;
            this.Direction.Y = 0;
            break;
          case 'D':
            this.Rect.Y = Wall.Rect.Y + this.Rect.Height;
            this.Direction.Y = 0;
            break;
        }
      }
      

      base.Update(deltaTime);
    }
  }


  public class Item : Sprite {
    public string DeactivatorImage;
    public bool Active;


    public Item(Texture2D texture, Vector2 position, List<Groups> groups, ZLayers z, string deactiavatorImage, bool active, Directions faceDirection = Directions.Left) : base(texture, position, groups, z, faceDirection) {
      this.DeactivatorImage = deactiavatorImage;
      this.Active = active;
    }

    public Item(Animation animation, Vector2 position, List<Groups> groups, ZLayers z, string deactiavatorImage, bool active, Directions faceDirection = Directions.Left) : base(animation, position, groups, z, faceDirection) {
      this.DeactivatorImage = deactiavatorImage;
      this.Active = active;
    }


    public void Deactivate() {
      if (!this.Active) 
        return;

      this.Image = GetTexture(this.DeactivatorImage);
      this.Active = false;
    }
  }

  public class SwitchBlock : Sprite {
    public string DeactivatorImagePath;


    public SwitchBlock(Texture2D texture, Vector2 position, List<Groups> groups, ZLayers z, string deactivatorImage, Directions faceDirection) : base(texture, position, groups, z, faceDirection) {
      this.DeactivatorImagePath = deactivatorImage;
    }
  }

  public class LifeBlock : Sprite {
    public bool Switch;
    public bool Active = true;


    public LifeBlock(Vector2 position, bool switchBlock) : base(GetTexture($"Images/TileImages/{(switchBlock ? "LifeBlockSwitch" : "LifeBlockOn")}"), position, switchBlock ? [Groups.All, Groups.Switch, Groups.Item] : [Groups.All, Groups.Item], ZLayers.Opaque) {
      this.Switch = switchBlock;
    }


    public void Activate(bool switchActivator) {
      if (this.Switch && switchActivator) {
        this.Image = GetTexture("Images/TileImages/LifeBlockOn");
        this.Switch = false;
        return;
      }

      if (!this.Active || switchActivator)
        return;

      this.Image = GetTexture("Images/CollectableImages/LifeBlockOff");
      this.Active = false;
    }
  }


  public class Teleporter : Sprite {
    public byte ID;
    public bool Active;

    public Teleporter(Texture2D texture, Vector2 position, byte id, bool active) : base(texture, position, active ? [Groups.All, Groups.Teleporter] : [Groups.All, Groups.Teleporter, Groups.Switch], ZLayers.Main) {
      this.ID = id;
      this.Active = active;
    }


    public void Activate() {
      if (this.Active)
        return;

      this.Image = GetTexture("Images/TileImages/TeleportPortalOn");
      this.Active = true;
    }
  }

  public class Spring : Sprite {
    public List<char> CollisionDirections = new() { 'L', 'R', 'U', 'D' };
    private bool Active = false;
    public bool Horizontal;
    public bool Multi;


    public Spring(Animation animation, Vector2 position, char faceDirection, bool multi) : base(animation, position, new() { Groups.All, Groups.Spring }, ZLayers.Main) {
      this.CollisionDirections.Remove(faceDirection);
      this.Horizontal = faceDirection == 'L' || faceDirection == 'R';
      this.Multi = multi;

      if (multi) this.CollisionDirections.Remove(OppositeDirection(faceDirection));
    }


    public override void Update(float deltaTime) {
      if (!this.Active)
        return;
    }
  }
}
