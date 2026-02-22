using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using static Cube_Run_C_.ConfigManager;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Sprites;


namespace Cube_Run_C_ {
  public static class Assets {
    /// <summary>
    /// Volume-Type index.
    /// </summary>
    public enum Volumes : byte {
      Master = 0,
      SFX = 1,
      Music = 2
    }

    /// <summary>
    /// Lightweight Color index.
    /// </summary>
    public enum Colors : byte {
      Black = 0,
      White = 1,
      Pink = 2,
      Tan = 3,
      DarkPurple = 4
    }


    /// <summary>
    /// Data accompanying a SoundEffect.
    /// </summary>
    public readonly struct SoundData {
      public readonly float Volume;
      public readonly float Pitch;
      public readonly float Pan;
      public readonly bool Loop;


      /// <summary>
      /// Constructs SoundData given Audio Effects.
      /// </summary>
      /// <param name="volume"> Effect Volume </param>
      /// <param name="pitch"> Effect Pitch </param>
      /// <param name="pan"> Effect Pan </param>
      /// <param name="loop"> Loop Effect </param>
      public SoundData(float volume, float pitch, float pan, bool loop = false) {
        this.Volume = volume;
        this.Pitch = pitch;
        this.Pan = pan;
        this.Loop = loop;
      }


      /// <summary>
      /// Default data
      /// </summary>
      public static readonly SoundData Default = new(1.0f, 0.0f, 0.0f, false);
    }


    /// <summary>
    /// Global class for managing Audio.
    /// </summary>
    public static class SoundManager {
      private static readonly Dictionary<string, SoundEffect> SoundEffectCache = new();
      private static readonly Dictionary<string, SoundEffectInstance> LoopingSounds = new();
      private static readonly float[] Volume = [Audio.DefaultMasterVolume, Audio.DefaultSFXVolume, Audio.DefaultMusicVolume];
      public static Song CurrentSong = null;
      public static bool Muted = false;


      /// <summary>
      /// Import SoundEffect via Assets ContentPipeline
      /// </summary>
      /// <param name="path"> SoundEffect file path </param>
      /// <param name="cache"> Whether to cache SoundEffect </param>
      /// <returns></returns>
      public static SoundEffect GetSound(string path, bool cache = true) {
        try {
          if (!cache)
            return VisualManager.Content.Load<SoundEffect>(path);

          string CacheKey = VisualManager.NormalizePath(path);

          if (SoundEffectCache.TryGetValue(CacheKey, out SoundEffect Sound))
            return Sound;

          Sound = VisualManager.Content.Load<SoundEffect>(path);
          SoundEffectCache[CacheKey] = Sound;
          return Sound;
        } catch (ContentLoadException Exception) {
          Console.WriteLine($"[WARNING]: Unable to load sound: {Path.GetFileName(path)} - {Exception.Message}");
          Console.WriteLine("[WARNING]: Playing no audio for file intsead.");

          return null;
        }
      }

      /// <summary>
      /// Import MediaPlayer Song via Assets ContentPipeline
      /// </summary>
      /// <param name="path"> Song file path </param>
      public static void LoadSong(string path) {
        try {
          CurrentSong = VisualManager.Content.Load<Song>(path);
        } catch (ContentLoadException Exception) {
          Console.WriteLine($"[WARNING]: Unable to load Song: {Path.GetFileName(path)} - {Exception.Message}");
          Console.WriteLine("[WARNING]: Playing no audio for song file instead.");

          CurrentSong = null;
        }
      }
    

      /// <summary>
      /// Play MediaPlayer CurrentSong
      /// </summary>
      /// <param name="repeat"> Loop MediaPlayer Song </param>
      public static void PlaySong(bool repeat = true) {
        MediaPlayer.Stop();

        if (CurrentSong == null) 
          return;

        MediaPlayer.IsRepeating = repeat;
        MediaPlayer.Play(CurrentSong);
      }

