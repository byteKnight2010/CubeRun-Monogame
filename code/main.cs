using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.Tools;
using static Cube_Run_C_.UI;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Debugger;
using static Cube_Run_C_.Tools.BitMask;


namespace Cube_Run_C_ {
  public class Main : Game {
    private GraphicsDeviceManager Graphics;
    private SpriteBatch SpriteBatch;
    private Effect BrightnessEffect;
    private Dimensions StoredScreenDimensions;
    private Point StoredWindowPosition;
    private bool InFullScreen = false;
    private bool Paused = false;


    public Main() {
      this.Graphics = new GraphicsDeviceManager(this);
      this.Content.RootDirectory = "Content";
      this.IsMouseVisible = true;
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
      SaveSystem.Load();
      
      this.IsFixedTimeStep = true;
      this.TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 144.0);

      base.Initialize();
    }

    protected override void LoadContent() {
      this.SpriteBatch = new SpriteBatch(GraphicsDevice);
      this.BrightnessEffect = this.Content.Load<Effect>("Effects/Brightness");
      this.BrightnessEffect.Parameters["Brightness"].SetValue(1.0f);

      Assets.Content = this.Content;
      Assets.SpriteBatch = this.SpriteBatch;
      PauseMenu.Font = this.Content.Load<SpriteFont>("Fonts/PauseMenu");

      PauseMenu.Initialize();
      LevelController.BeginLevel(PlayerData.CurrentLevel);
    }


    private void Resize(Dimensions windowSize) {
      this.Graphics.PreferredBackBufferWidth = windowSize.Width;
      this.Graphics.PreferredBackBufferHeight = windowSize.Height;
      this.Graphics.ApplyChanges();
      UpdateScaleFactor();
    }

    private void FullScreen() {
      this.InFullScreen = !this.InFullScreen;

      if (this.InFullScreen) {
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

      if (!this.InFullScreen)
        this.Window.Position = this.StoredWindowPosition;
    }

    private static void UpdateScaleFactor() {
      Camera.UpdateScale();
      PauseMenu.UpdateScale();
    }


    protected override void Draw(GameTime gameTime) {
      this.GraphicsDevice.Clear(this.Paused ? Color.Black : Camera.BackgroundColor);
      this.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, this.Paused ? null : this.BrightnessEffect);

      if (this.Paused) {
        PauseMenu.Draw(this.SpriteBatch);
      } else {
        Camera.Draw(this.SpriteBatch, Level.Active ? Globals.Player.Rect.Center() : new(96, 96));
      }

      this.SpriteBatch.End();
      base.Draw(gameTime);
    }

    protected override void Update(GameTime gameTime) {
      InputManager.Update();

      if (InputManager.IsButtonPressed(Buttons.Back) || InputManager.IsKeyPressed(Keys.Escape)) 
        this.Paused = !this.Paused;
      if (InputManager.IsKeyPressed(Keys.F)) 
        this.FullScreen();

      if (this.Paused) {
        PauseMenu.Update();
        return;
      }

      if (IsSet(PauseMenu.Status, (byte)MenuStatus.UpdateScreen)) {
        if (this.InFullScreen)
          this.FullScreen();

        this.Resize(PauseMenu.ValidResolutions[(int)PauseMenu.SelectedValues[0]]);
        Set(ref PauseMenu.Status, (byte)MenuStatus.UpdateScreen, false);
      }
      if (IsSet(PauseMenu.Status, (byte)MenuStatus.UpdateBrightness)) {
        this.BrightnessEffect.Parameters["Brightness"].SetValue(PauseMenu.SelectedValues[1]);
        Set(ref PauseMenu.Status, (byte)MenuStatus.UpdateBrightness, false);
      }
      if (IsSet(PauseMenu.Status, (byte)MenuStatus.UpdateVolume)) {
        SoundEffect.MasterVolume = PauseMenu.SelectedValues[2];
        Set(ref PauseMenu.Status, (byte)MenuStatus.UpdateVolume, false);
      }

      float DELTA_TIME = (float)gameTime.ElapsedGameTime.TotalSeconds;
      CurrentGameTime = gameTime.TotalGameTime;

      if (Level.Active)
        Level.Update(DELTA_TIME);

      DebugLogger.Update(gameTime);
      base.Update(gameTime);
    }
  }
}
