using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.ConfigManager;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.PlatformerPlayer;
using static Cube_Run_C_.Sprites;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;


namespace Cube_Run_C_ {
  public static class Tools {
    public enum MouseButton : byte {
      Left = 0,
      Right = 1,
      Middle = 2
    }


    /// <summary>
    /// Defines rendering and grouping metadata for a tile layer.
    /// </summary>
    public readonly struct TileLayerProperty {
      /// <summary>
      /// The sprite groups this tile layer is associated with.
      /// </summary>
      public readonly List<Groups> Groups;
      /// <summary>
      /// The Z-layer depth used for render ordering.
      /// </summary>
      public readonly ZLayers Z;

      /// <summary>
      /// Creates a tile layer property with a specified Z-layer and group collection.
      /// </summary>
      /// <param name="z"> The rendering depth layer. </param>
      /// <param name="groups"> The sprite groups assigned to this layer. </param>
      public TileLayerProperty(ZLayers z, List<Groups> groups) {
        this.Z = z;
        this.Groups = groups;
      }
    }

    /// <summary>
    /// Compact 2D vector using signed bytes for memory-efficient directional storage.
    /// </summary>
    public record struct BVector {
      /// <summary>
      /// Horizontal component (-128 to 127).
      /// </summary>
      public sbyte X;
      /// <summary>
      /// Vertical component (-128 to 127).
      /// </summary>
      public sbyte Y;


      /// <summary>
      /// Creates a new byte-based vector.
      /// </summary>
      public BVector(sbyte x, sbyte y) {
        this.X = x;
        this.Y = y;
      }


      /// <summary>
      /// Gets the Euclidean length of the vector.
      /// </summary>
      public readonly float Length => MathF.Sqrt(this.X * this.X + this.Y * this.Y);


      /// <summary>
      /// A zero vector (0, 0).
      /// </summary>
      public static readonly BVector Zero = new(0, 0);
    }

    /// <summary>
    /// Creates a new grid position.
    /// </summary>
    public struct GridPosition : IEquatable<GridPosition> {
      /// <summary>
      /// Horizontal grid coordinate.
      /// </summary>
      public ushort X;
      /// <summary>
      /// Vertical grid coordinate.
      /// </summary>
      public ushort Y;


      /// <summary>
      /// Creates a new grid position.
      /// </summary>
      public GridPosition(ushort x, ushort y) { 
        this.X = x; 
        this.Y = y; 
      }


      /// <summary>
      /// Compares two Grid Positions to check for equality. 
      /// </summary>
      /// <param name="other"> Grid to Compare </param>
      /// <returns> True if equivalent Grid Positions. False if Grid Positions differ. </returns>
      public readonly bool Equals(GridPosition other) => this.X == other.X && this.Y == other.Y;
      /// <summary>
      /// Compares Grid Position to Object for equality. 
      /// </summary>
      /// <param name="obj"> Object to Compare </param>
      /// <returns> True if equivalent Grid Positions. False if Grid Positions differ. </returns>
      public override readonly bool Equals(object obj) => obj is GridPosition Grid && Equals(Grid);
      /// <summary>
      /// Returns the hash code for this Grid Position.
      /// </summary>
      /// <returns> A 32-bit signed integer hash code. </returns>
      public override readonly int GetHashCode() => (X << 16) | Y;


      /// <summary>
      /// The origin grid position (0, 0).
      /// </summary>
      public static readonly GridPosition Zero = new(0, 0);
    }

    /// <summary>
    /// Represents integer width and height dimensions.
    /// </summary>
    public struct Dimensions {
      /// <summary>
      /// Width in pixels or units.
      /// </summary>
      public int Width;
      /// <summary>
      /// Height in pixels or units.
      /// </summary>
      public int Height;


      /// <summary>
      /// Creates a new dimensions structure.
      /// </summary>
      public Dimensions(int w, int h) {
        this.Width = w;
        this.Height = h;
      }

        
      /// <summary>
      /// Halves the current dimensions in-place.
      /// </summary>
      public void Halve() {
        this.Width >>= 1;
        this.Height >>= 1;
    }

      /// <summary>
      /// Returns a new Dimensions instance representing half the current size.
      /// </summary>
      public readonly Dimensions Half() => new(this.Width >> 1, this.Height >> 1);


      /// <summary>
      /// Zero dimensions (0, 0).
      /// </summary>
      public static readonly Dimensions Zero = new(0, 0);
    }

    /// <summary>
    /// Represents floating-point width and height dimensions.
    /// </summary>
    public struct DimensionsF {
      /// <summary>
      /// Width component.
      /// </summary>
      public float Width;
      /// <summary>
      /// Height component.
      /// </summary>
      public float Height;


