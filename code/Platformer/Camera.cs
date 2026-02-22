using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Cube_Run_C_.Assets;
using static Cube_Run_C_.Assets.VisualManager;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Sprites;
using static Cube_Run_C_.Tools;
using static Cube_Run_C_.Tools.BitMask;
using RectangleF = System.Drawing.RectangleF;


namespace Cube_Run_C_ {
  public static class Camera {
    public struct SpriteData {
      public Texture2D Texture;
      public SpriteTransform Transformations;
      public Rectangle? Frame;
      public Vector2 Position;
      public byte Effects = 0x00;


      public SpriteData(Texture2D texture, SpriteTransform transformations, Rectangle? frame, Vector2 position, byte effects) {
        this.Texture = texture;
        this.Transformations = transformations;
        this.Frame = frame;
        this.Position = position;
        this.Effects = effects;
      }


      public static SpriteData Empty => new(ColorTextures[(int)Colors.Black], SpriteTransform.Default, Rectangle.Empty, Vector2.Zero, 0x00);
    }



    private static readonly HashSet<Sprite> SpriteSet = [];
    public static List<Sprite> SpriteList = [];
    public static List<BasicSprite> BasicSpriteList = [];
    public static List<Sprite> NewSprites = [];
    public static List<Sprite> QuerySprites = [];
    private static List<BasicSprite> BasicQuerySprites = [];

    public static readonly SpatialGrid SpatialGrid = new();
    private static readonly BasicSpatialGrid BasicSpatialGrid = new();
    
    public static Matrix ScaleMatrix = new();
    public static Matrix SpriteMatrix = new();

    private static Vector2 DrawScale = Vector2.One;
    private static Vector2 Offset = Vector2.Zero;
    private static Vector2 LastTargetPosition = Vector2.Zero;

    private static DimensionsF LevelDimensions = DimensionsF.Zero;
    public static Dimensions HalfScreen = Dimensions.Zero;

    private static RectangleF CachedScreenRect = RectangleF.Empty;
    public static GraphicsDeviceManager Graphics;
    private static SpriteData SpriteProperties = SpriteData.Empty;

    public static Color BackgroundColor;

    public static float Scale = 1.0f;
    private static float ScaleY = 1.0f;
    private static float InverseScale = 1.0f;
    private static float InverseScaleY = 1.0f;
    private static float UnZoomedScale = 1.0f;
    private static float UnZoomedScaleY = 1.0f;
    public static byte Stats = 0x00;
    
    private static Rectangle LetterboxLeft = Rectangle.Empty;
    private static Rectangle LetterboxRight = Rectangle.Empty;

    public static Dimensions ViewportDimensions = Dimensions.Zero;
    public static Dimensions ScreenDimensions = Dimensions.Zero;

    private static Viewport FullViewport;
    public static Viewport GameViewport;


    public static void Reset(Color backgroundColor) {
      SpriteList.Clear();
      NewSprites.Clear();
      QuerySprites.Clear();
      BasicQuerySprites.Clear();
      SpatialGrid.Clear();
      BasicSpatialGrid.Clear();
      SpriteMatrix = new();
      Offset = Vector2.Zero;
      LastTargetPosition = Vector2.Zero;
      BackgroundColor = backgroundColor;

      SetAll(ref Stats, false);
      UpdateScale();
    }


    public static void Add(Sprite sprite) {
      if (SpriteList.Count == 0 || sprite.Z >= SpriteList[^1].Z) {
        SpriteSet.Add(sprite);
        SpriteList.Add(sprite);
        SpatialGrid.Insert(sprite);
      } else {
        SpriteSet.Add(sprite);
        NewSprites.Add(sprite);
        SpatialGrid.Insert(sprite);
        Set(ref Stats, (byte)CameraStats.UpdateSort, true);
      }
    }

    public static void Add(BasicSprite sprite) {
      BasicSpriteList.Add(sprite);
      BasicSpatialGrid.Insert(sprite);
    }

