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
    private bool SpritesMoved = false;
    private bool UseQuery = false;
    private bool GridDirty = false;


    public virtual void Add(T sprite) {
      this.SpriteList.Add(sprite);

      if (this.SpriteList.Count > 100) {
        this.UseQuery = true;
      } else {
        this.GridDirty = true;
      }
    }

    public virtual void Remove(T sprite) {
      bool Removed = this.SpriteList.Remove(sprite);

      if (Removed) {
        if (this.SpriteList.Count <= 100) {
          this.UseQuery = false;
        } else {
          this.GridDirty = true;
        }
      }
    }

    public virtual void Clear() {
      this.SpriteList.Clear();
      this.Grid.Clear();
      this.GridDirty = false;
      this.UseQuery = false;
    }


    public CollisionResult OverlapsWith(Sprite checkSprite) {
      if (this.UseQuery) {
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

    public (Sprite, bool) OverlapsWith(RectangleF checkSpriteRect) {
      if (this.UseQuery) {
        this.UpdateGrid();

        List<Sprite> Query = this.Grid.Query(checkSpriteRect);

        for (int Index = 0; Index < Query.Count; Index++) {
          if (Query[Index].Rect.IntersectsWith(checkSpriteRect))
            return (Query[Index], true);
        }
      } else {
        for (int Index = 0; Index < this.SpriteList.Count; Index++) {
          if (this.SpriteList[Index].Rect.IntersectsWith(checkSpriteRect))
            return (this.SpriteList[Index], true);
        }
      }

      return (null, false);
    }

    public CollisionResult OverlapsWith(SpriteGroup<Sprite> group) {
      if (group.SpriteList.Count == 0)
        return new(false, null, null);

      if (this.UseQuery) {
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
      if (!this.GridDirty || this.SpriteList.Count == 0) return;

      this.Grid.Clear();
      this.GridDirty = false;

      for (int Index = 0; Index < this.SpriteList.Count; Index++) {
        this.Grid.Insert(this.SpriteList[Index]);
      }
    }

    public void Update(float deltaTime) {
      for (int Index = 0; Index < this.SpriteList.Count; Index++) {
        Sprite Sprite = this.SpriteList[Index];
        Sprite.Update(deltaTime);

        SpritesMoved = this.UseQuery && !SpritesMoved && (Sprite.OldRect.X != Sprite.Rect.X || Sprite.OldRect.Y != Sprite.Rect.Y);
      }

      if (SpritesMoved)
        this.GridDirty = true;
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
    public byte Z = 0;


    public Sprite(Texture2D texture, Vector2 position, List<Groups> groups, ZLayers z, Directions faceDirection = Directions.Left) {
      this.Animation = null;
      this.Image = texture;
      this.Transformations = DirectionRotations[(int)faceDirection];
      this.Rect = texture.GetRectangleF(position);
      this.OldRect = this.Rect;
      this.Z = (byte)z;

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
    public Vector2 Speed;


    public MovingSprite(Texture2D texture, Vector2 position, Vector2 direction, Vector2 speed, List<Groups> groups, ZLayers z, Directions faceDirection = Directions.Left) : base(texture, position, groups, z, faceDirection) {
      this.Direction = direction;
      this.Speed = speed;

      if (this.Direction != Vector2.Zero)
        this.Direction.Normalize();
    }

    public MovingSprite(Animation animation, Vector2 position, Vector2 direction, Vector2 speed, List<Groups> groups, ZLayers z, Directions faceDirection = Directions.Left) : base(animation, position, groups, z, faceDirection) {
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
    private bool[] ActivatedCheckpoints = new bool[3];
    private Vector2 RespawnPos;
    public ushort Deaths = 0;
    public char Size = 'N';
    public char Environment = 'A';
    public byte ActivatedPowers = 0b00000000;
    public byte MovementChangers = 0b00000000;
    public byte Stats = 0b00000000;
    // Movement
    private (sbyte, sbyte) InputVector = (0, 0);
    public Vector2 Direction = Vector2.Zero;
    public Vector2 Acceleration = Vector2.Zero;
    private Vector2 Velocity = Vector2.Zero;
    public ushort Gravity = LevelData.Gravity;
    public ushort JumpHeight = 600;
    public ushort MovementSpeed = 300;
    // Collision
    private MovingSprite Platform;
    private byte OnSurface = 0b00000000;
    // Timers
    public Timer[] Timers = [new(250), new(0), new(0), new(0), new(0), new(0), new(200), new(100), new(100), new(200), new(100), new(100)];


   public Player(Vector2 position) : base(GetTexture("Images/PlayerImages/Player"), position, new() { Groups.All }, ZLayers.Player) {
      this.RespawnPos = position;

      Set(ref this.Stats, (byte)PlayerStats.HorizontalMovement, true);
      Set(ref this.Stats, (byte)PlayerStats.CanJump, true);

      this.Timers[(byte)PlayerTimers.RespawnStatus].Activate();
    }


    private void DeathConditions() {
      if (this.Velocity.Y >= 900 && IsSet(this.Stats, (byte)PlayerStats.FallDamageEnabled))
        Set(ref this.Stats, (byte)PlayerStats.FallDamageCondition, true);
      if (this.Velocity.Y == 0 && !IsSet(this.OnSurface, (byte)PlayerSurfaces.Bottom))
        Set(ref this.Stats, (byte)PlayerStats.FallDamageCondition, false);

      if (IsSet(this.OnSurface, (byte)PlayerSurfaces.Bottom) && IsSet(this.Stats, (byte)PlayerStats.FallDamageCondition)) {
        Set(ref this.Stats, (byte)PlayerStats.FallDamageCondition, false);

        if (!IsSet(this.ActivatedPowers, (byte)PlayerPowers.Invincibility) && !IsSet(this.ActivatedPowers, (byte)PlayerPowers.Honey))
          this.Death();
      }
    }

    public void Death() {
      if (this.Timers[(byte)PlayerTimers.RespawnStatus].Active || this.Timers[(byte)PlayerTimers.Invincibility].Active) return;

      Set(ref this.Stats, (byte)PlayerStats.FallDamageCondition, false);
      this.Rect.X = this.RespawnPos.X;
      this.Rect.Y = this.RespawnPos.Y;
      this.Direction = Vector2.Zero;

      this.Timers[(byte)PlayerTimers.RespawnStatus].Activate();

      Lives -= 1;
    }


    private void Input() {
      this.InputVector = (0, 0);

      if (!this.Timers[(byte)PlayerTimers.RespawnStatus].Active && !this.Timers[(byte)PlayerTimers.SpringMove].Active && !this.Timers[(byte)PlayerTimers.ShieldKnockback].Active) {
        if (IsSet(this.Stats, (byte)PlayerStats.HorizontalMovement)) {
          if (InputManager.CheckAction(GameAction.MoveRight, false))
            this.InputVector.Item1++;

          if (InputManager.CheckAction(GameAction.MoveLeft, false))
            this.InputVector.Item1--;

          this.Direction.X = this.InputVector.Item1 * (IsSet(this.ActivatedPowers, (byte)PlayerPowers.Sprint) && InputManager.IsKeyDown(Keys.LeftShift) ? 2 : 1);
        }

        if (IsSet(this.Stats, (byte)PlayerStats.VerticalMovement)) {
          if (InputManager.CheckAction(GameAction.MoveUp, false))
            this.InputVector.Item2--;

          if (InputManager.CheckAction(GameAction.MoveDown, false))
            this.InputVector.Item2++;

          this.Direction.Y = this.InputVector.Item2;
        }
      }

      if (IsSet(this.Stats, (byte)PlayerStats.CanJump) && InputManager.CheckAction(GameAction.Jump, true))
        this.StartJump();

      if (InputManager.IsKeyDown(Keys.Down) || InputManager.IsKeyDown(Keys.S)) {
        if (IsSet(this.ActivatedPowers, (byte)PlayerPowers.Honey) && IsSet(this.OnSurface, (byte)PlayerSurfaces.StickingCeiling)) {
          this.Rect.Y += 2;
          Set(ref this.OnSurface, (byte)PlayerSurfaces.StickingCeiling, false);
        }
      }
    }


    public void MovementChange(PlayerMovers change) {
      if (IsSet(this.MovementChangers, (byte)change))
        return;

      Set(ref this.MovementChangers, (byte)change, true);

      switch (change) {
        case PlayerMovers.Ladder:
          Set(ref this.Stats, (byte)PlayerStats.HorizontalMovement, true);
          Set(ref this.Stats, (byte)PlayerStats.VerticalMovement, true);
          break;
      }
    }

    private void MovementReturn() {
      if (IsSet(this.Stats, (byte)PlayerStats.ReturnedMovement) || !IsSet(this.Stats, (byte)PlayerStats.ReturnMovement) || !Any(this.MovementChangers))
        return;

      Set(ref this.Stats, (byte)PlayerStats.HorizontalMovement, true);
      Set(ref this.Stats, (byte)PlayerStats.VerticalMovement, false);
      this.Gravity = LevelData.Gravity;
      this.JumpHeight = 600;
      this.MovementSpeed = 300;
      Set(ref this.Stats, (byte)PlayerStats.ReturnedMovement, false);
    }


    private void Move(float deltaTime) {
      if (IsSet(this.ActivatedPowers, (byte)PlayerPowers.AutoMove)) {
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

      if (IsSet(this.ActivatedPowers, (byte)PlayerStats.VerticalMovement)) {
        this.Velocity.Y = this.Direction.Y * this.MovementSpeed * deltaTime;
        this.Rect.Y += this.Velocity.Y;
        return;
      }

      if (IsSet(this.ActivatedPowers, (byte)PlayerPowers.Honey)) {
        if (IsSet(this.OnSurface, (byte)PlayerSurfaces.Top)) {
          Set(ref this.OnSurface, (byte)PlayerSurfaces.StickingCeiling, true);
          this.Direction.Y = 0;
          this.Rect.Y -= 1;
        } else if (!this.Timers[(byte)PlayerTimers.WallJump].Active && !IsSet(this.OnSurface, (byte)PlayerSurfaces.Bottom) && (IsSet(this.OnSurface, (byte)PlayerSurfaces.Left) || IsSet(this.OnSurface, (byte)PlayerSurfaces.Right))) {
          this.Direction.Y = 0;
        }
      }

      this.Velocity.Y += this.Gravity * deltaTime;
      this.Rect.Y += this.Velocity.Y * deltaTime;

      this.Collision(false);
    }

    private void StartJump() {
      this.Direction.Y = 1;

      if (IsSet(this.OnSurface, (byte)PlayerSurfaces.Bottom)) {
        this.Timers[(byte)PlayerTimers.WallJump].Activate();
        this.Velocity.Y = -this.JumpHeight;
        this.Rect.Y -= 2;
      } else {
        if (IsSet(this.ActivatedPowers, (byte)PlayerPowers.Honey) && !this.Timers[(byte)PlayerTimers.WallJump].Active && (IsSet(this.OnSurface, (byte)PlayerSurfaces.Left) || IsSet(this.OnSurface, (byte)PlayerSurfaces.Right))) {
          this.Timers[(byte)PlayerTimers.WallJumpStun].Activate();
          this.Velocity.Y = -this.JumpHeight;
          this.Direction.X = (IsSet(this.OnSurface, (byte)PlayerSurfaces.Left) ? 1 : -1) * 1.5f;
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

      for (int I = 0; I < Rectangles.Length; I++) {
        (Sprite, bool) WallCollision = SpriteGroups[(uint)Groups.Collidable].OverlapsWith(Rectangles[I]);
        bool SemiCollision = false;

        if (!WallCollision.Item2) {
          List<Sprite> SemiCollidables = SpriteGroups[(int)Groups.SemiCollidable].SpriteList;

          for (int Index = 0; Index < SemiCollidables.Count; Index++) {

          }
        } else if (I == 3) {
          if (WallCollision.Item1 is MovingSprite MovingSprite && WallCollision.Item1.Rect != WallCollision.Item1.OldRect)
            this.Platform = MovingSprite;
        }

        Set(ref this.OnSurface, (byte)(1 << I), WallCollision.Item2 || SemiCollision);
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
        case 'L':
          this.CollisionPosition(WallRect.X - this.Rect.Width, Horizontal);
          break;
        case 'R':
          this.CollisionPosition(WallRect.Right, Horizontal);
          break;
        case 'U':
          this.CollisionPosition(WallRect.Y - this.Rect.Height, Horizontal);
          break;
        case 'D':
          this.CollisionPosition(WallRect.Bottom, Horizontal);
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

        if (!Wall.Rect.IntersectsWith(this.Rect)) continue;

        this.HandleWallCollision(Wall, CollisionDirection(this, Wall, horizontal ? 'H' : 'V'));
      }
      for (int Index = 0; Index < SpriteGroups[(int)Groups.SemiCollidable].SpriteList.Count; Index++) {
        Spring SemiWall = (Spring)SpriteGroups[(int)Groups.SemiCollidable].SpriteList[Index];

        if (!SemiWall.Rect.IntersectsWith(this.Rect)) continue;

        char Direction = CollisionDirection(this, SemiWall, horizontal ? 'H' : 'V');

        if (!SemiWall.CollisionDirections.Contains(Direction)) continue;

        this.HandleWallCollision(SemiWall, Direction);
      }
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

      base.Update(deltaTime);
    }
  }


  public class Item : Sprite {
    public string Name;
    public bool Active;


    public Item(Texture2D texture, Vector2 position, List<Groups> groups, ZLayers z, string name, bool active, Directions faceDirection = Directions.Left) : base(texture, position, groups, z, faceDirection) {
      this.Name = name;
      this.Active = active;
    }

    public Item(Animation animation, Vector2 position, List<Groups> groups, ZLayers z, string name, bool active, Directions faceDirection = Directions.Left) : base(animation, position, groups, z, faceDirection) {
      this.Name = name;
      this.Active = active;
    }


    public void Deactivate() {
      if (!this.Active) return;

      this.Image = GetTexture($"Images/TileImages/{Name}Off");
      this.Active = false;
    }
  }


  public class Teleporter : Sprite {
    public byte ID;
    public bool Active;

    public Teleporter(Texture2D texture, Vector2 position, byte id, bool active) : base(texture, position, new() { Groups.All, Groups.Teleporter }, ZLayers.Main) {
      this.ID = id;
      this.Active = active;
    }


    public void Activate() {
      if (this.Active) return;

      this.Image = GetTexture("TeleporterPortalOn");
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
  }
}
