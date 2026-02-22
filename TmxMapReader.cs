using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Sprites;
using static Cube_Run_C_.Tools.BitMask;
using static Cube_Run_C_.Tools.Engine;


namespace Cube_Run_C_ {
  public class TmxMap {
    public List<TmxTileset> Tilesets = [];
    public List<TmxLayer> Layers = [];
    public List<TmxObjectGroup> ObjectGroups = [];
    public LevelConfig Configuration;
    public int Width;
    public int Height;
    public int TileWidth;
    public int TileHeight;
  }

  public class TmxTileset {
    public Dictionary<int, TmxTile> Tiles = new();
    public string ImageSource;
    public string Name;
    public int FirstGid;
    public int TileWidth;
    public int TileHeight;
    public int TileCount;
    public int Columns;
    public int ImageWidth;
    public int ImageHeight;
  }

  public class TmxTile {
    public Dictionary<string, string> Properties = new();
    public string ImageSource;
  }

  public class TmxLayer {
    public Dictionary<string, string> Properties = new();
    public int[] Data;
    public string Name;
    public float Opacity;
    public int Width;
    public int Height;
    public bool Visible;
  }

  public class TmxObjectGroup {
    public List<TmxObject> Objects = [];
    public string Name;
    public bool Visible;
  }

  public class TmxObject {
    public Dictionary<string, string> Properties = new();

    public Directions Orientation = Directions.Right;
    public Fragment Trim = Fragment.None;
    public PlayerData.PlayerPowers Power = PlayerData.PlayerPowers.None;
    public uint DurationChange = 0;
    public int ID = -1;
    public ushort SpeedChange = 0;
    public ushort Stats = 0x0000;
    public float RateChange = 0.0f;

    public string Name;
    public string Type;
    public float X;
    public float Y;
    public float Width;
    public float Height;
    public int? Gid;
    public int Id;
  }


  public class TmxMapReader : ContentTypeReader<TmxMap> {
    protected override TmxMap Read(ContentReader input, TmxMap existingInstance) {
      TmxMap Map = existingInstance ?? new();

      Map.Width = input.ReadInt32();
      Map.Height = input.ReadInt32();
      Map.TileWidth = input.ReadInt32();
      Map.TileHeight = input.ReadInt32();

      int TileSetCount = input.ReadInt32();
      Map.Tilesets.Clear();
      for (int Index = 0; Index < TileSetCount; Index++) {
        Map.Tilesets.Add(ReadTileset(input));
      }

      int LayerCount = input.ReadInt32();
      Map.Layers.Clear();
      for (int Index = 0; Index < LayerCount; Index++) {
        Map.Layers.Add(ReadLayer(input));
      }

      int ObjectGroupCount = input.ReadInt32();
      Map.ObjectGroups.Clear();
      for (int Index = 0; Index < ObjectGroupCount; Index++) {
        Map.ObjectGroups.Add(ReadObjectGroup(input));
      }

      Map.Configuration = new(
        [input.ReadUInt32(), input.ReadUInt32(), input.ReadUInt32(), input.ReadUInt32(), input.ReadUInt32(), input.ReadUInt32(), input.ReadUInt32(), input.ReadUInt32()],
        input.ReadUInt32(),
        input.ReadUInt16(),
        input.ReadUInt16(),
        input.ReadUInt16(),
        input.ReadUInt16(),
        input.ReadSingle(),
        input.ReadSingle()
      );

      return Map;
    }

    private static TmxTileset ReadTileset(ContentReader input) {
      TmxTileset Tileset = new() {
        FirstGid = input.ReadInt32(),
        Name = input.ReadString(),
        TileWidth = input.ReadInt32(),
        TileHeight = input.ReadInt32(),
        TileCount = input.ReadInt32(),
        Columns = input.ReadInt32(),
        ImageSource = input.ReadString(),
        ImageWidth = input.ReadInt32(),
        ImageHeight = input.ReadInt32()
      };

      int TileCount = input.ReadInt32();
      for (int Index = 0; Index < TileCount; Index++) {
        int TileID = input.ReadInt32();
        TmxTile Tile = new() {
          ImageSource = input.ReadString()
        };

        int PropCount = input.ReadInt32();
        for (int Accessor = 0; Accessor < PropCount; Accessor++) {
          string Key = input.ReadString();
          string Value = input.ReadString();
          Tile.Properties[Key] = Value;
        }

        Tileset.Tiles[TileID] = Tile;
      }

      return Tileset;
    }

