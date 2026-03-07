using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static Cube_Run_C_.ConfigManager;
using static Cube_Run_C_.PlatformerPlayer;
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
      Activator = 9,
      Changer = 10,
      Quicksand = 11,
      Water = 12,
      All = 13
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
      if (CurrentWorld % 8 == 0) {
        return 7;
      } else {
        return 5;
      }
    }


    public static List<Sprite> DestructibleSprites = [];
    public static readonly SpriteGroup<Sprite>[] SpriteGroups = new SpriteGroup<Sprite>[13];
    public static readonly BVector[] DirectionVectors = [new(-1, 0), new(1, 0), new(0, -1), new(0, 1)];
    public static readonly Directions[] OppositeDirections = [Directions.Right, Directions.Left, Directions.Down, Directions.Up];

    public static Dimensions DEFAULT_DIMENSIONS => new(Graphics.DefaultDimensions.Width, Graphics.DefaultDimensions.Height);
    public static Dimensions MINIMUM_DIMENSIONS => new(Graphics.MinimumWidth, Graphics.MinimumHeight);
    public static float[] FPS_BOUNDS => [Graphics.MinFPS, Graphics.MaxFPS];

    public static Player Player;
    public static readonly Vector2 TILE_VECTOR = new(Gameplay.TileSize, Gameplay.TileSize);
    public static readonly Vector2 IMAGE_VECTOR = new(Graphics.BaseImageSize);
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

    private static float LLW = Gameplay.DefaultLanternWidth;
    public static ushort Fps = Graphics.TargetFPS;
    public static ushort GlobalStats = 0x00;
  }
} 