using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.Globals;
using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;


namespace Cube_Run_C_ {
  public static class Tools {
    public enum MouseButton : byte {
      Left,
      Right,
      Middle
    }


    public struct LevelStats {
      public ushort Deaths;
      public ushort Coins;
      public byte LifeBlocks;

      public LevelStats(ushort deaths, ushort coins, byte lifeBlocks) {
        this.Deaths = deaths;
        this.Coins = coins;
        this.LifeBlocks = lifeBlocks;
      }
    }

    public struct CollisionResult {
      public bool Collided;
      public Sprite SpriteA;
      public Sprite SpriteB;

      public CollisionResult(bool collided, Sprite spriteA, Sprite spriteB) {
        this.Collided = collided;
        this.SpriteA = spriteA;
        this.SpriteB = spriteB;
      }
    }

    public struct SpriteTransform {
      public SpriteEffects Effect;
      public float Rotation;


      public SpriteTransform(float rotation, SpriteEffects effect) {
        this.Effect = effect;
        this.Rotation = rotation;
      }


      public static readonly SpriteTransform Default = new(0f, SpriteEffects.None);
    }

    public struct Dimensions {
      public int Width;
      public int Height;


      public Dimensions(int w, int h) {
        this.Width = w;
        this.Height = h;
      }


      public static readonly Dimensions Zero = new(0, 0);
    }

    public struct DimensionsF {
      public float Width;
      public float Height;

      public DimensionsF(float w, float h) {
        this.Width = w;
        this.Height = h;
      }


      public static readonly DimensionsF Zero = new(0f, 0f);
    }

    public struct DisplayText {
      public Vector2 Position;
      public string Text;

      public DisplayText(string text, Vector2 position) {
        this.Position = position;
        this.Text = text;
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
      private static MouseState CurrentMouseState;
      private static MouseState PreviousMouseState;
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

      private static bool CheckMouseButton(MouseButton button, MouseState state, ButtonState checkState) {
        switch (button) {
          case MouseButton.Left:
            return state.LeftButton == checkState;
          case MouseButton.Middle:
            return state.RightButton == checkState;
          case MouseButton.Right:
            return state.MiddleButton == checkState;
        }

        return false;
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


      public static void Update() {
        InputManager.PreviousMouseState = InputManager.CurrentMouseState;
        InputManager.CurrentMouseState = Mouse.GetState();
        InputManager.PreviousState = InputManager.CurrentState;
        InputManager.CurrentState = Keyboard.GetState();
        InputManager.PreviousGamePadState = InputManager.CurrentGamePadState;
        InputManager.CurrentGamePadState = GamePad.GetState(PlayerIndex.One);
      }


      public static Point MousePosition() => InputManager.CurrentMouseState.Position;
      public static bool IsMouseButtonDown(MouseButton button) => InputManager.CheckMouseButton(button, InputManager.CurrentMouseState, ButtonState.Pressed);
      public static bool IsMouseButtonUp(MouseButton button) => InputManager.CheckMouseButton(button, InputManager.CurrentMouseState, ButtonState.Released);
      public static bool IsMouseButtonPressed(MouseButton button) => InputManager.CheckMouseButton(button, InputManager.CurrentMouseState, ButtonState.Pressed) && InputManager.CheckMouseButton(button, InputManager.PreviousMouseState, ButtonState.Released);
      public static bool IsMouseButtonReleased(MouseButton button) => InputManager.CheckMouseButton(button, InputManager.CurrentMouseState, ButtonState.Released) && InputManager.CheckMouseButton(button, InputManager.PreviousMouseState, ButtonState.Pressed);
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
      static private Dictionary<string, Texture2D> TextureCache = new();
      static public SpriteTransform[] DirectionRotations = [
        SpriteTransform.Default,
        new(0f, SpriteEffects.FlipHorizontally),
        new(MathHelper.PiOver2, SpriteEffects.None),
        new(MathHelper.PiOver2, SpriteEffects.None)
      ];
      static public GraphicsDevice GraphicsDevice;
      static public ContentManager Content;
      static public SpriteBatch SpriteBatch;


      public static Texture2D GetTexture(string path, bool cache = true) {
        if (!cache) {
          Texture2D Tex = Content.Load<Texture2D>(path);
          return Tex;
        }

        string CacheKey = Path.GetFileName(path);

        if (TextureCache.TryGetValue(CacheKey, out Texture2D Texture)) {
          return Texture;
        }

        Texture = Content.Load<Texture2D>(path);
        TextureCache[CacheKey] = Texture;
        return Texture;
      }


      public static void UnloadTexture(string path) {
        string CacheKey = Path.GetFileName(path);

        if (TextureCache.TryGetValue(CacheKey, out Texture2D Texture)) {
          Texture.Dispose();
          TextureCache.Remove(CacheKey);
        }
      }

      public static void UnloadAllTextures() {
        foreach (Texture2D Texture in TextureCache.Values) {
          Texture.Dispose();
        }

        TextureCache.Clear();
      }
    }

    public static class SaveSystem {
      public static List<LevelStats> LevelStats = new();
      private static string SaveFile = GetSavePath();
      private const byte SAVE_VERSION = 1;


      public static string Save() {
        using (FileStream Stream = File.Open(SaveFile, FileMode.Create))
        using (BinaryWriter Writer = new(Stream)) {
          Writer.Write(SAVE_VERSION);
          Writer.Write(PlayerData.Lives);
          Writer.Write(PlayerData.CurrentLevel);
          Writer.Write(LevelStats.Count);

          for (int Index = 0; Index < LevelStats.Count; Index++) {
            Writer.Write(LevelStats[Index].Deaths);
            Writer.Write(LevelStats[Index].Coins);
            Writer.Write(LevelStats[Index].LifeBlocks);
          }
        }

        return "Save successful.";
      }

      public static string Load() {
        if (!File.Exists(SaveFile))
          return "Load failed. Save file does not exist.";

        using (FileStream Stream = File.Open(SaveFile, FileMode.Open))
        using (BinaryReader Reader = new BinaryReader(Stream)) {
          int Version = Reader.ReadByte();

          if (Version > SAVE_VERSION)
            return "Load failed. Save file version is newer than game supports.";

          PlayerData.Lives = Reader.ReadUInt16();
          PlayerData.CurrentLevel = Reader.ReadByte();
          int LevelCount = Reader.ReadInt32();

          LevelStats.Clear();
          for (int Index = 0; Index < LevelCount; Index++) {
            ushort Deaths = Reader.ReadUInt16();
            ushort Coins = Reader.ReadUInt16();
            byte LifeBlocks = Reader.ReadByte();

            LevelStats[Index] = new(Deaths, Coins, LifeBlocks);
          }
        }

        return "Load successful.";
      }


      public static string GetSavePath() {
        string SaveDirectory = "";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
          SaveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CubeRun");
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
          SaveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", "CubeRun");
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
          SaveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".local", "share", "CubeRun");
        }

        if (!Directory.Exists(SaveDirectory)) {
          Directory.CreateDirectory(SaveDirectory);
        }

        return Path.Combine(SaveDirectory, "saveData.bin");
      }
    }

