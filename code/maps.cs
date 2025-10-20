using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Cube_Run_C_.Tools;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Tools.Assets;
using static Cube_Run_C_.Tools.Engine;
using static Cube_Run_C_.Tools.BitMask;
using static Cube_Run_C_.Globals.PlayerData;
using RectangleF = System.Drawing.RectangleF;


namespace Cube_Run_C_ {
  public static class LevelController {
    public static void BeginLevel() {
      Level.Reset();
      Level.Setup($"Content/Maps/Platformers/{CurrentLevel}.tmx");
    }
  }


  public static class Level {
    private static readonly Dictionary<string, (ZLayers, List<Groups>)> TileLayerProperties = new() {
      ["Terrain"] = (ZLayers.Main, new() { Groups.All, Groups.Collidable }),
      ["Enemies"] = (ZLayers.Main, new() { Groups.All, Groups.Damage }),
      ["Foreground"] = (ZLayers.Foreground, new() { Groups.All }),
      ["Background"] = (ZLayers.Background, new() { Groups.All })
    };
    private static Dictionary<int, Texture2D> GidTextureCache = new();
    public static Vector2[] TeleportLocations = [Vector2.Zero, Vector2.Zero, Vector2.Zero];
    public static readonly string[] TileLayers = ["Terrain", "Enemies", "Foreground", "Backgroud"];
    public static readonly string[] ObjectLayers = ["Interactable", "Interactable Enemies", "Collectable", "Moving Objects"];
    private static RectangleF PlayerRect;
    public static ushort Gravity = 800;


    public static void Reset() {
      Camera.Reset(Color.Teal);

      Globals.Player = null;
      for (int Index = 0; Index < SpriteGroups.Length; Index++) {
        SpriteGroups[Index].Clear();
      }

      GidTextureCache.Clear();
      TeleportLocations = [Vector2.Zero, Vector2.Zero, Vector2.Zero];
      Gravity = 800;
      Set(ref GlobalStats, (byte)GlobalFlags.LevelActive, false);
    }


