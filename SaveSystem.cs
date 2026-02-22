using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static Cube_Run_C_.Assets;
using static Cube_Run_C_.ConfigManager;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Tools;
using static Cube_Run_C_.Tools.BitMask;
using static Cube_Run_C_.UI;
using Color = Microsoft.Xna.Framework.Color;


namespace Cube_Run_C_ {
  /// <summary>
  /// Global class for managing: Level Saves, Setting Saves, and Screenshots.
  /// </summary>
  public static class SaveSystem {
    private static readonly SemaphoreSlim ScreenshotSaveLock = new(2, 2);
    private static readonly string SettingsSaveFile = Path.Combine(SaveDirectory, "SettingsData.bin");
    private static readonly string SaveFile = Path.Combine(SaveDirectory, "LevelSaveData.bin");
    private static readonly string ScreenshotFolder = ScreenshotPath();
    private static Color[] ScreenshotBuffer1 = [];
    private static Color[] ScreenshotBuffer2 = [];
    private static FileStream ReadStream;
    private static BinaryReader Reader;
    private static int CurrentScreenshotBuffer = 0;
    private const int LEVEL_POSITION = sizeof(int) + sizeof(byte) + sizeof(byte) + sizeof(byte) + sizeof(ushort) + sizeof(ushort);
    private const int LEVEL_ENTRY_SIZE = sizeof(ushort) + sizeof(ushort) + sizeof(byte);
    private const int LEVEL_SAVE_MAGIC = 0x416C6578;
    private const int SETTINGS_SAVE_MAGIC = 0x4E617261;
    private const byte SAVE_VERSION = 1;
    private static byte LevelSaveCount = 0;


    /// <summary>
    /// Save snapshot on a background Thread.
    /// </summary>
    /// <param name="screen"> Current frame texture </param>
    public static async void SaveScreenshotAsync(RenderTarget2D screen) {
      if (!await ScreenshotSaveLock.WaitAsync(0)) {
        Console.WriteLine("Screenshot already in progress, please wait...");
        return; 
      }
      
      try {
        Dimensions TextureDimensions = screen.GetDimensions();
        int ImageSize = TextureDimensions.Width * TextureDimensions.Height;
          
        if (ScreenshotBuffer1 == null || ScreenshotBuffer1.Length < ImageSize) {
          ScreenshotBuffer1 = new Color[ImageSize];
          ScreenshotBuffer2 = new Color[ImageSize];
        }
          
        Color[] Buffer = CurrentScreenshotBuffer == 0 ? ScreenshotBuffer1 : ScreenshotBuffer2;
        CurrentScreenshotBuffer = 1 - CurrentScreenshotBuffer; 

        screen.GetData(Buffer, 0, ImageSize);
          
        string ShotName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
          
        _ = Task.Run(() => {
          try {
            using Image<Rgba32> Screenshot = Image.LoadPixelData<Rgba32>(MemoryMarshal.Cast<Color, Rgba32>(Buffer),TextureDimensions.Width, TextureDimensions.Height);
            Screenshot.SaveAsPng(Path.Combine(ScreenshotFolder, ShotName));
          } catch (Exception Exception) {
            Console.WriteLine($"[ERROR]: Failed to save screenshot {ShotName} - {Exception}");
          } finally {
            ScreenshotSaveLock.Release();
          }
        });
      } catch (Exception Exception) {
        Console.WriteLine($"[ERROR]: Failed to generate screenshot - {Exception.Message}. Disposing resources...");
        ScreenshotSaveLock.Release();
      }
    }


    /// <summary>
    /// Save setting configuration on a background Thread.
    /// </summary>
    public static void SaveSettings() {
      try {
        byte SettingsStats = 0x00;

        Set(ref SettingsStats, 1 << 0, IsSet(GlobalStats, (ushort)GlobalFlags.Fullscreen));
        Set(ref SettingsStats, 1 << 1, IsSet(GlobalStats, (ushort)GlobalFlags.LetterBoxMode));
        Set(ref SettingsStats, 1 << 2, SoundManager.Muted);

        _ = Task.Run(() => {
          try {
            using FileStream Stream = File.Open(SettingsSaveFile, FileMode.Create);
            using BinaryWriter Writer = new(Stream);

            Writer.Write(SETTINGS_SAVE_MAGIC);
            Writer.Write(SAVE_VERSION);
            Writer.Write(SettingsStats);

            for (int Index = 0; Index < PauseMenu.SelectedValues.Length; Index++) {
              Writer.Write(PauseMenu.SelectedValues[Index].X);
              Writer.Write(PauseMenu.SelectedValues[Index].Y);
              Writer.Write(PauseMenu.SelectedValues[Index].Z);
            }
          } catch (Exception Exception) {
            Console.WriteLine($"[ERROR]: Failed to save Settings - {Exception}");
          }
        });
      } catch (Exception Exception) {
        Console.WriteLine($"[ERROR]: Failed to generate Save settings - {Exception}");
      }
    }