      /// <summary>
      /// Load and play SoundEffect
      /// </summary>
      /// <param name="path"> SoundEffect file path </param>
      /// <param name="data"> SoundData effects </param>
      public static void PlaySound(string path, SoundData data) {
        SoundEffect Sound = GetSound(path, true);

        if (Sound == null) 
          return;

        Sound.Play(data.Volume * Volume[(byte)Volumes.SFX] * Volume[(byte)Volumes.Master], data.Pitch, data.Pan);
      } 

      /// <summary>
      /// Load SoundEffect, create and play/loop SoundEffectInstance given SoundData
      /// </summary>
      /// <param name="path"> SoundEffect file path </param>
      /// <param name="data"> SoundData effects </param>
      public static void PlaySoundInstance(string path, SoundData data) {
        SoundEffect Sound = GetSound(path, true);

        if (Sound == null) 
          return;

        SoundEffectInstance Instance = Sound.CreateInstance();
        Instance.Volume = data.Volume * Volume[(byte)Volumes.SFX] * Volume[(byte)Volumes.Master];
        Instance.Pitch = data.Pitch;
        Instance.Pan = data.Pan;
        Instance.IsLooped = data.Loop;
        Instance.Play();

        if (data.Loop)
          LoopingSounds[VisualManager.NormalizePath(path)] = Instance;
      }


      /// <summary>
      /// Resume current MediaPlayer song.
      /// </summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void ResumeMusic() => MediaPlayer.Resume();

      /// <summary>
      /// Pause current MediaPlayer song.
      /// </summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void PauseMusic() => MediaPlayer.Pause();

      /// <summary>
      /// Stop and dispose current Song.
      /// </summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void StopMusic() => MediaPlayer.Stop();
      
      /// <summary>
      /// Stop specific SoundEffectInstance
      /// </summary>
      /// <param name="path"> Path of SoundEffect </param>
      public static void StopSoundLoop(string path) {
        string NormalizedPath = VisualManager.NormalizePath(path);

        if (LoopingSounds.TryGetValue(NormalizedPath, out SoundEffectInstance Instance)) {
          Instance.Stop();
          Instance.Dispose();
          LoopingSounds.Remove(NormalizedPath);
        }
      }

      /// <summary>
      /// Stop and Dispose all looping SoundEffectInstances
      /// </summary>
      public static void StopAllSoundLoops() {
        foreach (SoundEffectInstance Instance in LoopingSounds.Values) {
          Instance.Stop();
          Instance.Dispose();
        }

        LoopingSounds.Clear();
      }


      /// <summary>
      /// Stop and dispose all SoundEffectInstances
      /// </summary>
      public static void UnloadAllSounds() {
        StopAllSoundLoops();

        SoundEffectCache.Clear();
        LoopingSounds.Clear();
      }

      /// <summary>
      /// Stop and dispose current MediaPlayer song
      /// </summary>
      public static void UnloadMusic() {
        CurrentSong?.Dispose();
        StopMusic();
      }


      /// <summary>
      /// Retrieve current Master volume
      /// </summary>
      /// <returns> Master volume value </returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static float MasterVolume() => Volume[(byte)Volumes.Master];
      /// <summary>
      /// Retrieve current SFX volume
      /// </summary>
      /// <returns> SoundEffect volume value </returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static float SFXVolume() => Volume[(byte)Volumes.SFX];
      /// <summary>
      /// Retrieve current Music volume
      /// </summary>
      /// <returns> Music volume value </returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static float MusicVolume() => Volume[(byte)Volumes.Music];
      /// <summary>
      /// Retrieve current loaded Song
      /// </summary>
      /// <returns> Playing Song value </returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static Song PlayingSong() => CurrentSong;
      /// <summary>
      /// Check MediaPlayer Song slot
      /// </summary>
      /// <returns> 
      /// True if currently playing Song 
      /// -or- 
      /// False if not currently playing Song
      /// </returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool PlayingMusic() => MediaPlayer.State == MediaState.Playing;
      /// <summary>
      /// Check if MediaPlayer Song paused
      /// </summary>
      /// <returns>
      /// True if MediaPlayer paused
      /// -or-
      /// False if MediaPlayer playing
      /// </returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool MusicPaused() => MediaPlayer.State == MediaState.Paused;


