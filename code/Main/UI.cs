using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.Assets;
using static Cube_Run_C_.Assets.SoundManager;
using static Cube_Run_C_.Assets.VisualManager;
using static Cube_Run_C_.Camera;
using static Cube_Run_C_.ConfigManager;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Globals.PlayerData;
using static Cube_Run_C_.Sprites;
using static Cube_Run_C_.Tools;
using static Cube_Run_C_.Tools.BitMask;
using RectangleF = System.Drawing.RectangleF;


namespace Cube_Run_C_ {
  public class UI {
    public enum Menus : byte {
      Display = 0,
      Audio = 1,
      Controls = 2,
      Main = 3
    }

    public enum SaveOption : byte {
      Save = 0,
      No = 1,
      None = 2
    }

    public enum Corner : byte {
      TopLeft = 0,
      TopRight = 1,
      BottomLeft = 2,
      BottomRight = 3  
    }



    public static class TitleScreen {
      private static readonly Button[] SelectionButtons = [null, null, null, null];
      private static readonly BasicSprite[] SelectionImages = [null, null, null, null];
      private static readonly DisplayText[] SelectionStrings = [DisplayText.Empty, DisplayText.Empty, DisplayText.Empty, DisplayText.Empty];
      private static readonly string[] SelectionTexts = ["PLAY", "SETTINGS", "CREDITS", "EXIT"];
      private static readonly float[] HalfTextHeights = new float[SelectionTexts.Length];
      private const byte IMAGE_TEXT_PADDING = 50;
      private const byte BUTTON_PADDING = 100;
      public static byte Stats = 0x00;


      public static void Initialize() {
        SpriteFont Font = SpriteFonts[(int)Fonts.PauseMenu];

        for (int Index = 0; Index < HalfTextHeights.Length; Index++) {
          HalfTextHeights[Index] = Font.MeasureString(SelectionTexts[Index]).Y * 0.5f;
        }
      }

      public static void Reset() {
        for (int Index = 0; Index < SelectionTexts.Length; Index++) {
          SelectionButtons[Index] = null;
          SelectionImages[Index] = null;
          SelectionStrings[Index] = DisplayText.Empty;
        }

        Stats = 0x00;

        Set(ref GlobalStats, (ushort)GlobalFlags.DisableMouse, true);
      }


      public static void Display() {
        Set(ref GlobalStats, (ushort)GlobalFlags.MouseSpriteVisible, true);
        Set(ref Stats, (byte)TitleScreenStats.Active, true);

        UpdateScale();
      }

      public static void UpdateScale() {
        if (!IsSet(Stats, (byte)TitleScreenStats.Active))
          return;

        Texture2D[] SelectionIcons = [GetTexture("Images/UIImages/Medals/Medal_4"), GetTexture("Images/UIImages/Medals/Medal_3"), GetTexture("Images/UIImages/Medals/Medal_2"), GetTexture("Images/UIImages/Medals/Medal_1")];
        Texture2D ButtonTexture = GetTexture("Images/UIImages/TitleScreenButton");
        Dimensions ButtonSize = ButtonTexture.GetDimensions();
        const float YBegin = 150;
        int HalfWidth = DEFAULT_DIMENSIONS.Width >> 1;

        for (int Index = 0; Index < SelectionTexts.Length; Index++) {
          SelectionButtons[Index] = new(ButtonTexture, new(HalfWidth, YBegin + (Index * BUTTON_PADDING)));

          Vector2 ImagePosition = new Vector2(SelectionButtons[Index].OriginalRect.X + (SelectionIcons[Index].Width >> 1), SelectionButtons[Index].OriginalRect.Y + (ButtonTexture.Height >> 1) - (SelectionIcons[Index].Height >> 1)) * Scale;

          SelectionImages[Index] = new(SelectionIcons[Index], ImagePosition);
          SelectionStrings[Index] = new(SelectionTexts[Index], new(ImagePosition.X + SelectionIcons[Index].Width + IMAGE_TEXT_PADDING * Scale, ImagePosition.Y + HalfTextHeights[Index]), Color.White);
        }

        for (int Index = 0; Index < SelectionButtons.Length; Index++) {
          SelectionButtons[Index].UpdateScale();
        }
      }


      private static void Play() {
        FillColor = BackgroundColor;
        TmxLoadingScreen.Display(true);

        Task.Run(TmxLoadingScreen.LoadAsync);
      }