      /// <summary>
      /// Creates a floating-point dimensions structure.
      /// </summary>
      public DimensionsF(float w, float h) {
        this.Width = w;
        this.Height = h;
      }


      /// <summary>
      /// Zero floating-point dimensions (0f, 0f).
      /// </summary>
      public static readonly DimensionsF Zero = new(0f, 0f);
    }

    /// <summary>
    /// Represents text to be rendered on screen with position and color data.
    /// </summary>
    public record struct DisplayText {
      /// <summary>
      /// Screen-space position of the text.
      /// </summary>
      public Vector2 Position;
      /// <summary>
      /// Color tint applied to the text.
      /// </summary>
      public Color Color;
      /// <summary>
      /// The string content to display.
      /// </summary>
      public string Text;


      /// <summary>
      /// Creates a display text instance.
      /// </summary>
      public DisplayText(string text, Vector2 position, Color color) {
        this.Position = position;
        this.Color = color;
        this.Text = text;
      }


      /// <summary>
      /// An empty display text instance.
      /// </summary>
      public static readonly DisplayText Empty = new(string.Empty, Vector2.Zero, Color.White);
    }

    /// <summary>
    /// Represents a 2D circle defined by a center point and radius.
    /// </summary>
    public struct Circle {
      /// <summary>
      /// Center point of the circle.
      /// </summary>
      public Vector2 Center;
      /// <summary>
      /// Radius of the circle.
      /// </summary>
      public float Radius;


      /// <summary>
      /// Creates a circle with a specified center and radius.
      /// </summary>
      public Circle(Vector2 center, float radius) {
        this.Center = center;
        this.Radius = radius;
      }


      /// <summary>
      /// A zero-radius circle at the origin.
      /// </summary>
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
      public static Point PointToTile(Vector2 position) => new((int)position.X / Gameplay.TileSize, (int)position.Y / Gameplay.TileSize);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static Vector2 TileToPoint(Point tilePos) => new(tilePos.X * Gameplay.TileSize, tilePos.Y * Gameplay.TileSize);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static GridPosition PointToCell(Vector2 point) => new((ushort)Math.Max(0, (int)(point.X / Gameplay.CellSize)), (ushort)Math.Max(0, (int)(point.Y / Gameplay.CellSize)));
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
        RectangleF TargetRect = target.Rect;
        RectangleF TargetOldRect = target.OldRect;

        if (check == Directions.Horizontal || check == Directions.All) {
          if (MainRect.X <= TargetRect.Right && MainOldRect.X >= TargetOldRect.Right)
            return Directions.Left;
          if (TargetRect.X <= MainRect.Right && TargetOldRect.X >= MainOldRect.Right)
            return Directions.Right;
        }

        if (check == Directions.Vertical || check == Directions.All) {
          if (MainRect.Y <= TargetRect.Bottom && MainOldRect.Y >= TargetOldRect.Bottom)
            return Directions.Up;
          if (MainRect.Bottom >= TargetRect.Y && MainOldRect.Bottom <= TargetOldRect.Y)
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
      public static float ToRadians(float degrees) => degrees * Gameplay.RadianFactor;


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
      // =========================================================
      //  DEBUG / ENGINE
      // =========================================================

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
        EnableMouse = 1 << 3,
        DisableMouse = 1 << 4,
        DisplayMouse = 1 << 5,
        UpdateLantern = 1 << 6,
        Exit = 1 << 7,
        ForceExit = 1 << 8
      }

      // =========================================================
      //  CAMERA
      // =========================================================

      [Flags]
      public enum CameraFlags : byte {
        UpdateSort = 1 << 0,
        UpdateScreenRect = 1 << 1,
        LockedPosition = 1 << 2,
        Zoomed = 1 << 3
      }

      // =========================================================
      //  SCREENS / STATES
      // =========================================================

      [Flags]
      public enum TitleScreenFlags : byte {
        Active = 1 << 0,
        Play = 1 << 1,
        Settings = 1 << 2,
        Credits = 1 << 3
      }

      [Flags]
      public enum TmxLoadScreenFlags : byte {
        Active = 1 << 0,
        Loading = 1 << 1,
        Finished = 1 << 2
      }

      [Flags]
      public enum PlatformerLevelFlags : byte {
        Active = 1 << 0,
        Transitioning = 1 << 1
      }

      [Flags]
      public enum PlatformerEndFlags : uint {
        Displaying = 1 << 0,
        HoveringDeaths = 1 << 1,
        HoveringCoins = 1 << 2,
        HoveringLifeBlocks = 1 << 3,
        CanAdvance = 1 << 4
      }