      /// <summary>
      /// Set current Master volume
      /// </summary>
      /// <param name="volume"> New volume between 0.0f and 1.0f </param>
      public static void MasterVolume(float volume) {
        Volume[(byte)Volumes.Master] = MathHelper.Clamp(volume, 0.0f, 1.0f);
        MediaPlayer.Volume = Volume[(byte)Volumes.Music] * Volume[(byte)Volumes.Master];

        foreach (SoundEffectInstance Instance in LoopingSounds.Values) {
          Instance.Volume = Volume[(byte)Volumes.SFX] * Volume[(byte)Volumes.Master];
        }
      }

      /// <summary>
      /// Set current SFX volume
      /// </summary>
      /// <param name="volume"> New volume between 0.0f and 1.0f </param>
      public static void SFXVolume(float volume) {
        Volume[(byte)Volumes.SFX] = MathHelper.Clamp(volume, 0.0f, 1.0f);

        foreach (SoundEffectInstance Instance in LoopingSounds.Values) {
          Instance.Volume = Volume[(byte)Volumes.SFX] * Volume[(byte)Volumes.Master];
        }
      }

      /// <summary>
      /// Set current Music volume
      /// </summary>
      /// <param name="volume"> New volume between 0.0f and 1.0f </param>
      public static void MusicVolume(float volume) {
        Volume[(byte)Volumes.Music] = MathHelper.Clamp(volume, 0.0f, 1.0f);
        MediaPlayer.Volume = Volume[(byte)Volumes.Music] * Volume[(byte)Volumes.Master];
      }

      /// <summary>
      /// Unmute if currently muted, or mute if currently unmuted.
      /// </summary>
      public static void Mute() {
        Muted = !Muted;

        if (Muted) {
          MasterVolume(0f);
        } else {
          MasterVolume(Volume[(byte)Volumes.Master]);
        }
      }
    }

    /// <summary>
    /// Global class for managing: Fonts, Shaders, Animations, and Textures.
    /// </summary>
    public static class VisualManager {
      /// <summary>
      /// Global Animations index
      /// </summary>
      public enum Animations : byte {
        Coin = 0,
        FallingSpikeRegrow = 1,
        SpringRetraction = 2,
        PlayerDefault = 3,
        PlayerTeleport = 4,
        PlayerTeleportGold = 5,
        PlayerTeleportHoney = 6,
        Quicksand = 7,
        QuicksandDeep = 8,
        UIAButton = 9,
        SaveDisk = 10,
        ScreenshotSave = 11
      }
      
      /// <summary>
      /// Global Fonts index
      /// </summary>
      public enum Fonts : byte {
        EndLevelScreen = 0,
        PauseMenu = 1,
        SmallPauseMenu = 2
      }


      private static readonly Dictionary<string, Texture2D> TextureCache = new();
      public static AnimationData[] AnimationsData = new AnimationData[12];
      public static SpriteFont[] SpriteFonts = new SpriteFont[3];
      public static Texture2D[] ColorTextures = new Texture2D[5];
      public static readonly SpriteTransform[] DirectionRotations = [new(0f, SpriteEffects.FlipHorizontally, Vector2.One), SpriteTransform.Default, new(-MathHelper.PiOver2, SpriteEffects.None, Vector2.One), new(MathHelper.PiOver2, SpriteEffects.FlipVertically, Vector2.One)];
      public static GraphicsDevice GraphicsDevice;
      public static ContentManager Content;
      public static Effect BrightnessEffect = null;
      public static SpriteFont FallbackFont;
      private static Texture2D MissingTexture = null;
      public static TmxMap CurrentMap;