      public static void Draw(SpriteBatch spriteBatch) {
        SpriteFont Font = SpriteFonts[(int)Fonts.PauseMenu];

        for (int Index = 0; Index < SelectionTexts.Length; Index++) {
          SelectionButtons[Index].Draw(spriteBatch);
          spriteBatch.Draw(SelectionImages[Index].Image, SelectionImages[Index].Rect.TopLeft(), null, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
          spriteBatch.DrawString(Font, SelectionStrings[Index].Text, SelectionStrings[Index].Position, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }
      }

      public static void Update() {
        for (int Index = 0; Index < SelectionButtons.Length; Index++) {
          SelectionButtons[Index].Update();
        }

        if (Button.Clicking(SelectionButtons[0])) {
          Button.ResetClick(SelectionButtons[0]);
          Play();
        } else if (Button.Clicking(SelectionButtons[1])) {
          Button.ResetClick(SelectionButtons[1]);

          // Settings
        } else if (Button.Clicking(SelectionButtons[2])) {
          Button.ResetClick(SelectionButtons[2]);

          // Credits
        } else if (Button.Clicking(SelectionButtons[3])) {
          Button.ResetClick(SelectionButtons[3]);
          Set(ref GlobalStats, (ushort)GlobalFlags.Exit, true);
          return;
        } else {
          return;
        }

        Reset();
      }
    }


    public static class TmxLoadingScreen {
      private static readonly Vector2[] HalfTextSizes = [Vector2.Zero, Vector2.Zero];
      private static BasicAnimatedSprite Icon;
      private static DisplayText LoadingText = DisplayText.Empty;
      private static DisplayText ProgressText = DisplayText.Empty;
      private static readonly object ProgressLock = new();
      private static string LoadStage = "Loading";
      private static float DotTimer = 0.0f;
      private const byte TEXT_PADDING = 10;
      private static byte DotCount = 0;
      private static byte Progress = 0;
      public static byte Stats = 0x00;


      public static void Initialize() {
        SpriteFont Font = SpriteFonts[(int)Fonts.PauseMenu];
        Vector2 CenterPosition = new(DEFAULT_DIMENSIONS.Width >> 1, DEFAULT_DIMENSIONS.Height >> 1);

        LoadingText = new("Loading", CenterPosition, Color.White);
        ProgressText = new("0%", new(CenterPosition.X, CenterPosition.Y + TEXT_PADDING), Color.White);
      }


      public static void Display(bool show) {
        DotTimer = 0f;
        DotCount = 0;
        
        lock (ProgressLock) {
          LoadStage = "Loading";
          Progress = 0;
        }

        Set(ref Stats, (byte)TmxLoadScreenStats.Active, show);
      }

      public static void SetProgress(int progress, string stage) {
        lock (ProgressLock) {
          LoadStage = stage;
          Progress = (byte)Math.Clamp(progress, 0, 100);
        }
      }


      public static async Task LoadAsync() {
        Set(ref Stats, (byte)TmxLoadScreenStats.Loading, true);

        Thread.Sleep(100);
        SetProgress(0, "Loading Music");

        UnloadAllSounds();
        LoadSong("Sounds/Songs/MidLevel");
        GetSound("Sounds/Effects/Coin", true);
        GetSound("Sounds/Effects/FallingSpikeDestroy", true);

        Thread.Sleep(100);
        SetProgress(50, "Loading Animations");

        SetupAnimations(GameType.Platformer);

        Thread.Sleep(100);
        SetProgress(75, "Loading Player Save");

        await SaveSystem.Load();

        Thread.Sleep(100);
        SetProgress(95, "Loading Settings");

        await SaveSystem.LoadSettings();

        SetProgress(100, "Finished!");
        Thread.Sleep(100);

        CurrentLevel = 1;
        LevelData.Difficulty = "Base";
        
        Set(ref Stats, (byte)TmxLoadScreenStats.Loading, false);
        Set(ref Stats, (byte)TmxLoadScreenStats.Finished, true);
      }


      public static void Draw(SpriteBatch spriteBatch) {
        spriteBatch.DrawString(SpriteFonts[(int)Fonts.PauseMenu], LoadingText.Text, LoadingText.Position, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(SpriteFonts[(int)Fonts.PauseMenu], ProgressText.Text, ProgressText.Position, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
      }


      public static void UpdateText() {
        Vector2 CenterPosition = new(ViewportDimensions.Width >> 1, ViewportDimensions.Height >> 1);
        SpriteFont Font = SpriteFonts[(int)Fonts.PauseMenu];

        string Stage;
        byte Completion;

        lock (ProgressLock) {
          Stage = LoadStage;
          Completion = Progress;
        }

        string Dots = new('.', DotCount);
        string ConcatDots = Stage + Dots;
        string PercentText = $"{Completion}%";

        HalfTextSizes[0] = Font.MeasureString(ConcatDots) * 0.5f;
        HalfTextSizes[1] = Font.MeasureString(PercentText) * 0.5f;

        LoadingText = new(ConcatDots, new(CenterPosition.X - HalfTextSizes[0].X, CenterPosition.Y), Color.White);
        ProgressText = new(PercentText, new(CenterPosition.X - HalfTextSizes[1].X, CenterPosition.Y + HalfTextSizes[0].Y + TEXT_PADDING), Color.White);
      }

      public static void Update(float deltaTime) {
        if (IsSet(Stats, (byte)TmxLoadScreenStats.Finished)) {
          Set(ref Stats, (byte)TmxLoadScreenStats.Active, false);
          Display(false);

          LevelController.BeginLevel();
          return;
        }

        DotTimer += deltaTime;

        if (DotTimer >= ConfigManager.UI.LoadingDotInterval) {
          DotTimer = 0f;
          DotCount = (byte)((DotCount + 1) % 4);
        }

        UpdateText();
      }
    }

    public static class ScreenshotSaveScreen {
      private static BasicAnimatedSprite Icon;
      private static Rectangle BackgroundRect = Rectangle.Empty;
      private static DisplayText LoadingText = new("Saving", Vector2.Zero, Color.White);
      private static Vector2 HalfTextSize = Vector2.Zero;


      public static void Initialize() {
        Icon = new(new(AnimationsData[(int)Animations.ScreenshotSave]), Vector2.Zero);
        HalfTextSize = SpriteFonts[(int)Fonts.SmallPauseMenu].MeasureString(LoadingText.Text) * 0.5f;
      }


      public static void Display(Corner screenCorner) {
        
      }
    }


    public static class PlayerDisplay {
      private static DisplayText[] StatTexts = [DisplayText.Empty, DisplayText.Empty];
      private static List<BasicSprite> HeartSprites = new();
      private static List<BasicSprite> CoinSprites = new();
      private static List<BasicSprite> KeyCoinSprites = new();
      private static Rectangle[] DisplayRects = [Rectangle.Empty, Rectangle.Empty, Rectangle.Empty];
      private static readonly byte[] ImageWidths = [0, 0, 0];
      private static ushort StoredLives = Lives;
      private static ushort StoredCoins = Coins;
      private static byte StoredKeyCoins = KeyCoins;
      private static bool Active;


      public static void Initialize() {
        ImageWidths[0] = (byte)GetTexture("Images/UIImages/Heart").Width;
        ImageWidths[1] = (byte)GetTexture("Images/UIImages/Coin").Width;
        ImageWidths[2] = (byte)GetTexture("Images/UIImages/KeyCoin").Width;

        Active = true;
      }


      public static void UpdateLives(bool forceUpdate) {
        if (!forceUpdate && StoredLives == PlayerData.Lives)
          return;

        StoredLives = PlayerData.Lives;
      }

      public static void UpdateScore(bool forceUpdate) {
        if (!forceUpdate && StoredCoins == PlayerData.Coins)
          return;

        StoredCoins = PlayerData.Coins;
      }

      public static void UpdateKeyCoins(bool forceUpdate) {
        if (!forceUpdate && StoredKeyCoins == PlayerData.KeyCoins)
          return;

        StoredKeyCoins = PlayerData.KeyCoins;
      }


      public static void Draw(SpriteBatch spriteBatch) {
      }
      
      public static void Update() {
        if (!Active)
          return;

        UpdateLives(false);
        UpdateScore(false);
        UpdateKeyCoins(false);
      }
    }

    public static class EndLevelScreen {
      private static readonly Rectangle[] StatRects = [Rectangle.Empty, Rectangle.Empty, Rectangle.Empty];
      private static readonly DisplayText[] DisplayTexts = [DisplayText.Empty, DisplayText.Empty, DisplayText.Empty, DisplayText.Empty, DisplayText.Empty];
      private static readonly BasicSprite[] MedalSprites = [null, null, null];
      private static readonly string[] Texts = ["Deaths", "Life Blocks", "Coins"];
      private static readonly ushort[] LevelStats = [0, 0, 0];
      private static readonly ushort[] LevelMaxStats = [0, 0, 0];
      private static byte[] MedalScores = [0, 0, 0];
      private static BasicAnimatedSprite AdvanceButton;
      private static readonly Tools.Timer AdvanceTimer = new(2000, () => Set(ref Stats, (byte)EndLevelScreenStats.CanAdvance, true));
      private static Rectangle DrawRect = Rectangle.Empty;
      private static Rectangle InflatedDrawRect = Rectangle.Empty;
      private const float VERTICAL_TEXT_PADDING = 50 / 4;
      public static byte Stats = 0x00;
      private const byte TEXT_PADDING = 50;


      public static void Display() {
        Set(ref Stats, (byte)EndLevelScreenStats.Displaying, true);

        LevelStats[0] = Level.Stats.Deaths;
        LevelStats[1] = Level.Stats.Coins;
        LevelStats[2] = Level.Stats.LifeBlocks;
        LevelMaxStats[0] = Level.MaxStats.Deaths;
        LevelMaxStats[1] = Level.MaxStats.Coins;
        LevelMaxStats[2] = Level.MaxStats.LifeBlocks;

        DisplayTexts[3] = new($"FLOOR: {CurrentWorld}", new(TEXT_PADDING, VERTICAL_TEXT_PADDING), Color.White);
        DisplayTexts[4] = new($"LEVEL: {CurrentLevel - WorldToLevels() * CurrentWorld}", new(DEFAULT_DIMENSIONS.Width - TEXT_PADDING, VERTICAL_TEXT_PADDING), Color.White);
        MedalScores = CalculateMedalScores();

        UpdateScale();
        AdvanceTimer.Activate();
      }

      private static void Reset() {
        for (int Index = 0; Index < Texts.Length; Index++) {
          StatRects[Index] = Rectangle.Empty;
          DisplayTexts[Index] = DisplayText.Empty;
          MedalSprites[Index] = null;
        }

        DisplayTexts[3] = DisplayText.Empty;
        DisplayTexts[4] = DisplayText.Empty;

        AdvanceButton = null;
      }


      private static byte[] CalculateMedalScores() {
        byte[] Medals = [0, 0, 0];

        if (LevelStats[0] == 0) {
          Medals[0] = 4;
        } else if (LevelStats[0] <= 3) {
          Medals[0] = 3;
        } else if (LevelStats[0] <= 5) {
          Medals[0] = 2;
        } else if (LevelStats[0] <= 10) {
          Medals[0] = 1;
        }

        for (int Index = 1; Index < LevelStats.Length; Index++) {
          if (LevelMaxStats[Index] == 0) {
            Medals[Index] = 4;
            continue;
          }

          float Ratio = (float)LevelStats[Index] / LevelMaxStats[Index];

          if (Ratio >= 1.00f) {
            Medals[Index] = 4;
          } else if (Ratio >= 0.90f) {
            Medals[Index] = 3;
          } else if (Ratio >= 0.80f) {
            Medals[Index] = 2;
          } else if (Ratio >= 0.70f) {
            Medals[Index] = 1;
          }
        }

        return Medals;
      }

      public static void UpdateScale() {
        if (!IsSet(Stats, (byte)EndLevelScreenStats.Displaying))
          return;

        SpriteFont Font = SpriteFonts[(int)Fonts.EndLevelScreen];
        int YStep = DEFAULT_DIMENSIONS.Height / 5;
        int Width = (int)(DEFAULT_DIMENSIONS.Width * 0.75f);
        int HalfDefaultWidth = DEFAULT_DIMENSIONS.Width >> 1;
        int RectHeight = DEFAULT_DIMENSIONS.Width / 10;
        int HalfPadding = TEXT_PADDING >> 1;
        
        for (int Index = 0; Index < Texts.Length; Index++) {
          Texture2D MedalImage = GetTexture($"Images/UIImages/Medals/Medal_{MedalScores[Index]}");

          StatRects[Index] = new(HalfDefaultWidth - (Width >> 1), (Index * YStep) + YStep, Width, RectHeight);          
          DisplayTexts[Index] = new($"{Texts[Index]}: {LevelStats[Index]} {(Index == 0 ? "" : $"/ {LevelMaxStats[Index]}")}", new Vector2(StatRects[Index].X + HalfPadding, StatRects[Index].Y + (StatRects[Index].Height >> 1) - (Font.MeasureString(Texts[Index]).Y * 0.5f)) * Scale, Color.White);          
          MedalSprites[Index] = new(MedalImage, new Vector2(StatRects[Index].Right - MedalImage.Width - HalfPadding, StatRects[Index].Y + (StatRects[Index].Height >> 1) - (MedalImage.Height >> 1)) * Scale);
        }

        Animation AdvanceAnimation = new(AnimationsData[(int)Animations.UIAButton]);
        Vector2 ButtonPosition = new Vector2(HalfDefaultWidth - (AdvanceAnimation.AnimationData.SpriteSheet.Width >> 1), DEFAULT_DIMENSIONS.Height * 0.95f - AdvanceAnimation.AnimationData.SpriteSheet.Height / 3) * Scale;
        
        AdvanceButton = new(AdvanceAnimation, ButtonPosition);
        AdvanceButton.Animation.Play();
      }


      public static void Draw(SpriteBatch spriteBatch) {
        SpriteFont Font = SpriteFonts[(int)Fonts.EndLevelScreen];

        for (int Index = 0; Index < Texts.Length; Index++) {
          DrawRect = StatRects[Index];
          
          spriteBatch.Draw(ColorTextures[(int)Colors.Pink], DrawRect.TopLeft() * Scale, null, Color.White, 0f, Vector2.Zero, DrawRect.VectorDimensions() * Scale, SpriteEffects.None, 0f);
          
          InflatedDrawRect = DrawRect;
          InflatedDrawRect.Inflate(5, 5);

          UITools.DrawRectOutline(spriteBatch, ColorTextures[(int)Colors.Tan], InflatedDrawRect, 5);
          
          spriteBatch.DrawString(Font, DisplayTexts[Index].Text, DisplayTexts[Index].Position, DisplayTexts[Index].Color, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
          spriteBatch.Draw(MedalSprites[Index].Image, MedalSprites[Index].Rect.TopLeft(), null, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }

        UITools.DrawVerticalText(Font, DisplayTexts[3], TEXT_PADDING, spriteBatch, Directions.Left);
        UITools.DrawVerticalText(Font, DisplayTexts[4], TEXT_PADDING, spriteBatch, Directions.Right);

        if (IsSet(Stats, (byte)EndLevelScreenStats.CanAdvance)) 
          spriteBatch.Draw(AdvanceButton.GetImage(), AdvanceButton.Rect.TopLeft(), AdvanceButton.GetFrame(), Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
      }
    
      public static void Update(float deltaTime) {
        AdvanceTimer.Update();
        AdvanceButton?.Update(deltaTime);

        if (InputManager.IsKeyPressed(Keys.Space) || InputManager.IsButtonPressed(Buttons.A)) {
          if (IsSet(Stats, (byte)EndLevelScreenStats.CanAdvance)) {
            AdvanceButton = null;
            SaveWindow.Display();

            Set(ref Stats, (byte)EndLevelScreenStats.CanAdvance, false);
          } else {
            AdvanceTimer.Deactivate();
          }
        }

        if (IsSet(SaveWindow.Stats, (byte)SaveWindowStats.Advance)) {
          Reset();
          LevelController.BeginLevel();
          Set(ref Stats, (byte)EndLevelScreenStats.Displaying, false);
          Set(ref SaveWindow.Stats, (byte)SaveWindowStats.Advance, false);
        }
      }
    }


    public static class SaveWindow {
      private static readonly Button[] SelectionButtons = [null, null];
      private static readonly DisplayText[] SelectionTexts = [DisplayText.Empty, DisplayText.Empty, DisplayText.Empty];
      private static readonly Rectangle[] NailRects = [Rectangle.Empty, Rectangle.Empty, Rectangle.Empty, Rectangle.Empty];
      private static readonly Vector2[] TextSizes = [Vector2.Zero, Vector2.Zero, Vector2.Zero];
      private static readonly string[] SelectionStrings = ["SAVE?", "YES", "NO"];
      private static BasicAnimatedSprite SaveDisk;
      private static Rectangle BackgroundRect = Rectangle.Empty;
      private static Vector2 DiskPosition = Vector2.Zero;
      private static readonly Dimensions BackgroundDimensions = new(1000, 560);
      private static SaveOption SelectedOption = SaveOption.None;
      private const byte NailDimensions = 10;
      public static byte Stats = 0x00;


      public static void Initialize() {
        SpriteFont Font = SpriteFonts[(int)Fonts.PauseMenu];

        SaveDisk = new(new(AnimationsData[(int)Animations.SaveDisk]), Vector2.Zero);

        for (int Index = 0; Index < TextSizes.Length; Index++) {
          TextSizes[Index] = Font.MeasureString(SelectionStrings[Index]) * 0.5f;
        }
      }


      public static void Display() {
        Texture2D ButtonTexture = GetTexture("Images/UIImages/SaveButton");
        Dimensions ButtonSize = ButtonTexture.GetDimensions();
        Dimensions HalfDimensions = DEFAULT_DIMENSIONS.Half();
        const int DoubleNailDimensions = NailDimensions << 1;


        DiskPosition.X = HalfDimensions.Width - (SaveDisk.Animation.AnimationData.FrameSize.X * 0.5f);
        DiskPosition.Y = HalfDimensions.Height - (SaveDisk.Animation.AnimationData.FrameSize.Y * 0.5f);

        BackgroundRect = new(HalfDimensions.Width - (BackgroundDimensions.Width >> 1), HalfDimensions.Height - (BackgroundDimensions.Height >> 1), BackgroundDimensions.Width, BackgroundDimensions.Height);
        SelectionTexts[0] = new(SelectionStrings[0], new(HalfDimensions.Width - TextSizes[0].X, BackgroundRect.Y + TextSizes[0].Y * 3f), Color.Black);

        SelectionButtons[0] = new(ButtonTexture, new(BackgroundRect.X + ButtonSize.Width, DiskPosition.Y + ButtonSize.Height * 1.25f));
        SelectionButtons[1] = new(ButtonTexture, new(BackgroundRect.Right - ButtonSize.Width, DiskPosition.Y + ButtonSize.Height * 1.25f));

        SelectionTexts[1] = new(SelectionStrings[1], new(SelectionButtons[0].Rect.Center.X - (int)TextSizes[1].X, SelectionButtons[0].Rect.Center.Y - (int)TextSizes[1].Y), Color.White);
        SelectionTexts[2] = new(SelectionStrings[2], new(SelectionButtons[1].Rect.Center.X - (int)TextSizes[2].X, SelectionButtons[1].Rect.Center.Y - (int)TextSizes[2].Y), Color.White);

        NailRects[0] = new(BackgroundRect.X + NailDimensions, BackgroundRect.Y + NailDimensions, NailDimensions, NailDimensions);
        NailRects[1] = new(BackgroundRect.Right - DoubleNailDimensions, BackgroundRect.Y + NailDimensions, NailDimensions, NailDimensions);
        NailRects[2] = new(BackgroundRect.X + NailDimensions, BackgroundRect.Bottom - DoubleNailDimensions, NailDimensions, NailDimensions);
        NailRects[3] = new(BackgroundRect.Right - DoubleNailDimensions, BackgroundRect.Bottom - DoubleNailDimensions, NailDimensions, NailDimensions);


        Set(ref GlobalStats, (ushort)GlobalFlags.EnableMouse, true);
        Set(ref Stats, (byte)SaveWindowStats.Active, true);

        UpdateScale();
      }

      private static void Reset() {
        for (int Index = 0; Index < SelectionButtons.Length; Index++) {
          SelectionButtons[Index] = null;
        }

        for (int Index = 0; Index < SelectionTexts.Length; Index++) {
          SelectionTexts[Index] = DisplayText.Empty;
        }

        for (int Index = 0; Index < NailRects.Length; Index++) {
          NailRects[Index] = Rectangle.Empty;
        }

        BackgroundRect = Rectangle.Empty;
        DiskPosition = Vector2.Zero;
        Stats = 0x00;

        Set(ref GlobalStats, (ushort)GlobalFlags.DisableMouse, true);
      }


      public static void UpdateScale() {
        if (!IsSet(Stats, (byte)SaveWindowStats.Active))
          return;

        Texture2D ButtonTexture = GetTexture("Images/UIImages/SaveButton");
        Dimensions ButtonSize = ButtonTexture.GetDimensions();
        Dimensions HalfDimensions = DEFAULT_DIMENSIONS.Half();
        const int DoubleNailDimensions = NailDimensions << 1;

        DiskPosition.X = HalfDimensions.Width - (SaveDisk.Animation.AnimationData.FrameSize.X * 0.5f);
        DiskPosition.Y = HalfDimensions.Height - (SaveDisk.Animation.AnimationData.FrameSize.Y * 0.5f);
        DiskPosition *= Scale;

        BackgroundRect = new((int)((HalfDimensions.Width - (BackgroundDimensions.Width >> 1)) * Scale),(int)((HalfDimensions.Height - (BackgroundDimensions.Height >> 1)) * Scale),(int)(BackgroundDimensions.Width * Scale),(int)(BackgroundDimensions.Height * Scale));
        SelectionTexts[0] = new(SelectionStrings[0], new Vector2(HalfDimensions.Width - TextSizes[0].X, (BackgroundRect.Y / Scale) + TextSizes[0].Y * 3f) * Scale, Color.White);
        
        SelectionButtons[0].UpdateScale();
        SelectionButtons[1].UpdateScale();

        SelectionTexts[1] = new(SelectionStrings[1], new Vector2(SelectionButtons[0].Rect.Center.X - (int)(TextSizes[1].X * Scale), SelectionButtons[0].Rect.Center.Y - (int)(TextSizes[1].Y * Scale)), Color.White);
        SelectionTexts[2] = new(SelectionStrings[2], new Vector2(SelectionButtons[1].Rect.Center.X - (int)(TextSizes[2].X * Scale), SelectionButtons[1].Rect.Center.Y - (int)(TextSizes[2].Y * Scale)), Color.White);

        NailRects[0] = new((int)((BackgroundRect.X / Scale + NailDimensions) * Scale), (int)((BackgroundRect.Y / Scale + NailDimensions) * Scale), (int)(NailDimensions * Scale), (int)(NailDimensions * Scale));
        NailRects[1] = new((int)((BackgroundRect.Right / Scale - DoubleNailDimensions) * Scale), (int)((BackgroundRect.Y / Scale + NailDimensions) * Scale), (int)(NailDimensions * Scale), (int)(NailDimensions * Scale));
        NailRects[2] = new((int)((BackgroundRect.X / Scale + NailDimensions) * Scale), (int)((BackgroundRect.Bottom / Scale - DoubleNailDimensions) * Scale), (int)(NailDimensions * Scale), (int)(NailDimensions * Scale));
        NailRects[3] = new((int)((BackgroundRect.Right / Scale - DoubleNailDimensions) * Scale), (int)((BackgroundRect.Bottom / Scale - DoubleNailDimensions) * Scale), (int)(NailDimensions * Scale), (int)(NailDimensions * Scale));
      }


      public static void Draw(SpriteBatch spriteBatch) {
        if (!IsSet(Stats, (byte)SaveWindowStats.Active))
          return;

        spriteBatch.Draw(ColorTextures[(int)Colors.Tan], BackgroundRect, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f);
        
        for (int Index = 0; Index < SelectionButtons.Length; Index++) {
          SelectionButtons[Index]?.Draw(spriteBatch);
        }
        for (int Index = 0; Index < SelectionTexts.Length; Index++) {
          spriteBatch.DrawString(SpriteFonts[(int)Fonts.PauseMenu], SelectionTexts[Index].Text, SelectionTexts[Index].Position, SelectionTexts[Index].Color, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }
        for (int Index = 0; Index < NailRects.Length; Index++) {
          spriteBatch.Draw(ColorTextures[(int)Colors.DarkPurple], NailRects[Index], null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f);
        }

        spriteBatch.Draw(SaveDisk.GetImage(), DiskPosition, SaveDisk.GetFrame(), Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
      }

      public static void Update() {
        if (!IsSet(Stats, (byte)SaveWindowStats.Active))
          return;

        for (int Index = 0; Index < SelectionButtons.Length; Index++) {
          SelectionButtons[Index].Update();
        }

        if (Button.Clicking(SelectionButtons[0])) {
          Button.ResetClick(SelectionButtons[0]);
          SelectedOption = SaveOption.Save;
        }

        if (Button.Clicking(SelectionButtons[1])) {
          Button.ResetClick(SelectionButtons[1]);
          SelectedOption = SaveOption.No;
        }

        if (SelectedOption == SaveOption.Save) {
          if (IsSet(Stats, (byte)SaveWindowStats.SaveLevel)) {
            SaveSystem.Save();
          } else if (IsSet(Stats, (byte)SaveWindowStats.SaveSettings)) {
            SaveSystem.SaveSettings();
          }
        }

        if (SelectedOption != SaveOption.None) {
          Reset();
          Set(ref Stats, (byte)SaveWindowStats.Advance, true);
        }
      }
    }

    public static class ExitWindow {
      private static readonly Button[] SelectionButtons = [null, null];
      private static readonly DisplayText[] SelectionTexts = [DisplayText.Empty, DisplayText.Empty, DisplayText.Empty, DisplayText.Empty];
      private static readonly Rectangle[] NailRects = [Rectangle.Empty, Rectangle.Empty, Rectangle.Empty, Rectangle.Empty];
      private static readonly Vector2[] TextSizes = [Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero];
      private static readonly string[] SelectionStrings = ["ARE YOU SURE YOU WANT TO QUIT?", "Any unsaved progress will be lost.", "Yes", "No"];
      private static BasicSprite ExitSign;
      private static Rectangle BackgroundRect = Rectangle.Empty;
      private const byte NailDimensions = 10;
      public static byte Stats = 0x00;


      public static void Initialize() {
        SpriteFont Font = SpriteFonts[(int)Fonts.PauseMenu];

        ExitSign = new(GetTexture("Images/UIImages/SaveDisk"), Vector2.Zero);

        TextSizes[0] = Font.MeasureString(SelectionStrings[0]) * 0.5f;
        TextSizes[1] = SpriteFonts[(int)Fonts.SmallPauseMenu].MeasureString(SelectionStrings[1]) * 0.5f;
        TextSizes[2] = Font.MeasureString(SelectionStrings[2]) * 0.5f;
        TextSizes[3] = Font.MeasureString(SelectionStrings[3]) * 0.5f;
      }

      private static void Reset() {
        for (int Index = 0; Index < SelectionButtons.Length; Index++) {
          SelectionButtons[Index] = null;
        }

        for (int Index = 0; Index < SelectionTexts.Length; Index++) {
          SelectionTexts[Index] = DisplayText.Empty;
          NailRects[Index] = Rectangle.Empty;
        }

        BackgroundRect = Rectangle.Empty;
        Stats = 0x00;

        Set(ref GlobalStats, (ushort)GlobalFlags.DisableMouse, true);

        if (SoundManager.CurrentSong != null)
          SoundManager.ResumeMusic();
      }



      public static void Display() {
        Texture2D ButtonTexture = GetTexture("Images/UIImages/SaveButton");
        Dimensions ButtonSize = ButtonTexture.GetDimensions();
        Dimensions HalfDimensions = DEFAULT_DIMENSIONS.Half();
        const int DoubleNailDimensions = NailDimensions << 1;
        bool MouseDisplaying = IsSet(GlobalStats, (ushort)GlobalFlags.MouseSpriteVisible);

        ExitSign.Rect.X = HalfDimensions.Width - ExitSign.Rect.Width * 0.5f;
        ExitSign.Rect.Y = HalfDimensions.Height - ExitSign.Rect.Height * 0.5f;

        BackgroundRect = new(0, 0, DEFAULT_DIMENSIONS.Width, DEFAULT_DIMENSIONS.Height);
        SelectionTexts[0] = new(SelectionStrings[0], new(HalfDimensions.Width - TextSizes[0].X, BackgroundRect.Y + TextSizes[0].Y * 3f), Color.Black);

        SelectionButtons[0] = new(ButtonTexture, new(ExitSign.Rect.X - ButtonSize.Width * 1.5f, ExitSign.Rect.Y + ButtonSize.Height * 1.25f));
        SelectionButtons[1] = new(ButtonTexture, new(ExitSign.Rect.Right + ButtonSize.Width * 1.5f, ExitSign.Rect.Y + ButtonSize.Height * 1.25f));

        SelectionTexts[1] = new(SelectionStrings[1], new(SelectionButtons[0].Rect.Center.X - (int)TextSizes[1].X, SelectionButtons[0].Rect.Center.Y - (int)TextSizes[1].Y), Color.White);
        SelectionTexts[2] = new(SelectionStrings[2], new(SelectionButtons[1].Rect.Center.X - (int)TextSizes[2].X, SelectionButtons[1].Rect.Center.Y - (int)TextSizes[2].Y), Color.White);

        NailRects[0] = new(BackgroundRect.X + NailDimensions, BackgroundRect.Y + NailDimensions, NailDimensions, NailDimensions);
        NailRects[1] = new(BackgroundRect.Right - DoubleNailDimensions, BackgroundRect.Y + NailDimensions, NailDimensions, NailDimensions);
        NailRects[2] = new(BackgroundRect.X + NailDimensions, BackgroundRect.Bottom - DoubleNailDimensions, NailDimensions, NailDimensions);
        NailRects[3] = new(BackgroundRect.Right - DoubleNailDimensions, BackgroundRect.Bottom - DoubleNailDimensions, NailDimensions, NailDimensions);


        Set(ref Stats, (byte)ExitWindowStats.Active, true);
        Set(ref Stats, (byte)ExitWindowStats.DisableMouse, !MouseDisplaying);

        if (!MouseDisplaying)
          Set(ref GlobalStats, (ushort)GlobalFlags.MouseSpriteVisible, true);

        UpdateScale();

        if (SoundManager.CurrentSong != null)
          SoundManager.PauseMusic();
      }


      public static void UpdateScale() {
        if (!IsSet(Stats, (byte)ExitWindowStats.Active))
          return;

        Dimensions HalfDimensions = DEFAULT_DIMENSIONS.Half();
        const int DoubleNailDimensions = NailDimensions << 1;

        ExitSign.Rect.X = (HalfDimensions.Width * Scale) - ExitSign.Rect.Width * 0.5f * Scale;
        ExitSign.Rect.Y = (HalfDimensions.Height * Scale) - ExitSign.Rect.Height * 0.5f * Scale;

        BackgroundRect = new(0, 0,(int)(DEFAULT_DIMENSIONS.Width * Scale),(int)(DEFAULT_DIMENSIONS.Height * Scale));
        
        SelectionButtons[0].UpdateScale();
        SelectionButtons[1].UpdateScale();

        SelectionTexts[0] = new(SelectionStrings[0], new Vector2((HalfDimensions.Width - TextSizes[0].X) * Scale, TextSizes[0].Y * 3f * Scale), Color.White);
        SelectionTexts[1] = new(SelectionStrings[1], new Vector2((HalfDimensions.Width - TextSizes[1].X) * Scale, (TextSizes[0].Y * 3f + TextSizes[0].Y * 2f) * Scale), Color.White);
        SelectionTexts[2] = new(SelectionStrings[2], new Vector2(SelectionButtons[0].Rect.Center.X - TextSizes[2].X * Scale, SelectionButtons[0].Rect.Center.Y - TextSizes[2].Y * Scale), Color.White);
        SelectionTexts[3] = new(SelectionStrings[3], new Vector2(SelectionButtons[1].Rect.Center.X - TextSizes[3].X * Scale, SelectionButtons[1].Rect.Center.Y - TextSizes[3].Y * Scale), Color.White);
        
        NailRects[0] = new((int)(NailDimensions * Scale), (int)(NailDimensions * Scale), (int)(NailDimensions * Scale), (int)(NailDimensions * Scale));
        NailRects[1] = new((int)((DEFAULT_DIMENSIONS.Width - DoubleNailDimensions) * Scale), (int)(NailDimensions * Scale), (int)(NailDimensions * Scale), (int)(NailDimensions * Scale));
        NailRects[2] = new((int)(NailDimensions * Scale), (int)((DEFAULT_DIMENSIONS.Height - DoubleNailDimensions) * Scale), (int)(NailDimensions * Scale), (int)(NailDimensions * Scale));
        NailRects[3] = new((int)((DEFAULT_DIMENSIONS.Width - DoubleNailDimensions) * Scale), (int)((DEFAULT_DIMENSIONS.Height - DoubleNailDimensions) * Scale), (int)(NailDimensions * Scale), (int)(NailDimensions * Scale));
      }


      public static void Draw(SpriteBatch spriteBatch) {
        SpriteFont Font = SpriteFonts[(int)Fonts.PauseMenu];

        spriteBatch.Draw(ColorTextures[(int)Colors.Tan], BackgroundRect, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f);
        
        for (int Index = 0; Index < SelectionButtons.Length; Index++) {
          SelectionButtons[Index]?.Draw(spriteBatch);
        }
        for (int Index = 0; Index < SelectionTexts.Length; Index++) {
          spriteBatch.DrawString(Index == 1 ? SpriteFonts[(int)Fonts.SmallPauseMenu] : Font, SelectionTexts[Index].Text, SelectionTexts[Index].Position, SelectionTexts[Index].Color, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }
        for (int Index = 0; Index < NailRects.Length; Index++) {
          spriteBatch.Draw(ColorTextures[(int)Colors.DarkPurple], NailRects[Index], null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f);
        }

        spriteBatch.Draw(ExitSign.Image, ExitSign.Rect.TopLeft(), null, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
      }

      public static void Update() {
        for (int Index = 0; Index < SelectionButtons.Length; Index++) {
          SelectionButtons[Index].Update();
        }

        if (Button.Clicking(SelectionButtons[0])) {
          Button.ResetClick(SelectionButtons[0]);
          Set(ref Stats, (byte)ExitWindowStats.ConfirmedExit, true);
        }

        if (Button.Clicking(SelectionButtons[1])) {
          Button.ResetClick(SelectionButtons[1]);
          if (IsSet(Stats, (byte)ExitWindowStats.DisableMouse)) 
            Set(ref GlobalStats, (ushort)GlobalFlags.MouseSpriteVisible, false);
          
          Reset();
        }
      }
    }


    public static class PauseMenu {
      public static readonly List<Dimensions> ValidResolutions = GetValidResolutions();
      private static readonly Slider[] Sliders = [null, null, null];
      private static readonly Button[] MainMenuButtons = [null, null, null, null];
      private static readonly Button[] ContentButtons = [null, null, null, null, null, null];
      private static readonly DisplayText[] DisplayTexts = [DisplayText.Empty, DisplayText.Empty, DisplayText.Empty, DisplayText.Empty, DisplayText.Empty, DisplayText.Empty, DisplayText.Empty];
      private static readonly MenuStatus[] DisplaySliderFlags = [MenuStatus.UpdateScreen, MenuStatus.UpdateBrightness, MenuStatus.UpdateFPS];
      private static readonly MenuStatus[] AudioSliderFlags = [MenuStatus.UpdateVolume, MenuStatus.UpdateSFX, MenuStatus.UpdateMusic];
      private static readonly MenuStatus[] ContentFlags = [MenuStatus.Display, MenuStatus.Audio, MenuStatus.Controls];
      private static readonly MenuStatus[] MainFlags = [MenuStatus.Back, MenuStatus.Quit, MenuStatus.None, MenuStatus.None];
      public static Vector3[] SelectedValues = [new(0f, 1.0f, 144f), Vector3.One, Vector3.Zero];
      public static Menus CurrentMenu;
      private const float SLIDER_APPROXIMATION = 0.001f;
      public static ushort Stats = 0x0000;
      private const ushort BOTTOM_BUTTON_Y = 640;
      private const byte SLIDER_START_Y = 180;
      private const byte BUTTON_PADDING = 160;
      private static byte SetResolutionIndex;


      public static void Initialize(Menus menu) {
        CurrentMenu = menu;

        Vector2[] SliderCaps = [Vector2.Zero, Vector2.Zero, Vector2.Zero];
        Texture2D ButtonTexture = GetTexture("Images/UIImages/MainMenuButton");
        int HalfWidth = DEFAULT_DIMENSIONS.Width >> 1;
        int QuarterWidth = HalfWidth >> 1;

        for (int Index = 0; Index < Sliders.Length; Index++) {
          Sliders[Index] = null;
        }
        for (int Index = 0; Index < MainMenuButtons.Length; Index++) {
          MainMenuButtons[Index] = null;
        }
        for (int Index = 0; Index < ContentButtons.Length; Index++) {
          ContentButtons[Index] = null;
        }

        switch (menu) {
          case Menus.Main:
            for (int Index = 0; Index < 3; Index++) {
              ContentButtons[Index] = new(ButtonTexture, new(HalfWidth, BUTTON_PADDING * (Index + 1)));
            }
            break;
          case Menus.Display:
            SelectedValues[(int)Menus.Display].X = SetResolutionIndex;
            SliderCaps[0] = new(ValidResolutions.Count - 1f, 100f / ValidResolutions.Count);
            SliderCaps[1] = new(ConfigManager.Graphics.BrightnessMax, Gameplay.Hundredth * 2);
            SliderCaps[2] = new(FPS_BOUNDS[1], 100f / FPS_BOUNDS[1]);
            MainMenuButtons[2] = new(ButtonTexture, new(HalfWidth * 1.75f, BOTTOM_BUTTON_Y));
            MainMenuButtons[3] = new(ButtonTexture, new(HalfWidth * 1.75f, 520));
            MainFlags[2] = MenuStatus.Fullscreen;
            MainFlags[3] = MenuStatus.LetterBox;
            break;
          case Menus.Audio:
            SliderCaps[0] = new(Audio.AudioMax, Gameplay.Hundredth);
            SliderCaps[1] = new(Audio.AudioMax, Gameplay.Hundredth);
            SliderCaps[2] = new(Audio.AudioMax, Gameplay.Hundredth);
            MainMenuButtons[2] = new(ButtonTexture, new(ViewportDimensions.Width - QuarterWidth, BOTTOM_BUTTON_Y));
            MainFlags[2] = MenuStatus.Mute;
            MainFlags[3] = MenuStatus.None;
            break;
          case Menus.Controls:
            for (int Index = 0; Index < 3; Index++) {
              ContentButtons[Index] = new(ButtonTexture, new(QuarterWidth, BUTTON_PADDING * (Index + 1)));
              ContentButtons[Index] = new(ButtonTexture, new(ViewportDimensions.Width - QuarterWidth, BUTTON_PADDING * (Index + 1)));
            }
            MainMenuButtons[2] = new(ButtonTexture, new(ViewportDimensions.Width - QuarterWidth, BOTTOM_BUTTON_Y));
            break;
        }

        Vector3 SelectedValue = SelectedValues[Math.Clamp((int)CurrentMenu, 0, 2)];

        if (menu != Menus.Main && menu != Menus.Controls) {
          for (int Index = 0; Index < 3; Index++) {
            float Value = Index switch {
              0 => SelectedValue.X,
              1 => SelectedValue.Y,
              2 => SelectedValue.Z,
              _ => 0f
            };

            Sliders[Index] = new(Color.DimGray, new(QuarterWidth, SLIDER_START_Y + (BUTTON_PADDING * Index)), [((float)(menu == Menus.Display && Index == 2 ? FPS_BOUNDS[0] : 0.0f)), SliderCaps[Index].X], SliderCaps[Index].Y, Value, HalfWidth);
          }

          Set(ref Sliders[0].Stats, (byte)SliderFlags.Selected, true);
        }

        MainMenuButtons[0] = new(ButtonTexture, new(QuarterWidth, BOTTOM_BUTTON_Y));
        MainMenuButtons[1] = new(ButtonTexture, new(HalfWidth, BOTTOM_BUTTON_Y));

        UpdateScale();
      }


      private static List<Dimensions> GetValidResolutions() {
        Dimensions CalculateResolution(ushort width) {
          int RoundedWidth = (width + 7) / 8 * 8;

          return new(RoundedWidth, (int)Math.Ceiling(RoundedWidth / SCREEN_RATIO / 8f) * 8);
        }

        List<Dimensions> GenerateResolutions(float startWidth, float scaleFactor, bool descending) {
          List<Dimensions> Resolutions = [];
          float Width = startWidth;

          while (true) {
            Dimensions Resolution = CalculateResolution((ushort)Width);

            if (descending) {
              if (Resolution.Width < MINIMUM_DIMENSIONS.Width || Resolution.Height < MINIMUM_DIMENSIONS.Height) 
                break;
            } else {
              if (Resolution.Width > MonitorDimensions.Width || Resolution.Height > MonitorDimensions.Height) 
                break;
            }

            Resolutions.Add(Resolution);
            Width *= scaleFactor;
          }

          return Resolutions;
        }


        List<Dimensions> SmallerResolutions = GenerateResolutions(DEFAULT_DIMENSIONS.Width / 1.2f, 1 / 1.2f, true);
        List<Dimensions> LargerResolutions = GenerateResolutions(DEFAULT_DIMENSIONS.Width, 1.2f, false);

        if (LargerResolutions.Count > 0) {
          int LargerResolutionCount = LargerResolutions.Count - 1;
          if (LargerResolutions[LargerResolutionCount].Width == MonitorDimensions.Width) {
            LargerResolutions.RemoveAt(LargerResolutionCount);
          }
        }

        SmallerResolutions.Reverse();
        SetResolutionIndex = (byte)SmallerResolutions.Count;
        SmallerResolutions.AddRange(LargerResolutions);

        return SmallerResolutions;
      }


      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private static void UpdateButtonText(int index, string text, Vector2 target) => DisplayTexts[index] = new(text, (target - SpriteFonts[(int)Fonts.PauseMenu].MeasureString(text) * 0.5f) * Scale, Color.White);

      private static void UpdateAllText() {
        for (int Index = 0; Index < DisplayTexts.Length; Index++) {
          DisplayTexts[Index] = DisplayText.Empty;
        }

        Vector3 SelectedValue = SelectedValues[Math.Clamp((int)CurrentMenu, 0, 2)];
        string[] ButtonTexts = ["Back", "Quit", string.Empty, string.Empty];
        string[] ContentTexts = [string.Empty, string.Empty, string.Empty];
        int HalfWidth = DEFAULT_DIMENSIONS.Width >> 1;

        switch (CurrentMenu) {
          case Menus.Main:
            string[] MainMenuTexts = ["Display", "Audio", "Controls"];

            for (int Index = 0; Index < ButtonTexts.Length; Index++) {
              if (ContentButtons[Index] != null)
                UpdateButtonText(Index, MainMenuTexts[Index], ContentButtons[Index].OriginalRect.FCenter());
            }
            break;
          case Menus.Display:
            Dimensions ScreenDimensions = ValidResolutions[(int)SelectedValue.X];
            ButtonTexts[2] = "Fullscreen";
            ButtonTexts[3] = "Letterbox";
            ContentTexts[0] = $"Window-Size: ({ScreenDimensions.Width} x {ScreenDimensions.Height})";
            ContentTexts[1] = $"Brightness: {SelectedValue.Y:F2}x";
            ContentTexts[2] = $"Fps: {SelectedValue.Z:F0}";
            break;
          case Menus.Audio:
            ButtonTexts[2] = "Mute";
            ContentTexts[0] = $"Volume: {SelectedValue.X * 100f:F2}%";
            ContentTexts[1] = $"SFX Volume: {SelectedValue.Y * 100f:F2}%";
            ContentTexts[2] = $"Music Volume: {SelectedValue.Z * 100f:F2}%";
            break;
          case Menus.Controls:

            break;
        }

        for (int Index = 0; Index < ButtonTexts.Length - 1; Index++) {
          if (ButtonTexts[Index] != string.Empty)
            UpdateButtonText(Index + 3, ButtonTexts[Index], MainMenuButtons[Index].OriginalRect.FCenter());

          if (Sliders[Index] != null) {
            Vector2 TextSize = SpriteFonts[(int)Fonts.PauseMenu].MeasureString(ContentTexts[Index]);
            DisplayTexts[Index] = new(ContentTexts[Index], new((HalfWidth - TextSize.X * 0.5f) * Scale, (Sliders[Index].OriginalPosition.Y - TextSize.Y * 1.5f) * Scale), Color.White);
          }
        }

        if (ButtonTexts[3] != string.Empty)
          UpdateButtonText(6, ButtonTexts[3], MainMenuButtons[3].OriginalRect.FCenter());
      }


      public static void Draw(SpriteBatch spriteBatch) {
        if (CurrentMenu != Menus.Main && CurrentMenu != Menus.Controls) {
          for (int Index = 0; Index < Sliders.Length; Index++) {
            Sliders[Index]?.Draw(spriteBatch);
          }
        }

        for (int Index = 0; Index < MainMenuButtons.Length; Index++) {
          MainMenuButtons[Index]?.Draw(spriteBatch);
        }
        for (int Index = 0; Index < ContentButtons.Length; Index++) {
          ContentButtons[Index]?.Draw(spriteBatch);
        }

        for (int Index = 0; Index < DisplayTexts.Length; Index++) {
          spriteBatch.DrawString(SpriteFonts[(int)Fonts.PauseMenu], DisplayTexts[Index].Text, DisplayTexts[Index].Position, DisplayTexts[Index].Color, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }
      }

      public static void UpdateScale() {
        for (int Index = 0; Index < Sliders.Length; Index++) {
          Sliders[Index]?.UpdateScale();
        }
        for (int Index = 0; Index < MainMenuButtons.Length; Index++) {
          MainMenuButtons[Index]?.UpdateScale();
        }
        for (int Index = 0; Index < ContentButtons.Length; Index++) {
          ContentButtons[Index]?.UpdateScale();
        }

        UpdateAllText();
      }

      public static void Update() {
        Vector3 CurrentValue = SelectedValues[Math.Clamp((int)CurrentMenu, 0, 2)];

        if (CurrentMenu != Menus.Main && CurrentMenu != Menus.Controls) {
          for (byte Index = 0; Index < Sliders.Length; Index++) {
            if (Sliders[Index] != null) {
              Sliders[Index].Update();

              float OldValue = Index switch {
                0 => CurrentValue.X,
                1 => CurrentValue.Y,
                2 => CurrentValue.Z,
                _ => 0f
              };

              if (Math.Abs(Sliders[Index].Value - OldValue) > SLIDER_APPROXIMATION) {
                switch (Index) {
                  case 0:
                    SelectedValues[(int)CurrentMenu].X = Sliders[Index].Value;
                    break;
                  case 1:
                    SelectedValues[(int)CurrentMenu].Y = Sliders[Index].Value;
                    break;
                  case 2:
                    SelectedValues[(int)CurrentMenu].Z = Sliders[Index].Value;
                    break;
                }

                MenuStatus[] CurrentSliderFlags = CurrentMenu == Menus.Display ? DisplaySliderFlags : AudioSliderFlags;
                Set(ref Stats, (ushort)CurrentSliderFlags[Index], true);
                UpdateAllText();
              }
            }
          }
        }

        for (int Index = 0; Index < ContentButtons.Length; Index++) {
          if (ContentButtons[Index] == null)
            continue;

          ContentButtons[Index].Update();

          if (Button.Clicking(ContentButtons[Index])) {
            Button.ResetClick(ContentButtons[Index]);
            Set(ref Stats, (ushort)ContentFlags[Index], true);
          }
        }
        for (int Index = 0; Index < MainMenuButtons.Length; Index++) {
          if (MainMenuButtons[Index] == null)
            continue;

          MainMenuButtons[Index].Update();

          if (Button.Clicking(MainMenuButtons[Index])) {
            Button.ResetClick(MainMenuButtons[Index]);
            Set(ref Stats, (ushort)MainFlags[Index], true);
          }
        }
      }
    }

    public static class FocusOverlay {
      private static Texture2D OverlayPixel;
      private static DisplayText OverlayText = new("Game paused to save resources.", Vector2.Zero, Color.White);
      private static Color OverlayColor = Color.Black * 0.5f;
      public static bool Active = false;


      public static void Initialize() {
        OverlayPixel = new(VisualManager.GraphicsDevice, 1, 1);
        OverlayPixel.SetData([Color.Black]);
        UpdateScale();
      }


      public static void UpdateScale() {
        Vector2 TextSize = SpriteFonts[(int)Fonts.PauseMenu].MeasureString(OverlayText.Text) * 0.5f;
        OverlayText.Position = new(HalfScreen.Width - TextSize.X, HalfScreen.Height - TextSize.Y);
      }


      public static void Draw(SpriteBatch spriteBatch) {
        if (!Active)
          return;

        spriteBatch.Draw(OverlayPixel, ScreenRect, OverlayColor);
        spriteBatch.DrawString(SpriteFonts[(int)Fonts.PauseMenu], OverlayText.Text, OverlayText.Position, OverlayText.Color, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
      }
    }


    public static class UITools {
      public static void DrawVerticalText(SpriteFont font, DisplayText text, ushort padding, SpriteBatch spriteBatch, Directions alignment = Directions.Left) {
        float ScreenScale = Scale;
        
        Vector2 Position = new(-1f, text.Position.Y * ScreenScale);
        string Text = text.Text;
        float ScaledPadding = padding * ScreenScale;

        for (int Index = 0; Index < Text.Length; Index++) {
          Vector2 CharSize = Vector2.Zero;

          if (Text[Index] != ' ') {
            string Current = Text[Index].ToString();
            CharSize = font.MeasureString(Current) * ScreenScale;

            if (Position.X == -1f) {
              float ScaledX = text.Position.X * ScreenScale;

              switch (alignment) {
                case Directions.Left:
                  Position.X = ScaledX;
                  break;
                case Directions.Right:
                  Position.X = ScaledX - CharSize.X;
                  break;
                case Directions.Center:
                  Position.X = ScaledX - (CharSize.X / 2);
                  break;
              }
            }

            spriteBatch.DrawString(font, Current, Position, Color.White, 0f, Vector2.Zero, ScreenScale, SpriteEffects.None, 0f);
          }

          Position.Y += CharSize.Y + ScaledPadding;
        }
      }

      public static void DrawRectOutline(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, int thickness = 1) {
        float ScreenScale = Scale;

        Vector2 Position = rect.TopLeft() * ScreenScale;
        float ScaledThickness = thickness * ScreenScale;
        float ScaledWidth = rect.Width * ScreenScale;
        float ScaledHeight = rect.Height * ScreenScale;
        
        spriteBatch.Draw(pixel, Position, null, Color.White, 0f, Vector2.Zero, new Vector2(rect.Width * ScreenScale, ScaledThickness), SpriteEffects.None, 0f);
        spriteBatch.Draw(pixel, Position + new Vector2(0, (rect.Height * ScreenScale) - ScaledThickness), null, Color.White, 0f, Vector2.Zero, new Vector2(ScaledWidth, ScaledThickness), SpriteEffects.None, 0f);
        spriteBatch.Draw(pixel, Position, null, Color.White, 0f, Vector2.Zero, new Vector2(ScaledThickness, ScaledHeight), SpriteEffects.None, 0f);
        spriteBatch.Draw(pixel, Position + new Vector2(ScaledWidth - ScaledThickness, 0), null, Color.White, 0f, Vector2.Zero, new Vector2(ScaledThickness, ScaledHeight), SpriteEffects.None, 0f);
      }  
    }


    public class Slider {
      private readonly Texture2D BackgroundTexture;
      private Texture2D SliderTexture;
      private readonly Rectangle OriginalBackgroundRect;
      private Rectangle BackgroundRect;
      private RectangleF OriginalSliderRect;
      private RectangleF SliderRect;
      public readonly Vector2 OriginalPosition;
      public Vector2 Position;
      private readonly float[] ValueBounds;
      private readonly float OriginalWidth;
      private float Width;
      private float CurrentValue;
      public float Value {
        get { return this.CurrentValue; }
        set {
          this.CurrentValue = MathHelper.Clamp(value, this.ValueBounds[0], this.ValueBounds[1]);
          this.UpdateSliderPosition();
        }
      }
      private readonly float Increment;
      public byte Stats = 0x00;


      public Slider(Color backgroundColor, Vector2 position, float[] valueBounds, float increment, float startValue, float width) {
        this.BackgroundTexture = new(VisualManager.GraphicsDevice, 1, 1);
        this.BackgroundTexture.SetData([backgroundColor]);
        this.OriginalPosition = position;
        this.Position = position;
        this.ValueBounds = valueBounds;
        this.OriginalWidth = width;
        this.Width = width;
        this.CurrentValue = startValue;
        this.Increment = Math.Max(1, increment);

        this.UpdateSliderTexture();

        this.OriginalBackgroundRect = new((int)position.X, (int)(position.Y + this.SliderTexture.Height * 0.5f - (this.SliderTexture.Height * 0.25f)), (int)width, (int)(this.SliderTexture.Height * 0.5f));
        this.BackgroundRect = this.OriginalBackgroundRect;

        this.UpdateSliderPosition();
      }


      public void Draw(SpriteBatch spriteBatch) {
        spriteBatch.Draw(this.BackgroundTexture, this.BackgroundRect, Color.White);
        spriteBatch.Draw(this.SliderTexture, this.SliderRect.TopLeft(), null, Color.White, 0f, Vector2.Zero, Camera.Scale, SpriteEffects.None, 0f);
      }


      private void UpdateSliderTexture() {
        if (this.Value == this.ValueBounds[0]) {
          this.SliderTexture = GetTexture("Images/UIImages/SliderLeft");
        } else if (this.Value == this.ValueBounds[1]) {
          this.SliderTexture = GetTexture("Images/UIImages/SliderRight");
        } else {
          this.SliderTexture = GetTexture("Images/UIImages/SliderCenter");
        }
      }

      private void UpdateSliderPosition() {
        this.OriginalSliderRect = this.SliderTexture.GetRectangleF(new(this.Position.X + ((this.Value - this.ValueBounds[0]) / (this.ValueBounds[1] - this.ValueBounds[0])) * this.Width - (this.SliderTexture.Width >> 1), this.Position.Y));
        this.SliderRect = this.OriginalSliderRect;
      }

      public void UpdateScale() {
        this.BackgroundRect = this.OriginalBackgroundRect.Scaled(Camera.Scale);

        this.Position = this.OriginalPosition * Camera.Scale;
        this.Width = this.OriginalWidth * Camera.Scale;

        this.SliderRect = new(this.Position.X + (this.Value - this.ValueBounds[0]) / (this.ValueBounds[1] - this.ValueBounds[0]) * this.Width - this.SliderTexture.Width * Camera.Scale * 0.5f, this.Position.Y, this.SliderTexture.Width * Camera.Scale, this.SliderTexture.Height * Camera.Scale);
      }

      public void Update() {
        Vector2 MousePosition = InputManager.MousePosition().ToVector2();

        if (!IsSet(PauseMenu.Stats, (ushort)MenuStatus.DraggingSlider) && !IsSet(this.Stats, (byte)SliderFlags.Dragging) && InputManager.IsMouseButtonDown(MouseButton.Left) && this.SliderRect.Contains(MousePosition.ToPointF())) {
          Set(ref PauseMenu.Stats, (ushort)MenuStatus.DraggingSlider, true);
          Set(ref this.Stats, (byte)SliderFlags.Dragging, true);
        }
        if (InputManager.IsMouseButtonReleased(MouseButton.Left)) {
          Set(ref PauseMenu.Stats, (ushort)MenuStatus.DraggingSlider, false);
          Set(ref this.Stats, (byte)SliderFlags.Dragging, false);
        }

        if (IsSet(this.Stats, (byte)SliderFlags.Dragging)) {
          this.Value = this.ValueBounds[0] + (MousePosition.X - this.Position.X) / this.Width * (this.ValueBounds[1] - this.ValueBounds[0]);
          this.UpdateSliderTexture();
        }

        if (IsSet(this.Stats, (byte)SliderFlags.Selected)) {
          if (InputManager.IsKeyPressed(Keys.Left)) {
            this.Value = Math.Max(this.ValueBounds[0], this.Value - this.Increment);
            this.UpdateSliderTexture();
          }

          if (InputManager.IsKeyPressed(Keys.Right)) {
            this.Value = Math.Min(this.ValueBounds[1], this.Value + this.Increment);
            this.UpdateSliderTexture();
          }
        }
      }
    }

    public class Button {
      private readonly Texture2D Texture;
      public Rectangle OriginalRect;
      public Rectangle Rect;
      public byte Stats = 0x00;


      public Button(Texture2D image, Vector2 position) {
        this.Texture = image;
        this.OriginalRect = new((int)(position.X - (this.Texture.Width >> 1)), (int)(position.Y), this.Texture.Width, this.Texture.Height);
        this.Rect = this.OriginalRect;
      }


      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool Clicking(Button button) => IsSet(button.Stats, (byte)ButtonFlags.Clicking);
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void ResetClick(Button button) => Set(ref button.Stats, (byte)ButtonFlags.Clicking, false);


      public void Draw(SpriteBatch spriteBatch) => spriteBatch.Draw(this.Texture, this.Rect, Color.White);

      public void UpdateScale() => this.Rect = this.OriginalRect.Scaled(Scale);
      
      public void Update() {
        if ((this.Rect.Contains(InputManager.MousePosition()) && InputManager.IsMouseButtonPressed(MouseButton.Left)) || (IsSet(this.Stats, (byte)ButtonFlags.Selected) && InputManager.IsKeyPressed(Keys.Enter))) {
          if (InputManager.IsMouseButtonPressed(MouseButton.Left))
            Set(ref this.Stats, (byte)ButtonFlags.Clicking, true);
        } else {
          Set(ref this.Stats, (byte)ButtonFlags.Clicking, false);
        }
      }
    }
  }
}
