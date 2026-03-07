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

  
  public static class ConfigManager {
    public class GameConfig {
      public GraphicsConfig Graphics = new();
      public UIConfig UI = new();
      public GameplayConfig Gameplay = new();
      public SpatialConstants Spatial = new();
      public AudioConfig Audio = new();
      public CameraConfig Camera = new();
      public PathsConfig Paths = new();
      public TimeConfig Times = new();
    }


    public class GraphicsConfig {
      public Dimensions DefaultDimensions = new(1280, 720);
      public int MinimumWidth = 160;
      public int MinimumHeight = 90;
      public ushort TargetFPS = 144;
      public ushort MinFPS = 30;
      public ushort MaxFPS = 360;
      public byte BaseImageSize = 96;
      public float BaseScaleFactor = 3f;
      public float ScreenRatio = 16f / 9f;
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
    
    public class CameraConfig {
      public ushort MaxExpectedSprites = 200;
    }


    public class PathsConfig {
      public string FallbackFontPath = "Fonts/Fallback";
      public string FallbackTexturePath = "Images/FallbackTexture";
      public string EffectPath = "Effects/Shaders";
    }
    
    public class TimeConfig {
      public double[] TimeWarnings = [60, 180, 300];
    }



    public static readonly string SaveDirectory = GetConfigPath();
    private static readonly string ConfigJSONName = Path.Combine(SaveDirectory, "GameConfig.json");
    private static readonly string ConfigFileName = Path.Combine(SaveDirectory, "GameConfig.bin");
    private static readonly string CreditJSONName = Path.Combine(SaveDirectory, "Credits.json");
    private static FileStream Stream;
    private static GameConfig Current { get; set; } = new();
    public static Credits GameCredits { get; private set; } = new();
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

        Graphics.DefaultDimensions = new(Reader.ReadInt32(), Reader.ReadInt32());
        Graphics.MinimumWidth = Reader.ReadInt32();
        Graphics.MinimumHeight = Reader.ReadInt32();
        Graphics.TargetFPS = Reader.ReadUInt16();
        Graphics.MinFPS = Reader.ReadUInt16();
        Graphics.MaxFPS = Reader.ReadUInt16();
        Graphics.BaseImageSize = Reader.ReadByte();
        Graphics.BaseScaleFactor = Reader.ReadSingle();
        Graphics.ScreenRatio = Reader.ReadSingle();
        Graphics.BrightnessMax = Reader.ReadSingle();
        Graphics.GhostAlpha = Reader.ReadSingle();
        UI.LoadingDotInterval = Reader.ReadSingle();
        Gameplay.Player.TerminalVelocity = Reader.ReadSingle();
        Gameplay.Player.WallJumpDirectionFactor = Reader.ReadSingle();
        Gameplay.Player.QuicksandSpeedFactor = Reader.ReadSingle();
        Gameplay.Player.QuicksandDeepSpeedFactor = Reader.ReadSingle();
        Gameplay.Player.QuicksandJumpFactor = Reader.ReadSingle();
        Gameplay.Player.SpringEffect = Reader.ReadSingle();
        Gameplay.Player.AlternativeSpringEffect = Reader.ReadSingle();
        Gameplay.Player.Gravity = Reader.ReadUInt16();
        Gameplay.Player.JumpHeight = Reader.ReadUInt16();
        Gameplay.Player.Speed = Reader.ReadUInt16();
        Gameplay.Player.PowerBitOffset = Reader.ReadByte();
        Gameplay.Player.ContactBitOffset = Reader.ReadByte();
        Gameplay.TileSize = Reader.ReadByte();
        Gameplay.CellSize = Reader.ReadByte();
        Gameplay.AnimationSpeed = Reader.ReadByte();
        Gameplay.RadianFactor = Reader.ReadSingle();
        Gameplay.Hundredth = Reader.ReadSingle();
        Gameplay.GoalFragmentMax = Reader.ReadByte();
        Gameplay.DefaultEnemySpeed = Reader.ReadUInt16();
        Gameplay.DefaultBulletSpeed = Reader.ReadUInt16();
        Gameplay.DefaultFallingSpikeSpeed = Reader.ReadUInt16();
        Gameplay.DefaultFallingSpikeRegrow = Reader.ReadUInt16();
        Gameplay.DefaultCanonFiringRate = Reader.ReadSingle();
        Gameplay.DefaultLanternWidth = Reader.ReadSingle();
        Gameplay.QuicksandDrag = Reader.ReadSingle();
        Gameplay.QuicksandDeepDrag = Reader.ReadSingle();
        Spatial.MAX_Z_LAYERS = Reader.ReadByte();
        Spatial.SPRITE_GROUP_QUERY_THRESHOLD = Reader.ReadByte();
        Spatial.DEFAULT_CELL_CAPACITY = Reader.ReadByte();
        Spatial.CELL_LIST_POOL_SIZE = Reader.ReadByte();
        Spatial.BUCKET_SIZE_PIXELS = Reader.ReadByte();
        Audio.DefaultMasterVolume = Reader.ReadSingle();
        Audio.DefaultSFXVolume = Reader.ReadSingle();
        Audio.DefaultMusicVolume = Reader.ReadSingle();
        Audio.AudioMax = Reader.ReadSingle();
        Camera.MaxExpectedSprites = Reader.ReadUInt16();
        Paths.FallbackFontPath = Reader.ReadString();
        Paths.FallbackTexturePath = Reader.ReadString();
        Paths.EffectPath = Reader.ReadString();
        Times.TimeWarnings[0] = Reader.ReadDouble();
        Times.TimeWarnings[1] = Reader.ReadDouble();
        Times.TimeWarnings[2] = Reader.ReadDouble();

        CloseStream();
      } catch (Exception Exception) {
        Console.WriteLine($"[ERROR]: Failed to read ConfigBIN: {Exception.Message} Generating default config.");
      }
    }

    public static void LoadCredits() {
      try {
        if (File.Exists(CreditJSONName)) {
          string Json = File.ReadAllText(CreditJSONName);
          GameCredits = JsonConvert.DeserializeObject<Credits>(Json) ?? new();
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
        
        Writer.Write(Graphics.DefaultDimensions.Width);
        Writer.Write(Graphics.DefaultDimensions.Height);
        Writer.Write(Graphics.MinimumWidth);
        Writer.Write(Graphics.MinimumHeight);
        Writer.Write(Graphics.TargetFPS);
        Writer.Write(Graphics.MinFPS);
        Writer.Write(Graphics.MaxFPS);
        Writer.Write(Graphics.BaseImageSize);
        Writer.Write(Graphics.BaseScaleFactor);
        Writer.Write(Graphics.ScreenRatio);
        Writer.Write(Graphics.BrightnessMax);
        Writer.Write(Graphics.GhostAlpha);
        Writer.Write(UI.LoadingDotInterval);
        Writer.Write(Gameplay.Player.TerminalVelocity);
        Writer.Write(Gameplay.Player.WallJumpDirectionFactor);
        Writer.Write(Gameplay.Player.QuicksandSpeedFactor);
        Writer.Write(Gameplay.Player.QuicksandDeepSpeedFactor);
        Writer.Write(Gameplay.Player.QuicksandJumpFactor);
        Writer.Write(Gameplay.Player.SpringEffect);
        Writer.Write(Gameplay.Player.AlternativeSpringEffect);
        Writer.Write(Gameplay.Player.Gravity);
        Writer.Write(Gameplay.Player.JumpHeight);
        Writer.Write(Gameplay.Player.Speed);
        Writer.Write(Gameplay.Player.PowerBitOffset);
        Writer.Write(Gameplay.Player.ContactBitOffset);
        Writer.Write(Gameplay.TileSize);
        Writer.Write(Gameplay.CellSize);
        Writer.Write(Gameplay.AnimationSpeed);
        Writer.Write(Gameplay.RadianFactor);
        Writer.Write(Gameplay.Hundredth);
        Writer.Write(Gameplay.GoalFragmentMax);
        Writer.Write(Gameplay.DefaultEnemySpeed);
        Writer.Write(Gameplay.DefaultBulletSpeed);
        Writer.Write(Gameplay.DefaultFallingSpikeSpeed);
        Writer.Write(Gameplay.DefaultFallingSpikeRegrow);
        Writer.Write(Gameplay.DefaultCanonFiringRate);
        Writer.Write(Gameplay.DefaultLanternWidth);
        Writer.Write(Gameplay.QuicksandDrag);
        Writer.Write(Gameplay.QuicksandDeepDrag);
        Writer.Write(Spatial.MAX_Z_LAYERS);
        Writer.Write(Spatial.SPRITE_GROUP_QUERY_THRESHOLD);
        Writer.Write(Spatial.DEFAULT_CELL_CAPACITY);
        Writer.Write(Spatial.CELL_LIST_POOL_SIZE);
        Writer.Write(Spatial.BUCKET_SIZE_PIXELS);
        Writer.Write(Audio.DefaultMasterVolume);
        Writer.Write(Audio.DefaultSFXVolume);
        Writer.Write(Audio.DefaultMusicVolume);
        Writer.Write(Audio.AudioMax);
        Writer.Write(Camera.MaxExpectedSprites);
        Writer.Write(Paths.FallbackFontPath);
        Writer.Write(Paths.FallbackTexturePath);
        Writer.Write(Paths.EffectPath);
        Writer.Write(Times.TimeWarnings[0]);
        Writer.Write(Times.TimeWarnings[1]);
        Writer.Write(Times.TimeWarnings[2]);

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
    public static CameraConfig Camera => Current.Camera;
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
    public readonly struct BestiaryEntry {
      public readonly string ID;
      public readonly string Description;


      public BestiaryEntry(string id, string description) {
        this.ID = id;
        this.Description = description;
      }


      public static readonly BestiaryEntry Default = new("[Empty]", "[Empty]");
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


    private static readonly string BestiaryFileName = Path.Combine(ConfigManager.SaveDirectory, "Bestiary.bin");
    private static readonly string BestiaryJSONName = Path.Combine(ConfigManager.SaveDirectory, "Bestiary.json");
    public static Dictionary<string, BinDictHeader> BestiaryIndex = new();
    public static FileStream ReadStream;
    public static BinaryReader Reader;
    public static Bestiary Dictionary = new();
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
        File.WriteAllText(BestiaryJSONName, JsonConvert.SerializeObject(Dictionary, Formatting.Indented));
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

  public static class CutsceneManager {
    public class Event {
      public float StartTime;
      public float Duration;
      public Action Command;
    }

    
  }
}