      /// <summary>
      /// Load initial Graphics Features
      /// </summary>
      /// <param name="graphicsDevice"> Graphics Resource Builder </param>
      /// <param name="content"> Content Pipeline Manager </param>
      /// <exception cref="ContentLoadException"></exception>
      public static void Initialize(GraphicsDevice graphicsDevice, ContentManager content) {
        GraphicsDevice = graphicsDevice;
        Content = content;

        try {
          MissingTexture = Content.Load<Texture2D>(Paths.FallbackTexturePath);
        } catch (ContentLoadException) {
          Console.WriteLine("[CRITICAL ERROR]: Missing texture Fallback not found");
          Console.WriteLine("[CRITICAL ERROR]: Creating color texture as Fallback.");
          MissingTexture = new(graphicsDevice, 1, 1);
          MissingTexture.SetData([Color.Magenta]);
        }

        SetupColorTextures();
        SetupFonts();

        try {
          BrightnessEffect = Content.Load<Effect>(Paths.EffectPath);
        } catch (ContentLoadException) {
          throw new Exception("[CRITICAL ERROR]: Failed to load Brightness Effect.");
        }
      }

      /// <summary>
      /// Load Light-Weight Color Textures for Rectangle/Screen fills.
      /// </summary>
      public static void SetupColorTextures() {
        ColorTextures[(int)Colors.Black] = new(GraphicsDevice, 1, 1);
        ColorTextures[(int)Colors.White] = new(GraphicsDevice, 1, 1);
        ColorTextures[(int)Colors.DarkPurple] = new(GraphicsDevice, 1, 1);
        ColorTextures[(int)Colors.Pink] = new(GraphicsDevice, 1, 1);
        ColorTextures[(int)Colors.Tan] = new(GraphicsDevice, 1, 1);

        ColorTextures[(int)Colors.Black].SetData([Color.Black]);
        ColorTextures[(int)Colors.White].SetData([Color.White]);
        ColorTextures[(int)Colors.DarkPurple].SetData([new Color(94, 64, 108, 1)]);
        ColorTextures[(int)Colors.Pink].SetData([Color.Pink]);
        ColorTextures[(int)Colors.Tan].SetData([Color.Tan]);
      }

      /// <summary>
      /// Import and save initial Animations into cache.
      /// </summary>
      /// <param name="gameType"> Sub-Game Type </param>
      public static void SetupAnimations(GameType gameType) {
        if (gameType == GameType.Platformer) {
          AnimationsData[(int)Animations.FallingSpikeRegrow] = new(GetTexture("Animations/Spikes/FallingSpikeGrow"), TILE_VECTOR, 7, 250.0f, false, true);

          AnimationsData[(int)Animations.PlayerDefault] = new(GetTexture("Images/PlayerImages/Player"), TILE_VECTOR, 1, 0f, false, false);
          AnimationsData[(int)Animations.PlayerTeleport] = new(GetTexture("Animations/Player/PlayerTeleport"), TILE_VECTOR, 12, 150f, false, false);
          AnimationsData[(int)Animations.PlayerTeleportGold] = new(GetTexture("Animations/Player/PlayerTeleportGold"), TILE_VECTOR, 12, 150f, false, false);
          AnimationsData[(int)Animations.PlayerTeleportHoney] = new(GetTexture("Animations/Player/PlayerTeleportHoney"), TILE_VECTOR, 12, 150f, false, false);

          AnimationsData[(int)Animations.SpringRetraction] = new(GetTexture("Animations/Interactable/Spring"), TILE_VECTOR, 10, 100.0f, false, true);
          AnimationsData[(int)Animations.Quicksand] = new(GetTexture("Animations/Interactable/Quicksand"), TILE_VECTOR, 4, 200.0f, false, true);
          AnimationsData[(int)Animations.QuicksandDeep] = new(GetTexture("Animations/Interactable/QuicksandDeep"), TILE_VECTOR, 17, 500, true, false);

          AnimationsData[(int)Animations.Coin] = new(GetTexture("Animations/Collectable/Coin"), TILE_VECTOR, 9, 144.0f, true, false);
        } else if (gameType == GameType.PlatformerUI) {
          AnimationsData[(int)Animations.UIAButton] = new(GetTexture("Animations/UIAnimations/Input/ButtonA"), TILE_VECTOR, 2, 150f, true, true);
          AnimationsData[(int)Animations.SaveDisk] = new(GetTexture("Images/UIImages/SaveDisk"), new(TILE_SIZE, TILE_SIZE), 10, 1000f, false, false);
          AnimationsData[(int)Animations.ScreenshotSave] = new();
        }
      }
      
