using System;
using static Cube_Run_C_.Tools;


namespace Cube_Run_C_ {
  public static class Globals {
    public enum ZLayers : byte {
      Background,
      Background_Tiles,
      Path,
      Placeholders,
      Main,
      Player,
      Foreground,
      UI
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
      public enum PlayerSurfaces : byte {
        Left = 1 << 0,
        Right = 1 << 1,
        Top = 1 << 2,
        Bottom = 1 << 3,
        StickingCeiling = 1 << 4
      }

      [Flags]
      public enum PlayerStats : byte {
        ReturnedMovement = 1 << 0,
        ReturnMovement = 1 << 1,
        FallDamageEnabled = 1 << 2,
        FallDamageCondition = 1 << 3,
        Shielding = 1 << 4,
        CanJump = 1 << 5,
        HorizontalMovement = 1 << 6,
        VerticalMovement = 1 << 7
      }

      [Flags]
      public enum PlayerMovers : byte {
        Ladder = 1 << 0,
        Water = 2 << 1,
        DeepWater = 3 << 2,
        ThickWater = 4 << 3,
        Quicksand = 5 << 4,
        DeepQuicksand = 6 << 5
      }
      
      [Flags]
      public enum PlayerPowers : byte {
        Invincibility = 1 << 0,
        AutoMove = 1 << 1,
        Flying = 1 << 2,
        Frozen = 1 << 3,
        Goggles = 1 << 4,
        Honey = 1 << 5,
        Sprint = 1 << 6,
        Telescope = 1 << 7
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


      public static ushort Lives { get; set; } = 5;
      public static ushort Coins { get; set; } = 0;
      public static byte KeyCoins { get; set; } = 0;
      public static byte CurrentWorld { get; set; } = 1;
      public static byte CurrentLevel { get; set; } = 1;
    }

    public static class LevelData {
      public static ushort Gravity = 800;
      public static (Dimensions, Dimensions) Dimensions = (new(0, 0), new(0, 0));
    }


    public static SpriteGroup<Sprite>[] SpriteGroups = [new(), new(), new(), new(), new(), new(), new(), new(), new()];


    public static Dimensions MonitorDimensions;
    public static TimeSpan CurrentGameTime;
    public static Player Player;
    public static readonly Dimensions DEFAULT_DIMENSIONS = new(1280, 720);
    public static readonly Dimensions MINIMUM_DIMENSIONS = new(160, 90);
    public static readonly float SCREEN_RATIO = 16f / 9f;
    public const byte TILE_SIZE = 96;
    public const byte CELL_SIZE = 192;
    public const byte ANIMATION_SPEED = 6;
  }
} 
