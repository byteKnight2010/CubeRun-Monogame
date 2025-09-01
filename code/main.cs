using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.Globals;


namespace Cube_Run_C_ {
    public class Main : Game {
        private Dimensions StoredScreenDimensions;
        private Point StoredWindowPosition;
        private GraphicsDeviceManager Graphics;
        private SpriteBatch SpriteBatch;
        private bool InFullScreen = false;


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

            this.IsFixedTimeStep = true;
            this.TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 144.0);

            base.Initialize();
        }

        protected override void LoadContent() {
            this.SpriteBatch = new SpriteBatch(GraphicsDevice);
            Assets.Content = this.Content;
            Assets.SpriteBatch = this.SpriteBatch;
            Level.Reset($"Content/Maps/Platformers/{PlayerData.CurrentLevel}.tmx");
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
                this.Window.Position = new Point(0, 0);
            } else {
                this.Graphics.PreferredBackBufferWidth = this.StoredScreenDimensions.Width;
                this.Graphics.PreferredBackBufferHeight = this.StoredScreenDimensions.Height;
                this.Window.IsBorderless = false;
                this.Window.Position = this.StoredWindowPosition;
            }

            this.Graphics.ApplyChanges();
            Camera.UpdateScale();
        }


        protected override void Draw(GameTime gameTime) {
            this.GraphicsDevice.Clear(Camera.BackgroundColor);

            this.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            Vector2 drawposition = Level.Active ? Globals.Player.Rect.Center() : new(96, 96);
            Camera.Draw(SpriteBatch, drawposition);

            this.SpriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void Update(GameTime gameTime) {
            InputManager.Update(true, true);

            float DELTA_TIME = (float)gameTime.ElapsedGameTime.TotalSeconds;
            CurrentGameTime = gameTime.TotalGameTime;

            if (InputManager.IsButtonPressed(Buttons.Back) || InputManager.IsKeyDown(Keys.Escape))
                Exit();
            if (InputManager.IsKeyPressed(Keys.F)) {
                this.FullScreen();
            }

            if (Level.Active) {
                foreach (SpriteGroup<Sprite> SpriteGroup in SpriteGroups.Values) {
                    SpriteGroup.Update(DELTA_TIME);
                }

                Globals.Player.Update(DELTA_TIME);
            }


            base.Update(gameTime);
        }
    }
}
