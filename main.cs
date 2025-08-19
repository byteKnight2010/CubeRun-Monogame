using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.Globals;


namespace Cube_Run_C_ {
    public class Main : Game {
        public Dictionary<string, Texture2D> LevelImages;
        public Dictionary<string, List<Texture2D>> LevelAnimations;
        private (ushort, ushort) StoredScreenDimensions;
        private GraphicsDeviceManager Graphics;
        private SpriteBatch SpriteBatch;


        public Main() {
            this.Graphics = new GraphicsDeviceManager(this);
            this.Content.RootDirectory = "Content";
            this.IsMouseVisible = false;
        }


        protected override void Initialize() {
            this.Graphics.PreferredBackBufferWidth = DEFAULT_DIMENSIONS.Item1;
            this.Graphics.PreferredBackBufferHeight = DEFAULT_DIMENSIONS.Item2;
            this.Graphics.ApplyChanges();

            Globals.Graphics = this.GraphicsDevice;
            MonitorDimensions = ((ushort)GraphicsDevice.Adapter.CurrentDisplayMode.Width, (ushort)GraphicsDevice.Adapter.CurrentDisplayMode.Height);

            base.Initialize();
        }

        protected override void LoadContent() {
            /*List<Texture2D> LoadAnimationFrames(string FolderPath) {
                List<Texture2D> FrameList = new List<Texture2D>();
                ushort FrameIndex = 0;

                while (true) {
                    try {
                        Texture2D Frame = this.Content.Load<Texture2D>($"{FolderPath}/{FrameIndex}");
                        FrameList.Add(Frame);
                        FrameIndex++;
                    }
                    catch (ContentLoadException) {
                        break;
                    }
                }

                return FrameList;
            }*/


            this.SpriteBatch = new SpriteBatch(GraphicsDevice);
            Assets.Content = this.Content;
            Assets.SpriteBatch = this.SpriteBatch;
        }


        private void FullScreen(bool fullscreen) {
            this.Graphics.IsFullScreen = fullscreen;

            if (fullscreen) {
                this.StoredScreenDimensions.Item1 = (ushort)this.Graphics.PreferredBackBufferWidth;
                this.StoredScreenDimensions.Item2 = (ushort)this.Graphics.PreferredBackBufferHeight;
                this.Graphics.PreferredBackBufferWidth = MonitorDimensions.Item1;
                this.Graphics.PreferredBackBufferHeight = MonitorDimensions.Item2;
            } else {
                this.Graphics.PreferredBackBufferWidth = this.StoredScreenDimensions.Item1;
                this.Graphics.PreferredBackBufferHeight = this.StoredScreenDimensions.Item2;
            }

            this.Graphics.ApplyChanges();
        }


        protected override void Draw(GameTime gameTime) {
            this.GraphicsDevice.Clear(Color.Teal);

            this.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            foreach (SpriteGroup<Sprite> SpriteGroup in spriteGroups.Values) {
                SpriteGroup.Draw(this.SpriteBatch);
            }

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
                this.FullScreen(!this.Graphics.IsFullScreen);
            }

            foreach (SpriteGroup<Sprite> SpriteGroup in spriteGroups.Values) {
                SpriteGroup.Update(DELTA_TIME);
            }


            base.Update(gameTime);
        }
    }
}
