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

    [Flags]
    public enum SpriteGroupProperties : byte {
      SpritesMoved = 1 << 0,
      UseQuery = 1 << 1,
      GridDirty = 1 << 2
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

    public struct IVector2 {
      public int X;
      public int Y;


      public IVector2(int x, int y) {
        this.X = x;
        this.Y = y;
      }


      public static readonly IVector2 Zero = new(0, 0);
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


      public static readonly DisplayText Empty = new("", Vector2.Zero);
    }

    public struct Circle {
      public Vector2 Center;
      public float Radius;


      public Circle(Vector2 center, float radius) {
        this.Center = center;
        this.Radius = radius;
      }


      public static readonly Circle Zero = new(Vector2.Zero, 0f);
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
        Keys[] PressedKeys = CurrentState.GetPressedKeys();

        if (PressedKeys.Length > 0) {
          Controls[action] = (PressedKeys[0], Controls[action].Item2);
          return true;
        } else if (CurrentGamePadState.IsConnected) {
          Buttons? PressedButton = GetPressedButton();

          if (PressedButton != null && IsButtonPressed(PressedButton.Value)) {
            Controls[action] = (Controls[action].Item1, PressedButton.Value);
            return true;
          }
        }

        return false;
      }

      public static bool CheckAction(GameAction action, bool hold) {
        (Keys, Buttons) ActionInputs = Controls[action];

        if (hold) {
          return IsKeyPressed(ActionInputs.Item1) || IsButtonPressed(ActionInputs.Item2);
        } else {
          return IsKeyDown(ActionInputs.Item1) || IsButtonDown(ActionInputs.Item2);
        }
      }

      private static bool CheckMouseButton(MouseButton button, MouseState state, ButtonState checkState) {
        return button switch {
          MouseButton.Left => state.LeftButton == checkState,
          MouseButton.Middle => state.RightButton == checkState,
          MouseButton.Right => state.MiddleButton == checkState,
          _ => false,
        };
      }


      private static Buttons? GetPressedButton() {
        GamePadState State = CurrentGamePadState;

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
        PreviousMouseState = CurrentMouseState;
        CurrentMouseState = Mouse.GetState();
        PreviousState = CurrentState;
        CurrentState = Keyboard.GetState();
        PreviousGamePadState = CurrentGamePadState;
        CurrentGamePadState = GamePad.GetState(PlayerIndex.One);
      }


      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static Point MousePosition() => CurrentMouseState.Position;
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsMouseButtonDown(MouseButton button) => CheckMouseButton(button, CurrentMouseState, ButtonState.Pressed);
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsMouseButtonUp(MouseButton button) => CheckMouseButton(button, CurrentMouseState, ButtonState.Released);
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsMouseButtonPressed(MouseButton button) => CheckMouseButton(button, CurrentMouseState, ButtonState.Pressed) && CheckMouseButton(button, PreviousMouseState, ButtonState.Released);
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsMouseButtonReleased(MouseButton button) => CheckMouseButton(button, CurrentMouseState, ButtonState.Released) && CheckMouseButton(button, PreviousMouseState, ButtonState.Pressed);
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsKeyDown(Keys key) => CurrentState.IsKeyDown(key);
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsKeyUp(Keys key) => CurrentState.IsKeyUp(key);
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsKeyPressed(Keys key) => CurrentState.IsKeyDown(key) && PreviousState.IsKeyUp(key);
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsKeyReleased(Keys key) => CurrentState.IsKeyUp(key) && PreviousState.IsKeyDown(key);
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsGamePadConnected() => CurrentGamePadState.IsConnected;
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsButtonDown(Buttons button) => CurrentGamePadState.IsButtonDown(button);
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsButtonUp(Buttons button) => CurrentGamePadState.IsButtonUp(button);
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsButtonPressed(Buttons button) => CurrentGamePadState.IsButtonDown(button) && PreviousGamePadState.IsButtonUp(button);
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsButtonReleased(Buttons button) => CurrentGamePadState.IsButtonUp(button) && PreviousGamePadState.IsButtonDown(button);
    }

    public static class Assets {
      static private Dictionary<string, Texture2D> TextureCache = new();
      static readonly public SpriteTransform[] DirectionRotations = [
        SpriteTransform.Default,
        new(0f, SpriteEffects.FlipHorizontally),
        new(-MathHelper.PiOver2, SpriteEffects.None),
        new(MathHelper.PiOver2, SpriteEffects.FlipVertically)
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

      public static void UnloadAllTextures() => TextureCache.Clear();
    }

    public static class SaveSystem {
      public static List<LevelStats> LevelStats = new();
      private static string SaveFile = GetSavePath();
      private const byte SAVE_VERSION = 1;


      public static string Save() {
        using FileStream Stream = File.Open(SaveFile, FileMode.Create);
        using BinaryWriter Writer = new(Stream);
        Writer.Write(SAVE_VERSION);
        Writer.Write(PlayerData.Lives);
        Writer.Write(PlayerData.CurrentLevel);
        Writer.Write(LevelStats.Count);

        for (int Index = 0; Index < LevelStats.Count; Index++) {
          Writer.Write(LevelStats[Index].Deaths);
          Writer.Write(LevelStats[Index].Coins);
          Writer.Write(LevelStats[Index].LifeBlocks);
        }

        return "Save successful.";
      }

      public static string Load() {
        if (!File.Exists(SaveFile))
          return "Load failed. Save file does not exist.";

        using FileStream Stream = File.Open(SaveFile, FileMode.Open);
        using BinaryReader Reader = new BinaryReader(Stream);
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
      public static Random RNG = new();


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
          if (SpriteGroups[(int)groups[Index]].OverlapsWith(spriteRect) != null)
            return true;
        }

        return false;
      }

      public static bool InScreen(Vector2 position) => position.X >= 0 && position.X < Camera.ScreenDimensions.Width && position.Y >= 0 && position.Y < Camera.ScreenDimensions.Height;

      public static char CollisionDirection(Sprite sprite, Sprite target, char check) {
        RectangleF MainRect = sprite.Rect;
        RectangleF MainOldRect = sprite.OldRect;
        RectangleF TargetRect = target.Rect;

        if (check == 'H' || check == 'A') {
          if (MainOldRect.Right <= TargetRect.X && MainRect.Right > TargetRect.X) return 'R';
          if (MainOldRect.X >= TargetRect.Right && MainRect.X < TargetRect.Right) return 'L';
        } else if (check == 'V' || check == 'A') {
          if (MainOldRect.Bottom <= TargetRect.Y && MainRect.Bottom > TargetRect.Y) return 'D';
          if (MainOldRect.Y >= TargetRect.Bottom && MainRect.Y < TargetRect.Bottom) return 'U';
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

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static float ToRadians(float degrees) => degrees * RADIAN_FACTOR;

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
      public static void SetAll(ref byte mask, bool value) => mask = value ? (byte)0xFF : (byte)0x00;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void Flip(ref byte mask, byte index) => mask ^= index;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool Any(byte mask) => (mask & 0xFF) != 0;


      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsSet(uint mask, uint index) => (mask & index) != 0;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void Set(ref uint mask, uint index, bool value) {
        if (value) {
          mask |= index;
        } else {
          mask &= (uint)~index;
        }
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void SetAll(ref uint mask, bool value) => mask = value ? 0xFFFFFFFF : 0x00000000;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void Flip(ref uint mask, uint index) => mask ^= index;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool Any(uint mask) => (mask & 0xFFFFFFFF) != 0;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool Any(uint mask, uint start, uint end) => (mask & (((1u << (int)(end - start + 1)) - 1u) << (int)start)) != 0;
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
    public static Vector2 FCenter(this ref Rectangle rect) => new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);

    public static Rectangle Scaled(this Rectangle rect, float scale) => new((int)Math.Round(rect.X * scale), (int)Math.Round(rect.Y * scale), (int)Math.Round(rect.Width * scale), (int)Math.Round(rect.Height * scale));
  }

  public static class RectangleFExtensions {
    public static Vector2 TopLeft(this ref RectangleF rect) => new(rect.X, rect.Y);
    public static Vector2 Center(this ref RectangleF rect) => new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);


    public static void TopLeft(this ref RectangleF rect, Vector2 position) {
      rect.X = position.X;
      rect.Y = position.Y;
    }

    public static void Center(this ref RectangleF rect, Vector2 position) {
      rect.X = position.X - rect.Width * 0.5f;
      rect.Y = position.Y - rect.Height * 0.5f;
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
