using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using static Cube_Run_C_.Tools;


namespace Cube_Run_C_ {
  public enum GameType : byte {
    Platformer = 0,
    PlatformerUI = 1
  }


  public class GameConfig {
    public GraphicsConfig Graphics = new();
    public UIConfig UI = new();
    public GameplayConfig Gameplay = new();
    public SpatialConstants Spatial = new();
    public AudioConfig Audio = new();
    public PathsConfig Paths = new();
    public TimeConfig Times = new();
  }

  public readonly struct BinHeader {
    public readonly long Offset;
    public readonly int Hash;
    public readonly int Size;


    public BinHeader(long offset, int hash, int size) {
      this.Hash = hash;
      this.Offset = offset;
      this.Size = size;
    }


    public static readonly BinHeader Empty = new(0, 0, 0);
  }

  public readonly struct BinDictHeader {
    public readonly long Offset;
    public readonly int Size;


    public BinDictHeader(long offset, int size) {
      this.Offset = offset;
      this.Size = size;
    }


    public static readonly BinDictHeader Empty = new(0, 0);
  }

  

  public class GraphicsConfig {
    public Dimensions DefaultDimensions = new(1280, 720);
    public int MinimumWidth = 160;
    public int MinimumHeight = 90;
    public ushort TargetFPS = 144;
    public ushort MinFPS = 30;
    public ushort MaxFPS = 360;
    public float ScreenRatio = 4f / 3f;
    public float BrightnessMax = 2.0f;
    public float GhostAlpha = 0.4f;
  }

  public class UIConfig {
    public float LoadingDotInterval = 0.25f;
  }

  
  public class GameplayConfig {
    public PlayerConfig Player = new();

    public byte TileSize = 96;
    public byte CellSize = 192;
    public byte AnimationSpeed = 6;
    public float RadianFactor = 0.017453292f;
    public float Hundredth = 0.01f;
    
    public byte GoalFragmentMax = 4;
    public ushort DefaultEnemySpeed = 90;
    public ushort DefaultBulletSpeed = 150;
    public ushort DefaultFallingSpikeSpeed = 250;
    public ushort DefaultFallingSpikeRegrow = 5000;
    public float DefaultCanonFiringRate = 5.0f;
    public float DefaultLanternWidth = 90.0f;

    public float QuicksandDrag = 15f;
    public float QuicksandDeepDrag = 20f;
  }
  
  public class PlayerConfig {
    public float TerminalVelocity = 900.0f;
    public float WallJumpDirectionFactor = 1.5f;
    public float QuicksandSpeedFactor = 0.1f;
    public float QuicksandDeepSpeedFactor = 0.075555f;
    public float QuicksandJumpFactor = 0.5f;
    public float SpringEffect = 1125f;
    public float AlternativeSpringEffect = 1237.5f;
    public ushort Gravity = 800;
    public ushort JumpHeight = 600;
    public ushort Speed = 450;
    public byte PowerBitOffset = 23;
    public byte PowerBitEnd = 30;
    public byte ContactBitOffset = 8;
  }


  public class SpatialConstants {
    public byte MAX_Z_LAYERS = 8;
    public byte SPRITE_GROUP_QUERY_THRESHOLD = 100;
    public byte DEFAULT_CELL_CAPACITY = 4;
    public byte CELL_LIST_POOL_SIZE = 100;
    public byte BUCKET_SIZE_PIXELS = 96;

  }
  

  public class Bestiary {
    public Dictionary<string, BestiaryEntry> Entries = new() {
      ["Brick"] = new("brk", "descer"),
      ["Start Orb"] = BestiaryEntry.Default,
      ["End Orb"] = BestiaryEntry.Default,
      ["Start Tile"] = BestiaryEntry.Default,
      ["Spike"] = BestiaryEntry.Default,
      ["Teleport Portal"] = BestiaryEntry.Default,
      ["Life Block"] = BestiaryEntry.Default,
    };
  }

