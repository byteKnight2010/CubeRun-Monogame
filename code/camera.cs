using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Cube_Run_C_.Tools;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Tools.BitMask;
using RectangleF = System.Drawing.RectangleF;


namespace Cube_Run_C_ {
  static class Camera {
    [Flags]
    enum DirtyUpdates : byte {
      Sort = 1 << 0,
      ScreenRect = 1 << 1,
    }


    public static List<Sprite> SpriteList = new();
    private static List<Sprite> NewSprites = new();
    private static List<Sprite> QuerySprites = new();
    private static SpatialGrid SpatialGrid = new();
    private static Vector2 Offset = Vector2.Zero;
    private static Vector2 LastTargetPosition = Vector2.Zero;
    public static Vector2 DrawScale = Vector2.One;
    public static Dimensions ScreenDimensions;
    private static DimensionsF LevelDimensions;
    public static Dimensions HalfScreen = new();
    private static RectangleF CachedScreenRect;
    public static GraphicsDeviceManager Graphics;
    public static Color BackgroundColor;
    public static float Scale = 1.0f;
    private static float InverseScale = 1.0f;
    private static byte RequiredUpdates = 0b00000000;


    public static void Reset(Color backgroundColor) {
      SpriteList.Clear();
      NewSprites.Clear();
      QuerySprites.Clear();
      SpatialGrid.Clear();
      Offset = Vector2.Zero;
      LastTargetPosition = Vector2.Zero;
      BackgroundColor = backgroundColor;
      SetAll(ref RequiredUpdates, false);
      UpdateScale();
    }


    public static void Add(Sprite sprite) {
      if (SpriteList.Count == 0 || sprite.Z >= SpriteList[^1].Z) {
        SpriteList.Add(sprite);
        SpatialGrid.Insert(sprite);
      } else {
        NewSprites.Add(sprite);
        SpatialGrid.Insert(sprite);
        Set(ref RequiredUpdates, (byte)DirtyUpdates.Sort, true);
      }
    }

    public static bool Remove(Sprite sprite) {
      bool Removed = SpriteList.Remove(sprite);

      if (Removed) {
        SpatialGrid.Remove(sprite);
      } else {
        Removed = NewSprites.Remove(sprite);
      }

      return Removed;
    }

    public static void Clear() {
      SpriteList.Clear();
      NewSprites.Clear();
      QuerySprites.Clear();
      SpatialGrid.Clear();
      Set(ref RequiredUpdates, (byte)DirtyUpdates.Sort, false);
    }


    public static void LockPosition(Vector2 position) {
      if (Offset != position)
        Offset = position;
    }


    public static void UpdateScale() {
      Dimensions ScreenDimensions = new(Graphics.GraphicsDevice.Viewport.Width, Graphics.GraphicsDevice.Viewport.Height);
      float Scale = Math.Min((float)ScreenDimensions.Width / (float)DEFAULT_DIMENSIONS.Width, (float)ScreenDimensions.Height / (float)DEFAULT_DIMENSIONS.Height);

      Camera.ScreenDimensions = ScreenDimensions;
      HalfScreen = new(ScreenDimensions.Width >> 1, ScreenDimensions.Height >> 1);
      LevelDimensions = new(LevelData.Dimensions.Item2.Width * Scale, LevelData.Dimensions.Item2.Height * Scale);
      LastTargetPosition = Vector2.Zero;
      DrawScale.X = Scale;
      DrawScale.Y = Scale;
      Camera.Scale = Scale;
      InverseScale = 1 / Scale;
      Set(ref RequiredUpdates, (byte)DirtyUpdates.ScreenRect, true);
    }