    public static class GameConverter {
      public static Point PointToTile(Vector2 position) => new((int)position.X / TILE_SIZE, (int)position.Y / TILE_SIZE);
      public static Vector2 TileToPoint(Point tilePos) => new(tilePos.X * TILE_SIZE, tilePos.Y * TILE_SIZE);
    }

    public static class Engine {
      public static CollisionResult Overlap(Sprite spriteA, Sprite spriteB) => new(spriteA.Rect.IntersectsWith(spriteB.Rect), spriteA, spriteB);


      public static bool AnyTrue(bool[] array) {
        for (uint Index = 0; Index < array.Length; Index++) {
          if (array[Index])
            return true;
        }

        return false;
      }

      public static bool OverlappingAny(RectangleF spriteRect, List<Groups> groups) {
        for (int Index = 0; Index < groups.Count; Index++) {
          if (SpriteGroups[(int)groups[Index]].OverlapsWith(spriteRect).Item2)
            return true;
        }

        return false;
      }

      public static char CollisionDirection(Sprite sprite, Sprite target, char check) {
        RectangleF MainRect = sprite.Rect;
        RectangleF MainOldRect = sprite.OldRect;
        RectangleF TargetRect = target.Rect;
        RectangleF TargetOldRect = target.OldRect;

        if (check == 'H' || check == 'A') {
          if (MainOldRect.Right <= TargetRect.X && MainRect.Right > TargetRect.X) return 'L';
          if (MainOldRect.X >= TargetRect.Right && MainRect.X < TargetRect.Right) return 'R';
        } else if (check == 'V' || check == 'A') {
          if (MainOldRect.Bottom <= TargetRect.Y && MainRect.Bottom > TargetRect.Y) return 'U';
          if (MainOldRect.Y >= TargetRect.Bottom && MainRect.Y < TargetRect.Bottom) return 'D';
        }

        return 'N';
      }

      public static char OppositeDirection(char direction) => direction switch {
        'L' => 'R',
        'R' => 'L',
        'U' => 'D',
        'D' => 'U',
        _ => ' '
      };

      public static Directions StringToDirection(char direction) => direction switch {
        'R' => Directions.Right,
        'U' => Directions.Up,
        'D' => Directions.Down,
        _ => Directions.Left
      };
    }

    public static class BitMask {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsSet(byte mask, byte index) => (mask & index) != 0;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void Set(ref byte mask, byte index, bool value) {
        if (value) {
          mask |= index;
        } else {
          mask &= (byte)~index;
        }
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void SetAll(ref byte mask, bool value) => mask = value ? (byte)0b11111111 : (byte)0b00000000;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool Any(byte mask) => (mask & 0b11111111) != 0;
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


  public static class RectangleExtensions {
    public static Vector2 TopLeft(this ref Rectangle rect) => new(rect.X, rect.Y);

    public static Rectangle Scaled(this Rectangle rect, float scale) => new((int)(rect.X * scale), (int)(rect.Y * scale), (int)(rect.Width * scale), (int)(rect.Height * scale));
  }

  public static class RectangleFExtensions {
    public static Vector2 TopLeft(this ref RectangleF rect) => new(rect.X, rect.Y);
    public static Vector2 Center(this ref RectangleF rect) => new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);

    public static void TopLeft(this ref RectangleF rect, Vector2 position) {
      rect.X = position.X;
      rect.Y = position.Y;
    }
  }

  public static class Texture2DExtensions {
    public static RectangleF GetRectangleF(this Texture2D texture, Vector2 position) => new(position.X, position.Y, texture.Width, texture.Height);
  }

  public static class Vector2Extensions {
    public static PointF ToPointF(this ref Vector2 vector) => new(vector.X, vector.Y);
  }

  public static class PointExtensions {
    public static PointF ToPointF(this Point point) => new(point.X, point.Y);
  }
}
