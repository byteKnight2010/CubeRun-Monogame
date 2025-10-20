using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Cube_Run_C_.Tools;


namespace Cube_Run_C_ {
  public static class Globals {
    [Flags]
    public enum GlobalFlags : byte {
      Paused = 1 << 0,
      Fullscreen = 1 << 1,
      MouseSpriteVisible = 1 << 2,
      LevelActive = 1 << 3
    }


    public enum ZLayers : byte {
      Background,
      BackgroundTiles,
      Path,
      Placeholders,
      Main,
      Player,
      Opaque,
      Foreground,
    }

    public enum Groups : byte {
      Damage,
      Item,
      Collidable,
      SemiCollidable,
      Orb,
      Teleporter,
      Ladder,
      Spring,
      Switch,
      All
    }

    public enum Directions : byte {
      Left,
      Right,
      Up,
      Down
    }

    public enum GameAction : byte {
      MoveUp,
      MoveDown,
      MoveLeft,
      MoveRight,
      Jump,
      Sprint,
      Shield
    }

    
    
    public static class PlayerData {
      [Flags]
      public enum PlayerStats : uint {
        ReturnedMovement = 1 << 0,
        ReturnMovement = 1 << 1,
        FallDamageEnabled = 1 << 2,
        FallDamageCondition = 1 << 3,
        Shielding = 1 << 4,
        CanJump = 1 << 5,
        HorizontalMovement = 1 << 6,
        VerticalMovement = 1 << 7,
        Left = 1 << 8,
        Right = 1 << 9,
        Top = 1 << 10,
        Bottom = 1 << 11,
        StickingCeiling = 1 << 12,
        Ladder = 1 << 13,
        Water = 1 << 14,
        DeepWater = 1 << 15,
        ThickWater = 1 << 16,
        Quicksand = 1 << 17,
        DeepQuicksand = 1 << 18,
        Invincibility = 1 << 19,
        AutoMove = 1 << 20,
        Flying = 1 << 21,
        Frozen = 1 << 22,
        Goggles = 1 << 23,
        Honey = 1 << 24,
        Sprint = 1 << 25,
        Telescope = 1 << 26,
        CheckpointOne = 1 << 27,
        CheckpointTwo = 1 << 28,
        CheckpointThree = 1 << 29,
        LanternEnabled = 1 << 30
      }


      public enum PlayerTimers : byte {
        RespawnStatus,
        Invincibility,
        AutoMove,
        Flying,
        Frozen,
        Goggles,
        Honey,
        Telescope,
        WallJump,
        WallJumpStun,
        SpringMove,
        ShieldKnockback
      }


      public static ushort Lives = 5;
      public static ushort Coins = 0;
      public static byte KeyCoins = 0;
      public static byte CurrentWorld = 1;
      public static byte CurrentLevel = 16;
    }

    public static class LevelData {
      public static ushort Gravity = 800;
      public static ushort EnemySpeed = 90;
      public static (Dimensions, Dimensions) Dimensions = (new(0, 0), new(0, 0));
    }


    public static SpriteGroup<Sprite>[] SpriteGroups = [new(), new(), new(), new(), new(), new(), new(), new(), new()];


    public static Effect BrightnessEffect;
    public static Player Player;
    public static Rectangle ScreenRect;
    public static Dimensions MonitorDimensions;
    public static TimeSpan CurrentGameTime;
    public static float LanternLightWidth = 100.0f;
    public static byte GlobalStats = 0b00000000;
    public static readonly Dimensions DEFAULT_DIMENSIONS = new(1280, 720);
    public static readonly Dimensions MINIMUM_DIMENSIONS = new(160, 90);
    public static readonly float RADIAN_FACTOR = MathHelper.Pi / 180;
    public static readonly float SCREEN_RATIO = 16f / 9f;
    public const byte TILE_SIZE = 96;
    public const byte CELL_SIZE = 192;
    public const byte ANIMATION_SPEED = 6;
  }
} 
