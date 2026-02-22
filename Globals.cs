using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static Cube_Run_C_.ConfigManager;
using static Cube_Run_C_.Sprites;
using static Cube_Run_C_.Tools;
using static Cube_Run_C_.Tools.BitMask;


namespace Cube_Run_C_ {
  public static class Globals {
    public enum ZLayers : byte {
      Background = 0,
      BackgroundTiles = 1,
      Path = 2,
      Placeholders = 3,
      Items = 3,
      Main = 4,
      Player = 5,
      Opaque = 6,
      Foreground = 7,
    }

    public enum Groups : byte {
      Damage = 0,
      Item = 1,
      Collidable = 2,
      SemiCollidable = 3,
      Orb = 4,
      Teleporter = 5,
      Ladder = 6,
      Spring = 7,
      Switch = 8,
      Changer = 9,
      Quicksand = 10,
      Water = 11,
      All = 12
    }

    public enum Directions : byte {
      Left = 0,
      Right = 1,
      Up = 2,
      Down = 3,
      Horizontal = 4,
      Vertical = 5,
      Center = 6,
      All = 7,
      None = 8
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
      public enum PlayerStats : ulong {
        Animating = 1ul << 0,
        ReturnMovement = 1ul << 1,
        FallDamageEnabled = 1ul << 2,
        FallDamageCondition = 1ul << 3,
        Shielding = 1ul << 4,
        CanJump = 1ul << 5,
        HorizontalMovement = 1ul << 6,
        VerticalMovement = 1ul << 7,
        Left = 1ul << 8,
        Right = 1ul << 9,
        Top = 1ul << 10,
        Bottom = 1ul << 11,
        StickingCeiling = 1ul << 12,
        Ladder = 1ul << 13,
        Water = 1ul << 14,
        DeepWater = 1ul << 15,
        ThickWater = 1ul << 16,
        Quicksand = 1ul << 17,
        QuicksandDeep = 1ul << 18,
        CheckpointOne = 1ul << 19,
        CheckpointTwo = 1ul << 20,
        CheckpointThree = 1ul << 21,
        LanternEnabled = 1ul << 22,
        Invincibility = 1ul << 23,
        AutoMove = 1ul << 24,
        Flying = 1ul << 25,
        Frozen = 1ul << 26,
        Goggles = 1ul << 27,
        Honey = 1ul << 28,
        Sprint = 1ul << 29,
        Telescope = 1ul << 30,
        Normal = 1ul << 31,
        Small = 1ul << 32,
        Large = 1ul << 33,
        OnWall = 1ul << 34       
      }


      public enum PlayerPowers : byte {
        Invincibility = 0,
        AutoMove = 1,
        Flying = 2,
        Frozen = 3,
        Goggles = 4,
        Honey = 5,
        Sprint = 6,
        Telescope = 7,
        Canceller = 8,
        All = 9,
        None = 255
      }


      public enum PlayerTimers : byte {
        RespawnStatus,
        Invincibility,
        AutoMove,
        Flying,
        Frozen,
        Goggles,
        Honey,
        Sprint,
        Telescope,
        WallJump,
        WallJumpStun,
        SpringMove,
        ShieldKnockback
      }


      public static uint[] PowerDurations = [5000, 5000, 5000, 5000, 50, 50, 50, 50];
      public static ushort Lives = 5;
      public static ushort Coins = 0;
      public static byte KeyCoins = 0;
      public static byte CurrentWorld = 1;
      public static byte CurrentLevel = 1;
    }

    public static class LevelData {
      public static List<Vector2> FSBlockPositions = [];
      public static LevelDimensions Dimensions = LevelDimensions.Zero;
      public static string Difficulty = "Base";

      public static uint FallingSpikeRegrow = Gameplay.DefaultFallingSpikeRegrow;
      public static ushort Gravity = ConfigManager.Player.Gravity;
      public static ushort EnemySpeed = Gameplay.DefaultEnemySpeed;
      public static ushort BulletSpeed = Gameplay.DefaultBulletSpeed;
      public static ushort FallingSpikeSpeed = Gameplay.DefaultFallingSpikeSpeed;
      public static float LanternWidth = Gameplay.DefaultLanternWidth;
      public static float CanonFiringRate = Gameplay.DefaultCanonFiringRate;
    }


    public static byte WorldToLevels() {
      if (PlayerData.CurrentWorld % 8 == 0) {
        return 7;
      } else {
        return 5;
      }
    }


    public static List<Sprite> DestructibleSprites = [];
    public static readonly SpriteGroup<Sprite>[] SpriteGroups = [new(), new(), new(), new(), new(), new(), new(), new(), new(), new(), new(), new()];
    public static readonly BVector[] DirectionVectors = [new(-1, 0), new(1, 0), new(0, -1), new(0, 1)];
    public static readonly Directions[] OppositeDirections = [Directions.Right, Directions.Left, Directions.Down, Directions.Up];

    public static float RADIAN_FACTOR => Gameplay.RadianFactor;
    public static float SCREEN_RATIO => Graphics.ScreenRatio;
    public static byte TILE_SIZE => Gameplay.TileSize;
    public static byte CELL_SIZE => Gameplay.CellSize;
    public static byte ANIMATION_SPEED => Gameplay.AnimationSpeed;

    public static Dimensions DEFAULT_DIMENSIONS => new(Graphics.DefaultDimensions.Width, Graphics.DefaultDimensions.Height);
    public static Dimensions MINIMUM_DIMENSIONS => new(Graphics.MinimumWidth, Graphics.MinimumHeight);
    public static float[] FPS_BOUNDS => [Graphics.MinFPS, Graphics.MaxFPS];

    public static Player Player;
    public static readonly Vector2 TILE_VECTOR = new(TILE_SIZE, TILE_SIZE);
    public static Rectangle ScreenRect;
    public static Dimensions MonitorDimensions;
    public static TimeSpan CurrentGameTime;
    public static Point StoredMousePosition = Point.Zero;
    public static Color FillColor = Color.Black;

    public static float LanternLightWidth {
      get => LLW; 
      set {
        if (LLW != value) {
          Set(ref GlobalStats, (ushort)GlobalFlags.UpdateLantern, true);
          LLW = value;
        }
      }
    }

    public static readonly float GHOST_ALPHA = Graphics.GhostAlpha;
    private static float LLW = Gameplay.DefaultLanternWidth;
    public static ushort Fps = Graphics.TargetFPS;
    public static ushort GlobalStats = 0x00;
  }
} 