using System;
using System.Collections.Generic;
using System.Xml.XPath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.Globals;


namespace Cube_Run_C_ {
  public static class InputManager {
    private static KeyboardState currentState;
    private static KeyboardState previousState;
    private static GamePadState currentGamePadState;
    private static GamePadState previousGamePadState;


    public static void Update(bool keyboard, bool gamePad) {
      if (keyboard) {
        previousState = currentState;
        currentState = Keyboard.GetState();
      }
      if (gamePad) {
        previousGamePadState = currentGamePadState;
        currentGamePadState = GamePad.GetState(PlayerIndex.One);
      }
    }

    // Keyboard
    public static bool IsKeyDown(Keys key) => currentState.IsKeyDown(key);
    public static bool IsKeyUp(Keys key) => currentState.IsKeyUp(key);
    public static bool IsKeyPressed(Keys key) => currentState.IsKeyDown(key) && previousState.IsKeyUp(key);
    public static bool IsKeyReleased(Keys key) => currentState.IsKeyUp(key) && previousState.IsKeyDown(key);
    // Controller
    public static bool IsGamePadConnected() => currentGamePadState.IsConnected;
    public static bool IsButtonDown(Buttons button) => currentGamePadState.IsButtonDown(button);
    public static bool IsButtonUp(Buttons button) => currentGamePadState.IsButtonUp(button);
    public static bool IsButtonPressed(Buttons button) => currentGamePadState.IsButtonDown(button) && previousGamePadState.IsButtonUp(button);
    public static bool IsButtonReleased(Buttons button) => currentGamePadState.IsButtonUp(button) && previousGamePadState.IsButtonDown(button);
  }

  public static class Assets {
    static private Dictionary<string, Texture2D> TextureCache = new();
    static private RenderTarget2D RenderTarget;
    static public ContentManager Content;
    static public SpriteBatch SpriteBatch;


    public static Texture2D GetTexture(string path, float rotation = 0f) {
      string CacheKey = rotation == 0f ? path : $"{path}_{rotation}";

      if (!Assets.TextureCache.TryGetValue(CacheKey, out Texture2D Texture)) {
        if (rotation == 0f) {
          Texture = Assets.Content.Load<Texture2D>(path);
        } else {
          if (!Assets.TextureCache.TryGetValue(path, out Texture2D OriginalTexture)) {
            OriginalTexture = Assets.Content.Load<Texture2D>(path);
            Assets.TextureCache[path] = OriginalTexture;
          }

          Texture = Assets.RotateTexture(OriginalTexture, rotation);
        }

        Assets.TextureCache[CacheKey] = Texture;
      }
      return Texture;
    }

    public static void UnloadTexture(string path, float rotation = 0f) {
      string CacheKey = rotation == 0f ? path : $"{path}_{rotation}";

      if (Assets.TextureCache.TryGetValue(CacheKey, out Texture2D Texture)) {
        Texture.Dispose();
        Assets.TextureCache.Remove(CacheKey);
      }
    }

    public static void UnloadAllTextures() {
      foreach (Texture2D Texture in Assets.TextureCache.Values) {
        Texture.Dispose();
      }

      Assets.TextureCache.Clear();
    }


    private static Texture2D RotateTexture(Texture2D texture, float rotation) {
      int OriginalWidth = texture.Width;
      int OriginalHeight = texture.Height;
      int NewWidth = OriginalWidth;
      int NewHeight = OriginalHeight;
      Vector2 Origin = new(OriginalWidth * 0.5f, OriginalHeight * 0.5f);

      if (Math.Abs(rotation - MathHelper.PiOver2) < 0.01f || Math.Abs(rotation - (MathHelper.Pi + MathHelper.PiOver2)) < 0.01f) {
        NewWidth = OriginalHeight;
        NewHeight = OriginalWidth;
      }

      Assets.RenderTarget = new(Graphics, NewWidth, NewHeight);

      Graphics.SetRenderTarget(RenderTarget);
      Graphics.Clear(Color.Transparent);

      Assets.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
      Assets.SpriteBatch.Draw(texture, new(NewWidth * 0.5f, NewHeight * 0.5f), null, Color.White, rotation, Origin, 1f, SpriteEffects.None, 0f);
      Assets.SpriteBatch.End();

      Graphics.SetRenderTarget(null);

      Texture2D Result = new(Graphics, NewWidth, NewHeight);
      Color[] Data = new Color[NewWidth * NewHeight];
      RenderTarget.GetData<Color>(Data);
      Result.SetData<Color>(Data);

      Assets.RenderTarget = null;
      return Result;
    }
  }

  public static class GameConverter {
    public static (ushort, ushort) PointToTile(Vector2 position) => ((ushort)(position.X / TILE_SIZE), (ushort)(position.Y / TILE_SIZE));
    public static (ushort, ushort) PointToCell(Vector2 position) => ((ushort)(position.X / CELL_SIZE), (ushort)(position.Y / CELL_SIZE));

    public static Vector2 TileToPoint((ushort, ushort) tilePos) => new(tilePos.Item1 * TILE_SIZE, tilePos.Item2 * TILE_SIZE);
    public static Vector2 CellToPoint((ushort, ushort) cellPos) => new(cellPos.Item1 * CELL_SIZE, cellPos.Item2 * CELL_SIZE);
  }


  public class Timer {
    private TimeSpan Duration;
    private TimeSpan StartTime;
    private Action OnComplete;
    public bool Active = false;
    private bool Repeat;


    public Timer(int milliseconds, Action onComplete = null, bool repeat = false) {
      this.Duration = TimeSpan.FromMilliseconds(milliseconds);
      this.StartTime = TimeSpan.Zero;
      this.OnComplete = onComplete;
      this.Repeat = repeat;
    }


    public void Activate(TimeSpan gameTimeNow) {
      this.Active = true;
      this.StartTime = gameTimeNow;
    }

    private void Deactivate(TimeSpan gameTimeNow) {
      this.Active = false;

      if (this.Repeat)
        this.Activate(gameTimeNow);
    }


    public void update(GameTime gameTime) {
      if (!this.Active)
        return;

      TimeSpan CURRENT_TIME = gameTime.TotalGameTime;

      if (CURRENT_TIME - this.StartTime >= this.Duration) {
        this.OnComplete?.Invoke();
        this.Deactivate(CURRENT_TIME);
      }
    }
  }


}