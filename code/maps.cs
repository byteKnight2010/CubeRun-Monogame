using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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
    public static void BeginLevel(byte level) {
      if (CurrentLevel != level) CurrentLevel = level;

      Level.Reset();
      Level.Setup($"Content/Maps/Platformers/{level}.tmx");
    }
  }

  public static class Level {
    private static Dictionary<string, (ZLayers, List<Groups>)> TileLayerProperties = new() {
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
    public static bool Active = false;


    public static void Reset() {
      Camera.Reset(Color.Teal);

      Globals.Player = null;
      for (int Index = 0; Index < SpriteGroups.Length; Index++) {
        SpriteGroups[Index].Clear();
      }

      GidTextureCache.Clear();
      TeleportLocations = [Vector2.Zero, Vector2.Zero, Vector2.Zero];
      Gravity = 800;
      Active = false;
    }


    public static void Setup(string mapPath) {
      XDocument TmxMap = XDocument.Load(mapPath);
      XElement[] MapTilesets = [.. TmxMap.Descendants("tileset")];
      XElement[] MapTileLayers = [.. TmxMap.Descendants("layer")];
      XElement[] MapObjectLayers = [.. TmxMap.Descendants("objectgroup")];
      Dimensions LevelDimensions = new(int.Parse(TmxMap.Root.Attribute("width").Value), int.Parse(TmxMap.Root.Attribute("height").Value));
      Vector2 PlayerSpawnPosition = new();
      string MapDirectory = Path.GetDirectoryName(mapPath);


      string GetObjectProperty(IEnumerable<XElement> objectProperties, string propertyName) => objectProperties.FirstOrDefault(property => property.Attribute("name").Value == propertyName)?.Attribute("value").Value;


      void SetupTileset(XElement tileset) {
        foreach (XElement tile in XDocument.Load(Path.GetFullPath(Path.Combine(MapDirectory, tileset.Attribute("source").Value))).Root.Elements("tile")) {
          XElement TileImageElement = tile.Element("image");

          if (TileImageElement == null) continue;

          GidTextureCache[int.Parse(tileset.Attribute("firstgid").Value) + int.Parse(tile.Attribute("id").Value)] = GetTexture($"Images/TileImages/{Path.GetFileNameWithoutExtension(TileImageElement.Attribute("source").Value)}");
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
          XAttribute GidAttribute = Object.Attribute("gid");
          Vector2 Position = new(int.Parse(Object.Attribute("x").Value), int.Parse(Object.Attribute("y").Value) - TILE_SIZE);
          Texture2D Image = null;
          Directions FaceDirection = StringToDirection((GetObjectProperty(ObjectProperties, "Orientation") ?? "L")[0]);
          string Name = Object.Attribute("name")?.Value ?? "";


          if (GidAttribute != null && GidTextureCache.TryGetValue(int.Parse(GidAttribute.Value), out Texture2D Texture))
            Image = Texture;
          

          switch (LayerName) {
            case "Interactable":
              List<Groups> AssignedGroups = new() { Groups.All };

              switch (Name) {
                case "StartBall":
                  PlayerSpawnPosition = Position;
                  break;
                case "EndBall":
                  AssignedGroups.Add(Groups.Orb);
                  break;
                case "Teleport Start Location":
                  new Teleporter(Image, Position, byte.Parse(GetObjectProperty(ObjectProperties, "ID")), true);
                  continue;
                case "Teleport End Location":
                  TeleportLocations[byte.Parse(GetObjectProperty(ObjectProperties, "ID"))] = Position;
                  break;
                case "OnSwitchBlock":
                  AssignedGroups.Add(Groups.Collidable);
                  break;
                case "Ladder":
                  AssignedGroups.Add(Groups.Ladder);
                  break;
              }

              new Sprite(Image, Position, AssignedGroups, ZLayers.Main, FaceDirection);
              break;
            case "Interactable Enemies":

              break;
            case "Collectable":
              switch (Name) {
                case "LifeBlock":
                  new Item(Image, Position, new() { Groups.All, Groups.Item }, ZLayers.Main, Name, true);
                  break;
                case "Switch":
                  new Item(Image, Position, new() { Groups.All, Groups.Switch }, ZLayers.Main, Name, true);
                  break;
              }
              break;
            case "Moving Object":

              break;
          }
        }
      }


      Active = true;
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

      Globals.Player = new(PlayerSpawnPosition);
    }


    private static void CollectableCollision() {
      (Sprite, bool) CollisionResult = SpriteGroups[(uint)Groups.Item].OverlapsWith(PlayerRect);

      if (CollisionResult.Item2 && CollisionResult.Item1 is Item Item && Item.Active) {
        switch (Item.Name) {
          case "LifeBlock":
            PlayerData.Lives++;
            break;
        }

        Item.Deactivate();
      }
    }

    private static void InteractableCollision() {
      if (SpriteGroups[(uint)Groups.Orb].OverlapsWith(PlayerRect).Item2) {
        PlayerData.CurrentLevel++;
        LevelController.BeginLevel(PlayerData.CurrentLevel);
      }

      if (SpriteGroups[(uint)Groups.Ladder].OverlapsWith(PlayerRect).Item2) {
        Globals.Player.MovementChange(PlayerData.PlayerMovers.Ladder);
      }

      for (int Index = 0; Index < SpriteGroups[(int)Groups.Teleporter].SpriteList.Count; Index++) {
        if (SpriteGroups[(int)Groups.Teleporter].SpriteList[Index] is Teleporter TeleportPortal && TeleportPortal.Active && TeleportPortal.Rect.IntersectsWith(PlayerRect))
          Globals.Player.Teleport(TeleportPortal);
      }
    }

    private static void EnemyCollision() {
      if (SpriteGroups[(int)Groups.Damage].OverlapsWith(PlayerRect).Item2) {
        if (IsSet(Globals.Player.Stats, (byte)PlayerStats.Shielding)) {

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