    public static void Setup(string mapPath) {
      XDocument TmxMap = XDocument.Load(mapPath);
      List<Vector2> SpawnOrbPositions = new();
      XElement[] MapTilesets = [.. TmxMap.Descendants("tileset")];
      XElement[] MapTileLayers = [.. TmxMap.Descendants("layer")];
      XElement[] MapObjectLayers = [.. TmxMap.Descendants("objectgroup")];
      Dimensions LevelDimensions = new(int.Parse(TmxMap.Root.Attribute("width").Value), int.Parse(TmxMap.Root.Attribute("height").Value));
      string MapDirectory = Path.GetDirectoryName(mapPath);


      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      string GetObjectProperty(IEnumerable<XElement> objectProperties, string propertyName) => objectProperties.FirstOrDefault(property => property.Attribute("name").Value == propertyName)?.Attribute("value").Value;


      string DetermineSubDirectory(string sourcePath) {
        string LowerPath = sourcePath.ToLower();

        if (LowerPath.Contains("enemy")) {
          return "EnemyImages";
        } else if (LowerPath.Contains("collectable")) {
          return "CollectableImages";
        }
        
        return "TileImages";
      }

      void SetupTileset(XElement tileset) {
        foreach (XElement tile in XDocument.Load(Path.GetFullPath(Path.Combine(MapDirectory, tileset.Attribute("source").Value))).Root.Elements("tile")) {
          XElement TileImageElement = tile.Element("image");

          if (TileImageElement == null) 
            continue;

          string SourcePath = TileImageElement.Attribute("source").Value;
          string SubDirectory = DetermineSubDirectory(SourcePath);
          
          GidTextureCache[int.Parse(tileset.Attribute("firstgid").Value) + int.Parse(tile.Attribute("id").Value)] = GetTexture($"Images/{SubDirectory}/{Path.GetFileNameWithoutExtension(SourcePath)}");
        }
      }

      void SetupTileLayer(XElement tileLayer) {
        string[] Gids = tileLayer.Element("data").Value.Trim().Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        (ZLayers, List<Groups>) LayerProperty = TileLayerProperties[tileLayer.Attribute("name").Value];
        Dimensions LayerDimensions = new(int.Parse(tileLayer.Attribute("width").Value), int.Parse(tileLayer.Attribute("height").Value));

        for (int Y = 0; Y < LayerDimensions.Height; Y++) {
          for (int X = 0; X < LayerDimensions.Width; X++) {
            int Gid = int.Parse(Gids[Y * LayerDimensions.Width + X]);

            if (Gid == 0) continue;

            new Sprite(GidTextureCache.OrderByDescending(kvp => kvp.Key).First(kvp => kvp.Key <= Gid).Value, new(X * TILE_SIZE, Y * TILE_SIZE), LayerProperty.Item2, LayerProperty.Item1);
          }
        }
      }

      void SetupObjectLayer(XElement objectLayer) {
        string LayerName = objectLayer.Attribute("name")?.Value ?? "";

        foreach (XElement Object in objectLayer.Elements("object")) {
          IEnumerable<XElement> ObjectProperties = Object.Element("properties")?.Elements("property") ?? [];
          List<Groups> AssignedGroups = new() { Groups.All };
          ZLayers Layer = ZLayers.Main;
          XAttribute GidAttribute = Object.Attribute("gid");
          Vector2 Position = new(int.Parse(Object.Attribute("x").Value), int.Parse(Object.Attribute("y").Value) - TILE_SIZE);
          Texture2D Image = null;
          Directions FaceDirection = StringToDirection((GetObjectProperty(ObjectProperties, "Orientation") ?? "L")[0]);
          string Name = Object.Attribute("name")?.Value ?? "";
          bool Active = bool.Parse(GetObjectProperty(ObjectProperties, "Active") ?? "true");


          if (GidAttribute != null && GidTextureCache.TryGetValue(int.Parse(GidAttribute.Value), out Texture2D Texture))
            Image = Texture;

          switch (LayerName) {
            case "Interactable":
              switch (Name) {
                case "StartBall":
                  SpawnOrbPositions.Add(Position);
                  break;
                case "EndBall":
                  if (!Active) {
                    new SwitchBlock(Image, Position, new() { Groups.All, Groups.Switch }, ZLayers.Main, "Images/TileImages/OrbEnd", FaceDirection);
                    continue;
                  }

                  AssignedGroups.Add(Groups.Orb);
                  break;
                case "Teleport Start Location":
                  new Teleporter(Image, Position, byte.Parse(GetObjectProperty(ObjectProperties, "ID")), Active);
                  continue;
                case "Teleport End Location":
                  TeleportLocations[int.Parse(GetObjectProperty(ObjectProperties, "ID"))] = Position;

                  if (!Active) {
                    new SwitchBlock(Image, Position, new() { Groups.All, Groups.Switch }, ZLayers.Main, "Images/TileImages/TeleportLocationOn", FaceDirection);
                    continue;
                  }

                  break;
                case "SwitchBlockOn":
                  new SwitchBlock(Image, Position, new() { Groups.All, Groups.Switch, Groups.Collidable }, ZLayers.Main, "Images/TileImages/SwitchBlockOff", FaceDirection);
                  continue;
                case "SwitchBlockOff":
                  new SwitchBlock(Image, Position, new() { Groups.All, Groups.Switch }, ZLayers.Main, "Images/TileImages/SwitchBlockOn", FaceDirection);
                  continue;
                case "Ladder":
                  AssignedGroups.Add(Groups.Ladder);
                  break;
              }

              new Sprite(Image, Position, AssignedGroups, Layer, FaceDirection);
              break;
            case "Interactable Enemies":
              switch (Name) {
                case "DeathCube":
                  new DeathCube(Position, LevelData.EnemySpeed, bool.Parse(GetObjectProperty(ObjectProperties, "Horizontal")), Active);
                  break;
                case "Prowler":
                  new Prowler(Position, LevelData.EnemySpeed, bool.Parse(GetObjectProperty(ObjectProperties, "Horizontal")));
                  break;
                case "SpikeOn":
                  new SwitchBlock(Image, Position, new() { Groups.All, Groups.Damage, Groups.Switch }, ZLayers.Opaque, "Images/TileImages/SpikeOff", FaceDirection);
                  break;
                case "SpikeOff":
                  new SwitchBlock(Image, Position, new() { Groups.All, Groups.Switch }, ZLayers.Opaque, "Images/TileImages/SpikeOn", FaceDirection);
                  break;
              }

              break;
            case "Collectable":
              string DeactivatorImagePath = "";
              AssignedGroups.Add(Groups.Item);
              
              switch (Name) {
                case "LifeBlock":
                  new LifeBlock(Position, !Active);
                  break;
                case "Switch":
                  DeactivatorImagePath = "Images/CollectableImages/SwitchOff";
                  AssignedGroups.Add(Groups.Switch);
                  break;
                case "Checkpoint":
                  DeactivatorImagePath = $"Images/CollectableImages/CheckpointOff{int.Parse(GetObjectProperty(ObjectProperties, "ID")) + 1}";
                  break;
              }

              new Item(Image, Position, AssignedGroups, Layer, DeactivatorImagePath, Active, FaceDirection);
              break;
            case "Moving Object":

              break;
          }
        }
      }


      Set(ref GlobalStats, (byte)GlobalFlags.LevelActive, true);
      LevelData.Dimensions.Item1 = LevelDimensions;
      LevelData.Dimensions.Item2 = new(LevelDimensions.Width * TILE_SIZE, LevelDimensions.Height * TILE_SIZE);

      for (int Index = 0; Index < MapTilesets.Length; Index++) {
        SetupTileset(MapTilesets[Index]);
      }
      for (int Index = 0; Index < MapTileLayers.Length; Index++) {
        SetupTileLayer(MapTileLayers[Index]);
      }
      for (int Index = 0; Index < MapObjectLayers.Length; Index++) {
        SetupObjectLayer(MapObjectLayers[Index]);
      }

      Globals.Player = new(SpawnOrbPositions[RNG.Next(0, SpawnOrbPositions.Count)]);
    }