  public readonly struct BestiaryEntry {
    public readonly string ID;
    public readonly string Description;


    public BestiaryEntry(string id, string description) {
      this.ID = id;
      this.Description = description;
    }


    public static readonly BestiaryEntry Default = new("[Empty]", "[Empty]");
  }
  

  public class Credits {
    public string Art = "Jaden Perez";
    public string Music = "Jaden Perez";
    public string Story = "Roger Ramirez";
    public string Code = "Roger Ramirez";
    public string LevelDesign = "Roger Ramirez";
  }


  public class AudioConfig {
    public float DefaultMasterVolume = 1.0f;
    public float DefaultSFXVolume = 1.0f;
    public float DefaultMusicVolume = 1.0f;
    public float AudioMax = 1.0f;
  }
  

  public class PathsConfig {
    public string FallbackFontPath = "Fonts/Fallback";
    public string FallbackTexturePath = "Images/FallbackTexture";
    public string EffectPath = "Effects/Brightness";
  }
  
  public class TimeConfig {
    public double[] TimeWarnings = [60, 180, 300];
  }



  public static class ConfigManager {
    public static readonly string SaveDirectory = GetConfigPath();
    private static readonly string ConfigJSONName = Path.Combine(SaveDirectory, "GameConfig.json");
    private static readonly string ConfigFileName = Path.Combine(SaveDirectory, "GameConfig.bin");
    private static readonly string CreditJSONName = Path.Combine(SaveDirectory, "Credits.json");
    private static FileStream Stream;
    public static GameConfig Current { get; private set; } = new();
    public static Credits Credits { get; private set; } = new();
    private const int CONFIG_MAGIC = 0x4958456C;
    private const int CONFIG_VERSION = 1;


    public static void Load() {
      try {
        if (File.Exists(ConfigJSONName)) {
          string Json = File.ReadAllText(ConfigJSONName);
          Current = JsonConvert.DeserializeObject<GameConfig>(Json) ?? new();
        } else {
          Console.WriteLine($"No CONFIG file found. Creating default at {ConfigJSONName}");
          Current = new();
          Save();
        }
      } catch (Exception Exception) {
        Console.WriteLine($"[WARNING] Failed to load CONFIG: {Exception.Message}");
        Console.WriteLine("[WARNING] Using default configuration.");
        Current = new();
      }
    }