    public static void UpdateSpritePosition(Sprite sprite) {
      SpatialGrid.Remove(sprite);
      SpatialGrid.Insert(sprite);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 WorldToScreen(Vector2 worldPosition) => (worldPosition * Scale) - Offset;

    private static int BinarySearchInsertionPoint(List<Sprite> spriteList, byte Z) {
      int Left = 0;
      int Right = spriteList.Count;

      while (Left < Right) {
        int Mid = (Left + Right) >> 1;

        if (spriteList[Mid].Z <= Z) {
          Left = Mid + 1;
        } else {
          Right = Mid;
        }
      }

      return Left;
    }


    public static void Draw(SpriteBatch spriteBatch, Vector2 targetPosition) {
      if (SpriteList.Count == 0) 
        return;

      int ScreenWidth = ScreenDimensions.Width;
      int ScreenHeight = ScreenDimensions.Height;
      
      if (LevelDimensions.Width != LevelData.Dimensions.Item2.Width) {
        LevelDimensions = new(LevelData.Dimensions.Item2.Width * Scale, LevelData.Dimensions.Item2.Height * Scale);
        Set(ref RequiredUpdates, (byte)DirtyUpdates.ScreenRect, true);
      }

      if (LastTargetPosition != targetPosition) {
        Offset.X = Math.Max(0, Math.Min(LevelDimensions.Width - ScreenWidth, (targetPosition.X * Scale) - HalfScreen.Width));
        Offset.Y = Math.Max(0, Math.Min(LevelDimensions.Height - ScreenHeight, (targetPosition.Y * Scale) - HalfScreen.Height));
        LastTargetPosition = targetPosition;
        Set(ref RequiredUpdates, (byte)DirtyUpdates.ScreenRect, true);
      }

      if (IsSet(RequiredUpdates, (byte)DirtyUpdates.ScreenRect)) {
        CachedScreenRect = new(Offset.X * InverseScale, Offset.Y * InverseScale, ScreenWidth * InverseScale, ScreenHeight * InverseScale);
        Set(ref RequiredUpdates, (byte)DirtyUpdates.ScreenRect, false);
      }

      if (IsSet(RequiredUpdates, (byte)DirtyUpdates.Sort)) {
        if (NewSprites.Count > 0) {
          if (NewSprites.Count >= 10 && NewSprites.Count <= 50) {
            for (int Index = 0; Index < NewSprites.Count; Index++) {
              SpriteList.Insert(BinarySearchInsertionPoint(SpriteList, NewSprites[Index].Z), NewSprites[Index]);
            }
          } else {
            SpriteList.AddRange(NewSprites);
            SpriteList.Sort((spriteA, spriteB) => {
              int ZCompare = spriteA.Z.CompareTo(spriteB.Z);

              if (ZCompare != 0)
                return ZCompare;

              return spriteA.Image.GetHashCode().CompareTo(spriteB.Image.GetHashCode());
            });
          }

          NewSprites.Clear();
        }

        Set(ref RequiredUpdates, (byte)DirtyUpdates.Sort, false);
      }

      QuerySprites = SpatialGrid.Query(CachedScreenRect);

      if (QuerySprites.Count > 1) {
        Sprite[] SortedSprites = new Sprite[QuerySprites.Count];
        int[] Counts = new int[8];

        for (int Index = 0; Index < QuerySprites.Count; Index++) {
          Counts[QuerySprites[Index].Z]++;
        }

        for (int Index = 1; Index < 8; Index++) {
          Counts[Index] += Counts[Index - 1];
        }
        
        for (int Index = QuerySprites.Count - 1; Index >= 0; Index--) {
          Sprite Sprite = QuerySprites[Index];
          SortedSprites[--Counts[Sprite.Z]] = Sprite;
        }

        for (int Index = 0; Index < QuerySprites.Count; Index++) {
          QuerySprites[Index] = SortedSprites[Index];
        }
      }

      for (int Index = 0; Index < QuerySprites.Count; Index++) {
        Sprite Sprite = QuerySprites[Index];
        RectangleF SpriteRect = Sprite.Rect;

        if (!SpriteRect.IntersectsWith(CachedScreenRect)) 
          continue;

        Texture2D SpriteTexture = Sprite.GetImage();
        SpriteTransform SpriteTransformations = Sprite.Transformations;
        Rectangle? SpriteFrame = Sprite.GetFrame();
        Vector2 SpritePosition = SpriteRect.TopLeft() * Scale - Offset;

        if (SpriteTransformations.Rotation == 0f && SpriteTransformations.Effect == SpriteEffects.None) {
          spriteBatch.Draw(SpriteTexture, SpritePosition, SpriteFrame, Color.White, 0f, Vector2.Zero, DrawScale, SpriteEffects.None, 0f);
        } else {
          Vector2 RotationOffset = Sprite.RotationOffset;
          spriteBatch.Draw(SpriteTexture, SpritePosition + (RotationOffset * Scale), SpriteFrame, Color.White, SpriteTransformations.Rotation, RotationOffset, DrawScale, SpriteTransformations.Effect, 0f);
        }
      }
    }
  }
}
