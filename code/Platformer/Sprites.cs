using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.Assets;
using static Cube_Run_C_.Assets.SoundManager;
using static Cube_Run_C_.Assets.VisualManager;
using static Cube_Run_C_.Camera;
using static Cube_Run_C_.ConfigManager;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Globals.LevelData;
using static Cube_Run_C_.Globals.PlayerData;
using static Cube_Run_C_.Tools;
using static Cube_Run_C_.Tools.BitMask;
using static Cube_Run_C_.Tools.Engine;
using static Cube_Run_C_.Tools.GameConverter;
using static Cube_Run_C_.Tools.InputManager;
using RectangleF = System.Drawing.RectangleF;


namespace Cube_Run_C_ {
  public class Sprites {
    public enum Fragment : byte {
      TopLeft = 0,
      TopRight = 1,
      BottomLeft = 2,
      BottomRight = 3,
      LeftHalf = 4,
      RightHalf = 5,
      TopHalf = 6,
      BottomHalf = 7,
      None = 8
    }


    public class BasicSprite {
      public Texture2D Image;
      public RectangleF Rect;


      public BasicSprite(Texture2D image, Vector2 position) {
        this.Image = image;
        this.Rect = image.GetRectangleF(position);
      }
    }

    public readonly struct CollisionResult {
      public readonly Sprite SpriteA;
      public readonly Sprite SpriteB;
      public readonly bool Collided;


      public CollisionResult(bool collided, Sprite spriteA, Sprite spriteB) {
        this.Collided = collided;
        this.SpriteA = spriteA;
        this.SpriteB = spriteB;
      }
    

      public static CollisionResult Empty = new(false, null, null);
    }

    public record struct SpriteTransform {
      public SpriteEffects Effect;
      public Vector2 Scale;
      public float Rotation;


      public SpriteTransform(float rotation, SpriteEffects effect, Vector2 scale) {
        this.Effect = effect;
        this.Scale = scale;
        this.Rotation = rotation;
      }


      public static SpriteTransform Default = new(0f, SpriteEffects.None, Vector2.One);
    }

    public struct AnimationData {
      public Texture2D SpriteSheet;
      public Vector2 FrameSize;
      public ushort FrameCount;
      public float Interval;
      public byte Stats;


      public AnimationData(Texture2D spriteSheet, Vector2 frameSize, ushort frameCount, float intervalMS, bool loop = false, bool reset = false) {
        Set(ref this.Stats, (byte)AnimationFlags.Reset, reset);
        Set(ref this.Stats, (byte)AnimationFlags.Loop, loop);

        this.SpriteSheet = spriteSheet;
        this.FrameSize = frameSize;
        this.FrameCount = frameCount;
        this.Interval = intervalMS / 1000f;
      }


      public static readonly AnimationData Empty = new(null, Vector2.Zero, 0, 0.0f, false, false);
    }


    public class Animation {
      public AnimationData AnimationData;
      public int CurrentFrame = 0;
      private readonly ushort Columns;
      private float FrameTimer = 0f;


      public Animation(AnimationData data) {
        this.AnimationData = data;
        this.Columns = (ushort)(data.SpriteSheet.Width / data.FrameSize.X);
      }


      public void Play() => Set(ref this.AnimationData.Stats, (byte)AnimationFlags.Playing, true);
      public void Pause() => Set(ref this.AnimationData.Stats, (byte)AnimationFlags.Playing, false);
      public void Stop() {
        Set(ref this.AnimationData.Stats, (byte)AnimationFlags.Playing, false);
        this.FrameTimer = 0f;
        this.CurrentFrame = 0;
      }


      public Rectangle GetFrame() => new((int)(this.CurrentFrame % Columns * this.AnimationData.FrameSize.X), (int)(this.CurrentFrame / Columns * this.AnimationData.FrameSize.Y), (int)this.AnimationData.FrameSize.X, (int)this.AnimationData.FrameSize.Y);


      public void Update(float deltaTime) {
        if (!IsSet(this.AnimationData.Stats, (byte)AnimationFlags.Playing))
          return;

        this.FrameTimer += deltaTime;

        if (this.FrameTimer >= this.AnimationData.Interval) {
          this.FrameTimer -= this.AnimationData.Interval;
          this.CurrentFrame++;

          if (this.CurrentFrame >= this.AnimationData.FrameCount) {
            if (IsSet(this.AnimationData.Stats, (byte)AnimationFlags.Loop)) {
              this.CurrentFrame = 0;
            } else {
              this.CurrentFrame = IsSet(this.AnimationData.Stats, (byte)AnimationFlags.Reset) ? 0 : this.AnimationData.FrameCount - 1;
              Set(ref this.AnimationData.Stats, (byte)AnimationFlags.Playing, false);
            }
          }
        }
      }
    }

    public class SpriteGroup<T> where T : Sprite {
      public List<T> SpriteList = [];
      public SpatialGrid Grid = new();
      public byte Properties = 0x00;


      public virtual void Add(T sprite) {
        this.SpriteList.Add(sprite);
        this.Grid.Insert(sprite);

        if (this.SpriteList.Count > Spatial.SPRITE_GROUP_QUERY_THRESHOLD) {
          Set(ref this.Properties, (byte)SpriteGroupProperties.UseQuery, true);
        } else {
          Set(ref this.Properties, (byte)SpriteGroupProperties.GridDirty, true);
        }
      }

      public virtual void Remove(T sprite) {
        bool Removed = this.SpriteList.Remove(sprite);

        if (Removed) {
          this.Grid.Remove(sprite);
          Set(ref this.Properties, (byte)SpriteGroupProperties.GridDirty, true);

          if (this.SpriteList.Count <= Spatial.SPRITE_GROUP_QUERY_THRESHOLD)
            Set(ref this.Properties, (byte)SpriteGroupProperties.UseQuery, false);
        }
      }

      public bool Contains(T sprite) => this.SpriteList.Contains(sprite);

      public virtual void Clear() {
        this.SpriteList.Clear();
        this.Grid.Clear();

        Set(ref this.Properties, (byte)SpriteGroupProperties.GridDirty, false);
        Set(ref this.Properties, (byte)SpriteGroupProperties.UseQuery, false);
      }


      public CollisionResult OverlapsWith(T checkSprite) {
        if (this.SpriteList.Count == 0)
          return CollisionResult.Empty;

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

        return CollisionResult.Empty;
      }

      public Sprite OverlapsWith(RectangleF checkSpriteRect) {
        if (this.SpriteList.Count == 0)
          return null;

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

        return null;
      }