    public static void LoadBin() {
      Current = new();

      try {
        if (!File.Exists(ConfigFileName)) {
          Console.WriteLine("[ERROR]: Failed to load ConfigBIN. No ConfigBIN file found.");
          return;
        }

        Stream = File.Open(ConfigFileName, FileMode.Open, FileAccess.Read);
        using BinaryReader Reader = new(Stream);

        int Magic = Reader.ReadInt32();
        if (Magic != CONFIG_MAGIC) {
          Console.WriteLine("[ERROR]: Invalid Config File Format.");
          return;
        }

        int Version = Reader.ReadInt32();
        if (Version != CONFIG_VERSION) {
          Console.WriteLine("[ERROR]: Invalid Config File Version.");
          return;
        }

        Current.Graphics.DefaultDimensions = new(Reader.ReadInt32(), Reader.ReadInt32());
        Current.Graphics.MinimumWidth = Reader.ReadInt32();
        Current.Graphics.MinimumHeight = Reader.ReadInt32();
        Current.Graphics.TargetFPS = Reader.ReadUInt16();
        Current.Graphics.MinFPS = Reader.ReadUInt16();
        Current.Graphics.MaxFPS = Reader.ReadUInt16();
        Current.Graphics.ScreenRatio = Reader.ReadSingle();
        Current.Graphics.BrightnessMax = Reader.ReadSingle();
        Current.Graphics.GhostAlpha = Reader.ReadSingle();
        Current.UI.LoadingDotInterval = Reader.ReadSingle();
        Current.Gameplay.Player.TerminalVelocity = Reader.ReadSingle();
        Current.Gameplay.Player.WallJumpDirectionFactor = Reader.ReadSingle();
        Current.Gameplay.Player.QuicksandSpeedFactor = Reader.ReadSingle();
        Current.Gameplay.Player.QuicksandDeepSpeedFactor = Reader.ReadSingle();
        Current.Gameplay.Player.QuicksandJumpFactor = Reader.ReadSingle();
        Current.Gameplay.Player.SpringEffect = Reader.ReadSingle();
        Current.Gameplay.Player.AlternativeSpringEffect = Reader.ReadSingle();
        Current.Gameplay.Player.Gravity = Reader.ReadUInt16();
        Current.Gameplay.Player.JumpHeight = Reader.ReadUInt16();
        Current.Gameplay.Player.Speed = Reader.ReadUInt16();
        Current.Gameplay.Player.PowerBitOffset = Reader.ReadByte();
        Current.Gameplay.Player.ContactBitOffset = Reader.ReadByte();
        Current.Gameplay.TileSize = Reader.ReadByte();
        Current.Gameplay.CellSize = Reader.ReadByte();
        Current.Gameplay.AnimationSpeed = Reader.ReadByte();
        Current.Gameplay.RadianFactor = Reader.ReadSingle();
        Current.Gameplay.Hundredth = Reader.ReadSingle();
        Current.Gameplay.GoalFragmentMax = Reader.ReadByte();
        Current.Gameplay.DefaultEnemySpeed = Reader.ReadUInt16();
        Current.Gameplay.DefaultBulletSpeed = Reader.ReadUInt16();
        Current.Gameplay.DefaultFallingSpikeSpeed = Reader.ReadUInt16();
        Current.Gameplay.DefaultFallingSpikeRegrow = Reader.ReadUInt16();
        Current.Gameplay.DefaultCanonFiringRate = Reader.ReadSingle();
        Current.Gameplay.DefaultLanternWidth = Reader.ReadSingle();
        Current.Gameplay.QuicksandDrag = Reader.ReadSingle();
        Current.Gameplay.QuicksandDeepDrag = Reader.ReadSingle();
        Current.Spatial.MAX_Z_LAYERS = Reader.ReadByte();
        Current.Spatial.SPRITE_GROUP_QUERY_THRESHOLD = Reader.ReadByte();
        Current.Spatial.DEFAULT_CELL_CAPACITY = Reader.ReadByte();
        Current.Spatial.CELL_LIST_POOL_SIZE = Reader.ReadByte();
        Current.Spatial.BUCKET_SIZE_PIXELS = Reader.ReadByte();
        Current.Audio.DefaultMasterVolume = Reader.ReadSingle();
        Current.Audio.DefaultSFXVolume = Reader.ReadSingle();
        Current.Audio.DefaultMusicVolume = Reader.ReadSingle();
        Current.Audio.AudioMax = Reader.ReadSingle();
        Current.Paths.FallbackFontPath = Reader.ReadString();
        Current.Paths.FallbackTexturePath = Reader.ReadString();
        Current.Paths.EffectPath = Reader.ReadString();
        Current.Times.TimeWarnings[0] = Reader.ReadDouble();
        Current.Times.TimeWarnings[1] = Reader.ReadDouble();
        Current.Times.TimeWarnings[2] = Reader.ReadDouble();

        CloseStream();
      } catch (Exception Exception) {
        Console.WriteLine($"[ERROR]: Failed to read ConfigBIN: {Exception.Message} Generating default config.");
      }
    }

    public static void LoadCredits() {
      try {
        if (File.Exists(CreditJSONName)) {
          string Json = File.ReadAllText(CreditJSONName);
          Credits = JsonConvert.DeserializeObject<Credits>(Json) ?? new();
        } else {
          Console.WriteLine($"[ERROR]: Failed to load Credit JSON. File does not exist.");
        }
      } catch (Exception Exception) {
        Console.WriteLine($"[WARNING]: Failed to load Credit JSON: {Exception.Message}");
        Console.WriteLine("[WARNING]: Using default Credits.");
      }
    }

   
    public static void Save() {
      try {
        File.WriteAllText(ConfigJSONName, JsonConvert.SerializeObject(Current, Formatting.Indented));
      } catch (Exception Exception) {
        Console.WriteLine($"[ERROR]: Failed to save CONFIG: {Exception.Message}");
      }
    }

