using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


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
    static public ContentManager Content;
    static Dictionary<string, Texture2D> TextureCache = new();


    public static Texture2D GetTexture(string path) {
      if (!TextureCache.TryGetValue(path, out var texture)) {
        texture = Content.Load<Texture2D>(path);
        TextureCache[path] = texture;
      }

      return texture;
    }

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