      // =========================================================
      //  UI
      // =========================================================

      [Flags]
      public enum MenuFlags : uint {
        Active = 1 << 14,
        None = 1 << 15,

        // Sections
        Display = 1 << 8,
        Audio = 1 << 9,
        Controls = 1 << 10,

        // Actions
        UpdateScreen = 1 << 0,
        UpdateBrightness = 1 << 1,
        UpdateFPS = 1 << 2,
        Fullscreen = 1 << 3,
        UpdateVolume = 1 << 4,
        UpdateSFX = 1 << 5,
        UpdateMusic = 1 << 6,
        Mute = 1 << 7,

        Back = 1 << 11,
        Quit = 1 << 12,
        DraggingSlider = 1 << 13
      }

      [Flags]
      public enum SaveWindowFlags : byte {
        Active = 1 << 0,
        SaveLevel = 1 << 1,
        SaveSettings = 1 << 2,
        Advance = 1 << 3
      }

      [Flags]
      public enum ExitWindowFlags : byte {
        Active = 1 << 0,
        ConfirmedExit = 1 << 1,
        DisableMouse = 1 << 2
      }

      [Flags]
      public enum ButtonFlags : byte {
        Selected = 1 << 0,
        Clicking = 1 << 1
      }

      [Flags]
      public enum SliderFlags : byte {
        Dragging = 1 << 0,
        Selected = 1 << 1
      }

      // =========================================================
      //  PLAYER
      // =========================================================

      [Flags]
      public enum PlayerDisplayFlags : byte {
        Active = 1 << 0,
        DisplayHearts = 1 << 1,
        DisplayCoins = 1 << 2,
        DisplayKeyCoins = 1 << 3
      }

      [Flags]
      public enum TimeWarningFlags : byte {
        TimeWarning = 1 << 0,
        FirstWarning = 1 << 1,
        SecondWarning = 1 << 2,
        ThirdWarning = 1 << 3,
        SmallWarning = 1 << 4,
        LargeWarning = 1 << 5,
        WindowWarning = 1 << 6
      }

      // =========================================================
      //  OBJECTS / GAMEPLAY
      // =========================================================

      [Flags]
      public enum ObjectFlags : ushort {
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
      public enum FallingSpikeFlags : byte {
        Regrow = 1 << 0,
        Automatic = 1 << 1,
        LimitedRange = 1 << 2,
        Regrowing = 1 << 3,
        Falling = 1 << 4,
        Unbranched = 1 << 5,
        Destroyed = 1 << 6
      }

      [Flags]
      public enum SpringFlags : byte {
        Active = 1 << 0,
        Horizontal = 1 << 1,
        Multi = 1 << 2,
        Retracting = 1 << 3
      }

      [Flags]
      public enum BulletFlags : byte {
        Passthrough = 1 << 0,
        WallInvincibility = 1 << 1
      }

      [Flags]
      public enum PowerSpriteFlags : byte {
        Destroyed = 1 << 0,
        Respawn = 1 << 1,
        Canceller = 1 << 2
      }

      [Flags]
      public enum SemiCollidableFlags : byte {
        Left = 1 << 4,
        Right = 1 << 5,
        Up = 1 << 6,
        Down = 1 << 7
      }

      // =========================================================
      //  RENDERING / ANIMATION
      // =========================================================

      [Flags]
      public enum AnimationFlags : byte {
        Playing = 1 << 0,
        Reset = 1 << 1,
        Loop = 1 << 2
      }

      [Flags]
      public enum SpriteEffectFlags : byte {
        Ghost = 1 << 0
      }

      [Flags]
      public enum SpriteGroupFlags : byte {
        SpritesMoved = 1 << 0,
        UseQuery = 1 << 1,
        GridDirty = 1 << 2
      }

      // =========================================================
      //  GENERIC BIT OPERATIONS
      // =========================================================


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


  public static class EffectExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetVariable(this Effect effect, string name, Vector3 value) => effect.Parameters[name].SetValue(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetVariable(this Effect effect, string name, Vector2 value) => effect.Parameters[name].SetValue(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetVariable(this Effect effect, string name, float value) => effect.Parameters[name].SetValue(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetVariable(this Effect effect, string name, bool value) => effect.Parameters[name].SetValue(value);
  }

  public static class Texture2DExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectangleF GetRectangleF(this Texture2D texture, Vector2 position) => new(position.X, position.Y, texture.Width, texture.Height);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rectangle GetRectangle(this Texture2D texture, Vector2 position) => new((int)position.X, (int)position.Y, texture.Width, texture.Height);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tools.Dimensions GetDimensions(this Texture2D texture) => new(texture.Width, texture.Height);
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