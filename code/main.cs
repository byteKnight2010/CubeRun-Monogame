using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.UI;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Debugger;
using static Cube_Run_C_.Tools;
using static Cube_Run_C_.Tools.BitMask;
using static Cube_Run_C_.Tools.Engine;


namespace Cube_Run_C_ {
  public class Main : Game {
    private readonly GraphicsDeviceManager Graphics;
    private SpriteBatch SpriteBatch;
    private RenderTarget2D SceneRenderTarget;
    private BasicSprite MouseSprite;
    private Dimensions StoredScreenDimensions;
    private Point StoredWindowPosition;

 
    public Main() {
      this.Graphics = new(this);
      this.Content.RootDirectory = "Content";
      this.IsMouseVisible = false;
    }


    protected override void Initialize() {
      this.Graphics.PreferredBackBufferWidth = DEFAULT_DIMENSIONS.Width;
      this.Graphics.PreferredBackBufferHeight = DEFAULT_DIMENSIONS.Height;
      this.Graphics.SynchronizeWithVerticalRetrace = true;
      this.Graphics.ApplyChanges();

      Camera.Graphics = Graphics;
      Assets.GraphicsDevice = Graphics.GraphicsDevice;
      MonitorDimensions = new(GraphicsDevice.Adapter.CurrentDisplayMode.Width, GraphicsDevice.Adapter.CurrentDisplayMode.Height);

      Camera.Reset(Color.Teal);

      this.IsFixedTimeStep = true;
      this.TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 144L);

      base.Initialize();
    }

    protected override void LoadContent() {
      this.SpriteBatch = new SpriteBatch(this.GraphicsDevice);
      this.SceneRenderTarget = new(this.GraphicsDevice, GraphicsDevice.Viewport.Width, this.GraphicsDevice.Viewport.Height);

      Effect BrightnessEffect = this.Content.Load<Effect>("Effects/Brightness");
      BrightnessEffect.Parameters["ScreenSize"].SetValue(new Vector2(this.GraphicsDevice.Viewport.Width, this.GraphicsDevice.Viewport.Height));
      BrightnessEffect.Parameters["InnerRadius"].SetValue(LanternLightWidth);
      BrightnessEffect.Parameters["MiddleRadius"].SetValue(LanternLightWidth * 1.25f);
      BrightnessEffect.Parameters["OuterRadius"].SetValue(LanternLightWidth * 2);
      BrightnessEffect.Parameters["InnerBrightness"].SetValue(1.0f);
      BrightnessEffect.Parameters["MiddleBrightness"].SetValue(0.6f);
      BrightnessEffect.Parameters["OuterBrightness"].SetValue(0.02f);
      BrightnessEffect.Parameters["BeyondBrightness"].SetValue(0.0f);
      BrightnessEffect.Parameters["Brightness"].SetValue(1.0f);
      BrightnessEffect.Parameters["LanternEnabled"].SetValue(false);

      this.MouseSprite.Image = this.Content.Load<Texture2D>("Images/UIImages/MainMouse");

      Globals.BrightnessEffect = BrightnessEffect;
      Assets.Content = this.Content;
      Assets.SpriteBatch = this.SpriteBatch;
      PauseMenu.Font = this.Content.Load<SpriteFont>("Fonts/PauseMenu");
      FocusOverlay.Font = this.Content.Load<SpriteFont>("Fonts/PauseMenu");

      Start();
    }

    private static void Start() {
      PauseMenu.Initialize();
      FocusOverlay.Initialize();
      LevelController.BeginLevel();
    }


    private void Resize(Dimensions windowSize) {
      this.Graphics.PreferredBackBufferWidth = windowSize.Width;
      this.Graphics.PreferredBackBufferHeight = windowSize.Height;
      this.Graphics.ApplyChanges();
      UpdateScaleFactor();
    }

