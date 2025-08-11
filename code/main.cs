using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.Globals;


namespace Cube_Run_C_ {
    public class Main : Game {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch spriteBatch;
        private Point StoredScreenDimensions;


        public Main() {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;
        }


        protected override void Initialize() {
            this._graphics.PreferredBackBufferWidth = DEFAULT_DIMENSIONS.X;
            this._graphics.PreferredBackBufferHeight = DEFAULT_DIMENSIONS.Y;
            this._graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent() {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Assets.Content = this.Content;
        }


        private void FullScreen(bool fullscreen) {
            if (fullscreen) {
                this.StoredScreenDimensions.X = this._graphics.PreferredBackBufferWidth;
                this.StoredScreenDimensions.Y = this._graphics.PreferredBackBufferHeight;
                this._graphics.PreferredBackBufferWidth = MONITOR_DIMENSIONS.X;
                this._graphics.PreferredBackBufferHeight = MONITOR_DIMENSIONS.Y;
                this._graphics.IsFullScreen = true;
            } else {
                this._graphics.PreferredBackBufferWidth = this.StoredScreenDimensions.X;
                this._graphics.PreferredBackBufferHeight = this.StoredScreenDimensions.Y;
                this._graphics.IsFullScreen = false;
            }

            this._graphics.ApplyChanges();
        }


        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Teal);

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void Update(GameTime gameTime) {
            InputManager.Update(true, true);

            float DELTA_TIME = (float)gameTime.ElapsedGameTime.TotalSeconds;
            CurrentGameTime = gameTime.TotalGameTime;


            if (InputManager.IsButtonPressed(Buttons.Back) || InputManager.IsKeyDown(Keys.Escape))
                Exit();
            if (InputManager.IsKeyPressed(Keys.F)) {
                this.FullScreen(!this._graphics.IsFullScreen);
            }

            base.Update(gameTime);
        }
    }
}
