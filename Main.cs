using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.Assets;
using static Cube_Run_C_.Assets.SoundManager;
using static Cube_Run_C_.ConfigManager;
using static Cube_Run_C_.Debugger;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Sprites;
using static Cube_Run_C_.Tools;
using static Cube_Run_C_.Tools.BitMask;
using static Cube_Run_C_.Tools.Engine;
using static Cube_Run_C_.UI;


namespace Cube_Run_C_ {
  public class Main : Game {
    private readonly GraphicsDeviceManager Graphics;
    private SpriteBatch SpriteBatch;
    private RenderTarget2D SceneRenderTarget;
    private RenderTarget2D ScreenTarget;
    private BasicSprite MouseSprite;
    private Dimensions StoredScreenDimensions;
    private Point StoredWindowPosition;
    private Point StoredMousePosition;
    private float DeltaTime = 0.0f;
    private byte TimeWarnStats = 0x00;


    public Main() {
      this.Graphics = new(this);
      this.Content.RootDirectory = "Content";
      this.IsMouseVisible = false;

      Save();
      Load();
      SaveAsBin();
      LoadBin();
      LoadCredits();

      BestiaryManager.SaveBestiary();
      BestiaryManager.SaveAsBin();
      BestiaryManager.LoadIndex();
    }


    protected override void Initialize() {
      this.Graphics.PreferredBackBufferWidth = ConfigManager.Graphics.DefaultDimensions.Width;
      this.Graphics.PreferredBackBufferHeight = ConfigManager.Graphics.DefaultDimensions.Height;
      this.Graphics.SynchronizeWithVerticalRetrace = true;
      this.Graphics.ApplyChanges();

      Camera.Graphics = this.Graphics;
      MonitorDimensions = new(this.GraphicsDevice.Adapter.CurrentDisplayMode.Width, this.GraphicsDevice.Adapter.CurrentDisplayMode.Height);

      Camera.Reset(Color.Teal);

      this.IsFixedTimeStep = true;
      this.TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / (long)Fps);

      base.Initialize();
    }

    protected override void LoadContent() {
      this.SpriteBatch = new SpriteBatch(this.GraphicsDevice);
      this.SceneRenderTarget = new(this.GraphicsDevice, this.GraphicsDevice.Viewport.Width, this.GraphicsDevice.Viewport.Height);
      this.ScreenTarget = new(this.GraphicsDevice, this.GraphicsDevice.Viewport.Width, this.GraphicsDevice.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.None);
      this.MouseSprite = new(this.Content.Load<Texture2D>("Images/UIImages/MainMouse"), Vector2.Zero);

      VisualManager.Content = this.Content;
      VisualManager.Initialize(this.Graphics.GraphicsDevice, this.Content);
      VisualManager.SetupAnimations(GameType.PlatformerUI);

      Start();
    }