    public static void SaveAsBin() {
      try {
        if (Current == null) {
          Console.WriteLine("[ERROR]: Failed to create ConfigBIN. ConfigJSON not loaded.");
          return;
        }

        Stream = File.Open(ConfigFileName, FileMode.Create, FileAccess.Write);
        using BinaryWriter Writer = new(Stream);

        Writer.Write(CONFIG_MAGIC);
        Writer.Write(CONFIG_VERSION);
        
        Writer.Write(Current.Graphics.DefaultDimensions.Width);
        Writer.Write(Current.Graphics.DefaultDimensions.Height);
        Writer.Write(Current.Graphics.MinimumWidth);
        Writer.Write(Current.Graphics.MinimumHeight);
        Writer.Write(Current.Graphics.TargetFPS);
        Writer.Write(Current.Graphics.MinFPS);
        Writer.Write(Current.Graphics.MaxFPS);
        Writer.Write(Current.Graphics.ScreenRatio);
        Writer.Write(Current.Graphics.BrightnessMax);
        Writer.Write(Current.Graphics.GhostAlpha);
        Writer.Write(Current.UI.LoadingDotInterval);
        Writer.Write(Current.Gameplay.Player.TerminalVelocity);
        Writer.Write(Current.Gameplay.Player.WallJumpDirectionFactor);
        Writer.Write(Current.Gameplay.Player.QuicksandSpeedFactor);
        Writer.Write(Current.Gameplay.Player.QuicksandDeepSpeedFactor);
        Writer.Write(Current.Gameplay.Player.QuicksandJumpFactor);
        Writer.Write(Current.Gameplay.Player.SpringEffect);
        Writer.Write(Current.Gameplay.Player.AlternativeSpringEffect);
        Writer.Write(Current.Gameplay.Player.Gravity);
        Writer.Write(Current.Gameplay.Player.JumpHeight);
        Writer.Write(Current.Gameplay.Player.Speed);
        Writer.Write(Current.Gameplay.Player.PowerBitOffset);
        Writer.Write(Current.Gameplay.Player.ContactBitOffset);
        Writer.Write(Current.Gameplay.TileSize);
        Writer.Write(Current.Gameplay.CellSize);
        Writer.Write(Current.Gameplay.AnimationSpeed);
        Writer.Write(Current.Gameplay.RadianFactor);
        Writer.Write(Current.Gameplay.Hundredth);
        Writer.Write(Current.Gameplay.GoalFragmentMax);
        Writer.Write(Current.Gameplay.DefaultEnemySpeed);
        Writer.Write(Current.Gameplay.DefaultBulletSpeed);
        Writer.Write(Current.Gameplay.DefaultFallingSpikeSpeed);
        Writer.Write(Current.Gameplay.DefaultFallingSpikeRegrow);
        Writer.Write(Current.Gameplay.DefaultCanonFiringRate);
        Writer.Write(Current.Gameplay.DefaultLanternWidth);
        Writer.Write(Current.Gameplay.QuicksandDrag);
        Writer.Write(Current.Gameplay.QuicksandDeepDrag);
        Writer.Write(Current.Spatial.MAX_Z_LAYERS);
        Writer.Write(Current.Spatial.SPRITE_GROUP_QUERY_THRESHOLD);
        Writer.Write(Current.Spatial.DEFAULT_CELL_CAPACITY);
        Writer.Write(Current.Spatial.CELL_LIST_POOL_SIZE);
        Writer.Write(Current.Spatial.BUCKET_SIZE_PIXELS);
        Writer.Write(Current.Audio.DefaultMasterVolume);
        Writer.Write(Current.Audio.DefaultSFXVolume);
        Writer.Write(Current.Audio.DefaultMusicVolume);
        Writer.Write(Current.Audio.AudioMax);
        Writer.Write(Current.Paths.FallbackFontPath);
        Writer.Write(Current.Paths.FallbackTexturePath);
        Writer.Write(Current.Paths.EffectPath);
        Writer.Write(Current.Times.TimeWarnings[0]);
        Writer.Write(Current.Times.TimeWarnings[1]);
        Writer.Write(Current.Times.TimeWarnings[2]);

        CloseStream();
      } catch (Exception Exception) {
        Console.WriteLine($"[ERROR]: Failed to create Config BIN: {Exception.Message}");
      }
    }