    private static TmxLayer ReadLayer(ContentReader input) {
      TmxLayer Layer = new() {
        Name = input.ReadString(),
        Width = input.ReadInt32(),
        Height = input.ReadInt32(),
        Visible = input.ReadBoolean(),
        Opacity = input.ReadSingle()
      };

      int DataLength = input.ReadInt32();
      Layer.Data = new int[DataLength];
      for (int Index = 0; Index < DataLength; Index++) {
        Layer.Data[Index] = input.ReadInt32();
      }

      int PropCount = input.ReadInt32();
      for (int Index = 0; Index < PropCount; Index++) {
        string Key = input.ReadString();
        string Value = input.ReadString();
        Layer.Properties[Key] = Value;
      }
      
      return Layer;
    }

    private static TmxObjectGroup ReadObjectGroup(ContentReader input) {
      TmxObjectGroup ObjectGroup = new() {
        Name = input.ReadString(),
        Visible = input.ReadBoolean()
      };

      int ObjectCount = input.ReadInt32();
      for (int Index = 0; Index < ObjectCount; Index++) {
        ObjectGroup.Objects.Add(ReadObject(input));
      }

      return ObjectGroup;
    }

    private static TmxObject ReadObject(ContentReader input) {
      TmxObject Object = new() {
        Id = input.ReadInt32(),
        Name = input.ReadString(),
        Type = input.ReadString(),
        X = input.ReadSingle(),
        Y = input.ReadSingle(),
        Width = input.ReadSingle(),
        Height = input.ReadSingle()
      };
      string Key = string.Empty;
      string Value = string.Empty;

      if (input.ReadBoolean())
        Object.Gid = input.ReadInt32();

      int PropCount = input.ReadInt32();
      Set(ref Object.Stats, (byte)ObjectStats.Active, true);

      for (int Index = 0; Index < PropCount; Index++) {
        Key = input.ReadString();
        Value = input.ReadString();
        switch (Key) {
          case "Orientation":
            Object.Orientation = CharToDirection(Value[0]);
            break;
          case "Trim":
            Object.Trim = StringToTrim(Value);
            break;
          case "Power": 
            Object.Power = StringToPower(Value);
            break;
          case "Active":
            Set(ref Object.Stats, (ushort)ObjectStats.Active, bool.Parse(Value));
            break;
          case "ID":
            Object.ID = int.Parse(Value);
            break;
          case "Multi":
            Set(ref Object.Stats, (ushort)ObjectStats.Multi, bool.Parse(Value));
            break;
          case "Floor":
            Set(ref Object.Stats, (ushort)ObjectStats.Floor, bool.Parse(Value));
            break;
          case "Passthrough":
            Set(ref Object.Stats, (ushort)ObjectStats.Passthrough, bool.Parse(Value));
            break;
          case "Horizontal":
            Set(ref Object.Stats, (ushort)ObjectStats.Horizontal, bool.Parse(Value));
            break;
          case "Automatic":
            Set(ref Object.Stats, (ushort)ObjectStats.Automatic, bool.Parse(Value));
            break;
          case "LimitedRange":
            Set(ref Object.Stats, (ushort)ObjectStats.LimitedRange, bool.Parse(Value));
            break;
          case "Deep":
            Set(ref Object.Stats, (ushort)ObjectStats.Deep, bool.Parse(Value));
            break;
          case "Respawn":
            Set(ref Object.Stats, (ushort)ObjectStats.Respawn, bool.Parse(Value));
            break;
          case "RateChange":
          case "WidthChange":
            Object.RateChange = float.Parse(Value);
            break;
          case "SpeedChange":
            Object.SpeedChange = ushort.Parse(Value);
            break;
          case "DurationChange":
            Object.DurationChange = uint.Parse(Value);
            break;
          default:
            Object.Properties[Key] = Value;
          break;
        }
      }

      return Object;
    }
  }
}