    public static bool Remove(Sprite sprite) {
      SpriteSet.Remove(sprite);
      SpriteList.Remove(sprite);
      NewSprites.Remove(sprite);
      QuerySprites.Remove(sprite);
      SpatialGrid.Remove(sprite);

      return true;
    }

    public static bool Remove(BasicSprite sprite) {
      bool Removed = BasicSpriteList.Remove(sprite);

      if (Removed) {
        BasicQuerySprites.Remove(sprite);
        BasicSpatialGrid.Remove(sprite);
      }

      return Removed;
    }

    public static void Clear() {
      SpriteList.Clear();
      BasicSpriteList.Clear();
      NewSprites.Clear();
      QuerySprites.Clear();
      BasicQuerySprites.Clear();
      SpatialGrid.Clear();
      BasicSpatialGrid.Clear();
      Set(ref Stats, (byte)CameraStats.UpdateSort, false);
    }


    public static void LockPosition(Vector2 position, bool doLock) {
      if (doLock) 
        UpdateOffset(position);

      Set(ref Stats, (byte)CameraStats.LockedPosition, doLock);
    }

    public static void Zoom(float zoomScale, bool doZoom) {
      if (doZoom) {
        if (IsSet(Stats, (byte)CameraStats.Zoomed)) {
          Scale  = UnZoomedScale  * zoomScale;
          ScaleY = UnZoomedScaleY * zoomScale;
        } else {
          UnZoomedScale  = Scale;
          UnZoomedScaleY = ScaleY;
          Scale  *= zoomScale;
          ScaleY *= zoomScale;
        }
      } else {
        Scale  = UnZoomedScale;
        ScaleY = UnZoomedScaleY;
      }

      InverseScale  = 1f / Scale;
      InverseScaleY = 1f / ScaleY;
      LevelDimensions = new(LevelData.Dimensions.PixelWidth * Scale, LevelData.Dimensions.PixelHeight * ScaleY);

      Set(ref Stats, (byte)CameraStats.Zoomed, doZoom);
      UpdateOffset(LastTargetPosition);
    }


    public static void UpdateOffset(Vector2 position) {
      Offset.X = Math.Max(0, Math.Min(LevelDimensions.Width  - ViewportDimensions.Width,  (position.X * Scale)  - HalfScreen.Width));
      Offset.Y = Math.Max(0, Math.Min(LevelDimensions.Height - ViewportDimensions.Height, (position.Y * ScaleY) - HalfScreen.Height));
      LastTargetPosition = position;

      UpdateMatrix();
      Set(ref Stats, (byte)CameraStats.UpdateScreenRect, true);
    }