    /// <summary>
    /// Write settings to disk. Used during initial file creation.
    /// </summary>
    private static void SaveSettingsSync() => WriteSettingsCore(BuildSettingsStats());

    private static void WriteSettingsCore(byte settingsStats) {
      using FileStream Stream = File.Open(SettingsSaveFile, FileMode.Create);
      using BinaryWriter Writer = new(Stream);

      Writer.Write(SETTINGS_SAVE_MAGIC);
      Writer.Write(SAVE_VERSION);
      Writer.Write(settingsStats);

      for (int Index = 0; Index < PauseMenu.SelectedValues.Length; Index++) {
        Writer.Write(PauseMenu.SelectedValues[Index].X);
        Writer.Write(PauseMenu.SelectedValues[Index].Y);
        Writer.Write(PauseMenu.SelectedValues[Index].Z);
      }
    }

    private static byte BuildSettingsStats() {
      byte SettingsStats = 0x00;
      Set(ref SettingsStats, 1 << 0, IsSet(GlobalStats, (ushort)GlobalFlags.Fullscreen));
      Set(ref SettingsStats, 1 << 1, IsSet(GlobalStats, (ushort)GlobalFlags.LetterBoxMode));
      Set(ref SettingsStats, 1 << 2, SoundManager.Muted);
      return SettingsStats;
    }


    /// <summary>
    /// Save level data on a background Thread.
    /// </summary>
    public static void Save() {
      try {
        LevelStats Current = Level.Stats;

        if (PlayerData.CurrentLevel >= LevelSaveCount)
          LevelSaveCount = (byte)(PlayerData.CurrentLevel + 1);
        

        Task.Run(() => {
          using FileStream Stream = File.Open(SaveFile, FileMode.Create, FileAccess.Write);
          using BinaryWriter Writer = new(Stream);

          Writer.Write(LEVEL_SAVE_MAGIC);
          Writer.Write(SAVE_VERSION);

          Writer.Write(LevelSaveCount);

          Writer.Write(LevelData.Difficulty);
          Writer.Write(PlayerData.CurrentLevel);
          Writer.Write(PlayerData.Lives);
          Writer.Write(PlayerData.Coins);

          for (byte Index = 0; Index < LevelSaveCount; Index++) {
            LevelStats Existing = LoadLevelStats(Index);

            if (PlayerData.CurrentLevel == Index) {
              Writer.Write(Current.Deaths < Existing.Deaths ? Current.Deaths : Existing.Deaths);
              Writer.Write(Current.Coins > Existing.Coins ? Current.Coins : Existing.Coins);
              Writer.Write(Current.LifeBlocks > Existing.LifeBlocks ? Current.LifeBlocks : Existing.LifeBlocks);
            } else {
              Writer.Write(Existing.Deaths);
              Writer.Write(Existing.Coins);
              Writer.Write(Existing.LifeBlocks);
            }
          }
        });
      } catch (Exception Exception) {
        Console.WriteLine($"[ERROR]: Failed to save Level data - {Exception}");
      }
    }
    

    /// <summary>
    /// Load both settings and level data in parallel and wait for both to complete.
    /// </summary>
    public static Task LoadAll() => Task.WhenAll(LoadSettings(), Load());
   
    /// <summary>
    /// Load setting configuration on a background Thread.
    /// </summary>
    public static Task LoadSettings() {
      if (!File.Exists(SettingsSaveFile)) {
        Console.WriteLine($"[ERROR]: Settings file not found at {SettingsSaveFile}");
        CreateNew(true);
        LoadSettings();
        return Task.CompletedTask;
      }

      return Task.Run(() => {
        try {
          ReadStream = File.Open(SettingsSaveFile, FileMode.Open);
          Reader = new(ReadStream);

          int Magic = Reader.ReadInt32();
          if (Magic != SETTINGS_SAVE_MAGIC) {
            Console.WriteLine("[ERROR]: Invalid Settings file format.");
            CloseStreams();
            return;
          }

          byte Version = Reader.ReadByte();
          if (Version != SAVE_VERSION) {
            Console.WriteLine("[ERROR]: Invalid Settings file version format.");
            CloseStreams();
            return;
          }

          byte SettingsStats = Reader.ReadByte();

          Set(ref PauseMenu.Stats, (ushort)MenuStatus.Fullscreen, IsSet(SettingsStats, 1 << 0));
          Set(ref PauseMenu.Stats, (ushort)MenuStatus.LetterBox, IsSet(SettingsStats, 1 << 1));
          Set(ref PauseMenu.Stats, (ushort)MenuStatus.Mute, IsSet(SettingsStats, 1 << 2));

          for (int Index = 0; Index < PauseMenu.SelectedValues.Length; Index++) {
            PauseMenu.SelectedValues[Index].X = Reader.ReadSingle();
            PauseMenu.SelectedValues[Index].Y = Reader.ReadSingle();
            PauseMenu.SelectedValues[Index].Z = Reader.ReadSingle();

            if (Index == 0) { 
              Set(ref PauseMenu.Stats, (ushort)MenuStatus.UpdateScreen, true);
              Set(ref PauseMenu.Stats, (ushort)MenuStatus.UpdateBrightness, true);
              Set(ref PauseMenu.Stats, (ushort)MenuStatus.UpdateFPS, true);
            } else if (Index == 1) {
              Set(ref PauseMenu.Stats, (ushort)MenuStatus.UpdateVolume, true);
              Set(ref PauseMenu.Stats, (ushort)MenuStatus.UpdateSFX, true);
              Set(ref PauseMenu.Stats, (ushort)MenuStatus.UpdateMusic, true);
            }
          }
        } catch (Exception Exception) {
          Console.WriteLine($"[ERROR]: Failed to load Settings - {Exception}");
        } finally {
          CloseStreams();
        }
      });
    }