    private void FullScreen() {
      Flip(ref GlobalStats, (byte)GlobalFlags.Fullscreen);

      if (IsSet(GlobalStats, (byte)GlobalFlags.Fullscreen)) {
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

      if (!IsSet(GlobalStats, (byte)GlobalFlags.Fullscreen))
        this.Window.Position = this.StoredWindowPosition;
    }


    protected override void OnActivated(object sender, EventArgs args) {
      base.OnActivated(sender, args);
      Set(ref GlobalStats, (byte)GlobalFlags.Paused, false);
      FocusOverlay.Active = false;
    }

    protected override void OnDeactivated(object sender, EventArgs args) {
      base.OnDeactivated(sender, args);
      Set(ref GlobalStats, (byte)GlobalFlags.Paused, true);
      FocusOverlay.Active = true;
    }


    private void UpdateScaleFactor() {
      Camera.UpdateScale();
      PauseMenu.UpdateScale();
      FocusOverlay.UpdateScale();
      this.UpdateBrightnessEffect();
    }

    private void UpdatePauseStats(bool InMenu) {
      if (InMenu) {
        Vector2 MousePosition = InputManager.MousePosition().ToVector2();
        Set(ref GlobalStats, (byte)GlobalFlags.MouseSpriteVisible, InScreen(MousePosition));

        if (IsSet(GlobalStats, (byte)GlobalFlags.MouseSpriteVisible))
          this.MouseSprite.Position = MousePosition;

        PauseMenu.Update();

        if (IsSet(PauseMenu.Status, (byte)MenuStatus.Quit))
          Exit();

        if (IsSet(PauseMenu.Status, (byte)MenuStatus.Fullscreen)) {
          this.FullScreen();

          Set(ref PauseMenu.Status, (byte)MenuStatus.Fullscreen, false);
        }
        if (IsSet(PauseMenu.Status, (byte)MenuStatus.Controls)) {
          Set(ref PauseMenu.Status, (byte)MenuStatus.Controls, false);
        }
      } else {
        if (IsSet(PauseMenu.Status, (byte)MenuStatus.UpdateScreen)) {
          if (IsSet(GlobalStats, (byte)GlobalFlags.Fullscreen))
            this.FullScreen();

          this.Resize(PauseMenu.ValidResolutions[(int)PauseMenu.SelectedValues[0]]);

          Set(ref PauseMenu.Status, (byte)MenuStatus.UpdateScreen, false);
        }
        if (IsSet(PauseMenu.Status, (byte)MenuStatus.UpdateBrightness)) {
          BrightnessEffect.Parameters["Brightness"].SetValue(PauseMenu.SelectedValues[1]);
          Set(ref PauseMenu.Status, (byte)MenuStatus.UpdateBrightness, false);
        }
        if (IsSet(PauseMenu.Status, (byte)MenuStatus.UpdateVolume)) {
          SoundEffect.MasterVolume = PauseMenu.SelectedValues[2];
          Set(ref PauseMenu.Status, (byte)MenuStatus.UpdateVolume, false);
        }
      }
    }

    private void UpdateBrightnessEffect() {
      this.SceneRenderTarget?.Dispose();
      this.SceneRenderTarget = new(this.GraphicsDevice, this.GraphicsDevice.Viewport.Width, this.GraphicsDevice.Viewport.Height);
      BrightnessEffect.Parameters["ScreenSize"].SetValue(new Vector2(this.GraphicsDevice.Viewport.Width, this.GraphicsDevice.Viewport.Height));

      BrightnessEffect.Parameters["InnerRadius"].SetValue(LanternLightWidth * Camera.Scale);
      BrightnessEffect.Parameters["MiddleRadius"].SetValue(LanternLightWidth * 1.25f * Camera.Scale);
      BrightnessEffect.Parameters["OuterRadius"].SetValue(LanternLightWidth * 2 * Camera.Scale);
    }


    protected override void Draw(GameTime gameTime) {
      this.GraphicsDevice.SetRenderTarget(this.SceneRenderTarget);
      this.GraphicsDevice.Clear(IsSet(GlobalStats, (byte)GlobalFlags.Paused) ? Color.Black : Camera.BackgroundColor);
      this.SpriteBatch.Begin();

      if (IsSet(GlobalStats, (byte)GlobalFlags.Paused)) {
        if (FocusOverlay.Active) {
          FocusOverlay.Draw(this.SpriteBatch);
        } else {
          PauseMenu.Draw(this.SpriteBatch);
        }
      } else {
        Camera.Draw(this.SpriteBatch, IsSet(GlobalStats, (byte)GlobalFlags.LevelActive) ? Globals.Player.Rect.Center() : new(96, 96));
      }

      if (IsSet(GlobalStats, (byte)GlobalFlags.MouseSpriteVisible) && InScreen(this.MouseSprite.Position))
        this.SpriteBatch.Draw(this.MouseSprite.Image, this.MouseSprite.Position, null, Color.White, 0f, Vector2.Zero, Camera.Scale, SpriteEffects.None, 0f);

      this.SpriteBatch.End();
      this.GraphicsDevice.SetRenderTarget(null);
      this.GraphicsDevice.Clear(IsSet(GlobalStats, (byte)GlobalFlags.Paused) ? Color.Black : Camera.BackgroundColor);

      if (!IsSet(GlobalStats, (byte)GlobalFlags.Paused) && IsSet(Globals.Player.Stats, (uint)PlayerData.PlayerStats.LanternEnabled))
        BrightnessEffect.Parameters["PlayerScreenPosition"].SetValue(Camera.WorldToScreen(Globals.Player.Rect.Center()));

      this.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, IsSet(GlobalStats, (byte)GlobalFlags.Paused) ? null : BrightnessEffect);
      this.SpriteBatch.Draw(this.SceneRenderTarget, Vector2.Zero, Color.White);
      this.SpriteBatch.End();

      base.Draw(gameTime);
    }

    protected override void Update(GameTime gameTime) {
      InputManager.Update();

      if (InputManager.IsButtonPressed(Buttons.Back) || InputManager.IsKeyPressed(Keys.Escape)) {
        Flip(ref GlobalStats, (byte)GlobalFlags.Paused);
        Set(ref PauseMenu.Status, (byte)MenuStatus.Active, IsSet(GlobalStats, (byte)GlobalFlags.Paused));

        if (!IsSet(GlobalStats, (byte)GlobalFlags.Paused)) {
          Set(ref GlobalStats, (byte)GlobalFlags.MouseSpriteVisible, false);
          PauseMenu.StoredMousePosition = InputManager.MousePosition();
        } else {
          Mouse.SetPosition(PauseMenu.StoredMousePosition.X, PauseMenu.StoredMousePosition.Y);
        }
      }

      if (IsSet(GlobalStats, (byte)GlobalFlags.Paused) && IsSet(PauseMenu.Status, (byte)MenuStatus.Active)) {
        this.UpdatePauseStats(true);
      } else if (IsSet(GlobalStats, (byte)GlobalFlags.Paused) != IsSet(PauseMenu.Status, (byte)MenuStatus.Active)) {
        Set(ref GlobalStats, (byte)GlobalFlags.Paused, true);
      } else {
        this.UpdatePauseStats(false);

        float DELTA_TIME = (float)gameTime.ElapsedGameTime.TotalSeconds;
        CurrentGameTime = gameTime.TotalGameTime;

        if (IsSet(GlobalStats, (byte)GlobalFlags.LevelActive))
          Level.Update(DELTA_TIME);
      }

      DebugLogger.Update(gameTime);
      base.Update(gameTime);
    }
  }
}