    public static void UpdateScale() {
      Dimensions WindowDimensions = new(Graphics.GraphicsDevice.Viewport.Width, Graphics.GraphicsDevice.Viewport.Height);
      FullViewport = Graphics.GraphicsDevice.Viewport;
      ScreenDimensions = WindowDimensions;

      bool LetterboxMode = IsSet(GlobalStats, (ushort)GlobalFlags.LetterBoxMode);

      if (LetterboxMode) {
        float TargetAspectRatio = 4f / 3f;
        int ViewportWidth = (int)(WindowDimensions.Height * TargetAspectRatio);
        int ViewportHeight = WindowDimensions.Height;

        if (ViewportWidth > WindowDimensions.Width) {
          ViewportWidth = WindowDimensions.Width;
          ViewportHeight = (int)(WindowDimensions.Width / TargetAspectRatio);
        }

        int ViewportX = (WindowDimensions.Width - ViewportWidth) >> 1;
        int ViewportY = (WindowDimensions.Height - ViewportHeight) >> 1;

        GameViewport = new(ViewportX, ViewportY, ViewportWidth, ViewportHeight);
        
        if (ViewportX > 0) {
          LetterboxLeft  = new(0, 0, ViewportX, WindowDimensions.Height);
          LetterboxRight = new(ViewportX + ViewportWidth, 0, ViewportX, WindowDimensions.Height);
        } else {
          LetterboxLeft  = new(0, 0, WindowDimensions.Width, ViewportY);
          LetterboxRight = new(0, ViewportY + ViewportHeight, WindowDimensions.Width, ViewportY);
        }

        ViewportDimensions = new(ViewportWidth, ViewportHeight);

        float NewScale = (float)ViewportDimensions.Height / (float)DEFAULT_DIMENSIONS.Height;

        HalfScreen = new(ViewportDimensions.Width >> 1, ViewportDimensions.Height >> 1);
        LevelDimensions = new(LevelData.Dimensions.PixelWidth * NewScale, LevelData.Dimensions.PixelHeight * NewScale);

        DrawScale = new(NewScale);
        Scale = NewScale;
        ScaleY = NewScale;
        InverseScale = 1f / NewScale;
        InverseScaleY = 1f / NewScale;
        UnZoomedScale = NewScale;
        UnZoomedScaleY = NewScale;
      } else {
        GameViewport = FullViewport;
        LetterboxLeft = Rectangle.Empty;
        LetterboxRight = Rectangle.Empty;
        ViewportDimensions = WindowDimensions;

        float NewScale = (float)ViewportDimensions.Width / (float)DEFAULT_DIMENSIONS.Width;

        float NewScaleY = (float)ViewportDimensions.Height / (float)DEFAULT_DIMENSIONS.Height;

        HalfScreen = new(ViewportDimensions.Width >> 1, ViewportDimensions.Height >> 1);
        LevelDimensions = new(LevelData.Dimensions.PixelWidth * NewScale, LevelData.Dimensions.PixelHeight * NewScaleY);

        DrawScale = new(NewScale, NewScaleY);
        Scale = NewScale;
        ScaleY = NewScaleY;
        InverseScale = 1f / NewScale;
        InverseScaleY = 1f / NewScaleY;
        UnZoomedScale = NewScale;
        UnZoomedScaleY = NewScaleY;
      }

      UpdateOffset(LastTargetPosition);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UpdateMatrix() {
      ScaleMatrix = Matrix.CreateScale(DrawScale.X, DrawScale.Y, 0f);
      SpriteMatrix = ScaleMatrix * Matrix.CreateTranslation(MathF.Round(-Offset.X), MathF.Round(-Offset.Y), 0f);
    }


    public static void UpdateSpritePosition(Sprite sprite) {
      SpatialGrid.Remove(sprite);
      SpatialGrid.Insert(sprite);
    }

    public static void UpdateBasicSpritePosition(BasicSprite sprite) {
      BasicSpatialGrid.Remove(sprite);
      BasicSpatialGrid.Insert(sprite);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 WorldToScreen(Vector2 worldPosition) => new(worldPosition.X * Scale - Offset.X, worldPosition.Y * ScaleY - Offset.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InScreen(Vector2 topLeft, Vector2 bottomRight) => bottomRight.X >= CachedScreenRect.X && topLeft.X <= CachedScreenRect.Right && bottomRight.Y >= CachedScreenRect.Y && topLeft.Y <= CachedScreenRect.Bottom;

    private static int BinarySearchInsertionPoint(List<Sprite> spriteList, byte Z) {
      int Left  = 0;
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


    public static void UpdateDraw(Vector2 targetPosition) {
      if (SpriteList.Count == 0 && BasicSpriteList.Count == 0)
        return;

      QuerySprites.Clear();
      BasicQuerySprites.Clear();

      float ExpectedWidth  = LevelData.Dimensions.PixelWidth  * Scale;
      float ExpectedHeight = LevelData.Dimensions.PixelHeight * ScaleY;

      if (LevelDimensions.Width != ExpectedWidth || LevelDimensions.Height != ExpectedHeight) {
        LevelDimensions = new(ExpectedWidth, ExpectedHeight);
        Set(ref Stats, (byte)CameraStats.UpdateScreenRect, true);
      }

      if (!IsSet(Stats, (byte)CameraStats.LockedPosition) && LastTargetPosition != targetPosition)
        UpdateOffset(targetPosition);

      if (IsSet(Stats, (byte)CameraStats.UpdateScreenRect)) {
        CachedScreenRect = new(Offset.X * InverseScale, Offset.Y * InverseScaleY, ViewportDimensions.Width * InverseScale, ViewportDimensions.Height * InverseScaleY);
        Set(ref Stats, (byte)CameraStats.UpdateScreenRect, false);
      }

      if (IsSet(Stats, (byte)CameraStats.UpdateSort)) {
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

              return spriteA.GetImage().GetHashCode().CompareTo(spriteB.GetImage().GetHashCode());
            });
          }

          NewSprites.Clear();
        }

        Set(ref Stats, (byte)CameraStats.UpdateSort, false);
      }

      BasicQuerySprites = BasicSpatialGrid.Query(CachedScreenRect);
      QuerySprites = SpatialGrid.Query(CachedScreenRect);

      QuerySprites.RemoveAll(sprite => !SpriteSet.Contains(sprite));
      
      if (QuerySprites.Count > 1) {
        Sprite[] SortedSprites = new Sprite[QuerySprites.Count];
        int[] Counts = new int[ConfigManager.Current.Spatial.MAX_Z_LAYERS];

        for (int Index = 0; Index < QuerySprites.Count; Index++) {
          Counts[QuerySprites[Index].Z]++;
        }

        for (int Index = 1; Index < ConfigManager.Current.Spatial.MAX_Z_LAYERS; Index++) {
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
    }

    public static void Draw(SpriteBatch spriteBatch) {
      if (SpriteList.Count == 0 && BasicSpriteList.Count == 0)
        return;
      
      for (int Index = 0; Index < BasicQuerySprites.Count; Index++) {
        BasicSprite Sprite = BasicQuerySprites[Index];
        RectangleF SpriteRect = Sprite.Rect;
        
        if (!SpriteRect.IntersectsWith(CachedScreenRect)) 
          continue;

        spriteBatch.Draw(Sprite.Image, SpriteRect.TopLeft(), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
      }

      for (int Index = 0; Index < QuerySprites.Count; Index++) {
        Sprite Sprite = QuerySprites[Index];
        RectangleF SpriteRect = Sprite.Rect;

        if (!SpriteRect.IntersectsWith(CachedScreenRect))
          continue;

        SpriteProperties.Texture = Sprite.GetImage();
        SpriteProperties.Transformations = Sprite.Transformations;
        SpriteProperties.Frame = Sprite.GetFrame();
        SpriteProperties.Position = SpriteRect.TopLeft();
        SpriteProperties.Effects = Sprite.Effects;

        Color SpriteColor = IsSet(Sprite.Effects, (byte)SpriteEffectFlags.Ghost) ? Color.White * GHOST_ALPHA : Color.White;

        if (SpriteProperties.Transformations != SpriteTransform.Default) {
          spriteBatch.Draw(SpriteProperties.Texture, SpriteProperties.Position + (Sprite.DrawOffset * Scale), SpriteProperties.Frame, SpriteColor, SpriteProperties.Transformations.Rotation, Sprite.RotationOffset, 1f, SpriteProperties.Transformations.Effect, 0f);
        } else {
          spriteBatch.Draw(SpriteProperties.Texture, SpriteProperties.Position, SpriteProperties.Frame, SpriteColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }
      }
    }

    public static void DrawLetterBoxBars(SpriteBatch spriteBatch) {
      if (LetterboxLeft.Width > 0 && LetterboxLeft.Height > 0)
        spriteBatch.Draw(ColorTextures[(int)Colors.Black], LetterboxLeft, Color.Black);

      if (LetterboxRight.Width > 0 && LetterboxRight.Height > 0)
        spriteBatch.Draw(ColorTextures[(int)Colors.Black], LetterboxRight, Color.Black);
    }


    public static void SetGameViewport() => Graphics.GraphicsDevice.Viewport = GameViewport;
    public static void SetFullViewport()  => Graphics.GraphicsDevice.Viewport = FullViewport;
  }
}