    /// <summary>
    /// Load level data on a background Thread.
    /// </summary>
    public static Task Load() {
      if (!File.Exists(SaveFile)) {
        Console.WriteLine($"[ERROR]: Level save file not found at {SaveFile}");
        CreateNew(false);
        return Task.CompletedTask;
      }

      return Task.Run(() => {
        try {
          ReadStream = File.Open(SaveFile, FileMode.Open, FileAccess.Read);
          Reader = new(ReadStream);

          int Magic = Reader.ReadInt32();
          if (Magic != LEVEL_SAVE_MAGIC) {
            Console.WriteLine("[ERROR]: Invalid Level save file format.");
            CloseStreams();
            return;
          }

          byte Version = Reader.ReadByte();
          if (Version != SAVE_VERSION) {
            Console.WriteLine("[ERROR]: Invalid Level save file version.");
            CloseStreams();
            return;
          }

          byte LevelCount = Reader.ReadByte();
          LevelSaveCount = LevelCount;

          LevelData.Difficulty = Reader.ReadString();
          PlayerData.CurrentLevel = Reader.ReadByte();
          PlayerData.Lives = Reader.ReadUInt16();
          PlayerData.Coins = Reader.ReadUInt16();
        } catch (Exception Exception) {
          Console.WriteLine($"[ERROR]: Failed to load Level data - {Exception}");
        }
      });
    }

    /// <summary>
    /// Load only a given Level's stats on a background Thread.
    /// </summary>
    /// <param name="level"> Level to load </param>
    /// <returns> 
    /// True if <paramref name="level"/> load successful 
    /// -or-
    /// False if <paramref name="level"/> load unsuccessful
    /// </returns>
    public static LevelStats LoadLevelStats(byte level) {
      try {
        if (Reader == null || ReadStream == null) {
          Console.WriteLine("[ERROR]: Level save not loaded. Read streams closed.");
          return LevelStats.Zero;
        }

        if (level >= LevelSaveCount) {
          Console.WriteLine($"[WARNING]: Level save entry {level} not found.");
          return LevelStats.Zero;
        }

        ReadStream.Position = LEVEL_POSITION + (level * LEVEL_ENTRY_SIZE);
        using BinaryReader Slice = new(new MemoryStream(Reader.ReadBytes(LEVEL_ENTRY_SIZE)));

        return new(Slice.ReadUInt16(), Slice.ReadUInt16(), Slice.ReadByte());
      } catch (Exception Exception) {
        Console.WriteLine($"[ERROR]: Failed to load Level {level} save data - {Exception.Message}");
        return LevelStats.Zero;
      }
    }


    /// <summary>
    /// Close and Dispose file read streams
    /// </summary>
    public static void CloseStreams() {
      ReadStream?.Dispose();
      Reader?.Dispose();
      ReadStream = null;
      Reader = null;
    }


    /// <summary>
    /// Create new save file.
    /// </summary>
    /// <param name="settings"> Whether to create new Settings file </param>
    private static void CreateNew(bool settings) {
      if (settings) {
        SaveSettingsSync();
      } else {
        File.Create(SaveFile).Dispose();
        Save();
      }
    }


    /// <summary>
    /// Retrieve/Create OS-based Screenshot folder directory
    /// </summary>
    /// <returns> Screenshot Folder Directory </returns>
    private static string ScreenshotPath() {
      string ScreenshotDirectory = Path.Combine(SaveDirectory, "Screenshots");

      try {
        if (!Directory.Exists(ScreenshotDirectory))
          Directory.CreateDirectory(ScreenshotDirectory);
        
        return ScreenshotDirectory;
      } catch (Exception Exception) {
        Console.WriteLine($"[ERROR]: Failed to create directory {ScreenshotDirectory} - {Exception}");
        return null;
      }
    }
  }
}