      /// <summary>
      /// Load all fonts from ContentPipeline
      /// </summary>
      /// <exception cref="ContentLoadException"></exception>
      public static void SetupFonts() {
        try {
          FallbackFont = Content.Load<SpriteFont>(Paths.FallbackFontPath);
        } catch (ContentLoadException Exception) {
          throw new Exception("[CRITICAL ERROR]: Failed to load Fallback font.", Exception);
        }

        SpriteFonts[0] = GetFont("Fonts/EndLevelScreen");
        SpriteFonts[1] = GetFont("Fonts/PauseMenu");
        SpriteFonts[2] = GetFont("Fonts/PauseMenu16");
      }


      /// <summary>
      /// Import (and optionally cache) a 2D Texture
      /// </summary>
      /// <param name="path"> Texture file path </param>
      /// <param name="cache"> Whether to cache </param>
      /// <returns> Imported Texture2D or FallbackTexture if import failed </returns>
      public static Texture2D GetTexture(string path, bool cache = true) {
        try {
          if (!cache)
            return Content.Load<Texture2D>(path);

          string CacheKey = NormalizePath(path);

          if (TextureCache.TryGetValue(CacheKey, out Texture2D Texture))
            return Texture;

          Texture = Content.Load<Texture2D>(path);
          TextureCache[CacheKey] = Texture;

          return Texture;
        } catch (ContentLoadException Exception) {
          Console.WriteLine($"[WARNING]: Unable to load texture: {Path.GetFileName(path)} - {Exception.Message}");
          Console.WriteLine("[WARNING]: Defaulting to Fallback texture.");

          return MissingTexture;
        }
      }

      /// <summary>
      /// Fetch SpriteFont from ContentPipeline
      /// </summary>
      /// <param name="path"> SpriteFont file path </param>
      /// <returns> Imported SpriteFont or FallBackFont if import failed </returns>
      public static SpriteFont GetFont(string path) {
        try {
          return Content.Load<SpriteFont>(path);
        } catch (ContentLoadException Exception) {
          Console.WriteLine($"[WARNING]: Unable to load font: {Path.GetFileName(path)} - {Exception.Message}");
          Console.WriteLine("[WARNING]: Defaulting to Fallback Arial Font.");

          return FallbackFont;
        }
      } 


      /// <summary>
      /// Load specified TMX Map into RAM
      /// </summary>
      /// <param name="path"> Map file path </param>
      /// <exception cref="ContentLoadException"></exception>
      public static void LoadTmxMap(string path) {
        try {
          CurrentMap = Content.Load<TmxMap>(path);
        } catch (ContentLoadException Exception) {
          throw new ContentLoadException($"[CRITICAL ERROR]: Failed to load Map: {Path.GetFileName(path)} - {Exception.Message}");
        }
      }


      /// <summary>
      /// Convert file path to culture-normalized string
      /// </summary>
      /// <param name="path"> Current path </param>
      /// <returns></returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static string NormalizePath(string path) => path.Replace('\\', '/').ToLowerInvariant();


      /// <summary>
      /// Unload specific Texture.
      /// </summary>
      /// <param name="path"> Texture file path </param>
      public static void UnloadTexture(string path) {
        string CacheKey = NormalizePath(path);

        if (TextureCache.TryGetValue(CacheKey, out Texture2D Texture)) {
          Texture.Dispose();
          TextureCache.Remove(CacheKey);
        }
      }

      /// <summary>
      /// Unload all textures from cache.
      /// </summary>
      public static void UnloadAllTextures() {
        foreach (Texture2D Texture in TextureCache.Values) {
          Texture.Dispose();
        }

        TextureCache.Clear();
      }
    }
  }

}