    public static void SaveCredits() {
      try {
        File.WriteAllText(CreditJSONName, JsonConvert.SerializeObject(Current, Formatting.Indented));
        Console.WriteLine($"Credit JSON saved to {CreditJSONName} sucessfully.");
      } catch (Exception Exception) {
        Console.WriteLine($"[ERROR]: Failed to save Credit JSON: {Exception.Message}");
      }
    }


    public static GraphicsConfig Graphics => Current.Graphics;
    public static UIConfig UI => Current.UI;
    public static GameplayConfig Gameplay => Current.Gameplay;
    public static PlayerConfig Player => Current.Gameplay.Player;
    public static SpatialConstants Spatial => Current.Spatial;
    public static AudioConfig Audio => Current.Audio;
    public static PathsConfig Paths => Current.Paths;
    public static TimeConfig Times => Current.Times;

    
    public static void ResetToDefaults() {
      Current = new();
      Save();
      Console.WriteLine("CONFIG reset to default.");
    }
    
    private static void CloseStream() {
      Stream?.Dispose();
      Stream = null;
    }

  
    public static string GetConfigPath() {
      string SaveDirectory = "";

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        SaveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CubeRun");
      } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
        SaveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", "CubeRun");
      } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
        SaveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".local", "share", "CubeRun");
      }

      if (!Directory.Exists(SaveDirectory))
        Directory.CreateDirectory(SaveDirectory);

      return SaveDirectory;
    }
  }


  public static class BestiaryManager {
    private static readonly string BestiaryFileName = Path.Combine(ConfigManager.SaveDirectory, "Bestiary.bin");
    private static readonly string BestiaryJSONName = Path.Combine(ConfigManager.SaveDirectory, "Bestiary.json");
    public static Dictionary<string, BinDictHeader> BestiaryIndex = new();
    public static FileStream ReadStream;
    public static BinaryReader Reader;
    public static Bestiary Bestiary = new();
    private const int BESTIARY_MAGIC = 0x6978654C;
    private const int BESTIARY_VERSION = 1;
    private const int INDEX_START_POSITION = sizeof(int) + sizeof(int) + sizeof(int);
    private const int ENTRY_SIZE = sizeof(long) + sizeof(int) + sizeof(int);


    public static bool LoadIndex() {
      try {
        if (!File.Exists(BestiaryFileName)) {
          Console.WriteLine($"[WARNING]: Bestiary file not found at {BestiaryFileName}");
          return false;
        }

        CloseStreams();

        ReadStream = File.Open(BestiaryFileName, FileMode.Open, FileAccess.Read);
        Reader = new(ReadStream);

        int Magic = Reader.ReadInt32();
        if (Magic != BESTIARY_MAGIC) {
          Console.WriteLine("[ERROR]: Invalid bestiary file format.");
          CloseStreams();
          return false;
        }

        int Version = Reader.ReadInt32();
        if (Version != BESTIARY_VERSION) {
          Console.WriteLine("[ERROR]: Invalid bestiary file version.");
          CloseStreams();
          return false;
        }

        int Count = Reader.ReadInt32();

        BestiaryIndex.Clear();

        for (int Index = 0; Index < Count; Index++) {
          long Offset = Reader.ReadInt64();
          int Hash = Reader.ReadInt32();
          int Size = Reader.ReadInt32();

          if (Offset < 0 || Offset > ReadStream.Length) {
            Console.WriteLine($"[ERROR]: Invalid offset in bestiary index: {Offset}");
            CloseStreams();
            return false;
          }

          long CurrentPosition = ReadStream.Position;
          ReadStream.Position = Offset;

          string EntryID = Reader.ReadString();
          BestiaryIndex[EntryID] = new(Offset, Size);

          ReadStream.Position = CurrentPosition;
        }

        return true;
      } catch (Exception Exception) {
        Console.WriteLine($"[ERROR]: Failed to load bestiary: {Exception.Message}");
        CloseStreams();

        return false;
      }
    }

    public static void SaveBestiary() {
      try {
        File.WriteAllText(BestiaryJSONName, JsonConvert.SerializeObject(Bestiary, Formatting.Indented));
      } catch (Exception Exception) {
        Console.WriteLine($"[ERROR]: Failed to save Bestiary JSON - {Exception.Message}");
      }
    }

    public static bool SaveAsBin() {
      FileStream WriteStream = null;

      try {
        if (!File.Exists(BestiaryJSONName)) {
          Console.WriteLine($"[ERROR]: Bestiary JSON file not found at {BestiaryJSONName}");
          return false;
        }

        string Json = File.ReadAllText(BestiaryJSONName);
        Bestiary BestiaryData = JsonConvert.DeserializeObject<Bestiary>(Json);

        if (BestiaryData == null) {
          Console.WriteLine("[ERROR]: Failed to deserialize bestiary JSON.");
          return false;
        }

        Dictionary<string, BestiaryEntry> Entries = BestiaryData?.Entries;

        if (Entries == null) {
          Console.WriteLine("[ERROR]: Failed to deserialize bestiary JSON.");
          return false;
        }

        List<BinHeader> Headers = [];
        WriteStream = File.Open(BestiaryFileName, FileMode.Create, FileAccess.Write);
        using BinaryWriter Wrtier = new(WriteStream);

        Wrtier.Write(BESTIARY_MAGIC);
        Wrtier.Write(BESTIARY_VERSION);
        Wrtier.Write(Entries.Count);

        WriteStream.Position = INDEX_START_POSITION + (Entries.Count * ENTRY_SIZE);

        foreach (KeyValuePair<string, BestiaryEntry> ValuePair in Entries) {
          long Offset = WriteStream.Position;

          string ID = ValuePair.Value.ID ?? "";
          string Description = ValuePair.Value.Description ?? "";

          Wrtier.Write(ID);
          Wrtier.Write(Description);

          Headers.Add(new(Offset, ID.GetHashCode(), (int)(WriteStream.Position - Offset)));
        }

        WriteStream.Position = INDEX_START_POSITION;
        for (int Index = 0; Index < Headers.Count; Index++) {
          Wrtier.Write(Headers[Index].Offset);
          Wrtier.Write(Headers[Index].Hash);
          Wrtier.Write(Headers[Index].Size);
        }

        return true;
      } catch (Exception Exception) {
        Console.WriteLine($"[ERROR]: Failed to save bestiary binary - {Exception.Message}");
        return false;
      } finally {
        WriteStream?.Dispose();
      }
    }
  

    public static BestiaryEntry? LoadBestiaryEntry(string id) {
      try {
        if (Reader == null || ReadStream == null) {
          Console.WriteLine("[ERROR]: Bestiary not loaded. Read streams closed.");
          return null;
        }

        if (!BestiaryIndex.TryGetValue(id, out BinDictHeader Entry)) {
          Console.WriteLine($"[WARNING]: Bestiary entry {id} not found.");
          return null;
        }

        if (Entry.Offset < 0 || Entry.Offset >= ReadStream.Length) {
          Console.WriteLine($"[ERROR]: Invalid offset for entry {id}.");
          return null;
        }

        ReadStream.Position = Entry.Offset;
        using BinaryReader Slice = new(new MemoryStream(Reader.ReadBytes(Entry.Size)));

        return new(Slice.ReadString(), Slice.ReadString());
      } catch (Exception Exception) {
        Console.WriteLine($"[ERROR]: Failed to load bestiary entry {id} : {Exception.Message}");
        return null;
      }
    }


    private static void CloseStreams() {
      ReadStream?.Dispose();
      Reader?.Dispose();
      ReadStream = null;
      Reader = null;
    }
  }
}
