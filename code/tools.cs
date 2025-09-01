using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.Globals;
using RectangleF = System.Drawing.RectangleF;


namespace Cube_Run_C_ {
  public struct Dimensions {
    public int Width;
    public int Height;


    public Dimensions(int w, int h) {
      this.Width = w;
      this.Height = h;
    }
  }

  public struct DimensionsF {
    public float Width;
    public float Height;

    public DimensionsF(float w, float h) {
      this.Width = w;
      this.Height = h;
    }
  }


  public static class RectangleFExtensions {
    public static Vector2 TopLeft(this RectangleF rect) => new(rect.X, rect.Y);
    public static Vector2 Center(this RectangleF rect) => new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);

    public static void TopLeft(this RectangleF rect, Vector2 position) {
      rect.X = position.X;
      rect.Y = position.Y;
    }
  }


  public static class InputManager {
    private static Dictionary<GameAction, (Keys, Buttons)> Controls = new() {
      [GameAction.MoveLeft] = (Keys.A, Buttons.DPadLeft),
      [GameAction.MoveRight] = (Keys.D, Buttons.DPadRight),
      [GameAction.MoveUp] = (Keys.W, Buttons.DPadUp),
      [GameAction.MoveDown] = (Keys.S, Buttons.DPadDown),
      [GameAction.Jump] = (Keys.Space, Buttons.A),
      [GameAction.Sprint] = (Keys.LeftShift, Buttons.Y),
      [GameAction.Shield] = (Keys.X, Buttons.B)
    };
    private static KeyboardState CurrentState;
    private static KeyboardState PreviousState;
    private static GamePadState CurrentGamePadState;
    private static GamePadState PreviousGamePadState;


    public static bool ChangeControl(GameAction action) {
      Keys[] PressedKeys = InputManager.CurrentState.GetPressedKeys();

      if (PressedKeys.Length > 0) {
        InputManager.Controls[action] = (PressedKeys[0], InputManager.Controls[action].Item2);
        return true;
      } else if (InputManager.CurrentGamePadState.IsConnected) {
        Buttons? PressedButton = GetPressedButton();

        if (PressedButton != null && InputManager.IsButtonPressed(PressedButton.Value)) {
          InputManager.Controls[action] = (InputManager.Controls[action].Item1, PressedButton.Value);
          return true;
        }
      }

      return false;
    }

    public static bool CheckAction(GameAction action, bool hold) {
      (Keys, Buttons) ActionInputs = InputManager.Controls[action];

      if (hold) {
        return InputManager.IsKeyPressed(ActionInputs.Item1) || InputManager.IsButtonPressed(ActionInputs.Item2);
      } else {
        return InputManager.IsKeyDown(ActionInputs.Item1) || InputManager.IsButtonDown(ActionInputs.Item2);
      }
    }


    private static Buttons? GetPressedButton() {
      GamePadState State = InputManager.CurrentGamePadState;

      if (State.Buttons.A == ButtonState.Pressed) return Buttons.A;
      if (State.Buttons.B == ButtonState.Pressed) return Buttons.B;
      if (State.Buttons.X == ButtonState.Pressed) return Buttons.X;
      if (State.Buttons.Y == ButtonState.Pressed) return Buttons.Y;

      if (State.Buttons.Start == ButtonState.Pressed) return Buttons.Start;
      if (State.Buttons.Back == ButtonState.Pressed) return Buttons.Back;

      if (State.Buttons.LeftShoulder == ButtonState.Pressed) return Buttons.LeftShoulder;
      if (State.Buttons.RightShoulder == ButtonState.Pressed) return Buttons.RightShoulder;

      if (State.DPad.Up == ButtonState.Pressed) return Buttons.DPadUp;
      if (State.DPad.Down == ButtonState.Pressed) return Buttons.DPadDown;
      if (State.DPad.Left == ButtonState.Pressed) return Buttons.DPadLeft;
      if (State.DPad.Right == ButtonState.Pressed) return Buttons.DPadRight;

      if (State.Buttons.LeftStick == ButtonState.Pressed) return Buttons.LeftStick;
      if (State.Buttons.RightStick == ButtonState.Pressed) return Buttons.RightStick;

      return null;
    }


    public static void Update(bool keyboard, bool gamePad) {
      if (keyboard) {
        InputManager.PreviousState = InputManager.CurrentState;
        InputManager.CurrentState = Keyboard.GetState();
      }
      if (gamePad) {
        InputManager.PreviousGamePadState = InputManager.CurrentGamePadState;
        InputManager.CurrentGamePadState = GamePad.GetState(PlayerIndex.One);
      }
    }


    public static bool IsKeyDown(Keys key) => InputManager.CurrentState.IsKeyDown(key);
    public static bool IsKeyUp(Keys key) => InputManager.CurrentState.IsKeyUp(key);
    public static bool IsKeyPressed(Keys key) => InputManager.CurrentState.IsKeyDown(key) && InputManager.PreviousState.IsKeyUp(key);
    public static bool IsKeyReleased(Keys key) => InputManager.CurrentState.IsKeyUp(key) && InputManager.PreviousState.IsKeyDown(key);
    public static bool IsGamePadConnected() => InputManager.CurrentGamePadState.IsConnected;
    public static bool IsButtonDown(Buttons button) => InputManager.CurrentGamePadState.IsButtonDown(button);
    public static bool IsButtonUp(Buttons button) => InputManager.CurrentGamePadState.IsButtonUp(button);
    public static bool IsButtonPressed(Buttons button) => InputManager.CurrentGamePadState.IsButtonDown(button) && InputManager.PreviousGamePadState.IsButtonUp(button);
    public static bool IsButtonReleased(Buttons button) => InputManager.CurrentGamePadState.IsButtonUp(button) && InputManager.PreviousGamePadState.IsButtonDown(button);
  }

  public static class Assets {
    static private Dictionary<(string, float), Texture2D> TextureCache = new();
    static public GraphicsDevice GraphicsDevice;
    static private RenderTarget2D RenderTarget;
    static public ContentManager Content;
    static public SpriteBatch SpriteBatch;


    public static Texture2D GetTexture(string path, float rotation = 0f) {
      (string, float) CacheKey = (Path.GetFileName(path), rotation);

      if (Assets.TextureCache.TryGetValue(CacheKey, out Texture2D Texture)) {
        return Texture;
      }

      if (rotation == 0f) {
        Texture = Assets.Content.Load<Texture2D>(path);
      } else {
        if (!Assets.TextureCache.TryGetValue((Path.GetFileName(path), 0f), out Texture2D OriginalTexture)) {
          OriginalTexture = Assets.Content.Load<Texture2D>(path);
          Assets.TextureCache[CacheKey] = OriginalTexture;
        }

        Texture = Assets.RotateTexture(OriginalTexture, rotation);
      }

      Assets.TextureCache[CacheKey] = Texture;
      return Texture;
    }


    public static void UnloadTexture(string path, float rotation = 0f) {
      (string, float) CacheKey = (Path.GetFileName(path), rotation);

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

      Assets.RenderTarget = new(Assets.GraphicsDevice, NewWidth, NewHeight);

      Assets.GraphicsDevice.SetRenderTarget(RenderTarget);
      Assets.GraphicsDevice.Clear(Color.Transparent);

      Assets.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
      Assets.SpriteBatch.Draw(texture, new(NewWidth * 0.5f, NewHeight * 0.5f), null, Color.White, rotation, Origin, 1f, SpriteEffects.None, 0f);
      Assets.SpriteBatch.End();

      Assets.GraphicsDevice.SetRenderTarget(null);

      Texture2D Result = new(Assets.GraphicsDevice, NewWidth, NewHeight);
      Color[] Data = new Color[NewWidth * NewHeight];
      RenderTarget.GetData<Color>(Data);
      Result.SetData<Color>(Data);

      Assets.RenderTarget = null;
      return Result;
    }
  }

  public static class GameConverter {
    public static (ushort, ushort) PointToTile(Vector2 position) => ((ushort)(position.X / TILE_SIZE), (ushort)(position.Y / TILE_SIZE));
    public static Vector2 TileToPoint((ushort, ushort) tilePos) => new(tilePos.Item1 * TILE_SIZE, tilePos.Item2 * TILE_SIZE);
  }

  public static class Engine {
    public static CollisionResult Overlap(Sprite spriteA, Sprite spriteB) => new(spriteA.Rect.IntersectsWith(spriteB.Rect), spriteA, spriteB);

    public static char CollisionDirection(Sprite sprite, Sprite target, char check) {
      RectangleF MainRect = sprite.Rect;
      RectangleF MainOldRect = sprite.OldRect;
      RectangleF TargetRect = target.Rect;
      RectangleF TargetOldRect = target.OldRect;

      if (check == 'H' || check == 'A') {
        if (MainRect.X <= TargetRect.Right && MainOldRect.X >= TargetOldRect.Right) return 'L';
        if (MainRect.Right >= TargetRect.X && MainOldRect.Right <= TargetOldRect.X) return 'R';
      } else if (check == 'V' || check == 'A') {
        if (MainRect.Y <= TargetRect.Bottom && MainOldRect.Y >= TargetOldRect.Bottom) return 'U';
        if (MainRect.Bottom >= TargetRect.Y && MainOldRect.Bottom <= TargetOldRect.Y) return 'D';
      }

      return 'N';
    }
  }


  public class Timer {
    private Action OnComplete;
    private uint Duration;
    private double StartTime;
    public bool Active = false;
    private bool Repeat;


    public Timer(uint milliseconds, Action onComplete = null, bool repeat = false) {
      this.OnComplete = onComplete;
      this.Duration = milliseconds;
      this.StartTime = 0;
      this.Repeat = repeat;
    }


    public void Activate() {
      this.StartTime = CurrentGameTime.TotalMilliseconds;
      this.Active = true;
    }

    private void Deactivate() {
      this.Active = false;

      if (this.Repeat)
        this.Activate();
    }


    public void Update() {
      if (!this.Active) return;


      if (CurrentGameTime.TotalMilliseconds - this.StartTime >= this.Duration) {
        this.OnComplete?.Invoke();
        this.Deactivate();
      }
    }
  }

}
