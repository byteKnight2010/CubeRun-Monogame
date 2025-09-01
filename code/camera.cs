using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Cube_Run_C_.Globals;
using RectangleF = System.Drawing.RectangleF;


namespace Cube_Run_C_ {
  static class Camera {
    private static List<Sprite> SpriteList = new();
    private static List<Sprite> NewSprites = new();
    private static SpatialGrid SpatialGrid = new();
    private static Vector2 Offset = Vector2.Zero;
    private static Vector2 LastTargetPosition = Vector2.Zero;
    private static Vector2 DrawScale = Vector2.One;
    private static Dimensions ScreenDimensions;
    private static DimensionsF LevelDimensions;
    public static Dimensions HalfScreen = new();
    private static RectangleF CachedScreenRect;
    public static GraphicsDeviceManager Graphics;
    public static Color BackgroundColor;
    public static float Scale = 1.0f;
    private static float InverseScale = 1.0f;
    private static bool SortDirty = false;
    private static bool ScreenRectDirty = true;


    public static void Reset(Color backgroundColor) {
      Camera.SpriteList.Clear();
      Camera.NewSprites.Clear();
      Camera.SpatialGrid.Clear();
      Camera.Offset = Vector2.Zero;
      Camera.LastTargetPosition = Vector2.Zero;
      Camera.BackgroundColor = backgroundColor;
      Camera.SortDirty = false;
      Camera.UpdateScale();
    }


    public static void Add(Sprite sprite) {
      if (Camera.SpriteList.Count == 0 || sprite.Z >= Camera.SpriteList[^1].Z) {
        Camera.SpriteList.Add(sprite);
        Camera.SpatialGrid.Insert(sprite);
      } else {
        Camera.NewSprites.Add(sprite);
        Camera.SortDirty = true;
      }
    }

    public static bool Remove(Sprite sprite) {
      bool Removed = Camera.SpriteList.Remove(sprite);

      if (Removed) {
        Camera.SpatialGrid.Remove(sprite);
      } else {
        Removed = NewSprites.Remove(sprite);
      }

      return Removed;
    }

    public static void Clear() {
      Camera.SpriteList.Clear();
      Camera.NewSprites.Clear();
      Camera.SpatialGrid.Clear();
      Camera.SortDirty = false;
    }


    public static void LockPosition(Vector2 position) {
      if (Camera.Offset != position)
        Camera.Offset = position;
    }

    public static void UpdateScale() {
      Dimensions ScreenDimensions = new(Camera.Graphics.GraphicsDevice.Viewport.Width, Camera.Graphics.GraphicsDevice.Viewport.Height);
      float Scale = Math.Min(ScreenDimensions.Width / DEFAULT_DIMENSIONS.Width, ScreenDimensions.Height / DEFAULT_DIMENSIONS.Height);

      Camera.ScreenDimensions = ScreenDimensions;
      Camera.HalfScreen = new((int)(ScreenDimensions.Width * 0.5f), (int)(ScreenDimensions.Height * 0.5f));
      Camera.LevelDimensions = new(LevelData.Dimensions.Item2.Width * Scale, LevelData.Dimensions.Item2.Height * Scale);
      Camera.LastTargetPosition = new(0, 0);
      Camera.DrawScale.X = Scale;
      Camera.DrawScale.Y = Scale;
      Camera.Scale = Scale;
      Camera.InverseScale = 1 / Scale;
      Camera.ScreenRectDirty = true;
    }


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
      if (Camera.SpriteList.Count == 0) return;

      List<Sprite> Sprites = Camera.SpriteList;
      int ScreenWidth = Camera.ScreenDimensions.Width;
      int ScreenHeight = Camera.ScreenDimensions.Height;

      if (Camera.LevelDimensions.Width != LevelData.Dimensions.Item2.Width) {
        Camera.LevelDimensions = new(LevelData.Dimensions.Item2.Width * Camera.Scale, LevelData.Dimensions.Item2.Height * Camera.Scale);
        Camera.ScreenRectDirty = true;
      }

      if (Camera.LastTargetPosition != targetPosition) {
        Camera.Offset.X = Math.Max(0, Math.Min(Camera.LevelDimensions.Width - ScreenWidth, (targetPosition.X * Camera.Scale) - Camera.HalfScreen.Width));
        Camera.Offset.Y = Math.Max(0, Math.Min(Camera.LevelDimensions.Height - ScreenHeight, (targetPosition.Y * Camera.Scale) - Camera.HalfScreen.Height));
        Camera.LastTargetPosition = targetPosition;
        Camera.ScreenRectDirty = true;
      }

      if (Camera.ScreenRectDirty) {
        Camera.CachedScreenRect = new(Offset.X * Camera.InverseScale, Offset.Y * Camera.InverseScale, ScreenWidth * Camera.InverseScale, ScreenHeight * Camera.InverseScale);
        Camera.ScreenRectDirty = false;
      }

      if (Camera.SortDirty) {
        if (Camera.NewSprites.Count > 0) {
          if (Camera.NewSprites.Count < 10 && Camera.NewSprites.Count > 50) {
            foreach (Sprite NewSprite in Camera.NewSprites) {
              SpriteList.Insert(Camera.BinarySearchInsertionPoint(Camera.SpriteList, NewSprite.Z), NewSprite);
            }
          } else {
            Camera.SpriteList.AddRange(Camera.NewSprites);
            Camera.SpriteList.Sort((a, b) => a.Z.CompareTo(b.Z));
          }
        }

        Camera.SortDirty = false;
      }

      foreach (Sprite Sprite in Camera.SpatialGrid.Query(Camera.CachedScreenRect)) {
        if (!Sprite.Rect.IntersectsWith(Camera.CachedScreenRect)) continue;

        spriteBatch.Draw(Sprite.GetImage(), Sprite.Rect.TopLeft() * Camera.Scale - Offset, Sprite.GetFrame(), Color.White, 0f, new Vector2(0, 0), Camera.DrawScale, SpriteEffects.None, 0f);
      }
    }
  }
}
