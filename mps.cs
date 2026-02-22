/*
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Cube_Run_C_.Assets;
using static Cube_Run_C_.Assets.SoundManager;
using static Cube_Run_C_.Assets.VisualManager;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Globals.PlayerData;
using static Cube_Run_C_.Sprites;
using static Cube_Run_C_.Tools;
using static Cube_Run_C_.Tools.BitMask;
using static Cube_Run_C_.Tools.Engine;
using static Cube_Run_C_.UI;
using RectangleF = System.Drawing.RectangleF;


namespace Cube_Run_C_ {
  public struct LevelStats {
    public ushort Deaths;
    public ushort Coins;
    public byte LifeBlocks;


    public LevelStats(ushort deaths, ushort coins, byte lifeBlocks) {
      this.Deaths = deaths;
      this.Coins = coins;
      this.LifeBlocks = lifeBlocks;
    }
    

    public static readonly LevelStats Zero = new(0, 0, 0);
  }
  
  public readonly struct LevelDimensions {
    public readonly int PixelWidth;
    public readonly int PixelHeight;
    public readonly int TileWidth;
    public readonly int TileHeight;


    public LevelDimensions(int pixelWidth, int pixelHeight, int tileWidth, int tileHeight) {
      this.PixelWidth = pixelWidth;
      this.PixelHeight = pixelHeight;
      this.TileWidth = tileWidth;
      this.TileHeight = tileHeight;
    }


    public static readonly LevelDimensions Zero = new(0, 0, 0, 0);
  }

  public struct EnemyCollisionSprites {
    public Sprite EnemyA;
    public Sprite EnemyB;
    public Sprite WallCollision;


    public EnemyCollisionSprites(Sprite enemyA, Sprite enemyB, Sprite wallCollision) {
      this.EnemyA = enemyA;
      this.EnemyB = enemyB;
      this.WallCollision = wallCollision;
    }


    public static readonly EnemyCollisionSprites Empty = new(null, null, null);
  }

  public readonly struct LevelConfig {
    public readonly uint[] PowerDurations = [5000, 5000, 5000, 5000, 50, 50, 50, 50];
    public readonly uint FallingSpikeRegrow;
    public readonly ushort Gravity;
    public readonly ushort EnemySpeed;
    public readonly ushort BulletSpeed;
    public readonly ushort FallingSpikeSpeed;
    public readonly float CanonFiringRate;
    public readonly float LanternWidth;


    public LevelConfig(uint[] powerDurations, uint fallingSpikeRegrow, ushort gravity, ushort enemySpeed, ushort bulletSpeed, ushort fallingSpikeSpeed, float canonFiringRate, float lanternWidth) {
      this.PowerDurations = powerDurations;
      this.FallingSpikeRegrow = fallingSpikeRegrow;
      this.Gravity = gravity;
      this.EnemySpeed = enemySpeed;
      this.BulletSpeed = bulletSpeed;
      this.FallingSpikeSpeed = fallingSpikeSpeed;
      this.CanonFiringRate = canonFiringRate;
      this.LanternWidth = lanternWidth;
    }
    

    public static readonly LevelConfig Default = new([5000, 5000, 5000, 5000, 50, 50, 50, 50], 5000, 800, 90, 150, 250, 5.0f, 90.0f);
  }


  public static class LevelController {
    public static void BeginLevel() {
      BrightnessEffect.Parameters["LanternEnabled"].SetValue(false);
      PlaySong();
      Level.Setup($"Maps/Platformers/{LevelData.Difficulty}Mode/{CurrentLevel}");
    }

    public static void EndLevel() {
      if (IsSet(Level.FlagStats, (byte)LevelStatFlags.Transitioning))
        return;

      Set(ref Level.FlagStats, (byte)LevelStatFlags.Transitioning, true);
      Set(ref Level.FlagStats, (byte)LevelStatFlags.Active, false);

      CurrentLevel++;
      StopMusic();
      Level.Reset();
      EndLevelScreen.Display();
    }
  }

  public static class Level {
    private static readonly Dictionary<string, TileLayerProperty> TileLayerProperties = new() {
      ["Terrain"] = new(ZLayers.Main, [Groups.All, Groups.Collidable]),
      ["Enemies"] = new(ZLayers.Main, [Groups.All, Groups.Damage]),
      ["Foreground"] = new(ZLayers.Foreground, [Groups.All]),
      ["Background"] = new(ZLayers.Background, [Groups.All])
    };
    private static readonly Dictionary<int, Texture2D> GidTextureCache = new();
    private static readonly Dictionary<int, Texture2D> GidLookupCache = new();
    private static List<Groups> AssignedGroups = [Groups.All];
    private static List<Sprite> SpriteList = [];
    private static readonly Vector2[] SpawnOrbPositions = [Vector2.Zero, Vector2.Zero, Vector2.Zero];
    public static readonly Vector2[] TeleportLocations = [Vector2.Zero, Vector2.Zero, Vector2.Zero];
    public static readonly string[] TileLayers = ["Terrain", "Enemies", "Foreground", "Backgroud"];
    public static readonly string[] ObjectLayers = ["Interactable", "Interactable Enemies", "Collectable", "Moving Objects"];
    private static Sprite CollisionResult;
    private static SwitchBlock ScrapGoal;
    public static LevelStats Stats = LevelStats.Zero;
    public static LevelStats MaxStats = LevelStats.Zero;
    private static EnemyCollisionSprites EnemyCollisions;
    private static RectangleF PlayerRect;
    private static string ImagePath = string.Empty;
    private static byte GoalFragments = 0;
    public static byte FlagStats = 0x00;


    public static void Reset() {
      Camera.Reset(Color.Teal);
      
      Globals.Player = null;
      LevelData.Gravity = 800;
      LevelData.FSBlockPositions.Clear();
      LevelData.Dimensions = LevelDimensions.Zero;

      for (int Index = 0; Index < SpriteGroups.Length; Index++) {
        SpriteGroups[Index].Clear();
      }

      DestructibleSprites.Clear();
      GidTextureCache.Clear();
      GidLookupCache.Clear();

      TeleportLocations[0] = Vector2.Zero;
      TeleportLocations[1] = Vector2.Zero;
      TeleportLocations[2] = Vector2.Zero;
      CollisionResult = null;
      ScrapGoal = null;
      EnemyCollisions = EnemyCollisionSprites.Empty;
      Stats = LevelStats.Zero;
      MaxStats = LevelStats.Zero;
      GoalFragments = 0;
      FlagStats = 0x00;
    }


    public static void SetupLevelVariables() {
      LevelConfig CurrentConfig = CurrentMap.Configuration;

      LevelData.FallingSpikeRegrow = CurrentConfig.FallingSpikeRegrow;
      LevelData.Gravity = CurrentConfig.Gravity;
      LevelData.EnemySpeed = CurrentConfig.EnemySpeed;
      LevelData.BulletSpeed = CurrentConfig.BulletSpeed;
      LevelData.FallingSpikeSpeed = CurrentConfig.FallingSpikeSpeed;
      LevelData.CanonFiringRate = CurrentConfig.CanonFiringRate;

      LanternLightWidth = CurrentConfig.LanternWidth;

      for (int Index = 0; Index < PowerDurations.Length; Index++) {
        PowerDurations[Index] = CurrentConfig.PowerDurations[Index];
      }
    }

    public static void Setup(string mapPath) {
      LoadTmxMap(mapPath);
      SetupLevelVariables();
      
      TmxObject Object;
      Texture2D Image = null;
      byte SpawnOrbIndex = 0;

      SpawnOrbPositions[0] = Vector2.Zero;
      SpawnOrbPositions[1] = Vector2.Zero;
      SpawnOrbPositions[2] = Vector2.Zero;
      LevelData.Dimensions = new(CurrentMap.Width * TILE_SIZE, CurrentMap.Height * TILE_SIZE, CurrentMap.Width, CurrentMap.Height);

      for (int Index = 0; Index < CurrentMap.Tilesets.Count; Index++) {
        SetupTileset(CurrentMap.Tilesets[Index]);
      }

      for (int Index = 0; Index < CurrentMap.Layers.Count; Index++) {
        SetupTileLayer(CurrentMap.Layers[Index]);
      }

      for (int Index = 0; Index < CurrentMap.ObjectGroups.Count; Index++) {
        SetupObjectLayer(CurrentMap.ObjectGroups[Index]);
      }

      Set(ref FlagStats, (byte)LevelStatFlags.Active, true);
      Globals.Player = new(SpawnOrbPositions[RNG.Next(0, SpawnOrbIndex)]);


      void SetupTileset(TmxTileset tileset) {
        ImagePath = string.Empty;

        if (!string.IsNullOrEmpty(tileset.ImageSource)) {                        
          for (int Index = 0; Index < tileset.TileCount; Index++) {                
            if (!tileset.Tiles.ContainsKey(Index))
              GidTextureCache[tileset.FirstGid + Index] = GetTexture(DetermineTexturePath(tileset.ImageSource));
          }
        }
    
        foreach (KeyValuePair<int, TmxTile> Tile in tileset.Tiles) {
          ImagePath = Tile.Value.ImageSource;

          if (string.IsNullOrEmpty(ImagePath))
            continue;

          GidTextureCache[tileset.FirstGid + Tile.Key] = GetTexture(DetermineTexturePath(ImagePath));
        }
      }

      void SetupTileLayer(TmxLayer tileLayer) {
        string LayerName = tileLayer.Name;
        int LayerWidth = tileLayer.Width;
        int LayerHeight = tileLayer.Height;
        int[] LayerData = tileLayer.Data;
          
        for (int Y = 0; Y < LayerHeight; Y++) {
          for (int X = 0; X < LayerWidth; X++) {
            int Gid = LayerData[Y * LayerWidth + X];

            if (Gid == 0)
              continue;

            Image = GetTextureFromGid(Gid);
                        
            if (Image == null)
              continue;

            if (LayerName == "Decoration") {
              Camera.Add(new BasicSprite(Image, new(X * TILE_SIZE, Y * TILE_SIZE)));
            } else {
              if (!TileLayerProperties.TryGetValue(LayerName, out TileLayerProperty LayerProperty))
                continue;

              _ = new Sprite(Image, new(X * TILE_SIZE, Y * TILE_SIZE), LayerProperty.Groups, LayerProperty.Z);
            }
          }
        }
      }

      

      void SetupObjectLayer(TmxObjectGroup objectGroup) {
        for (int Index = 0; Index < objectGroup.Objects.Count; Index++) {
          Object = objectGroup.Objects[Index];
          string Name = Object.Name ?? "";
          
          if (objectGroup.Name == "Interactable Enemies" && Name == "FallingSpikeBlock") {
            if (Object.Gid.HasValue)
              Image = GetTextureFromGid(Object.Gid.Value);

            Vector2 Position = new(Object.X, Object.Y - TILE_SIZE);
            _ = new Sprite(Image, Position, [Groups.All, Groups.Damage], ZLayers.Main);
            LevelData.FSBlockPositions.Add(Position);
          }
        }

        for (int Index = 0; Index < objectGroup.Objects.Count; Index++) {
          Object = objectGroup.Objects[Index];

          AssignedGroups.Clear();
          AssignedGroups.Add(Groups.All);

          Image = null;
          Vector2 Position = new(Object.X, Object.Y - TILE_SIZE);
          string Name = Object.Name ?? "";

          if (objectGroup.Name == "Interactable Enemies" && Name == "FallingSpikeBlock") {
            continue;
          }

          if (Object.Gid.HasValue) 
            Image = GetTextureFromGid(Object.Gid.Value);

          switch (objectGroup.Name) {
            case "Interactable":
              SetupInteractableObject(Object, ref AssignedGroups, ZLayers.Main, Position, Image, Object.Orientation, Name, IsSet(Object.Stats, (ushort)ObjectStats.Active), Object.Trim);
              break;
            case "Interactable Enemies":
              SetupInteractableEnemy(Object, Position, Object.Orientation, Name, IsSet(Object.Stats, (ushort)ObjectStats.Active), Image);
              break;
            case "Collectable":
              SetupCollectable(Object, ref AssignedGroups, ZLayers.Main, Position, Image, Object.Orientation, Name, IsSet(Object.Stats, (ushort)ObjectStats.Active), ref SpawnOrbIndex);
              break;
            case "Moving Object":
              break;
          }
        }
      }

      void SetupInteractableObject(TmxObject obj, ref List<Groups> assignedGroups, ZLayers layer, Vector2 position, Texture2D image, Directions faceDirection, string name, bool active, Fragment trim) {
        if (image == null) {
          Console.WriteLine("no image bruh");
          return;
        }
        
        switch (name) {
          case "StartBall":
            SpawnOrbPositions[SpawnOrbIndex] = position;
            SpawnOrbIndex++;
            break;
          case "EndBall":
            if (!active) {
              _ = new SwitchBlock(image, position, [Groups.All, Groups.Switch], layer, "Images/TileImages/OrbEnd", faceDirection);
              return;
            }

            assignedGroups.Add(Groups.Orb);
            break;
          case "EndFragmentBall":
            ScrapGoal = new(image, position, [Groups.All], layer, "Images/TileImages/OrbEnd", faceDirection);
            return;
          case "Teleport Start Location":
            _ = new Teleporter(image, position, (byte)obj.ID, active);
            return;
          case "Teleport End Location":
            TeleportLocations[obj.ID] = position;
            layer = ZLayers.BackgroundTiles;

            if (!active) {
              _ = new SwitchBlock(image, position, [Groups.All, Groups.Switch], layer, "Images/TileImages/TeleportLocationOn", faceDirection);
              return;
            }
            break;
          case "Changer":
            _ = new Changer(image, position, obj.Type, obj.RateChange, obj.SpeedChange, obj.DurationChange);
            return;
          case "SwitchBlock":
            bool Active = IsSet(obj.Stats, (byte)ObjectStats.Active);
            _ = new SwitchBlock(image, position, Active ? [Groups.All, Groups.Switch, Groups.Collidable] : [Groups.All, Groups.Switch], ZLayers.BackgroundTiles, $"Images/TileImages/SwitchBlock{(Active ? "Off" : "On")}", faceDirection);
            return;
          case "SwitchBlockOn":
            _ = new SwitchBlock(image, position, [Groups.All, Groups.Switch, Groups.Collidable], ZLayers.BackgroundTiles, "Images/TileImages/SwitchBlockOff", faceDirection);
            return;
          case "SwitchBlockOff":
            _ = new SwitchBlock(image, position, [Groups.All, Groups.Switch], ZLayers.BackgroundTiles, "Images/TileImages/SwitchBlockOn", faceDirection);
            return;
          case "Quicksand":
            _ = new Quicksand(new(AnimationsData[(int)(IsSet(Object.Stats, (ushort)ObjectStats.Deep) ? Animations.QuicksandDeep : Animations.Quicksand)]), position, IsSet(Object.Stats, (ushort)ObjectStats.Deep));
            return;
          case "Spring":
            _ = new Spring(position, faceDirection, IsSet(Object.Stats, (ushort)ObjectStats.Multi));
            return;
          case "Ladder":
            assignedGroups.Add(Groups.Ladder);
            break;
        }

        if (trim == Fragment.None) {
          _ = new Sprite(image, position, assignedGroups, layer, faceDirection);
        } else {
          _ = new TrimmedSprite(image, position, assignedGroups, layer, trim, faceDirection);
        }
      }

      void SetupInteractableEnemy(TmxObject obj, Vector2 position, Directions faceDirection, string name, bool active, Texture2D image) {
        switch (name) {
          case "Canon":
            _ = new Canon(position, faceDirection, IsSet(Object.Stats, (ushort)ObjectStats.Floor), IsSet(Object.Stats, (ushort)ObjectStats.Passthrough));
            break;
          case "DeathCube":
            _ = new DeathCube(position, IsSet(Object.Stats, (ushort)ObjectStats.Horizontal), active);
            break;
          case "FallingSpike":
            _ = new FallingSpike(position, IsSet(Object.Stats, (ushort)ObjectStats.Automatic), IsSet(Object.Stats, (ushort)ObjectStats.LimitedRange), faceDirection);
            break;
          case "Prowler":
            _ = new Prowler(position, IsSet(Object.Stats, (ushort)ObjectStats.Horizontal));
            break;
          case "SpikeOn":
            if (obj.Gid.HasValue)
              _ = new SwitchBlock(GetTextureFromGid(obj.Gid.Value), position, [Groups.All, Groups.Damage, Groups.Switch], ZLayers.Opaque, "Images/TileImages/SpikeOff", faceDirection);
            
            break;
          case "SpikeOff":
            if (obj.Gid.HasValue)
              _ = new SwitchBlock(GetTextureFromGid(obj.Gid.Value), position, [Groups.All, Groups.Switch], ZLayers.Opaque, "Images/TileImages/SpikeOn", faceDirection);
            break;
        }
      }

      void SetupCollectable(TmxObject obj, ref List<Groups> assignedGroups, ZLayers layer, Vector2 position, Texture2D image, Directions faceDirection, string name, bool active, ref byte spawnOrbIndex) {
        ImagePath = "Images/CollectableImages/";
        assignedGroups.Add(Groups.Item);

        switch (name) {
          case "Checkpoint":
            ImagePath += $"CheckpointOff{obj.ID + 1}";
            break;
          case "LifeBlock":
            _ = new LifeBlock(position, !active);
            MaxStats.LifeBlocks++;
            return;
          case "PowerInvincible":
            _ = new PowerUp(image, position, PlayerPowers.Invincibility, IsSet(Object.Stats, (ushort)ObjectStats.Respawn));
            return;
          case "PowerCanceller":
            _ = new PowerUp(image, position, obj.Power, false, true);
            return;
          case "Switch":
            ImagePath += "SwitchOff";
            assignedGroups.Add(Groups.Switch);
            break;
          case "Coin":
            Item Coin = new(new Animation(AnimationsData[(int)Animations.Coin]), position, assignedGroups, layer, "Images/CollectableImages/CoinOff", active);
            Coin.Animation.Play();
            MaxStats.Coins++;
            return;
          default:
            ImagePath += $"{name}Off";
            break;
        }

        _ = new Item(image, position, assignedGroups, layer, ImagePath, active, faceDirection);
      }
    }


    private static Texture2D GetTextureFromGid(int gid) {
      if (GidTextureCache.TryGetValue(gid, out Texture2D CachedTexture))
        return CachedTexture;

      if (GidTextureCache.TryGetValue(gid, out Texture2D Texture)) {
        GidLookupCache[gid] = Texture;
        return Texture;
      }
      
      Texture2D TextureMatch = null;
      int BestGid = 0;
         
      foreach (KeyValuePair<int, Texture2D> TexturePair in GidTextureCache) {
        if (TexturePair.Key <= gid && TexturePair.Key > BestGid) {
          BestGid = TexturePair.Key;
          TextureMatch = TexturePair.Value;
        }
      }

      if (TextureMatch != null) 
        GidLookupCache[gid] = TextureMatch;
      
      return TextureMatch;
    }

    private static string DetermineTexturePath(string sourcePath) {
      string LowerPath = sourcePath.ToLower();
      string FileName = Path.GetFileNameWithoutExtension(sourcePath);
        
      if (LowerPath.Contains("enemy")) {
        return $"Images/EnemyImages/{FileName}";
      } else if (LowerPath.Contains("collectable")) {
        return $"Images/CollectableImages/{FileName}";
      }
        
      return $"Images/TileImages/{FileName}";
    }



    private static void ActivateSwitch() {
      List<Sprite> TeleporterSprites = SpriteGroups[(int)Groups.Teleporter].SpriteList;
      List<Sprite> SwitchSprites = SpriteGroups[(int)Groups.Switch].SpriteList;

      for (int Index = 0; Index < SwitchSprites.Count; Index++) {
        if (SwitchSprites[Index] is LifeBlock LifeBlock) {
          LifeBlock.Activate(true);
          continue;
        }

        if (SwitchSprites[Index] is Item Item) {
          Item.Deactivate();
          continue;
        }

        if (SwitchSprites[Index] is DeathCube DeathCube) {
          DeathCube.Activate();
          continue;
        }

        if (TeleporterSprites.Contains(SwitchSprites[Index])) {
          if (SwitchSprites[Index] is Teleporter TeleportPortal) {
            TeleportPortal.Activate();
          } else {
            SwitchSprites[Index].Image = GetTexture("Images/TileImages/TeleportLocationOn");
          }

          continue;
        }

        if (SwitchSprites[Index] is not SwitchBlock Block)
          continue;

        Block.Image = GetTexture(Block.DeactivatorImagePath);

        switch (Path.GetFileName(Block.DeactivatorImagePath)) {
          case "SpikeOff":
            SpriteGroups[(int)Groups.Damage].Remove(Block);
            break;
          case "SpikeOn":
            SpriteGroups[(int)Groups.Damage].Add(Block);
            break;
          case "SwitchBlockOff":
            SpriteGroups[(int)Groups.Collidable].Remove(Block);
            break;
          case "SwitchBlockOn":
            SpriteGroups[(int)Groups.Collidable].Add(Block);
            break;
          case "OrbEnd":
            SpriteGroups[(int)Groups.Orb].Add(Block);
            break;
        }
      }
    }


    private static void CollectableCollision() {
      CollisionResult = SpriteGroups[(int)Groups.Item].OverlapsWith(PlayerRect);

      if (CollisionResult != null) {
        if (CollisionResult is LifeBlock LifeBlock && LifeBlock.Active) {
          LifeBlock.Activate(false);
          Stats.LifeBlocks++;
          return;
        }

        if (CollisionResult is PowerUp PowerSprite && !IsSet(PowerSprite.Stats, (byte)PowerSpriteFlags.Destroyed)) {
          if (IsSet(PowerSprite.Stats, (byte)PowerSpriteFlags.Canceller)) {
            Globals.Player.DeactivatePower(PowerSprite.Power);
          } else {
            Globals.Player.ActivatePower(PowerSprite.Power);
            PowerSprite.Destroy();
          }
        } else if (CollisionResult is Item Item && Item.Active) {
          string Name = Path.GetFileName(Item.DeactivatorImage);
          bool Destroy = false;

          switch (Name) {
            case "CoinOff":
              Coins++;
              Stats.Coins++;
              Destroy = true;
              PlaySound("Sounds/Effects/Coin", SoundData.Default);
              break;
            case "LanternOff":
              Globals.Player.Lantern(true);
              break;
            case "SwitchOff":
              ActivateSwitch();
              return;
          }

          if (Name.Contains("CheckpointOff"))
            Globals.Player.ActivateCheckpoint(Item.Rect.TopLeft(), ParseChar(Name[^1]));

          if (Destroy) {
            Item.Destroy();
          } else {
            Item.Deactivate();
          }
        }
      }
    }

    private static void InteractableCollision() {
      SpriteList = [];
      CollisionResult = SpriteGroups[(int)Groups.Orb].OverlapsWith(PlayerRect);

      if (CollisionResult != null) {
        if (CollisionResult is TrimmedSprite TrimmedOrb) {
          GoalFragments += (byte)(IsHalf(TrimmedOrb.FragmentType) ? 2 : 1);
          TrimmedOrb.Destroy();

          if (GoalFragments == 4) {
            ScrapGoal.Image = GetTexture(ScrapGoal.DeactivatorImagePath);
            SpriteGroups[(int)Groups.Orb].Add(ScrapGoal);
          }
        } else {
          LevelController.EndLevel();
        }

        return;
      }
 
      if (SpriteGroups[(int)Groups.Ladder].OverlapsWith(PlayerRect) != null)
        Globals.Player.MovementChange(PlayerStats.Ladder);

      CollisionResult = SpriteGroups[(int)Groups.Spring].OverlapsWith(PlayerRect);

      if (CollisionResult != null && CollisionResult is Spring Spring)
        Spring.Activate();

      for (int Index = 0; Index < SpriteGroups[(int)Groups.Teleporter].SpriteList.Count; Index++) {
        if (SpriteGroups[(int)Groups.Teleporter].SpriteList[Index] is Teleporter TeleportPortal && TeleportPortal.Active && Overlap(TeleportPortal, PlayerRect))
          Globals.Player.Teleport(TeleportPortal);
      }


      float SandTop = float.MaxValue;

      if (IsSet(SpriteGroups[(int)Groups.Quicksand].Properties, (byte)SpriteGroupProperties.UseQuery)) {
        SpriteGroups[(int)Groups.Quicksand].UpdateGrid();
        SpriteList = SpriteGroups[(int)Groups.Quicksand].Grid.Query(PlayerRect);
      } else {
        SpriteList = SpriteGroups[(int)Groups.Quicksand].SpriteList;
      }

      for (int Index = 0; Index < SpriteList.Count; Index++) {
        if (SpriteList[Index] is Quicksand Quicksand && Quicksand.Rect.IntersectsWith(PlayerRect)) {
          SandTop = MathF.Min(SandTop, Quicksand.Rect.Y);
          Quicksand.Overlap();

          if (Quicksand.Deep) {
            Set(ref Globals.Player.Stats, (ulong)PlayerStats.QuicksandDeep, true);
          } else {
            Set(ref Globals.Player.Stats, (ulong)PlayerStats.Quicksand, true);
          }
        }
      }

      Globals.Player.QuicksandTop = SandTop;
    }


    private static void EnemyCollision() {
      SpriteList = [];

      if (IsSet(SpriteGroups[(int)Groups.Damage].Properties, (byte)SpriteGroupProperties.UseQuery)) {
        SpriteGroups[(int)Groups.Damage].UpdateGrid();
        SpriteList = SpriteGroups[(int)Groups.Damage].Grid.Query(PlayerRect);
      } else {
        SpriteList = SpriteGroups[(int)Groups.Damage].SpriteList;
      }

      for (int Index = 0; Index < SpriteList.Count; Index++) {
        EnemyCollisions.EnemyA = SpriteList[Index];

        if (EnemyCollisions.EnemyA.Rect.IntersectsWith(PlayerRect)) {
          PlayerEnemyCollision(EnemyCollisions.EnemyA);
          continue;
        }

        CollisionResult = SpriteGroups[(int)Groups.Collidable].OverlapsWith(EnemyCollisions.EnemyA.Rect);

        if (CollisionResult != null) {
          if (EnemyCollisions.EnemyA is Bullet Bullet && !IsSet(Bullet.Stats, (byte)BulletFlags.Passthrough) && !IsSet(Bullet.Stats, (byte)BulletFlags.WallInvincibility)) {
            Bullet.Destroy();
          } else if (EnemyCollisions.EnemyA is FallingSpike FallingSpikeA) {
            FallingSpikeA.Collision(null);
          } else if (EnemyCollisions.EnemyA is DeathCube DeathCubeA) {
            DeathCubeA.Collision(CollisionResult);
          } else if (EnemyCollisions.EnemyA is Prowler ProwlerA) {
            HandleCollision(ProwlerA, CollisionResult, ProwlerA.Horizontal ? Directions.Horizontal : Directions.Vertical);
          }

          continue;
        } else {
          if (EnemyCollisions.EnemyA is Bullet Bullet && !IsSet(Bullet.Stats, (byte)BulletFlags.Passthrough))
            Set(ref Bullet.Stats, (byte)BulletFlags.WallInvincibility, false);
        }

        CollisionResult = SpriteGroups[(int)Groups.Quicksand].OverlapsWith(EnemyCollisions.EnemyA.Rect);

        if (CollisionResult != null) {
          if (EnemyCollisions.EnemyA is Bullet Bullet && !IsSet(Bullet.Stats, (byte)BulletFlags.Passthrough) && !IsSet(Bullet.Stats, (byte)BulletFlags.WallInvincibility)) {
            Bullet.Destroy();
          } else if (EnemyCollisions.EnemyA is FallingSpike FallingSpikeA) {
            FallingSpikeA.Collision(null);
          }
        }

        for (int Navigator = 0; Navigator < SpriteList.Count; Navigator++) {
          EnemyCollisions.EnemyB = SpriteList[Navigator];

          if (EnemyCollisions.EnemyA == EnemyCollisions.EnemyB || !EnemyCollisions.EnemyA.Rect.IntersectsWith(EnemyCollisions.EnemyB.Rect))
            continue;

          if (EnemyCollisions.EnemyA is Bullet BulletA) {
            if (EnemyCollisions.EnemyB is Bullet BulletB) {
              BulletB.Destroy();
            } else if (EnemyCollisions.EnemyB is DeathCube DeathCubeB) {
              DeathCubeB.Destroy();
            } else if (EnemyCollisions.EnemyB is FallingSpike FallingSpikeB) {
              FallingSpikeB.Collision(BulletA);
            }

            BulletA.Destroy();
            break;
          } else if (EnemyCollisions.EnemyA is DeathCube DeathCubeA) {
            if (EnemyCollisions.EnemyB is Bullet BulletB) {
              DeathCubeA.Destroy();
              BulletB.Destroy();
            } else if (EnemyCollisions.EnemyB is not DeathCube && EnemyCollisions.EnemyB is not Prowler) {
              DeathCubeA.Collision(EnemyCollisions.EnemyB);
            }
          } else if (EnemyCollisions.EnemyA is Prowler ProwlerA) {
            if (EnemyCollisions.EnemyB is Bullet BulletB) {
              ProwlerA.Destroy();
              BulletB.Destroy();
            } else if (EnemyCollisions.EnemyB is not DeathCube && EnemyCollisions.EnemyB is not Prowler) {
              HandleCollision(ProwlerA, EnemyCollisions.EnemyB, ProwlerA.Horizontal ? Directions.Horizontal : Directions.Vertical);
            }
          } else if (EnemyCollisions.EnemyA is FallingSpike FallingSpikeA) {
            if (EnemyCollisions.EnemyB is Bullet BulletB) {
              FallingSpikeA.Collision(BulletB);
            } else if (EnemyCollisions.EnemyB is DeathCube DeathCubeB) {
              FallingSpikeA.Collision(DeathCubeB);
            } else if (EnemyCollisions.EnemyB is Prowler ProwlerB) {
              FallingSpikeA.Collision(ProwlerB);
            } else if (EnemyCollisions.EnemyB is FallingSpike FallingSpikeB) {
              FallingSpikeA.Collision(FallingSpikeB);
            } else {
              FallingSpikeA.Collision(null);
            }
          }
        }
      }
    }

    private static void PlayerEnemyCollision(Sprite enemy) {
      if (IsSet(Globals.Player.Stats, (uint)PlayerStats.Shielding)) {
        
      } else if (IsSet(Globals.Player.Stats, (uint)PlayerStats.Invincibility)) {
        Coins += 1;

        if (enemy is Bullet || enemy is FallingSpike || enemy is DeathCube || enemy is Prowler)
          enemy.Destroy();

        return;
      } else {
        Stats.Deaths++;
        Globals.Player.Death();
      }

      if (enemy is Bullet BulletA) {
        BulletA.Destroy();
      } else if (enemy is FallingSpike FallingSpikeA) {
        FallingSpikeA.Collision(Globals.Player);
      }
    }


    public static void Update(float deltaTime) {
      for (int Index = 0; Index < SpriteGroups.Length; Index++) {
        SpriteGroups[Index].Update(deltaTime);
      }
      for (int Index = 0; Index < DestructibleSprites.Count; Index++) {
        if (DestructibleSprites[Index] is FallingSpike FallingSpike && IsSet(FallingSpike.Stats, (byte)FallingSpikeFlags.Destroyed))
          FallingSpike.Update(deltaTime);
      }

      Globals.Player.Update(deltaTime);
      PlayerRect = Globals.Player.Rect;

      CollectableCollision();
      InteractableCollision();
      EnemyCollision();
    }
  }
}
*/