      public CollisionResult OverlapsWith(SpriteGroup<T> group) {
        if (group.SpriteList.Count == 0)
          return CollisionResult.Empty;

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

        return CollisionResult.Empty;
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

    public class SpatialGrid {
      private readonly Dictionary<GridPosition, List<Sprite>> Cells = new();
      private readonly Dictionary<Sprite, List<GridPosition>> OccupiedCells = new();
      private readonly List<Sprite> QueryResult = [];
      private readonly List<GridPosition> TempCellBuffer = [];
      private readonly HashSet<Sprite> SeenSprites = [];
      private GridPosition TopLeft;
      private GridPosition BottomRight;


      public void Insert(Sprite sprite) {
        TempCellBuffer.Clear();

        this.TopLeft = PointToCell(sprite.Rect.TopLeft());
        this.BottomRight = PointToCell(new((ushort)Math.Max(sprite.Rect.X, sprite.Rect.Right - 1f), (ushort)Math.Max(sprite.Rect.Y, sprite.Rect.Bottom - 1f)));
        
        for (ushort X = TopLeft.X; X <= BottomRight.X; X++) {
          for (ushort Y = TopLeft.Y; Y <= BottomRight.Y; Y++) {
            GridPosition Cell = new(X, Y);

            if (!this.Cells.TryGetValue(Cell, out List<Sprite> Sprites)) {
              Sprites = new(Spatial.DEFAULT_CELL_CAPACITY);
              this.Cells[Cell] = Sprites;
            }

            Sprites.Add(sprite);
            TempCellBuffer.Add(Cell);
          }
        }

        OccupiedCells[sprite] = [.. TempCellBuffer];
      }

      public void Remove(Sprite sprite) {
        if (!OccupiedCells.TryGetValue(sprite, out List<GridPosition> SpriteCells))
          return;

        for (int Index = 0; Index < SpriteCells.Count; Index++) {
          if (Cells.TryGetValue(SpriteCells[Index], out List<Sprite> Sprites)) {
            Sprites.Remove(sprite);

            if (Sprites.Count == 0)
              Cells.Remove(SpriteCells[Index]);
          }
        }

        OccupiedCells.Remove(sprite);
      }

      public void Clear() {
        this.Cells.Clear();
        this.OccupiedCells.Clear();
        this.QueryResult.Clear();
        this.SeenSprites.Clear();
      }


      public List<Sprite> Query(RectangleF rect) {
        this.QueryResult.Clear();
        this.SeenSprites.Clear();

        this.TopLeft = PointToCell(new(rect.X, rect.Y));
        this.BottomRight = PointToCell(new((ushort)Math.Max(rect.X, rect.Right - 1f), (ushort)Math.Max(rect.Y, rect.Bottom - 1f)));

        for (ushort X = TopLeft.X; X <= BottomRight.X; X++) {
          for (ushort Y = TopLeft.Y; Y <= BottomRight.Y; Y++) {
            if (this.Cells.TryGetValue(new(X, Y), out List<Sprite> List)) {
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

    public class BasicSpatialGrid {
      private readonly Dictionary<GridPosition, List<BasicSprite>> Cells = new();
      private readonly Dictionary<BasicSprite, List<GridPosition>> OccupiedCells = new();
      private readonly List<BasicSprite> QueryResult = [];
      private readonly List<GridPosition> TempCellBuffer = [];
      private readonly HashSet<BasicSprite> SeenSprites = [];
      private GridPosition TopLeft;
      private GridPosition BottomRight;


      public void Insert(BasicSprite sprite) {
        TempCellBuffer.Clear();

        this.TopLeft = PointToCell(sprite.Rect.TopLeft());
        this.BottomRight = PointToCell(new((ushort)Math.Max(sprite.Rect.X, sprite.Rect.Right - 1f), (ushort)Math.Max(sprite.Rect.Y, sprite.Rect.Bottom - 1f)));

        for (ushort X = TopLeft.X; X <= BottomRight.X; X++) {
          for (ushort Y = TopLeft.Y; Y <= BottomRight.Y; Y++) {
            GridPosition Cell = new(X, Y);

            if (!this.Cells.TryGetValue(Cell, out List<BasicSprite> Sprites)) {
              Sprites = new(Spatial.DEFAULT_CELL_CAPACITY);
              this.Cells[Cell] = Sprites;
            }

            Sprites.Add(sprite);
            TempCellBuffer.Add(Cell);
          }
        }

        OccupiedCells[sprite] = [.. TempCellBuffer];
      }

      public void Remove(BasicSprite sprite) {
        if (!OccupiedCells.TryGetValue(sprite, out List<GridPosition> SpriteCells))
          return;

        for (int Index = 0; Index < SpriteCells.Count; Index++) {
          if (Cells.TryGetValue(SpriteCells[Index], out List<BasicSprite> Sprites)) {
            Sprites.Remove(sprite);

            if (Sprites.Count == 0)
              Cells.Remove(SpriteCells[Index]);
          }
        }

        OccupiedCells.Remove(sprite);
      }

      public void Clear() {
        this.Cells.Clear();
        this.QueryResult.Clear();
        this.SeenSprites.Clear();
      }


      public List<BasicSprite> Query(RectangleF rect) {
        this.QueryResult.Clear();
        this.SeenSprites.Clear();

        this.TopLeft = PointToCell(new(rect.X, rect.Y));
        this.BottomRight = PointToCell(new((ushort)Math.Max(rect.X, rect.Right - 1f), (ushort)Math.Max(rect.Y, rect.Bottom - 1f)));

        for (ushort X = TopLeft.X; X <= BottomRight.X; X++) {
          for (ushort Y = TopLeft.Y; Y <= BottomRight.Y; Y++) {
            if (this.Cells.TryGetValue(new(X, Y), out List<BasicSprite> List)) {
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

    
    public class BasicAnimatedSprite {
      public Animation Animation;
      public RectangleF Rect;


      public BasicAnimatedSprite(Animation animation, Vector2 position) {
        this.Animation = animation;
        this.Rect = new(position.X, position.Y, animation.AnimationData.FrameSize.X, animation.AnimationData.FrameSize.Y);
      }


      public Texture2D GetImage() => this.Animation.AnimationData.SpriteSheet;
      public Rectangle GetFrame() => this.Animation.GetFrame();

      public void Update(float deltaTime) => this.Animation.Update(deltaTime);
    }

    public class Sprite {
      public Animation Animation;
      public Texture2D Image;
      public SpriteTransform Transformations;
      public RectangleF Rect;
      public RectangleF OldRect;
      public Vector2 RotationOffset = Vector2.Zero;
      public Vector2 DrawOffset = Vector2.Zero;
      public byte Effects = 0x00;
      public byte Z = 0;


      public Sprite(Texture2D texture, Vector2 position, List<Groups> groups, ZLayers z, Directions faceDirection = Directions.Right, bool rotationException = false) {
        this.Animation = null;
        this.Image = texture;
        this.Transformations = rotationException ? new(0f, faceDirection == Directions.Down ? SpriteEffects.FlipVertically : SpriteEffects.None, Vector2.One) : DirectionRotations[(int)faceDirection];
        this.Rect = texture.GetRectangleF(position);
        this.OldRect = this.Rect;
        this.Z = (byte)z;

        if (this.Transformations != SpriteTransform.Default)        
          this.ApplyRotationOffset(new(texture.Width >> 1, texture.Height >> 1), faceDirection);

        PopulateGroups(groups);
      }

      public Sprite(Animation animation, Vector2 position, List<Groups> groups, ZLayers z, Directions faceDirection = Directions.Right, bool rotationException = false) {
        this.Animation = animation;
        this.Image = null;
        this.Transformations = rotationException ? new(0f, faceDirection == Directions.Down ? SpriteEffects.FlipVertically : SpriteEffects.None, Vector2.One) : DirectionRotations[(int)faceDirection];
        this.Rect = new(position.X, position.Y, animation.AnimationData.FrameSize.X, animation.AnimationData.FrameSize.Y);
        this.OldRect = this.Rect;
        this.Z = (byte)z;

        if (this.Transformations != SpriteTransform.Default)
          this.ApplyRotationOffset(new(animation.AnimationData.FrameSize.X * 0.5f, animation.AnimationData.FrameSize.Y * 0.5f), faceDirection);

        PopulateGroups(groups);
      }


      private void ApplyRotationOffset(Vector2 rotationOffset, Directions faceDirection) {
        this.RotationOffset = rotationOffset;

        if (faceDirection == Directions.Up || faceDirection == Directions.Down) {
          float Width = this.Rect.Width;
          float Height = this.Rect.Height;
          float AdjustX = (Width - Height) * 0.5f;
          float AdjustY = (Height - Width) * 0.5f;
            
          this.Rect.X += AdjustX;
          this.Rect.Y += AdjustY;
            
          this.DrawOffset.X = rotationOffset.X - AdjustX;
          this.DrawOffset.Y = rotationOffset.Y - AdjustY;
            
          this.Rect.Width = Height;
          this.Rect.Height = Width;
        } else if (faceDirection == Directions.Left) {
          this.DrawOffset = rotationOffset;
        }
      }

      private void PopulateGroups(List<Groups> groups) {
        for (int Index = 0; Index < groups.Count; Index++) {
          if (groups[Index] == Groups.All) {
            Add(this);
            continue;
          }

          SpriteGroups[(int)groups[Index]].Add(this);
        }
      }


      public virtual void Destroy() {
        Remove(this);

        for (int Index = 0; Index < SpriteGroups.Length; Index++) {
          SpriteGroups[Index].Remove(this);
        }
      }

      public virtual void Update(float deltaTime) => this.Animation?.Update(deltaTime);


      public Texture2D GetImage() => this.Animation?.AnimationData.SpriteSheet ?? this.Image;
      public virtual Rectangle? GetFrame() => this.Animation?.GetFrame() ?? null;
    }

    public class TrimmedSprite : Sprite {
      public readonly Fragment FragmentType;
      private readonly Rectangle SourceRectangle;


      public TrimmedSprite(Texture2D texture, Vector2 position, List<Groups> groups, ZLayers z, Fragment fragment, Directions faceDirection = Directions.Right, bool rotationException = false, bool scaleUp = false) : base(texture, position, groups, z, faceDirection, rotationException) {
        this.FragmentType = fragment;
        this.SourceRectangle = CalculateSourceRectangle(texture.GetDimensions(), fragment);
        
        if (scaleUp) {
          this.Transformations.Scale *= CalculateScaleMultiplier(fragment);
        } else {
          Dimensions TextureDimensions = texture.GetDimensions().Half();
          Vector2 Offset = CalculatePositionOffset(TextureDimensions, fragment);
          this.Rect.X += Offset.X;
          this.Rect.Y += Offset.Y;

          AdjustRectForFragment(TextureDimensions, fragment);
        }
      }

      public TrimmedSprite(Animation animation, Vector2 position, List<Groups> groups, ZLayers z, Fragment fragment, Directions faceDirection = Directions.Right, bool rotationException = false, bool scaleUp = false) : base(animation, position, groups, z, faceDirection, rotationException) {
        this.FragmentType = fragment;
        
        if (scaleUp) {
          this.Transformations.Scale *= CalculateScaleMultiplier(fragment);
        } else {
          Dimensions FrameDimensions = animation.AnimationData.FrameSize.ToDimensions().Half();

          Vector2 Offset = CalculatePositionOffset(FrameDimensions, fragment);
          this.Rect.X += Offset.X;
          this.Rect.Y += Offset.Y;

          AdjustRectForFragment(FrameDimensions, fragment);
        }
      }


      
      private static Vector2 CalculateScaleMultiplier(Fragment fragment) {
        return fragment switch {
          Fragment.TopLeft => new(2f, 2f),
          Fragment.TopRight => new(2f, 2f),
          Fragment.BottomLeft => new(2f, 2f),
          Fragment.BottomRight => new(2f, 2f),
          Fragment.LeftHalf => new(2f, 1f),
          Fragment.RightHalf => new(2f, 1f),
          Fragment.TopHalf => new(1f, 2f),
          Fragment.BottomHalf => new(1f, 2f),
          _ => Vector2.One
        };
      }

      private static Vector2 CalculatePositionOffset(Dimensions dimensions, Fragment fragment) {
        return fragment switch {
          Fragment.TopRight => new(dimensions.Width, 0),
          Fragment.BottomLeft => new(0, dimensions.Height),
          Fragment.BottomRight => new(dimensions.Width, dimensions.Height),
          Fragment.RightHalf => new(dimensions.Width, 0),
          Fragment.BottomHalf => new(0, dimensions.Height),
          _ => Vector2.Zero
        };
      }


      private void AdjustRectForFragment(Dimensions dimensions, Fragment fragment) {        
        switch (fragment) {
          case Fragment.TopLeft:
          case Fragment.TopRight:
          case Fragment.BottomLeft:
          case Fragment.BottomRight:
            this.Rect = new(this.Rect.X, this.Rect.Y, dimensions.Width, dimensions.Height);
            break;
          case Fragment.LeftHalf:
          case Fragment.RightHalf:
            this.Rect = new(this.Rect.X, this.Rect.Y, dimensions.Width, dimensions.Height);
            break;
          case Fragment.TopHalf:
          case Fragment.BottomHalf:
            this.Rect = new(this.Rect.X, this.Rect.Y, dimensions.Width, dimensions.Height);
            break;
        }
      }


      private static Rectangle CalculateSourceRectangle(Dimensions textureDimensions, Fragment fragment) {
        Dimensions HalfDimensions = textureDimensions.Half();

        return fragment switch {
          Fragment.TopLeft => new(0, 0, HalfDimensions.Width, HalfDimensions.Height),
          Fragment.TopRight => new(HalfDimensions.Width, 0, HalfDimensions.Width, HalfDimensions.Height),
          Fragment.BottomLeft => new(0, HalfDimensions.Height, HalfDimensions.Width, HalfDimensions.Height),
          Fragment.BottomRight => new(HalfDimensions.Width, HalfDimensions.Height, HalfDimensions.Width, HalfDimensions.Height),
          Fragment.LeftHalf => new(0, 0, HalfDimensions.Width, textureDimensions.Height),
          Fragment.RightHalf => new(HalfDimensions.Width, 0, HalfDimensions.Width, textureDimensions.Height),
          Fragment.TopHalf => new(0, 0, textureDimensions.Width, HalfDimensions.Height),
          Fragment.BottomHalf => new(0, HalfDimensions.Height, textureDimensions.Width, HalfDimensions.Height),
          _ => new(0, 0, textureDimensions.Width, textureDimensions.Height)
        };
      }

      private Rectangle GetTrimmedFrame() {
        if (this.Animation != null) {
          Rectangle BaseFrame = this.Animation.GetFrame();
          Dimensions FrameSize = this.Animation.AnimationData.FrameSize.ToDimensions();
          Dimensions HalfFrameSize = FrameSize.Half();

          return this.FragmentType switch {
            Fragment.TopLeft => new(BaseFrame.X, BaseFrame.Y, HalfFrameSize.Width, HalfFrameSize.Height),
            Fragment.TopRight => new(BaseFrame.X + HalfFrameSize.Width, BaseFrame.Y, HalfFrameSize.Width, HalfFrameSize.Height),
            Fragment.BottomLeft => new(BaseFrame.X, BaseFrame.Y + HalfFrameSize.Height, HalfFrameSize.Width, HalfFrameSize.Height),
            Fragment.BottomRight => new(BaseFrame.X + HalfFrameSize.Width, BaseFrame.Y + HalfFrameSize.Height, HalfFrameSize.Width, HalfFrameSize.Height),
            Fragment.LeftHalf => new(BaseFrame.X, BaseFrame.Y, HalfFrameSize.Width, FrameSize.Height),
            Fragment.RightHalf => new(BaseFrame.X + HalfFrameSize.Width, BaseFrame.Y, HalfFrameSize.Width, FrameSize.Height),
            Fragment.TopHalf => new(BaseFrame.X, BaseFrame.Y, FrameSize.Width, HalfFrameSize.Height),
            Fragment.BottomHalf => new(BaseFrame.X, BaseFrame.Y + HalfFrameSize.Height, FrameSize.Width, HalfFrameSize.Height),
            _ => BaseFrame
          };
        }

        return this.SourceRectangle;
      }

      public override Rectangle? GetFrame() => GetTrimmedFrame();
    }


    public class MovingSprite : Sprite {
      public BVector Direction = BVector.Zero;
      public Vector2 MoveChange = Vector2.Zero;
      public ushort Speed;


      public MovingSprite(Texture2D texture, Vector2 position, BVector direction, ushort speed, List<Groups> groups, ZLayers z, Directions faceDirection = Directions.Right) : base(texture, position, groups, z, faceDirection) {
        this.Direction = direction;
        this.Speed = speed;
      }

      public MovingSprite(Animation animation, Vector2 position, BVector direction, ushort speed, List<Groups> groups, ZLayers z, Directions faceDirection = Directions.Right) : base(animation, position, groups, z, faceDirection) {
        this.Direction = direction;
        this.Speed = speed;
      }


      public override void Update(float deltaTime) {
        this.OldRect = this.Rect;

        this.MoveChange.X = this.Direction.X * this.Speed * deltaTime;
        this.MoveChange.Y = this.Direction.Y * this.Speed * deltaTime;

        this.Rect.X += this.MoveChange.X;
        this.Rect.Y += this.MoveChange.Y;

        if (this.MoveChange != Vector2.Zero)
          UpdateSpritePosition(this);

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

        this.Rect.TopLeft(new Vector2(this.Circle.Center.X + this.Circle.Radius * MathF.Cos(this.Theta), this.Circle.Center.Y + this.Circle.Radius * MathF.Sin(this.Theta)));
        base.Update(deltaTime);
      }
    }

    public class HomingSprite : Sprite {
      public Sprite Target;
      public Vector2 Direction = Vector2.Zero;
      private Vector2 Distance = Vector2.Zero;
      public ushort Speed;
      

      public HomingSprite(Texture2D texture, Vector2 position, Sprite target, ushort speed, List<Groups> groups, ZLayers z) : base(texture, position, groups, z) {
        this.Target = target;
        this.Speed = speed;
      }

      public HomingSprite(Animation animation, Vector2 position, Sprite target, ushort speed, List<Groups> groups, ZLayers z) : base(animation, position, groups, z) {
        this.Target = target;
        this.Speed = speed;
      }


      public override void Update(float deltaTime) {
        if (this.Target != null) {
          this.Distance = this.Target.Rect.Center() - this.Rect.Center();

          if (this.Distance != Vector2.Zero) {
            this.Direction = Vector2.Normalize(this.Distance);
            this.Transformations.Rotation = MathF.Atan2(this.Direction.Y, this.Direction.X);

            this.Rect.X += this.Direction.X * this.Speed * deltaTime;
            this.Rect.Y += this.Direction.Y * this.Speed * deltaTime;
          }
        }

        base.Update(deltaTime);
      }
    }


    public class DottedLine {
      private Vector2 Direction;
      private Vector2 Start;
      private Color Color;
      private float Distance;
      private readonly float DotSpacing;
      private readonly float Thickness;


      public DottedLine(Vector2 start, Vector2 end, Color color, float dotSpacing, float thickness) {
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
        for (float Index = 0; Index < this.Distance; Index += this.DotSpacing) {
          spriteBatch.Draw(ColorTextures[(int)Colors.White], this.Start + this.Direction * Index, null, this.Color, 0f, new Vector2(0.5f), this.Thickness, SpriteEffects.None, 0f);
        }
      }
    }


    public class Player : Sprite {
      private readonly RectangleF[] CollisionRectangles = [RectangleF.Empty, RectangleF.Empty, RectangleF.Empty, RectangleF.Empty];
      private readonly PlayerStats[] CheckpointFlags = [PlayerStats.CheckpointOne, PlayerStats.CheckpointTwo, PlayerStats.CheckpointThree];
      private readonly Texture2D DefaultImage = GetTexture("Images/PlayerImages/Player");
      private MovingSprite Platform;
      private Vector2 RespawnPos;
      public Vector2 Direction = Vector2.Zero;
      public Vector2 Acceleration = Vector2.Zero;
      public Vector2 Velocity = Vector2.Zero;
      private Vector2 InputVector = Vector2.Zero;
      private Animations CurrentAnimation = Animations.PlayerDefault;
      public ulong Stats = 0x0000000000000000;
      public ushort Deaths = 0;
      public ushort Gravity = LevelData.Gravity;
      public float JumpHeight = ConfigManager.Player.JumpHeight;
      public float MovementSpeed = ConfigManager.Player.Speed;
      public float QuicksandTop = 0f;
      public Timer[] Timers = [new(150), null, null, null, null, null, null, null, null, new(200), new(200), new(200), new(250)];


      public Player(Vector2 position) : base(new Animation(AnimationsData[(int)Animations.PlayerDefault]), position, [Groups.All], ZLayers.Player) {
        this.RespawnPos = position;

        for (int Index = 0; Index < PowerDurations.Length; Index++) {
          PlayerPowers CapturedIndex = (PlayerPowers)Index;
          Timers[Index + 1] = new(PowerDurations[Index], () => this.DeactivatePower(CapturedIndex));
        }

        Set(ref this.Stats, (ulong)PlayerStats.HorizontalMovement, true);
        Set(ref this.Stats, (ulong)PlayerStats.CanJump, true);
        Set(ref this.Stats, (ulong)PlayerStats.ReturnMovement, true);

        this.Timers[(byte)PlayerTimers.RespawnStatus].Activate();
      }


      private void PlayAnimation(Animations animation) {
        this.StopAnimation();
        this.CurrentAnimation = animation;
        this.Image = this.Animation.AnimationData.SpriteSheet;
        this.Animation = new(AnimationsData[(int)animation]);
        this.Animation.Play();

        Set(ref this.Stats, (ulong)PlayerStats.Animating, true);
      }

      private void StopAnimation() {
        if (!IsSet(this.Stats, (ulong)PlayerStats.Animating))
          return;

        this.CurrentAnimation = Animations.PlayerDefault;
        this.Animation = new(AnimationsData[(int)Animations.PlayerDefault]);
        this.Animation.AnimationData.SpriteSheet = this.Image;
        this.Animation.Stop();

        Set(ref this.Stats, (ulong)PlayerStats.Animating, false);
      }


      private void DeathConditions() {
        if (this.Velocity.Y >= 900 && IsSet(this.Stats, (byte)PlayerStats.FallDamageEnabled))
          Set(ref this.Stats, (byte)PlayerStats.FallDamageCondition, true);
        if (this.Velocity.Y == 0 && !IsSet(this.Stats, (ulong)PlayerStats.Bottom))
          Set(ref this.Stats, (byte)PlayerStats.FallDamageCondition, false);

        if (IsSet(this.Stats, (ulong)PlayerStats.Bottom) && IsSet(this.Stats, (byte)PlayerStats.FallDamageCondition)) {
          Set(ref this.Stats, (byte)PlayerStats.FallDamageCondition, false);

          if (!IsSet(this.Stats, (ulong)PlayerStats.Invincibility) && !IsSet(this.Stats, (ulong)PlayerStats.Honey))
            this.Death();
        }
      }

      public void Death() {
        if (this.Timers[(byte)PlayerTimers.RespawnStatus].Active || this.Timers[(byte)PlayerTimers.Invincibility].Active)
          return;

        Set(ref this.Stats, (byte)PlayerStats.FallDamageCondition, false);
        
        this.Rect.X = this.RespawnPos.X;
        this.Rect.Y = this.RespawnPos.Y;
        this.StopMovement();

        for (int Index = 0; Index < DestructibleSprites.Count; Index++) {
          Sprite Destructible = DestructibleSprites[Index];

          if (Destructible is PowerUp PowerSprite && IsSet(PowerSprite.Stats, (byte)PowerSpriteFlags.Destroyed))
            PowerSprite.Regenerate();
        }

        this.Timers[(byte)PlayerTimers.RespawnStatus].Activate();

        Lives--;
      }


      private void Input() {
        this.InputVector = Vector2.Zero;

        if (!this.Timers[(byte)PlayerTimers.RespawnStatus].Active && !this.Timers[(byte)PlayerTimers.SpringMove].Active && !this.Timers[(byte)PlayerTimers.ShieldKnockback].Active) {
          if (IsSet(this.Stats, (ulong)PlayerStats.HorizontalMovement)) {
            if (CheckAction(GameAction.MoveRight, false))
              this.InputVector.X++;

            if (CheckAction(GameAction.MoveLeft, false))
              this.InputVector.X--;

            this.Direction.X = this.InputVector.X * (IsSet(this.Stats, (ulong)PlayerStats.Sprint) && IsKeyDown(Keys.LeftShift) ? 2 : 1);
          }

          if (IsSet(this.Stats, (ulong)PlayerStats.VerticalMovement)) {
            if (CheckAction(GameAction.MoveUp, false))
              this.InputVector.Y--;

            if (CheckAction(GameAction.MoveDown, false))
              this.InputVector.Y++;

            this.Direction.Y = this.InputVector.Y;
          }
        }

        if (IsSet(this.Stats, (ulong)PlayerStats.CanJump) && CheckAction(GameAction.Jump, true))
          this.StartJump();

        if (CheckAction(GameAction.MoveDown, false)) {
          if (IsSet(this.Stats, (ulong)PlayerStats.Honey) && IsSet(this.Stats, (ulong)PlayerStats.StickingCeiling)) {
            this.Rect.Y += 2;
            Set(ref this.Stats, (ulong)PlayerStats.StickingCeiling, false);
          }
        }
      }


      public void ActivatePower(PlayerPowers power) {
        ulong PowerFlag = 1ul << ((int)(power + ConfigManager.Player.PowerBitOffset));

        if (IsSet(this.Stats, PowerFlag))
          return;

        Texture2D NewImage = null;

        switch (power) {
          case PlayerPowers.Invincibility:
            NewImage = GetTexture("Images/PlayerImages/PlayerGold");
            break;
          case PlayerPowers.AutoMove:

            break;
          case PlayerPowers.Flying:

            break;
          case PlayerPowers.Frozen:

            break;
          case PlayerPowers.Goggles:

            break;
          case PlayerPowers.Honey:
            NewImage = GetTexture("Images/PlayerImages/PlayerHoney");
            break;
          case PlayerPowers.Sprint:

            break;
          case PlayerPowers.Telescope:

            break;
        }

        if (NewImage != null)
          this.Animation.AnimationData.SpriteSheet = NewImage;
        

        Set(ref this.Stats, PowerFlag, true);
        this.Timers[(int)power + 1].Activate();
      }

      public void DeactivatePower(PlayerPowers power) {
        if (power == PlayerPowers.All) {
          byte PowerCount = (byte)(ConfigManager.Player.PowerBitEnd - ConfigManager.Player.PowerBitOffset);

          for (byte Index = 0; Index < PowerCount; Index++) {
            this.PowerDeactivate(Index);
          }
        } else {
          this.PowerDeactivate((byte)power);
        }
      }

      private void PowerDeactivate(byte power) {
        if (!IsSet(this.Stats, 1ul << (power + ConfigManager.Player.PowerBitOffset)))
          return;

        Texture2D DefaultedSheet = this.DefaultImage;

        switch (this.CurrentAnimation) {
          case Animations.PlayerTeleportGold:
          case Animations.PlayerTeleportHoney:
            DefaultedSheet = AnimationsData[(int)Animations.PlayerTeleport].SpriteSheet;
            break;
        }

        this.Animation.AnimationData.SpriteSheet = DefaultedSheet;
        this.Image = this.DefaultImage;
        Set(ref this.Stats, 1ul << (power + ConfigManager.Player.PowerBitOffset), false);
      }




      public void MovementChange(PlayerStats change) {
        if (IsSet(this.Stats, (ulong)change))
          return;

        Set(ref this.Stats, (ulong)change, true);

        switch (change) {
          case PlayerStats.Ladder:
            Set(ref this.Stats, (ulong)PlayerStats.HorizontalMovement, true);
            Set(ref this.Stats, (ulong)PlayerStats.VerticalMovement, true);
            break;
        }
      }

      private void MovementReturn() {
        if (!IsSet(this.Stats, (ulong)PlayerStats.ReturnMovement) || !Any(this.Stats, 13, 18))
          return;

        byte AirTouch = 0x00;

        if (SpriteGroups[(int)Groups.Quicksand].SpriteList.Count > 0) {
          if (SpriteGroups[(int)Groups.Quicksand].OverlapsWith(this.Rect) == null) {
            Set(ref this.Stats, (ulong)PlayerStats.Quicksand, false);
            Set(ref this.Stats, (ulong)PlayerStats.QuicksandDeep, false);
            Set(ref AirTouch, 1 << 0, true);
          } else {
            return;
          }
        } else {
          Set(ref AirTouch, 1 << 0, true);  
        }

        if (SpriteGroups[(int)Groups.Water].SpriteList.Count > 0) {
          if (SpriteGroups[(int)Groups.Water].OverlapsWith(this.Rect) == null) {
            Set(ref this.Stats, (ulong)PlayerStats.Water, false);
            Set(ref this.Stats, (ulong)PlayerStats.DeepWater, false);
            Set(ref this.Stats, (ulong)PlayerStats.ThickWater, false);
            Set(ref AirTouch, 1 << 1, true);
          } else {
            return;
          }
        } else {
          Set(ref AirTouch, 1 << 1, true);
        }

        if (IsSet(AirTouch, 1 << 0) && IsSet(AirTouch, 1 << 1)) {
          this.Gravity = LevelData.Gravity;
          this.JumpHeight = ConfigManager.Player.JumpHeight;
          this.MovementSpeed = ConfigManager.Player.Speed;

          Set(ref this.Stats, (ulong)PlayerStats.HorizontalMovement, true);
          Set(ref this.Stats, (ulong)PlayerStats.VerticalMovement, false);
        }
      }

      private void StopMovement() {
        this.Direction = Vector2.Zero;
        this.Velocity = Vector2.Zero;
      }


      private void Move(float deltaTime) {
        if (IsSet(this.Stats, (ulong)PlayerStats.AutoMove)) {
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

        if (IsSet(this.Stats, (ulong)PlayerStats.VerticalMovement)) {
          this.Velocity.Y = this.Direction.Y * this.MovementSpeed * deltaTime;
          this.Rect.Y += this.Velocity.Y;
          return;
        }

        if (IsSet(this.Stats, (ulong)PlayerStats.Quicksand) || IsSet(this.Stats, (ulong)PlayerStats.QuicksandDeep)) {
          this.QuicksandDrag(deltaTime);
          this.Rect.Y += this.Velocity.Y * deltaTime;
          return;
        }

        if (IsSet(this.Stats, (ulong)PlayerStats.Honey)) {
          if (IsSet(this.Stats, (ulong)PlayerStats.Top)) {
            Set(ref this.Stats, (ulong)PlayerStats.StickingCeiling, true);
            this.Direction.Y = 0;
            this.Rect.Y -= 1;
          } else if (!this.Timers[(byte)PlayerTimers.WallJump].Active && !IsSet(this.Stats, (ulong)PlayerStats.Bottom) && (IsSet(this.Stats, (ulong)PlayerStats.Left) || IsSet(this.Stats, (ulong)PlayerStats.Right))) {
            this.Direction.Y = 0;
          }
        }

        this.Velocity.Y += this.Gravity * deltaTime;
        this.Velocity.Y = Math.Clamp(this.Velocity.Y, -ConfigManager.Player.TerminalVelocity, ConfigManager.Player.TerminalVelocity);

        this.Rect.Y += this.Velocity.Y * deltaTime;

        this.Collision(false);
      }

      private void StartJump() {
        if (IsSet(this.Stats, (ulong)PlayerStats.Bottom) && !IsSet(this.Stats, (ulong)PlayerStats.Top)) {
          this.Timers[(byte)PlayerTimers.WallJump].Activate();
          this.Direction.Y = 1.0f;

          if (IsSet(this.Stats, (ulong)PlayerStats.Quicksand) || IsSet(this.Stats, (ulong)PlayerStats.QuicksandDeep)) {
            this.Velocity.Y -= this.JumpHeight;
          } else {
            this.Velocity.Y = -this.JumpHeight;
          }
        } else {
          if (IsSet(this.Stats, (ulong)PlayerStats.Quicksand) || IsSet(this.Stats, (ulong)PlayerStats.QuicksandDeep)) {
            this.Velocity.Y -= this.JumpHeight;
          } else if (IsSet(this.Stats, (ulong)PlayerStats.Honey) && !this.Timers[(byte)PlayerTimers.WallJump].Active && (IsSet(this.Stats, (ulong)PlayerStats.Left) || IsSet(this.Stats, (ulong)PlayerStats.Right))) {
            this.Timers[(byte)PlayerTimers.WallJumpStun].Activate();
            this.Direction.Y = 1;
            this.Direction.X = IsSet(this.Stats, (ulong)PlayerStats.Left) ? ConfigManager.Player.WallJumpDirectionFactor : -ConfigManager.Player.WallJumpDirectionFactor;
            this.Velocity.Y = -this.JumpHeight;
          }
        }
      }


      public void ActivateSpring(Directions bounceDirection) {
        switch (bounceDirection) {
          case Directions.Left:
            this.Velocity.X = -(CheckAction(GameAction.MoveLeft, true) ? ConfigManager.Player.AlternativeSpringEffect : ConfigManager.Player.SpringEffect);
            break;
          case Directions.Right:
            this.Velocity.X = CheckAction(GameAction.MoveRight, true) ? ConfigManager.Player.AlternativeSpringEffect : ConfigManager.Player.SpringEffect;
            break;
          case Directions.Up:
            this.Velocity.Y = -(CheckAction(GameAction.MoveUp, true) ? ConfigManager.Player.AlternativeSpringEffect : ConfigManager.Player.SpringEffect);
            break;
          case Directions.Down:
            this.Velocity.Y = CheckAction(GameAction.MoveDown, true) ? ConfigManager.Player.AlternativeSpringEffect : ConfigManager.Player.SpringEffect;
            break;
        }
      }

      private void QuicksandDrag(float deltaTime) {
        float Depth = Quicksand.GetDepth(this.Rect);
        float Drag = MathHelper.Lerp(Gameplay.QuicksandDrag, Gameplay.QuicksandDeepDrag, Depth);
        float DampFactor = MathF.Exp(-Drag * deltaTime);
        float SandGravity = Gravity * MathHelper.Lerp(1.0f, 0.1f, Depth * Depth);

        this.Velocity *= DampFactor;

        this.Velocity.Y += SandGravity * deltaTime;
        this.Velocity.Y = MathF.Min(this.Velocity.Y, Gravity / Drag);
      }


      private void CheckContact() {
        float QuarterHeight = this.Rect.Height * 0.25f;

        Set(ref this.Stats, (ulong)PlayerStats.Left, false);
        Set(ref this.Stats, (ulong)PlayerStats.Right, false);
        Set(ref this.Stats, (ulong)PlayerStats.Top, false);
        Set(ref this.Stats, (ulong)PlayerStats.Bottom, false);

        this.CollisionRectangles[0] = new(this.Rect.X - 2, this.Rect.Y + QuarterHeight, 2, this.Rect.Height);
        this.CollisionRectangles[1] = new(this.Rect.Right, this.Rect.Y + QuarterHeight, 2, this.Rect.Height * 0.5f);
        this.CollisionRectangles[2] = new(this.Rect.X, this.Rect.Y - QuarterHeight, this.Rect.Width, QuarterHeight);
        this.CollisionRectangles[3] = new(this.Rect.X, this.Rect.Bottom, this.Rect.Width, QuarterHeight);
        this.Platform = null;

        for (int I = 0; I < this.CollisionRectangles.Length; I++) {
          Sprite WallCollision = SpriteGroups[(ulong)Groups.Collidable].OverlapsWith(this.CollisionRectangles[I]);
          bool SemiCollision = false;

          if (WallCollision == null) {
            List<Sprite> SemiCollidables = SpriteGroups[(int)Groups.SemiCollidable].SpriteList;

            for (int Index = 0; Index < SemiCollidables.Count; Index++) {

            }
          } else if (I == 3) {
            if (WallCollision is MovingSprite MovingSprite && WallCollision.Rect != WallCollision.OldRect)
              this.Platform = MovingSprite;
          }
          bool WallContact = WallCollision != null || SemiCollision;

          Set(ref this.Stats, (ulong)(1 << (I + 8)), WallContact);
          Set(ref this.Stats, (ulong)PlayerStats.OnWall, WallContact);
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

      private void HandleWallCollision(Sprite wall, Directions direction) {
        RectangleF WallRect = wall.Rect;
        bool Horizontal = IsHorizontal(direction);

        switch (direction) {
          case Directions.Left:
            this.CollisionPosition(WallRect.Right, Horizontal);
            break;
          case Directions.Right:
            this.CollisionPosition(WallRect.X - this.Rect.Width, Horizontal);
            break;
          case Directions.Up:
            this.CollisionPosition(WallRect.Bottom, Horizontal);
            Set(ref this.Stats, (ulong)PlayerStats.Top, true);
            break;
          case Directions.Down:
            this.CollisionPosition(WallRect.Y - this.Rect.Height, Horizontal);
            Set(ref this.Stats, (ulong)PlayerStats.Bottom, true);

            if (wall is MovingSprite MovingSprite)
              this.Platform = MovingSprite;
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

          this.HandleWallCollision(Wall, CollisionDirection(this, Wall, horizontal ? Directions.Horizontal : Directions.Vertical));
        }
        for (int Index = 0; Index < SpriteGroups[(int)Groups.SemiCollidable].SpriteList.Count; Index++) {
          Spring SemiWall = (Spring)SpriteGroups[(int)Groups.SemiCollidable].SpriteList[Index];

          if (!SemiWall.Rect.IntersectsWith(this.Rect))
            continue;

          Directions Direction = CollisionDirection(this, SemiWall, horizontal ? Directions.Horizontal : Directions.Vertical);

          if (IsSet(SemiWall.Stats, (byte)DirectionToFlag(Direction))) {
            this.HandleWallCollision(SemiWall, Direction);
          }
        }
      }


      public void Lantern(bool active) {
        BrightnessEffect.Parameters["LanternEnabled"].SetValue(active);
        Set(ref this.Stats, (ulong)PlayerStats.LanternEnabled, active);
      }

      public void ActivateCheckpoint(Vector2 position, byte index) {
        this.RespawnPos = position;

        for (int Index = 0; Index < this.CheckpointFlags.Length; Index++) {
          Set(ref this.Stats, (ulong)this.CheckpointFlags[index], Index == index);
        }
      }

      public void Teleport(Teleporter teleportPortal) {
        Animations TeleportAnimation = Animations.PlayerTeleport;

        if (IsSet(this.Stats, (ulong)PlayerStats.Invincibility)) {
          TeleportAnimation = Animations.PlayerTeleportGold;
        } else if (IsSet(this.Stats, (ulong)PlayerStats.Honey)) {
          TeleportAnimation = Animations.PlayerTeleportHoney;
        }

        this.PlayAnimation(TeleportAnimation);
        this.Rect.TopLeft(Level.TeleportLocations[teleportPortal.ID]);
        this.StopMovement();
      }


      private void KeepInScreen() {
        if (this.Rect.X < 0) {
          this.CollisionPosition(0, true);
        } else if (this.Rect.Right > LevelData.Dimensions.PixelWidth) {
          this.CollisionPosition( LevelData.Dimensions.PixelWidth - this.Rect.Width, true);
        }

        if (this.Rect.Y < 0) {
          this.CollisionPosition(0, false);
        } else if (this.Rect.Bottom >  LevelData.Dimensions.PixelHeight) {
          this.CollisionPosition( LevelData.Dimensions.PixelHeight - this.Rect.Height, false);
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
        this.MovementReturn();
        this.Move(deltaTime);
        this.KeepInScreen();
        this.CheckContact();
        this.DeathConditions();

        if (this.Rect.TopLeft() != this.OldRect.TopLeft())
          UpdateSpritePosition(this);
        
        if (IsSet(this.Stats, (ulong)PlayerStats.Animating)) {
          base.Update(deltaTime);

          if (!IsSet(this.Animation.AnimationData.Stats, (byte)AnimationFlags.Playing))
            this.StopAnimation();
        }
      }
    }


    public class DeathCube : MovingSprite {
      public readonly bool Horizontal;
      public bool Active;


      public DeathCube(Vector2 position, bool horizontal, bool active) : base(GetTexture($"Images/EnemyImages/{(active ? "DeathCube" : "DeadDeathCube")}"), position, new((sbyte)(horizontal ? 1 : 0), (sbyte)(horizontal ? 0 : 1)), LevelData.EnemySpeed, active ? [Groups.All, Groups.Damage] : [Groups.All, Groups.Damage, Groups.Switch], ZLayers.Main) {        
        this.Horizontal = horizontal;
        this.Active = active;
      }


      public void Activate() {
        if (this.Active)
          return;

        this.Image = GetTexture($"Images/EnemyImages/DeathCube");
        this.Active = true;
      }


      public void Collision(Sprite sprite) {
        switch (CollisionDirection(this, sprite, this.Horizontal ? Directions.Horizontal : Directions.Vertical)) {
          case Directions.Left:
          case Directions.Right:
            this.Direction.X *= -1;
            break;
          case Directions.Up:
          case Directions.Down:
            this.Direction.Y *= -1;
            break;
        }
      }

      public override void Update(float deltaTime) {
        if (this.Active)
          base.Update(deltaTime);
      }
    }

    public class Prowler : MovingSprite {
      public readonly bool Horizontal;


      public Prowler(Vector2 position, bool horizontal) : base(GetTexture("Images/EnemyImages/Prowler"), position, new(0, 0), LevelData.EnemySpeed, [Groups.All, Groups.Damage], ZLayers.Main) {
        this.Horizontal = horizontal;
      }

      private void Follow() {
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
      }


      public override void Update(float deltaTime) {
        this.Follow();
        base.Update(deltaTime);
      }
    }

    public class Canon : Sprite {
      private Vector2 LaunchPosition = Vector2.Zero;
      private readonly Directions FaceDirection;
      private float FiringTimer = 0f;
      private readonly bool Passthrough;


      public Canon(Vector2 position, Directions direction, bool floor, bool passthrough) : base(GetTexture($"Images/EnemyImages/{(floor ? (IsHorizontal(direction) ? "FloorCanon" : "VerticalFloorCanon") : "Canon")}"), position, [Groups.All, Groups.Collidable], ZLayers.Main, direction, floor && !IsHorizontal(direction)) {
        Dimensions BulletDimensions = GetTexture("Images/EnemyImages/Bullet").GetDimensions();

        switch (direction) {
          case Directions.Left:
            this.LaunchPosition = new(position.X - (BulletDimensions.Width >> 1), position.Y + (BulletDimensions.Height >> 1));
            break;
          case Directions.Right:
            this.LaunchPosition = new(this.Rect.Right - (BulletDimensions.Width >> 1), position.Y + (BulletDimensions.Height >> 1));
            break;
          case Directions.Up:
            this.LaunchPosition = new(this.Rect.Center().X - (BulletDimensions.Width >> 1), position.Y - BulletDimensions.Height);
            break;
          case Directions.Down:
            this.LaunchPosition = new(this.Rect.Center().X - (BulletDimensions.Width >> 1), this.Rect.Bottom);
            break;
        }

        this.FaceDirection = direction;
        this.Passthrough = passthrough;
      }


      public override void Update(float deltaTime) {
        this.FiringTimer += deltaTime;

        if (this.FiringTimer >= LevelData.CanonFiringRate) {
          this.FiringTimer = 0f;

          if (InScreen(this.Rect))
            _ = new Bullet(this.LaunchPosition, this.FaceDirection, this.Passthrough);
        }

        base.Update(deltaTime);
      }
    }

    public class Bullet : MovingSprite {
      public float WallInvincibility = 0.0f;
      public byte Stats = 0x00;


      public Bullet(Vector2 position, Directions direction, bool passthrough) : base(GetTexture("Images/EnemyImages/Bullet"), position, new(0, 0), LevelData.BulletSpeed, [Groups.All, Groups.Damage], ZLayers.Main, direction) {
        switch (direction) {
          case Directions.Left:
            this.Direction = new(-1, 0);
            break;
          case Directions.Right:
            this.Direction = new(1, 0);
            break;
          case Directions.Up:
            this.Direction = new(0, -1);
            break;
          case Directions.Down:
            this.Direction = new(0, 1);
            break;
        }

        Set(ref this.Stats, (byte)BulletFlags.Passthrough, passthrough);
        Set(ref this.Stats, (byte)BulletFlags.WallInvincibility, true);
      }


      public override void Update(float deltaTime) {
        if (!IsSet(this.Stats, (byte)BulletFlags.WallInvincibility) && !InScreen(this.Rect)) {
          this.Destroy();
          return;
        }

        base.Update(deltaTime);
      }
    }

    public class FallingSpike : Sprite {
      private readonly Timer RegrowTimer;
      private readonly Vector2 PlaceholderPosition = Vector2.Zero;
      private Vector2 PlayerPos = Vector2.Zero;
      private BVector Direction = BVector.Zero;
      private Directions FaceDirection;
      private float CheckPosition = 0.0f;
      private float Acceleration = 0.0f;
      public byte Stats = 0x00;

      
      public FallingSpike(Vector2 position, bool automatic, bool limitedRange, Directions direction) : base(new Animation(AnimationsData[(int)Animations.FallingSpikeRegrow]), position, [Groups.All, Groups.Damage], ZLayers.Main, direction) {
        Set(ref this.Stats, (byte)FallingSpikeFlags.Automatic, automatic);

        this.Mark(position, direction, limitedRange);

        if (IsSet(this.Stats, (byte)FallingSpikeFlags.Regrow)) {
          Sprite FallingSpikePlaceholder = new(GetTexture("Images/EnemyImages/SpikeFallingPlaceholder"), position, [Groups.All], ZLayers.Placeholders, direction);
          
          this.RegrowTimer = new(FallingSpikeRegrow, () => this.Regrow(true));
          this.RegrowTimer.Activate();
          this.PlaceholderPosition = position;
        }
      }


      private void Mark(Vector2 position, Directions direction, bool limitedRange) {
        List<Vector2> BlockPositions = LevelData.FSBlockPositions;
        Vector2 CheckBlockPosition = Vector2.Zero;
        int CheckRadius = 0;
        int Index = 0;
        sbyte Increment = 0;
        byte Status = 0x00;

        switch (direction) {
          case Directions.Left:
            CheckBlockPosition = new(position.X + TILE_SIZE, position.Y);
            CheckRadius = (int)position.X;
            Increment = (sbyte)-TILE_SIZE;
            break;
          case Directions.Right:
            CheckBlockPosition = new(position.X - TILE_SIZE, position.Y);
            CheckRadius = LevelData.Dimensions.PixelWidth - (int)position.X;
            Increment = (sbyte)TILE_SIZE;
            break;
          case Directions.Up:
            CheckBlockPosition = new(position.X, position.Y + TILE_SIZE);
            CheckRadius = (int)position.Y - TILE_SIZE;
            Increment = (sbyte)-TILE_SIZE;
            break;
          case Directions.Down:
            CheckBlockPosition = new(position.X, position.Y - TILE_SIZE);
            CheckRadius = LevelData.Dimensions.PixelHeight - (int)position.Y - TILE_SIZE;
            Increment = (sbyte)TILE_SIZE;
            break;
        }

        Set(ref Status, 1 << 1, IsHorizontal(direction));

        do {
          Index += TILE_SIZE;

          if (IsSet(Status, 1 << 0))
            break;

          if (IsSet(Status, 1 << 1)) {
            position.X += Increment;
          } else {
            position.Y += Increment;
          }

          for (int Accessor = 0; Accessor < BlockPositions.Count; Accessor++) {
            if (position == BlockPositions[Accessor]) {
              this.CheckPosition = IsSet(Status, 1 << 1) ? position.X : position.Y;  
              Set(ref Status, 1 << 0, true);
            }

            if (!IsSet(this.Stats, (byte)FallingSpikeFlags.Regrow) && CheckBlockPosition == BlockPositions[Accessor]) 
              Set(ref this.Stats, (byte)FallingSpikeFlags.Regrow, true);
          }
        } while (limitedRange && Index < CheckRadius);

        this.FaceDirection = direction;
        Set(ref this.Stats, (byte)FallingSpikeFlags.LimitedRange, limitedRange && IsSet(Status, 1 << 0));
      }


      private void Regrow(bool start) {
        if (!start) {
          Set(ref this.Stats, (byte)FallingSpikeFlags.Regrowing, false);
          Set(ref this.Stats, (byte)FallingSpikeFlags.Destroyed, false);
          return;
        }

        Set(ref this.Stats, (byte)FallingSpikeFlags.Regrowing, true);

        this.Animation.CurrentFrame = 0;
        this.Animation.Play();
        this.Animation.CurrentFrame++;
        this.Direction = BVector.Zero;
        this.Acceleration = 0.0f;

        SpriteGroups[(int)Groups.Damage].Add(this);
        DestructibleSprites.Remove(this);

        Remove(this);
        Add(this);
        UpdateSpritePosition(this);
      }

      private void Fall() {
        this.Direction = DirectionVectors[(int)this.FaceDirection];
        this.RegrowTimer?.Activate();

        Set(ref this.Stats, (byte)FallingSpikeFlags.Falling, true);
        Set(ref this.Stats, (byte)FallingSpikeFlags.Unbranched, true);
      }


      private void Move(float deltaTime) {
        this.OldRect = this.Rect;
        this.Acceleration += (TILE_SIZE << 1) * deltaTime;
        this.Rect.X += this.Direction.X * (FallingSpikeSpeed + this.Acceleration) * deltaTime;
        this.Rect.Y += this.Direction.Y * (FallingSpikeSpeed + this.Acceleration) * deltaTime;
        UpdateSpritePosition(this);
      }

      public void Collision(Sprite enemy) {
        if (enemy == null) {
          PlaySound("Sounds/Effects/FallingSpikeDestroy", SoundData.Default);
        } else {
          if (enemy is not Player)
            enemy.Destroy();
        }

        if (IsSet(this.Stats, (byte)FallingSpikeFlags.Regrow)) {
          Set(ref this.Stats, (byte)FallingSpikeFlags.Destroyed, true);
          Set(ref this.Stats, (byte)FallingSpikeFlags.Falling, false);
          Set(ref this.Stats, (byte)FallingSpikeFlags.Regrowing, false);

          this.Rect.TopLeft(this.PlaceholderPosition);
          DestructibleSprites.Add(this);
          UpdateSpritePosition(this);
        }

        this.Destroy();
      }


      public override void Update(float deltaTime) {
        base.Update(deltaTime);

        if (IsSet(this.Stats, (byte)FallingSpikeFlags.Destroyed)) {
          if (IsSet(this.Stats, (byte)FallingSpikeFlags.Regrowing)) {
            if (!IsSet(this.Animation.AnimationData.Stats, (byte)AnimationFlags.Playing))
              this.Regrow(false);
          } else {
            this.RegrowTimer?.Update();
          }

          return;
        }

        if (IsSet(this.Stats, (byte)FallingSpikeFlags.Falling)) {
          this.Move(deltaTime);
          return;
        }

        bool LimitedRange = IsSet(this.Stats, (byte)FallingSpikeFlags.LimitedRange);
        bool Automatic = IsSet(this.Stats, (byte)FallingSpikeFlags.Automatic);

        if (!Automatic)
          PlayerPos = Globals.Player.Rect.TopLeft();

        switch (this.FaceDirection) {
          case Directions.Left:
            if (Automatic || (PlayerPos.X < this.Rect.X && PlayerPos.Y == this.Rect.Y && (!LimitedRange || PlayerPos.X > this.CheckPosition)))
              this.Fall();
            break;
          case Directions.Right:
            if (Automatic || (PlayerPos.X > this.Rect.X && PlayerPos.Y == this.Rect.Y && (!LimitedRange || PlayerPos.X < this.CheckPosition)))
              this.Fall();
            break;
          case Directions.Up:
            if (Automatic || (PlayerPos.Y < this.Rect.Y && PlayerPos.X == this.Rect.X && (!LimitedRange || PlayerPos.Y > this.CheckPosition)))
              this.Fall();
            break;
          case Directions.Down:
            if (Automatic || (PlayerPos.Y > this.Rect.Y && PlayerPos.X == this.Rect.X && (!LimitedRange || PlayerPos.Y < this.CheckPosition)))
              this.Fall();
            break;
        }
      }
    }


    public class Changer : Sprite {
      private readonly string Type = string.Empty;
      private readonly uint DurationChange = uint.MinValue;
      private readonly ushort SpeedChange = ushort.MinValue;
      private readonly float RateChange = float.MinValue;
      private bool Active = true;


      public Changer(Texture2D image, Vector2 position, string type, float rateChange, ushort speedChange, uint durationChange) : base(image, position, [Groups.Changer], ZLayers.BackgroundTiles) {
        this.Type = type;
        this.DurationChange = durationChange;
        this.SpeedChange = speedChange;
        this.RateChange = rateChange;
      }


      public void Overlap() {
        if (!this.Active)
          return;

        List<Sprite> ChangerSprites = SpriteGroups[(int)Groups.Changer].SpriteList;

        for (int Index = 0; Index < ChangerSprites.Count; Index++) {
          if (ChangerSprites[Index] is Changer Changer && Changer.Type == this.Type)
            Changer.Active = true;
        }

        switch (this.Type) {
          case "Bullet":
            if (this.SpeedChange != ushort.MinValue)
              BulletSpeed = this.SpeedChange;
            if (this.RateChange != float.MinValue) 
              CanonFiringRate = this.RateChange;
            break;
          case "Enemy":
            if (this.SpeedChange != ushort.MinValue)
              EnemySpeed = this.SpeedChange;
            break;
          case "Lantern":
            if (this.RateChange != float.MinValue)
              LanternLightWidth = this.RateChange;
            break;
          case "Invincibility":
            if (this.DurationChange != uint.MinValue)
              PowerDurations[(int)PlayerPowers.Invincibility] = this.DurationChange;
            break;
          case "AutoMove":
            if (this.DurationChange != uint.MinValue)
              PowerDurations[(int)PlayerPowers.AutoMove] = this.DurationChange;
            break;
          case "Flying":
            if (this.DurationChange != uint.MinValue)
              PowerDurations[(int)PlayerPowers.Flying] = this.DurationChange;
            break;
          case "Frozen":
            if (this.DurationChange != uint.MinValue)
              PowerDurations[(int)PlayerPowers.Frozen] = this.DurationChange;
            break;
          case "Goggles":
            if (this.DurationChange != uint.MinValue)
              PowerDurations[(int)PlayerPowers.Goggles] = this.DurationChange;
            break;
          case "Honey":
            if (this.DurationChange != uint.MinValue)
              PowerDurations[(int)PlayerPowers.Honey] = this.DurationChange;
            break;
          case "Sprint":
            if (this.DurationChange != uint.MinValue)
              PowerDurations[(int)PlayerPowers.Sprint] = this.DurationChange;
            break;
          case "Telescope":
            if (this.DurationChange != uint.MinValue)
              PowerDurations[(int)PlayerPowers.Telescope] = this.DurationChange;
            break;
        }

        this.Active = false;
      }
    }

    public class Item : Sprite {
      public string DeactivatorImage;
      public bool Active;


      public Item(Texture2D texture, Vector2 position, List<Groups> groups, ZLayers z, string deactiavatorImage, bool active, Directions faceDirection = Directions.Right) : base(texture, position, groups, z, faceDirection) {
        this.DeactivatorImage = deactiavatorImage;
        this.Active = active;
      }

      public Item(Animation animation, Vector2 position, List<Groups> groups, ZLayers z, string deactiavatorImage, bool active, Directions faceDirection = Directions.Right) : base(animation, position, groups, z, faceDirection) {
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

    public class PowerUp : Sprite {
      public readonly PlayerPowers Power;
      public byte Stats = 0x00;

      
      public PowerUp(Texture2D texture, Vector2 position, PlayerPowers power, bool respawn = false, bool canceller = false) : base(texture, position, [Groups.All, Groups.Item], ZLayers.Items) {
        this.Power = power;
        Set(ref this.Stats, (byte)PowerSpriteFlags.Respawn, respawn);
        Set(ref this.Stats, (byte)PowerSpriteFlags.Canceller, canceller);
      }

      public PowerUp(Animation animation, Vector2 position, PlayerPowers power, bool respawn = false, bool canceller = false) : base(animation, position, [Groups.All, Groups.Item], ZLayers.Items) {
        this.Power = power;
        Set(ref this.Stats, (byte)PowerSpriteFlags.Respawn, respawn);
        Set(ref this.Stats, (byte)PowerSpriteFlags.Canceller, canceller);
      }


      public override void Destroy() {
        if (IsSet(this.Stats, (byte)PowerSpriteFlags.Respawn)) {
          Set(ref this.Stats, (byte)PowerSpriteFlags.Destroyed, true);
          DestructibleSprites.Add(this);
        }

        base.Destroy();
      }

      public void Regenerate() {
        if (!IsSet(this.Stats, (byte)PowerSpriteFlags.Respawn))
          return;

        Set(ref this.Stats, (byte)PowerSpriteFlags.Destroyed, false);

        Add(this);
        SpriteGroups[(int)Groups.Item].Add(this);
        DestructibleSprites.Remove(this);
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


      public LifeBlock(Vector2 position, bool switchBlock) : base(GetTexture($"Images/CollectableImages/{(switchBlock ? "LifeBlockSwitch" : "LifeBlockOn")}"), position, switchBlock ? [Groups.All, Groups.Switch, Groups.Item] : [Groups.All, Groups.Item], ZLayers.Opaque) {
        this.Switch = switchBlock;
      }


      public void Activate(bool switchActivator) {
        if (this.Switch && switchActivator) {
          this.Image = GetTexture("Images/CollectableImages/LifeBlockOn");
          this.Switch = false;
          return;
        }

        if (!this.Active || switchActivator)
          return;

        Lives++;
        this.Image = GetTexture("Images/CollectableImages/LifeBlockOff");
        this.Active = false;
      }
    }


    public class Quicksand : Sprite {
      public readonly bool Deep;


      public Quicksand(Animation animation, Vector2 position, bool deep) : base(animation, position, [Groups.All, Groups.Quicksand], ZLayers.Main) {
        this.Deep = deep;

        if (deep)
          this.Animation.Play();
      }


      public void Overlap() {
        if (!this.Deep && (Globals.Player.Rect.Bottom - Globals.Player.QuicksandTop <= Globals.Player.Rect.Height))
          this.Animation.Play();

        if (IsSet(Globals.Player.Stats, (ulong)PlayerStats.Quicksand) || IsSet(Globals.Player.Stats, (ulong)PlayerStats.QuicksandDeep))
          return;

        Globals.Player.JumpHeight *= ConfigManager.Player.QuicksandJumpFactor;
        Globals.Player.MovementSpeed *= this.Deep ? ConfigManager.Player.QuicksandDeepSpeedFactor : ConfigManager.Player.QuicksandDeepSpeedFactor;
      }


      public static float GetDepth(RectangleF rect) => Math.Clamp((rect.Bottom - Globals.Player.QuicksandTop) / rect.Height, 0f, 1f);
    }


    public class Teleporter : Sprite {
      public byte ID;
      public bool Active;

      public Teleporter(Texture2D texture, Vector2 position, byte id, bool active) : base(texture, position, active ? [Groups.All, Groups.Teleporter] : [Groups.All, Groups.Teleporter, Groups.Switch], ZLayers.BackgroundTiles) {
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
      public readonly Directions[] CollisionDirections = [Directions.Left, Directions.Right, Directions.Up, Directions.Down];
      public byte Stats = 0x00;


      public Spring(Vector2 position, Directions faceDirection, bool multi) : base(new Animation(AnimationsData[(int)Animations.SpringRetraction]), position, [Groups.All, Groups.Spring], ZLayers.Main, faceDirection) {
        Set(ref this.Stats, (byte)SpringFlags.Horizontal, IsHorizontal(faceDirection));
        Set(ref this.Stats, (byte)SpringFlags.Multi, multi);
        Set(ref this.Stats, (byte)DirectionToFlag(faceDirection), false);

        if (multi)
          CollisionDirections[(byte)OppositeDirections[(int)faceDirection]] = Directions.None;
      }


      public void Activate(Directions collisionDirection, Sprite collided) {
        if (!Contains(this.CollisionDirections, collisionDirection))
          return;

        Set(ref this.Stats, (byte)SpringFlags.Active, true);
        this.Animation.CurrentFrame = 0;
        this.Animation.Play();

        if (collided is MovingSprite Moveable) {
          Moveable.Direction = DirectionVectors[(int)collisionDirection];
        } else if (collided is Player Player) {
          Player.ActivateSpring(collisionDirection);
        }
      }


      public override void Update(float deltaTime) {
        base.Update(deltaTime);

        if (IsSet(this.Stats, (byte)SpringFlags.Active) && !IsSet(this.Animation.AnimationData.Stats, (byte)AnimationFlags.Playing))
          Set(ref this.Stats, (byte)SpringFlags.Active, false);
      }
    }
  }
}
