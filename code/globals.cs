using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


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

    public enum PlayerTimers : byte {
      RespawnInvincibility,
      Invincibility,
      AutoMove,
      Flying,
      Frozen,
      Goggles,
      Honey,
      Telescope,
      WallJump,
      WallJumpStun,
      DeathStun,
      SpringMove,
      ShieldKnockback
    }

    
    public static class PlayerData {
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


    public static Dictionary<string, SpriteGroup<Sprite>> SpriteGroups = new() {
      ["Damage"] = new(),
      ["Collidable"] = new(),
      ["Semi-Collidable"] = new(),
      ["Orb"] = new(),
      ["Teleporter"] = new(),
      ["Spring"] = new()
    };

    public static Dictionary<char, char> OppositeDirections = new() {
      ['L'] = 'R',
      ['R'] = 'L',
      ['U'] = 'D',
      ['D'] = 'U'
    };


    public static Dimensions MonitorDimensions;
    public static TimeSpan CurrentGameTime;
    public static Player Player;
    public static readonly Dimensions DEFAULT_DIMENSIONS = new(960, 720);
    public const byte TILE_SIZE = 96;
    public const byte CELL_SIZE = 192;
    public const byte ANIMATION_SPEED = 6;
  }
} 
