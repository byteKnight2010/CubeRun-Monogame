using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Globals.PlayerData;
using static Cube_Run_C_.Sprites;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;


namespace Cube_Run_C_ {
  public static class Tools {
    public enum MouseButton : byte {
      Left,
      Right,
      Middle
    }


    public struct TileLayerProperty {
      public List<Groups> Groups;
      public ZLayers Z;


      public TileLayerProperty(ZLayers z, List<Groups> groups) {
        this.Z = z;
        this.Groups = groups;
      }
    }

    public struct BVector {
      public sbyte X;
      public sbyte Y;


      public BVector(sbyte x, sbyte y) {
        this.X = x;
        this.Y = y;
      }


      public readonly float Length => MathF.Sqrt(this.X * this.X + this.Y * this.Y);

      public void Normalize() {
        float Len = Length;

        if (Len == 0) {
          this.X = 0;
          this.Y = 0;
        }

        this.X = (sbyte)Math.Clamp((int)(this.X / Len * sbyte.MaxValue), sbyte.MinValue, sbyte.MaxValue);
        this.Y = (sbyte)Math.Clamp((int)(this.Y / Len * sbyte.MaxValue), sbyte.MinValue, sbyte.MaxValue);
      }

      public readonly bool Equals(Vector2 other) {
        if (X == other.X)
          return Y == other.Y;

        return false;
      }

      public override readonly bool Equals(object obj) {
        if (obj is Vector2 vector)
          return Equals(vector);

        return false;
      }


      public override readonly int GetHashCode() => HashCode.Combine(this.X, this.Y);


      public static bool operator ==(BVector vectorA, BVector vectorB) => vectorA.Equals(vectorB);
      public static bool operator !=(BVector vectorA, BVector vectorB) => !vectorB.Equals(vectorB);


      public static readonly BVector Zero = new(0, 0);
    }

    public struct GridPosition {
      public ushort X;
      public ushort Y;


      public GridPosition(ushort x, ushort y) {
        this.X = x;
        this.Y = y;
      }


      public static readonly GridPosition Zero = new(0, 0);
    }

    public struct Dimensions {
      public int Width;
      public int Height;


      public Dimensions(int w, int h) {
        this.Width = w;
        this.Height = h;
      }


      public void Halve() {
        this.Width >>= 1;
        this.Height >>= 1;
      }

      public Dimensions Half() => new(this.Width >> 1, this.Height >> 1);


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
      public Color Color;
      public string Text;

      public DisplayText(string text, Vector2 position, Color color) {
        this.Position = position;
        this.Color = color;
        this.Text = text;
      }