    private void Start() {
      VisualManager.BrightnessEffect.Parameters["InnerColor"].SetValue(new Vector3(1.0f, 0.784f, 0.165f));
      VisualManager.BrightnessEffect.Parameters["MiddleColor"].SetValue(new Vector3(1.0f, 0.863f, 0.322f));
      VisualManager.BrightnessEffect.Parameters["OuterColor"].SetValue(new Vector3(0.969f, 0.914f, 0.592f));
      VisualManager.BrightnessEffect.Parameters["FarColor"].SetValue(new Vector3(1.0f, 0.957f, 0.788f));
      VisualManager.BrightnessEffect.Parameters["BeyondColor"].SetValue(new Vector3(1.0f, 1.0f, 1.0f));
      VisualManager.BrightnessEffect.Parameters["ScreenSize"].SetValue(new Vector2(this.GraphicsDevice.Viewport.Width, this.GraphicsDevice.Viewport.Height));
      VisualManager.BrightnessEffect.Parameters["InnerRadius"].SetValue(LanternLightWidth);
      VisualManager.BrightnessEffect.Parameters["MiddleRadius"].SetValue(LanternLightWidth * 1.25f);
      VisualManager.BrightnessEffect.Parameters["OuterRadius"].SetValue(LanternLightWidth * 2.0f);
      VisualManager.BrightnessEffect.Parameters["FarRadius"].SetValue(LanternLightWidth * 2.5f);
      VisualManager.BrightnessEffect.Parameters["InnerBrightness"].SetValue(1.0f);
      VisualManager.BrightnessEffect.Parameters["MiddleBrightness"].SetValue(0.6f);
      VisualManager.BrightnessEffect.Parameters["OuterBrightness"].SetValue(0.02f);
      VisualManager.BrightnessEffect.Parameters["FarBrightness"].SetValue(0.002f);
      VisualManager.BrightnessEffect.Parameters["BeyondBrightness"].SetValue(0.0f);
      VisualManager.BrightnessEffect.Parameters["PixelSize"].SetValue(5.25f);
      VisualManager.BrightnessEffect.Parameters["Brightness"].SetValue(1.0f);
      VisualManager.BrightnessEffect.Parameters["LanternEnabled"].SetValue(false);

      PauseMenu.Initialize(Menus.Audio);
      PauseMenu.Initialize(Menus.Display);
      PauseMenu.Initialize(Menus.Main);
      FocusOverlay.Initialize();
      SaveWindow.Initialize();
      ExitWindow.Initialize();

      SaveSystem.LoadSettings();
      
      this.UpdatePauseStats(true);
      Set(ref GlobalStats, (ushort)GlobalFlags.MouseSpriteVisible, false);
      this.UpdatePauseStats(false);

      TitleScreen.Initialize();
      TitleScreen.Display();
    }


    private void Resize(Dimensions windowSize) {
      this.Graphics.PreferredBackBufferWidth = windowSize.Width;
      this.Graphics.PreferredBackBufferHeight = windowSize.Height;
      this.Graphics.ApplyChanges();
      UpdateScaleFactor();
    }

    private void FullScreen() {
      Flip(ref GlobalStats, (ushort)GlobalFlags.Fullscreen);

      if (IsSet(GlobalStats, (ushort)GlobalFlags.Fullscreen)) {
        this.StoredScreenDimensions.Width = this.Graphics.PreferredBackBufferWidth;
        this.StoredScreenDimensions.Height = this.Graphics.PreferredBackBufferHeight;
        this.StoredWindowPosition = this.Window.Position;
        this.Graphics.PreferredBackBufferWidth = MonitorDimensions.Width;
        this.Graphics.PreferredBackBufferHeight = MonitorDimensions.Height;

        this.Window.IsBorderless = true;
        this.Window.Position = Point.Zero;
      } else {
        this.Graphics.PreferredBackBufferWidth = this.StoredScreenDimensions.Width;
        this.Graphics.PreferredBackBufferHeight = this.StoredScreenDimensions.Height;
        this.Window.IsBorderless = false;
      }

      this.Graphics.ApplyChanges();
      UpdateScaleFactor();

      if (!IsSet(GlobalStats, (uint)GlobalFlags.Fullscreen))
        this.Window.Position = this.StoredWindowPosition;
    }


    protected override void OnActivated(object sender, EventArgs args) {
      base.OnActivated(sender, args);
      Set(ref GlobalStats, (ushort)GlobalFlags.Paused, false);
      FocusOverlay.Active = false;

      if (CurrentSong != null)
        ResumeMusic();
    }

    protected override void OnDeactivated(object sender, EventArgs args) {
      base.OnDeactivated(sender, args);
      Set(ref GlobalStats, (ushort)GlobalFlags.Paused, true);
      FocusOverlay.Active = true;

      if (CurrentSong != null)
        PauseMusic();
    }

