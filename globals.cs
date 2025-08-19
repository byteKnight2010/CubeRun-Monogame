using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Cube_Run_C_ {
  public static class Globals {
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
      ["All"] = new(),
      ["Player"] = new SpriteGroup<Sprite>(),
      ["Damage"] = new SpriteGroup<Sprite>(),
      ["Collidable"] = new SpriteGroup<Sprite>(),
      ["Semi-Collidable"] = new SpriteGroup<Sprite>(),
      ["Orb"] = new SpriteGroup<Sprite>(),
      ["Teleporter"] = new SpriteGroup<Sprite>()
    };


    public static (ushort, ushort) MonitorDimensions;
    public const byte TILE_SIZE = 96;
    public const byte CELL_SIZE = 192;
    public const byte ANIMATION_SPEED = 6;
    public static readonly (ushort, ushort) DEFAULT_DIMENSIONS = new(960, 720);
    public static GraphicsDevice Graphics;
    public static TimeSpan CurrentGameTime;
    public static float ScaleFactor = 1.0f;
  }
} 