    private static void ActivateSwitch() {
      SpriteGroup<Sprite> TeleporterSprites = SpriteGroups[(int)Groups.Teleporter];
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
      SpriteGroup<Sprite> ItemGroup = SpriteGroups[(int)Groups.Item];
      Sprite CollisionResult = ItemGroup.OverlapsWith(PlayerRect);

      if (CollisionResult != null) {
        if (CollisionResult is LifeBlock LifeBlock && LifeBlock.Active) {
          for (int Index = 0; Index < ItemGroup.SpriteList.Count; Index++) {
            if (ItemGroup.SpriteList[Index] is LifeBlock Block && Block.Active)
              Block.Activate(false);
          }
          return;
        }

        if (CollisionResult is Item Item && Item.Active) {
          switch (Path.GetFileName(Item.DeactivatorImage)) {
            case "SwitchOff":
              ActivateSwitch();
              return;
            case "CheckpointOff1":
              Globals.Player.ActivateCheckpoint(Item.Rect.TopLeft(), 0);
              break;
            case "CheckpointOff2":
              Globals.Player.ActivateCheckpoint(Item.Rect.TopLeft(), 1);
              break;
            case "CheckpointOff3":
              Globals.Player.ActivateCheckpoint(Item.Rect.TopLeft(), 2);
              break;
          }

          Item.Deactivate();
        }
      }
    }

    private static void InteractableCollision() {
      if (SpriteGroups[(uint)Groups.Orb].OverlapsWith(PlayerRect) != null) {
        CurrentLevel++;
        LevelController.BeginLevel();
      }

      if (SpriteGroups[(uint)Groups.Ladder].OverlapsWith(PlayerRect) != null) {
        Globals.Player.MovementChange(PlayerStats.Ladder);
      }

      for (int Index = 0; Index < SpriteGroups[(int)Groups.Teleporter].SpriteList.Count; Index++) {
        if (SpriteGroups[(int)Groups.Teleporter].SpriteList[Index] is Teleporter TeleportPortal && TeleportPortal.Active && TeleportPortal.Rect.IntersectsWith(PlayerRect))
          Globals.Player.Teleport(TeleportPortal);
      }
    }

    private static void EnemyCollision() {
      if (SpriteGroups[(int)Groups.Damage].OverlapsWith(PlayerRect) != null) {
        if (IsSet(Globals.Player.Stats, (uint)PlayerStats.Shielding) || IsSet(Globals.Player.Stats, (uint)PlayerStats.Invincibility)) {

        } else {
          Globals.Player.Death();
        }
      }
    }


    public static void Update(float deltaTime) {
      for (int Index = 0; Index < SpriteGroups.Length; Index++) {
        SpriteGroups[Index].Update(deltaTime);
      }

      Globals.Player.Update(deltaTime);
      PlayerRect = Globals.Player.Rect;

      CollectableCollision();
      InteractableCollision();
      EnemyCollision();
    }
  }
}