    protected override void OnExiting(object sender, ExitingEventArgs args) {
      if (!IsSet(TitleScreen.Stats, (byte)TitleScreenStats.Active) && !IsSet(ExitWindow.Stats, (byte)ExitWindowStats.ConfirmedExit)) {
        args.Cancel = true;
        ExitWindow.Display(); 
        return;
      }

      base.OnExiting(sender, args);
    }


    private void Pause(bool pause) {
      Set(ref GlobalStats, (ushort)GlobalFlags.Paused, pause);
      Set(ref PauseMenu.Stats, (ushort)MenuStatus.Active, pause);

      this.EnableMouse(pause);
    }


    private static void UpdateScaleFactor() {
      Camera.UpdateScale();
      SaveWindow.UpdateScale();
      PauseMenu.UpdateScale();
      EndLevelScreen.UpdateScale();
      FocusOverlay.UpdateScale();
      
      Set(ref GlobalStats, (ushort)GlobalFlags.UpdateLantern, true);
    }

    private void UpdatePauseStats(bool inMenu) {
      if (inMenu) {
        PauseMenu.Update();


        if (IsSet(PauseMenu.Stats, (ushort)MenuStatus.Quit)) {
          Set(ref PauseMenu.Stats, (ushort)MenuStatus.Quit, false);
          Exit();
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuStatus.Back)) {
          if (PauseMenu.CurrentMenu == Menus.Main) {
            SaveSystem.SaveSettings();
            Pause(false);
            FillColor = Camera.BackgroundColor;
          } else {
            PauseMenu.Initialize(Menus.Main);
          }

          Set(ref PauseMenu.Stats, (ushort)MenuStatus.Back, false);
        }

        if (IsSet(PauseMenu.Stats, (ushort)MenuStatus.UpdateBrightness)) {
          VisualManager.BrightnessEffect.Parameters["Brightness"].SetValue(PauseMenu.SelectedValues[(int)Menus.Display].Y);
          Set(ref PauseMenu.Stats, (ushort)MenuStatus.UpdateBrightness, false);
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuStatus.UpdateFPS)) {
          Fps = (ushort)PauseMenu.SelectedValues[(int)Menus.Display].Z;
          this.TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / (long)Fps);
          Set(ref PauseMenu.Stats, (ushort)MenuStatus.UpdateFPS, false);
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuStatus.Fullscreen)) {
          this.FullScreen();
          Set(ref PauseMenu.Stats, (ushort)MenuStatus.Fullscreen, false);
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuStatus.LetterBox)) {
          Flip(ref GlobalStats, (ushort)GlobalFlags.LetterBoxMode);
          UpdateScaleFactor();
          Set(ref PauseMenu.Stats, (ushort)MenuStatus.LetterBox, false);
        }

        if (IsSet(PauseMenu.Stats, (ushort)MenuStatus.UpdateVolume)) {
          MasterVolume(PauseMenu.SelectedValues[(int)Menus.Audio].X);
          Set(ref PauseMenu.Stats, (ushort)MenuStatus.UpdateVolume, false);
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuStatus.UpdateSFX)) {
          SFXVolume(PauseMenu.SelectedValues[(int)Menus.Audio].Y);
          Set(ref PauseMenu.Stats, (ushort)MenuStatus.UpdateSFX, false);
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuStatus.UpdateMusic)) {
          MusicVolume(PauseMenu.SelectedValues[(int)Menus.Audio].Z);
          Set(ref PauseMenu.Stats, (ushort)MenuStatus.UpdateMusic, false);
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuStatus.Mute)) {
          Mute();
          Set(ref PauseMenu.Stats, (ushort)MenuStatus.Mute, false);
        }

        if (IsSet(PauseMenu.Stats, (ushort)MenuStatus.Display)) {
          PauseMenu.Initialize(Menus.Display);
          Set(ref PauseMenu.Stats, (ushort)MenuStatus.Display, false);
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuStatus.Audio)) {
          PauseMenu.Initialize(Menus.Audio);
          Set(ref PauseMenu.Stats, (ushort)MenuStatus.Audio, false);
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuStatus.Controls)) {
          PauseMenu.Initialize(Menus.Controls);
          Set(ref PauseMenu.Stats, (ushort)MenuStatus.Controls, false);
        }
      } else {
        if (IsSet(PauseMenu.Stats, (ushort)MenuStatus.UpdateScreen)) {
          if (IsSet(GlobalStats, (uint)GlobalFlags.Fullscreen))
            this.FullScreen();

          this.Resize(PauseMenu.ValidResolutions[(int)PauseMenu.SelectedValues[(int)Menus.Display].X]);

          Set(ref PauseMenu.Stats, (ushort)MenuStatus.UpdateScreen, false);
        }
      }
    }

    private void UpdateBrightnessEffect() {
      if (!IsSet(GlobalStats, (ushort)GlobalFlags.UpdateLantern))
        return;

      Dimensions RenderSize = Camera.ViewportDimensions;
      Dimensions ScreenSize = Camera.ScreenDimensions;

      this.SceneRenderTarget?.Dispose();
      this.ScreenTarget?.Dispose();
      this.SceneRenderTarget = new(this.GraphicsDevice, RenderSize.Width, RenderSize.Height);
      this.ScreenTarget      = new(this.GraphicsDevice, ScreenSize.Width, ScreenSize.Height, false, SurfaceFormat.Color, DepthFormat.None);
      
      VisualManager.BrightnessEffect.Parameters["ScreenSize"].SetValue(new Vector2(RenderSize.Width, RenderSize.Height));
      VisualManager.BrightnessEffect.Parameters["InnerRadius"].SetValue(LanternLightWidth * Camera.Scale);
      VisualManager.BrightnessEffect.Parameters["MiddleRadius"].SetValue(LanternLightWidth * 1.25f * Camera.Scale);
      VisualManager.BrightnessEffect.Parameters["OuterRadius"].SetValue(LanternLightWidth * 2.0f * Camera.Scale);
      VisualManager.BrightnessEffect.Parameters["FarRadius"].SetValue(LanternLightWidth * 2.5f * Camera.Scale);
      VisualManager.BrightnessEffect.Parameters["PixelSize"].SetValue(5.25f * Camera.Scale);

      Set(ref GlobalStats, (ushort)GlobalFlags.UpdateLantern, false);
    }


    private void EnableMouse(bool enable) {
      Set(ref GlobalStats, (ushort)GlobalFlags.MouseSpriteVisible, enable);
      Set(ref GlobalStats, (ushort)GlobalFlags.DisplayMouse, enable);

      if (enable) {
        Mouse.SetPosition(this.StoredMousePosition.X, this.StoredMousePosition.Y);
      } else {
        this.StoredMousePosition = InputManager.MousePosition();
      }
    }

    private void UpdateMouse() {
      if (IsSet(GlobalStats, (ushort)GlobalFlags.EnableMouse)) {
        this.EnableMouse(true);
        Set(ref GlobalStats, (ushort)GlobalFlags.EnableMouse, false);
      }

      if (IsSet(GlobalStats, (ushort)GlobalFlags.DisableMouse)) {
        this.EnableMouse(false);
        Set(ref GlobalStats, (ushort)GlobalFlags.DisableMouse, false);
      }

      if (!IsSet(GlobalStats, (ushort)GlobalFlags.MouseSpriteVisible)) 
        return;

      Point MousePosition = InputManager.MousePosition();
      Set(ref GlobalStats, (ushort)GlobalFlags.DisplayMouse, InScreen(MousePosition));

      if (IsSet(GlobalStats, (ushort)GlobalFlags.DisplayMouse))
        this.MouseSprite.Rect.TopLeft(MousePosition);
    }
    

    private void TimeWarn() {
      if (!IsSet(this.TimeWarnStats, (byte)TimeWarnFlags.TimeWarning))
        return;

      double CurrentMinutes = CurrentGameTime.TotalMinutes;
      
      if (!IsSet(this.TimeWarnStats, (byte)TimeWarnFlags.FirstWarning) && CurrentMinutes >= Times.TimeWarnings[0]) {
        Set(ref this.TimeWarnStats, (byte)TimeWarnFlags.FirstWarning, true);
      
      } else if (!IsSet(this.TimeWarnStats, (byte)TimeWarnFlags.SecondWarning) && CurrentMinutes >= Times.TimeWarnings[1]) {
        Set(ref this.TimeWarnStats, (byte)TimeWarnFlags.SecondWarning, true);

      } else if (!IsSet(this.TimeWarnStats, (byte)TimeWarnFlags.ThirdWarning) && CurrentMinutes >= Times.TimeWarnings[2]) {
        Set(ref this.TimeWarnStats, (byte)TimeWarnFlags.ThirdWarning, true);

      }
    }


    private void CheckShortcuts() {
      if (InputManager.IsKeyPressed(Keys.F2))
        SaveSystem.SaveScreenshotAsync(this.ScreenTarget);
      
      if (InputManager.IsKeyPressed(Keys.F11))
        this.FullScreen();
    }

    private void CheckExit() {
      if (IsSet(GlobalStats, (ushort)GlobalFlags.Exit)) {
        Set(ref GlobalStats, (ushort)GlobalFlags.Exit, false);
        Exit();
        return;
      }

      if (IsSet(ExitWindow.Stats, (byte)ExitWindowStats.Active)) {
        ExitWindow.Update();

        if (IsSet(ExitWindow.Stats, (byte)ExitWindowStats.ConfirmedExit)) {
          Exit();
          return;
        }

        this.UpdateMouse();
        return;
      }
    }


    protected override void Draw(GameTime gameTime) {
      bool Paused = IsSet(GlobalStats, (ushort)GlobalFlags.Paused);
      bool DisplayMouse = IsSet(GlobalStats, (ushort)GlobalFlags.DisplayMouse);

      this.GraphicsDevice.SetRenderTarget(this.SceneRenderTarget);
      this.GraphicsDevice.Clear(FillColor);
      
      if (!Paused && IsSet(Level.FlagStats, (byte)LevelStatFlags.Active)) {
        Camera.UpdateDraw(Globals.Player.Rect.Center());

        this.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Camera.SpriteMatrix);
        Camera.Draw(this.SpriteBatch);
        this.SpriteBatch.End();
      }

      this.GraphicsDevice.SetRenderTarget(this.ScreenTarget);
      Camera.SetFullViewport();
      this.GraphicsDevice.Clear(Color.Black);

      if (Paused) {
        this.SpriteBatch.Begin();
          
        if (FocusOverlay.Active) {
          FocusOverlay.Draw(this.SpriteBatch);
        } else {
          PauseMenu.Draw(this.SpriteBatch);
        }
          
        if (DisplayMouse)
          this.SpriteBatch.Draw(this.MouseSprite.Image, this.MouseSprite.Rect.TopLeft(), null, Color.White, 0f, Vector2.Zero, Camera.Scale, SpriteEffects.None, 0f);
          
        this.SpriteBatch.End();
      } else {
        if (IsSet(Level.FlagStats, (byte)LevelStatFlags.Active)) {
          if (IsSet(Globals.Player.Stats, (uint)PlayerData.PlayerStats.LanternEnabled))
            VisualManager.BrightnessEffect.Parameters["PlayerScreenPosition"].SetValue(Camera.WorldToScreen(Globals.Player.Rect.Center()));

          this.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, VisualManager.BrightnessEffect);
          this.SpriteBatch.Draw(this.SceneRenderTarget, Camera.GameViewport.ToVector2(), Color.White);
          this.SpriteBatch.End();
         
          if (IsSet(GlobalStats, (ushort)GlobalFlags.LetterBoxMode)) {
            this.SpriteBatch.Begin();
            Camera.DrawLetterBoxBars(this.SpriteBatch);
            this.SpriteBatch.End();
          }
        }

        if (IsSet(EndLevelScreen.Stats, (byte)EndLevelScreenStats.Displaying) || IsSet(TmxLoadingScreen.Stats, (byte)TmxLoadScreenStats.Active)) {
          this.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);

          if (IsSet(EndLevelScreen.Stats, (byte)EndLevelScreenStats.Displaying)) {
            EndLevelScreen.Draw(this.SpriteBatch);

            if (IsSet(SaveWindow.Stats, (byte)SaveWindowStats.Active))
              SaveWindow.Draw(this.SpriteBatch);
          }

          if (IsSet(TmxLoadingScreen.Stats, (byte)TmxLoadScreenStats.Active))
            TmxLoadingScreen.Draw(this.SpriteBatch);

          this.SpriteBatch.End();
        }
      }

      this.GraphicsDevice.SetRenderTarget(null);
      this.SpriteBatch.Begin();
      this.SpriteBatch.Draw(this.ScreenTarget, Vector2.Zero, Color.White);
      this.SpriteBatch.End();

      if (IsSet(ExitWindow.Stats, (byte)ExitWindowStats.Active) || IsSet(TitleScreen.Stats, (byte)TitleScreenStats.Active) || DisplayMouse) {
        this.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);

        if (IsSet(TitleScreen.Stats, (byte)TitleScreenStats.Active)) {
          TitleScreen.Draw(this.SpriteBatch);
        } else if (IsSet(ExitWindow.Stats, (byte)ExitWindowStats.Active)) {
          ExitWindow.Draw(this.SpriteBatch);
        }
          
        if (DisplayMouse)
          this.SpriteBatch.Draw(this.MouseSprite.Image, this.MouseSprite.Rect.TopLeft(), null, Color.White, 0f, Vector2.Zero, Camera.Scale, SpriteEffects.None, 0f);
          
        this.SpriteBatch.End();
      }

      base.Draw(gameTime);
    }

    protected override void Update(GameTime gameTime) {
      InputManager.Update();

      this.CheckExit();
      this.CheckShortcuts();

      if (!IsSet(PauseMenu.Stats, (ushort)MenuStatus.Active) && (InputManager.IsButtonPressed(Buttons.Back) || InputManager.IsKeyPressed(Keys.Escape)) && !IsSet(EndLevelScreen.Stats, (byte)EndLevelScreenStats.Displaying)) {
        Pause(true);
        FillColor = Color.Black;
      }

      if (IsSet(TitleScreen.Stats, (byte)TitleScreenStats.Active)) {
        TitleScreen.Update();
      } else {
        if (IsSet(GlobalStats, (ushort)GlobalFlags.Paused) && IsSet(PauseMenu.Stats, (ushort)MenuStatus.Active)) {
          this.UpdatePauseStats(true);
        } else if (IsSet(GlobalStats, (ushort)GlobalFlags.Paused) != IsSet(PauseMenu.Stats, (ushort)MenuStatus.Active)) {
          Set(ref GlobalStats, (ushort)GlobalFlags.Paused, true);
        } else {
          this.UpdatePauseStats(false);

          this.DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
          CurrentGameTime = gameTime.TotalGameTime;

          if (IsSet(Level.FlagStats, (byte)LevelStatFlags.Active)) {
            Level.Update(this.DeltaTime);
          } else if (IsSet(EndLevelScreen.Stats, (byte)EndLevelScreenStats.Displaying)) {
            EndLevelScreen.Update(this.DeltaTime);
            SaveWindow.Update();
          } else if (IsSet(TmxLoadingScreen.Stats, (byte)TmxLoadScreenStats.Active)) {
            TmxLoadingScreen.Update(this.DeltaTime);
          }

          this.TimeWarn();
        }
      }

      this.UpdateMouse();
      this.UpdateBrightnessEffect();

      DebugLogger.Update(gameTime);
      base.Update(gameTime);
    }
  }
}