      public static readonly DisplayText Empty = new("", Vector2.Zero, Color.White);
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
      private static readonly Dictionary<GameAction, (Keys, Buttons)> Controls = new() {
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

    public static class GameConverter {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static Point PointToTile(Vector2 position) => new((int)position.X / TILE_SIZE, (int)position.Y / TILE_SIZE);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static Vector2 TileToPoint(Point tilePos) => new(tilePos.X * TILE_SIZE, tilePos.Y * TILE_SIZE);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static GridPosition PointToCell(Vector2 point) => new((ushort)Math.Max(0, (int)(point.X / CELL_SIZE)), (ushort)Math.Max(0, (int)(point.Y / CELL_SIZE)));
    }

    public static class Engine {
      private static readonly Dictionary<char, Directions> DirectionStrings = new() {
        ['L'] = Directions.Left,
        ['R'] = Directions.Right,
        ['U'] = Directions.Up,
        ['D'] = Directions.Down
      };
      private static readonly Dictionary<Directions, char> StringDirections = new() {
        [Directions.Left] = 'L',
        [Directions.Right] = 'R',
        [Directions.Up] = 'U',
        [Directions.Down] = 'D'
      };
      private static readonly Dictionary<Directions, BitMask.SemiCollidableFlags> DirectionFlags = new() {
        [Directions.Left] = BitMask.SemiCollidableFlags.Left,
        [Directions.Right] = BitMask.SemiCollidableFlags.Right,
        [Directions.Up] = BitMask.SemiCollidableFlags.Up,
        [Directions.Down] = BitMask.SemiCollidableFlags.Down
      };
      public static Random RNG = new();


      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static BitMask.SemiCollidableFlags DirectionToFlag(Directions d) => DirectionFlags[d];


      public static CollisionResult Overlap(Sprite spriteA, Sprite spriteB) => new(spriteA.Rect.IntersectsWith(spriteB.Rect), spriteA, spriteB);


      public static Fragment StringToTrim(string trim) => trim switch {
        "TopLeft" => Fragment.TopLeft,
        "TopRight" => Fragment.TopRight,
        "BottomLeft" => Fragment.BottomLeft,
        "BottomRight" => Fragment.BottomRight,
        "LeftHalf" => Fragment.LeftHalf,
        "RightHalf" => Fragment.RightHalf,
        "TopHalf" => Fragment.TopHalf,
        "BottomHalf" => Fragment.BottomHalf,
        _ => Fragment.None
      };

      public static PlayerPowers StringToPower(string power) => power switch {
        "Invinciblity" => PlayerPowers.Invincibility,
        "AutoMove" => PlayerPowers.AutoMove,
        "Flying" => PlayerPowers.Flying,
        "Frozen" => PlayerPowers.Frozen,
        "Goggles" => PlayerPowers.Goggles,
        "Honey" => PlayerPowers.Honey,
        "Sprint" => PlayerPowers.Sprint,
        "Telescope" => PlayerPowers.Telescope,
        _ => PlayerPowers.None
      };


      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static Directions CharToDirection(char c) => DirectionStrings[c];

      public static Directions CollisionDirection(Sprite sprite, Sprite target, Directions check) {
        RectangleF MainRect = sprite.Rect;
        RectangleF MainOldRect = sprite.OldRect;

        if (check == Directions.Horizontal || check == Directions.All) {
          if (MainOldRect.X > MainRect.X) 
            return Directions.Left;  
          if (MainOldRect.X < MainRect.X) 
            return Directions.Right; 
        }
        
        if (check == Directions.Vertical || check == Directions.All) {
          if (MainOldRect.Y > MainRect.Y) 
            return Directions.Up;   
          if (MainOldRect.Y < MainRect.Y) 
            return Directions.Down;  
        }

        return Directions.None;
      }


      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool Overlap(Vector2 position, Sprite sprite) => sprite.Rect.Contains(position.ToPointF());

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool Overlap(Sprite spriteA, RectangleF spriteBRect) => spriteA.Rect.IntersectsWith(spriteBRect);

      public static bool OverlappingAny(RectangleF spriteRect, List<Groups> groups) {
        for (int Index = 0; Index < groups.Count; Index++) {
          if (SpriteGroups[(int)groups[Index]].OverlapsWith(spriteRect) != null)
            return true;
        }

        return false;
      }


      public static bool InScreen(Vector2 position) => position.X >= 0 && position.X < Camera.ViewportDimensions.Width && position.Y >= 0 && position.Y < Camera.ViewportDimensions.Height;

      public static bool InScreen(Point position) => position.X >= 0 && position.X < Camera.ViewportDimensions.Width && position.Y >= 0 && position.Y < Camera.ViewportDimensions.Height;

      public static bool InScreen(RectangleF rect) {
        Vector2 TopLeftScreen = Camera.WorldToScreen(rect.TopLeft());
        Vector2 BottomRightScreen = Camera.WorldToScreen(new Vector2(rect.Right, rect.Bottom));
          
        return BottomRightScreen.X >= 0 && TopLeftScreen.X < Camera.ViewportDimensions.Width && BottomRightScreen.Y >= 0 && TopLeftScreen.Y < Camera.ViewportDimensions.Height;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsHorizontal(Directions direction) => direction == Directions.Left || direction == Directions.Right;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsHalf(Fragment fragment) => fragment == Fragment.LeftHalf || fragment == Fragment.RightHalf || fragment == Fragment.TopHalf || fragment == Fragment.BottomHalf;

      public static bool Contains(Directions[] directions, Directions direction) {
        for (int Index = 0; Index < directions.Length; Index++) {
          if (directions[Index] == direction) 
            return true;
        }

        return false;
      }


      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static byte ParseChar(char c) => (byte)(c - '0');


      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static float ToRadians(float degrees) => degrees * RADIAN_FACTOR;


      public static void HandleCollision(MovingSprite sprite, Sprite obstacle, Directions check) {
        switch (CollisionDirection(sprite, obstacle, check)) {
          case Directions.Left:
            sprite.Rect.X = obstacle.Rect.Right; 
            sprite.Direction.X = 0;
            break;
          case Directions.Right:
            sprite.Rect.X = obstacle.Rect.X - sprite.Rect.Width; 
            sprite.Direction.X = 0;
            break;
          case Directions.Up:
            sprite.Rect.Y = obstacle.Rect.Bottom;  
            sprite.Direction.Y = 0;
            break;
          case Directions.Down:
            sprite.Rect.Y = obstacle.Rect.Y - sprite.Rect.Height;  
            sprite.Direction.Y = 0;
            break;
        }
      }
    }

    public static class BitMask {
      [Flags]
      public enum DebugInfo : uint {
        FPS = 1 << 0,
        RAM = 1 << 1,
        CPU = 1 << 2,
        SessionTime = 1 << 3,
        DeltaTime = 1 << 4
      }


      [Flags]
      public enum GlobalFlags : ushort {
        Paused = 1 << 0,
        Fullscreen = 1 << 1,
        MouseSpriteVisible = 1 << 2,
        LetterBoxMode = 1 << 3,
        EnableMouse = 1 << 4,
        DisableMouse = 1 << 5,
        DisplayMouse = 1 << 6,
        UpdateLantern = 1 << 7,
        Exit = 1 << 8
      }

      [Flags]
      public enum TimeWarnFlags : byte {
        TimeWarning = 1 << 0,
        FirstWarning = 1 << 1,
        SecondWarning = 1 << 2,
        ThirdWarning = 1 << 3,
        SmallWarning = 1 << 4,
        LargeWarning = 1 << 5,
        WindowWarning = 1 << 6
      }


      [Flags]
      public enum LevelStatFlags : byte {
        Active = 1 << 0,
        Transitioning = 1 << 1
      }


      [Flags]
      public enum CameraStats : byte {
        UpdateSort = 1 << 0,
        UpdateScreenRect = 1 << 1,
        LockedPosition = 1 << 2,
        Zoomed = 1 << 3
      }


      [Flags]
      public enum TmxLoadScreenStats : byte {
        Active = 1 << 0,
        Loading = 1 << 1,
        Finished = 1 << 2
      }



      [Flags]
      public enum TitleScreenStats : byte {
        Active = 1 << 0,
        Play = 1 << 1,
        Settings = 1 << 2,
        Credits = 1 << 3
      }

      [Flags]
      public enum MenuStatus : uint {
        UpdateScreen = 1 << 0,
        UpdateBrightness = 1 << 1,
        UpdateFPS = 1 << 2,
        Fullscreen = 1 << 3,
        UpdateVolume = 1 << 4,
        UpdateSFX = 1 << 5,
        UpdateMusic = 1 << 6,
        Mute = 1 << 7,
        Display = 1 << 8,
        Audio = 1 << 9,
        Controls = 1 << 10,
        Back = 1 << 11,
        Quit = 1 << 12,
        DraggingSlider = 1 << 13,
        Active = 1 << 14,
        LetterBox = 1 << 15,
        None = 1 << 16,
      }

      [Flags]
      public enum SaveWindowStats : byte {
        Active = 1 << 0,
        SaveLevel = 1 << 1,
        SaveSettings = 1 << 2,
        Advance = 1 << 3
      }

      [Flags]
      public enum ExitWindowStats: byte {
        Active = 1 << 0,
        ConfirmedExit = 1 << 1,
        DisableMouse = 1 << 2
      }


      [Flags]
      public enum EndLevelScreenStats : uint {
        Displaying = 1 << 0,
        HoveringDeaths = 1 << 1,
        HoveringCoins = 1 << 2,
        HoveringLifeBlocks = 1 << 3,
        CanAdvance = 1 << 4,
      }

      [Flags]
      public enum SliderFlags : byte {
        Dragging = 1 << 0,
        Selected = 1 << 1
      }

      [Flags]
      public enum ButtonFlags : byte {
        Selected = 1 << 0,
        Clicking = 1 << 1
      }


      [Flags]
      public enum ObjectStats : ushort {
        Active = 1 << 0,
        Multi = 1 << 1,
        Floor = 1 << 2,
        Passthrough = 1 << 3,
        Horizontal = 1 << 4,
        Automatic = 1 << 5,
        LimitedRange = 1 << 6,
        Deep = 1 << 7,
        Respawn = 1 << 8
      }


      [Flags]
      public enum AnimationFlags : byte {
        Playing = 1 << 0,
        Reset = 1 << 1,
        Loop = 1 << 2,
      }

      [Flags]
      public enum SpriteEffectFlags : byte {
        Ghost = 1 << 0
      }

      [Flags]
      public enum SpriteGroupProperties : byte {
        SpritesMoved = 1 << 0,
        UseQuery = 1 << 1,
        GridDirty = 1 << 2
      }

      [Flags]
      public enum FallingSpikeFlags : byte {
        Regrow = 1 << 0,
        Automatic = 1 << 1,
        LimitedRange = 1 << 2,
        Regrowing = 1 << 3,
        Falling = 1 << 4,
        Unbranched = 1 << 5,
        Destroyed = 1 << 6,
      }
      
      [Flags]
      public enum BulletFlags : byte {
        Passthrough = 1 << 0,
        WallInvincibility = 1 << 1
      }

      [Flags]
      public enum PowerSpriteFlags {
        Destroyed = 1 << 0,
        Respawn = 1 << 1,
        Canceller = 1 << 2
      }

      [Flags]
      public enum SpringFlags {
        Active = 1 << 0,
        Horizontal = 1 << 1,
        Multi = 1 << 2,
        Retracting = 1 << 3
      }

      [Flags]
      public enum SemiCollidableFlags : byte {
        Left = 1 << 4,
        Right = 1 << 5,
        Up = 1 << 6,
        Down = 1 << 7
      }



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
      public static bool IsSet(ushort mask, ushort index) => (mask & index) != 0;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void Set(ref ushort mask, ushort index, bool value) {
        if (value) {
          mask |= index;
        } else {
          mask &= (ushort)~index;
        }
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void SetAll(ref ushort mask, bool value) => mask = value ? (ushort)0xFFFF : (ushort)0x0000;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void Flip(ref ushort mask, ushort index) => mask ^= index;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool Any(ushort mask) => (mask & 0xFFFF) != 0;


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


      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsSet(ulong mask, ulong index) => (mask & index) != 0;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void Set(ref ulong mask, ulong index, bool value) {
        if (value) {
          mask |= index;
        } else {
          mask &= (ulong)~index;
        }
      }
    
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void SetAll(ref ulong mask, bool value) => mask = value ? 0xFFFFFFFFFFFFFFFF : 0x0000000000000000;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void Flip(ref ulong mask, ulong index) => mask ^= index;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool Any(ulong mask) => (mask & 0xFFFFFFFFFFFFFFFF) != 0;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool Any(ulong mask, ulong start, ulong end) => (mask & (((1ul << (int)(end - start + 1)) - 1ul) << (int)start)) != 0;
    }


    public class Timer {
      private readonly Action OnComplete;
      private readonly uint Duration;
      private double StartTime;
      public bool Active = false;
      private readonly bool Repeat;


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

      public void Deactivate() {
        this.OnComplete?.Invoke();
        this.Active = false;

        if (this.Repeat)
          this.Activate();
      }


      public void Update() {
        if (!this.Active) 
          return;

        if (CurrentGameTime.TotalMilliseconds - this.StartTime >= this.Duration)
          this.Deactivate();
      }
    }
  }


  public static class ViewportExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 ToVector2(this Viewport viewport) => new(viewport.X, viewport.Y);
  }

  public static class RectangleExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 TopLeft(this Rectangle rect) => new(rect.X, rect.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 FCenter(this Rectangle rect) => new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 VectorDimensions(this Rectangle rect) => new(rect.Width, rect.Height);

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rectangle Scaled(this Rectangle rect, float scale) => new((int)Math.Round(rect.X * scale), (int)Math.Round(rect.Y * scale), (int)Math.Round(rect.Width * scale), (int)Math.Round(rect.Height * scale));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectangleF ToRectangleF(this Rectangle rect) => new(rect.X, rect.Y, rect.Width, rect.Height);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TopLeft(this ref Rectangle rect, Vector2 position) {
      rect.X = (int)position.X;
      rect.Y = (int)position.Y;
    }
  
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TopLeft(this ref Rectangle rect, Point position) {
      rect.X = position.X;
      rect.Y = position.Y;
    }
  }

  public static class RectangleFExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 TopLeft(this RectangleF rect) => new(rect.X, rect.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 BottomRight(this RectangleF rect) => new(rect.X + rect.Width, rect.Y + rect.Height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Center(this RectangleF rect) => new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TopLeft(this ref RectangleF rect, Vector2 position) {
      rect.X = position.X;
      rect.Y = position.Y;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TopLeft(this ref RectangleF rect, Point position) {
      rect.X = position.X;
      rect.Y = position.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Center(this ref RectangleF rect, Vector2 position) {
      rect.X = position.X - rect.Width * 0.5f;
      rect.Y = position.Y - rect.Height * 0.5f;
    }
  }

  public static class Texture2DExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectangleF GetRectangleF(this Texture2D texture, Vector2 position) => new(position.X, position.Y, texture.Width, texture.Height);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rectangle GetRectangle(this Texture2D texture, Vector2 position) => new((int)position.X, (int)position.Y, texture.Width, texture.Height);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tools.Dimensions GetDimensions(this Texture2D texture) => new(texture.Width, texture.Height);
  }

  public static class Vector2Extensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointF ToPointF(this Vector2 vector) => new(vector.X, vector.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tools.Dimensions ToDimensions(this Vector2 vector) => new((int)vector.X, (int)vector.Y);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 FastNormalize(in Vector2 vector) {
      float InverseLength = 1.0f / MathF.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
      return new(vector.X * InverseLength, vector.Y * InverseLength);
    }
  }

  public static class PointExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointF ToPointF(this Point point) => new(point.X, point.Y);
  }
}
