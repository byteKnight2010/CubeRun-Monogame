using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.Assets;
using static Cube_Run_C_.Assets.SoundManager;
using static Cube_Run_C_.Assets.VisualManager;
using static Cube_Run_C_.ConfigManager;
using static Cube_Run_C_.Debugger;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.PlatformerPlayer;
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

      this.IsFixedTimeStep = true;
      this.TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / (long)Fps);

      base.Initialize();
    }

    protected override void LoadContent() {
      this.SpriteBatch = new SpriteBatch(this.GraphicsDevice);
      this.SceneRenderTarget = new(this.GraphicsDevice, DEFAULT_DIMENSIONS.Width, DEFAULT_DIMENSIONS.Height);
      this.ScreenTarget = new(this.GraphicsDevice, DEFAULT_DIMENSIONS.Width, DEFAULT_DIMENSIONS.Height, false, SurfaceFormat.Color, DepthFormat.None);
      this.MouseSprite = new(this.Content.Load<Texture2D>("Images/UIImages/MainMouse"), Vector2.Zero);

      VisualManager.Content = this.Content;
      VisualManager.Initialize(this.Graphics.GraphicsDevice, this.Content);
      VisualManager.SetupAnimations(GameType.PlatformerUI);

      Start();
    }

    private void Start() {
      Shader.SetVariable("InnerColor", new Vector3(1.0f, 0.784f, 0.165f));
      Shader.SetVariable("MiddleColor", new Vector3(1.0f, 0.863f, 0.322f));
      Shader.SetVariable("OuterColor", new Vector3(0.969f, 0.914f, 0.592f));
      Shader.SetVariable("FarColor", new Vector3(1.0f, 0.957f, 0.788f));
      Shader.SetVariable("BeyondColor", new Vector3(1.0f, 1.0f, 1.0f));
      Shader.SetVariable("ScreenSize", new Vector2(this.GraphicsDevice.Viewport.Width, this.GraphicsDevice.Viewport.Height));
      Shader.SetVariable("InnerRadius", LanternLightWidth);
      Shader.SetVariable("MiddleRadius", LanternLightWidth * 1.25f);
      Shader.SetVariable("OuterRadius", LanternLightWidth * 2.0f);
      Shader.SetVariable("FarRadius", LanternLightWidth * 2.5f);
      Shader.SetVariable("InnerBrightness", 1.0f);
      Shader.SetVariable("MiddleBrightness", 0.6f);
      Shader.SetVariable("OuterBrightness", 0.02f);
      Shader.SetVariable("FarBrightness", 0.002f);
      Shader.SetVariable("BeyondBrightness", 0.0f);
      Shader.SetVariable("PixelSize", 5.25f);
      Shader.SetVariable("Brightness", 1.0f);
      Shader.SetVariable("LanternEnabled", false);

      SaveSystem.LoadSettings();

      PauseMenu.Initialize();
      FocusOverlay.Initialize();
      SaveWindow.Initialize();
      ExitWindow.Initialize();
      
      this.UpdatePauseStats(true);
      Set(ref GlobalStats, (ushort)GlobalFlags.MouseSpriteVisible, false);
      this.UpdatePauseStats(false);

      TitleScreen.Initialize();

      this.Resize(ConfigManager.Graphics.DefaultDimensions);

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
      if (!IsSet(GlobalStats, (ushort)GlobalFlags.ForceExit) && !IsSet(TitleScreen.Stats, (byte)TitleScreenFlags.Active) && !IsSet(ExitWindow.Stats, (byte)ExitWindowFlags.ConfirmedExit)) {
        args.Cancel = true;
        ExitWindow.Display(); 
        return;
      }

      base.OnExiting(sender, args);
    }


    private void Pause(bool pause) {
      Set(ref GlobalStats, (ushort)GlobalFlags.Paused, pause);
      Set(ref PauseMenu.Stats, (ushort)MenuFlags.Active, pause);

      this.EnableMouse(pause);
    }


    private static void UpdateScaleFactor() {
      Camera.UpdateScale();
      SaveWindow.UpdateScale();
      PauseMenu.UpdateScale();
      PlatformerEndScreen.UpdateScale();
      FocusOverlay.UpdateScale();
      PlayerDisplay.UpdateScaleFactor();
      
      Set(ref GlobalStats, (ushort)GlobalFlags.UpdateLantern, true);
    }

    private void UpdatePauseStats(bool inMenu) {
      if (inMenu) {
        PauseMenu.Update();


        if (IsSet(PauseMenu.Stats, (ushort)MenuFlags.Quit)) {
          Set(ref PauseMenu.Stats, (ushort)MenuFlags.Quit, false);
          Exit();
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuFlags.Back)) {
          if (PauseMenu.CurrentMenu == Menus.Main) {
            SaveSystem.SaveSettings();
            Pause(false);
            FillColor = Camera.BackgroundColor;
          } else {
            PauseMenu.InitializeMenu(Menus.Main);
          }

          Set(ref PauseMenu.Stats, (ushort)MenuFlags.Back, false);
        }

        if (IsSet(PauseMenu.Stats, (ushort)MenuFlags.UpdateBrightness)) {
          Shader.SetVariable("Brightness", PauseMenu.SelectedValues[(int)Menus.Display].Y);
          Set(ref PauseMenu.Stats, (ushort)MenuFlags.UpdateBrightness, false);
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuFlags.UpdateFPS)) {
          Fps = (ushort)PauseMenu.SelectedValues[(int)Menus.Display].Z;
          this.TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / (long)Fps);
          Set(ref PauseMenu.Stats, (ushort)MenuFlags.UpdateFPS, false);
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuFlags.Fullscreen)) {
          this.FullScreen();
          Set(ref PauseMenu.Stats, (ushort)MenuFlags.Fullscreen, false);
        }

        if (IsSet(PauseMenu.Stats, (ushort)MenuFlags.UpdateVolume)) {
          MasterVolume(PauseMenu.SelectedValues[(int)Menus.Audio].X);
          Set(ref PauseMenu.Stats, (ushort)MenuFlags.UpdateVolume, false);
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuFlags.UpdateSFX)) {
          SFXVolume(PauseMenu.SelectedValues[(int)Menus.Audio].Y);
          Set(ref PauseMenu.Stats, (ushort)MenuFlags.UpdateSFX, false);
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuFlags.UpdateMusic)) {
          MusicVolume(PauseMenu.SelectedValues[(int)Menus.Audio].Z);
          Set(ref PauseMenu.Stats, (ushort)MenuFlags.UpdateMusic, false);
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuFlags.Mute)) {
          Mute();
          Set(ref PauseMenu.Stats, (ushort)MenuFlags.Mute, false);
        }

        if (IsSet(PauseMenu.Stats, (ushort)MenuFlags.Display)) {
          PauseMenu.InitializeMenu(Menus.Display);
          Set(ref PauseMenu.Stats, (ushort)MenuFlags.Display, false);
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuFlags.Audio)) {
          PauseMenu.InitializeMenu(Menus.Audio);
          Set(ref PauseMenu.Stats, (ushort)MenuFlags.Audio, false);
        }
        if (IsSet(PauseMenu.Stats, (ushort)MenuFlags.Controls)) {
          PauseMenu.InitializeMenu(Menus.Controls);
          Set(ref PauseMenu.Stats, (ushort)MenuFlags.Controls, false);
        }
      } else {
        if (IsSet(PauseMenu.Stats, (ushort)MenuFlags.UpdateScreen)) {
          if (IsSet(GlobalStats, (uint)GlobalFlags.Fullscreen))
            this.FullScreen();

          this.Resize(PauseMenu.ValidResolutions[(int)PauseMenu.SelectedValues[(int)Menus.Display].X]);

          Set(ref PauseMenu.Stats, (ushort)MenuFlags.UpdateScreen, false);
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
      this.ScreenTarget = new(this.GraphicsDevice, ScreenSize.Width, ScreenSize.Height, false, SurfaceFormat.Color, DepthFormat.None);
      
      Shader.SetVariable("ScreenSize", new Vector2(RenderSize.Width, RenderSize.Height));
      Shader.SetVariable("InnerRadius", LanternLightWidth * Camera.Scale);
      Shader.SetVariable("MiddleRadius", LanternLightWidth * 1.25f * Camera.Scale);
      Shader.SetVariable("OuterRadius", LanternLightWidth * 2.0f * Camera.Scale);
      Shader.SetVariable("FarRadius", LanternLightWidth * 2.5f * Camera.Scale);
      Shader.SetVariable("PixelSize", 5.25f * Camera.Scale);

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
      if (!IsSet(this.TimeWarnStats, (byte)TimeWarningFlags.TimeWarning))
        return;

      double CurrentMinutes = CurrentGameTime.TotalMinutes;
      
      if (!IsSet(this.TimeWarnStats, (byte)TimeWarningFlags.FirstWarning) && CurrentMinutes >= Times.TimeWarnings[0]) {
        Set(ref this.TimeWarnStats, (byte)TimeWarningFlags.FirstWarning, true);
      
      } else if (!IsSet(this.TimeWarnStats, (byte)TimeWarningFlags.SecondWarning) && CurrentMinutes >= Times.TimeWarnings[1]) {
        Set(ref this.TimeWarnStats, (byte)TimeWarningFlags.SecondWarning, true);

      } else if (!IsSet(this.TimeWarnStats, (byte)TimeWarningFlags.ThirdWarning) && CurrentMinutes >= Times.TimeWarnings[2]) {
        Set(ref this.TimeWarnStats, (byte)TimeWarningFlags.ThirdWarning, true);

      }
    }


    private void CheckShortcuts() {
      if (InputManager.IsKeyPressed(Keys.F2))
        SaveSystem.SaveScreenshotAsync(this.ScreenTarget);
      
      if (InputManager.IsKeyPressed(Keys.F11))
        this.FullScreen();
    }

    private void CheckExit() {
      if (IsSet(GlobalStats, (ushort)GlobalFlags.ForceExit) || IsSet(GlobalStats, (ushort)GlobalFlags.Exit)) {
        Set(ref GlobalStats, (ushort)GlobalFlags.Exit, false);
        Exit();
        return;
      }

      if (IsSet(ExitWindow.Stats, (byte)ExitWindowFlags.Active)) {
        ExitWindow.Update();

        if (IsSet(ExitWindow.Stats, (byte)ExitWindowFlags.ConfirmedExit)) {
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
      
      if (!Paused && IsSet(PlatformerLevel.FlagStats, (byte)PlatformerLevelFlags.Active)) {
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
        if (IsSet(PlatformerLevel.FlagStats, (byte)PlatformerLevelFlags.Active)) {
          this.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, Shader);
          this.SpriteBatch.Draw(this.SceneRenderTarget, Camera.GameViewport.ToVector2(), Color.White);
          this.SpriteBatch.End();

          this.SpriteBatch.Begin();

          Camera.DrawLetterBoxBars(this.SpriteBatch);

          if (IsSet(PlayerDisplay.Stats, (byte)PlayerDisplayFlags.Active))
            PlayerDisplay.Draw(this.SpriteBatch);

          this.SpriteBatch.End();
        }

        if (IsSet(PlatformerEndScreen.Stats, (byte)PlatformerEndFlags.Displaying) || IsSet(TmxLoadingScreen.Stats, (byte)TmxLoadScreenFlags.Active)) {
          this.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);

          if (IsSet(PlatformerEndScreen.Stats, (byte)PlatformerEndFlags.Displaying)) {
            PlatformerEndScreen.Draw(this.SpriteBatch);

            if (IsSet(SaveWindow.Stats, (byte)SaveWindowFlags.Active))
              SaveWindow.Draw(this.SpriteBatch);
          }

          if (IsSet(TmxLoadingScreen.Stats, (byte)TmxLoadScreenFlags.Active))
            TmxLoadingScreen.Draw(this.SpriteBatch);

          this.SpriteBatch.End();
        }
      }

      this.GraphicsDevice.SetRenderTarget(null);
      this.SpriteBatch.Begin();
      this.SpriteBatch.Draw(this.ScreenTarget, Vector2.Zero, Color.White);
      this.SpriteBatch.End();

      if (IsSet(ExitWindow.Stats, (byte)ExitWindowFlags.Active) || IsSet(TitleScreen.Stats, (byte)TitleScreenFlags.Active) || DisplayMouse) {
        this.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);

        if (IsSet(TitleScreen.Stats, (byte)TitleScreenFlags.Active)) {
          TitleScreen.Draw(this.SpriteBatch);
        } else if (IsSet(ExitWindow.Stats, (byte)ExitWindowFlags.Active)) {
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

      if (!IsSet(PauseMenu.Stats, (ushort)MenuFlags.Active) && (InputManager.IsButtonPressed(Buttons.Back) || InputManager.IsKeyPressed(Keys.Escape)) && !IsSet(PlatformerEndScreen.Stats, (byte)PlatformerEndFlags.Displaying)) {
        Pause(true);
        FillColor = Color.Black;
      }

      if (IsSet(TitleScreen.Stats, (byte)TitleScreenFlags.Active)) {
        TitleScreen.Update();
      } else {
        if (IsSet(GlobalStats, (ushort)GlobalFlags.Paused) && IsSet(PauseMenu.Stats, (ushort)MenuFlags.Active)) {
          this.UpdatePauseStats(true);
        } else if (IsSet(GlobalStats, (ushort)GlobalFlags.Paused) != IsSet(PauseMenu.Stats, (ushort)MenuFlags.Active)) {
          Set(ref GlobalStats, (ushort)GlobalFlags.Paused, true);
        } else {
          this.UpdatePauseStats(false);

          this.DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
          CurrentGameTime = gameTime.TotalGameTime;

          if (IsSet(PlatformerLevel.FlagStats, (byte)PlatformerLevelFlags.Active)) {
            PlatformerLevel.Update(this.DeltaTime);

            if (Globals.Player is not null) {
              if (IsSet(Globals.Player.Stats, (uint)PlayerStats.LanternEnabled))
                Shader.SetVariable("PlayerScreenPosition", Camera.WorldToScreen(Globals.Player.Rect.Center()));

              Camera.Update(Globals.Player.Rect.Center());
            }
          } else if (IsSet(PlatformerEndScreen.Stats, (byte)PlatformerEndFlags.Displaying)) {
            PlatformerEndScreen.Update(this.DeltaTime);
            SaveWindow.Update();
          } else if (IsSet(TmxLoadingScreen.Stats, (byte)TmxLoadScreenFlags.Active)) {
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