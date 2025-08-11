using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monitor = Microsoft.Xna.Framework.Graphics.GraphicsAdapter;


namespace Cube_Run_C_ {
  public static class Globals {
    public static class PlayerData {
      public static ushort Lives { get; set; } = 5;
      public static ushort Coins { get; set; } = 0;
      public static byte KeyCoins { get; set; } = 0;
      public static byte CurrentWorld { get; set; } = 0;
      public static byte CurrentLevel { get; set; } = 0;
    }

    public static class LevelData {
      public static ushort Gravity = 800;
    }


    public static Dictionary<string, SpriteGroup<Sprite>> spriteGroups = new() {
      ["All"] = null,
      ["Player"] = new(),
      ["Damage"] = new(),
      ["Collidable"] = new(),
      ["Semi-Collidable"] = new(),
      ["Orb"] = new(),
      ["Teleporter"] = new()
    };

    public enum Z_LAYERS : byte {
      Background,
      Background_Tiles,
      Path,
      Placeholders,
      Main,
      Player,
      Foreground,
      UI
    }


    public const int TILE_SIZE = 96;
    public const int ANIMATION_SPEED = 6;
    public static readonly Point DEFAULT_DIMENSIONS = new(960, 720);
    public static readonly Point MONITOR_DIMENSIONS = new(Monitor.DefaultAdapter.CurrentDisplayMode.Width, Monitor.DefaultAdapter.CurrentDisplayMode.Height);
    public static TimeSpan CurrentGameTime;
  }
} 