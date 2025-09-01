using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Cube_Run_C_.Globals;


namespace Cube_Run_C_ {
  public static class Level {
    private static Dictionary<int, Texture2D> GidTextureCache = new();
    public static List<Vector2> TeleportLocations = new() { new(0, 0), new(0, 0), new(0, 0) };
    public static (Dimensions, Dimensions) Dimensions = new(new(0, 0), new(0, 0));
    public static ushort Gravity = 800;
    public static bool Active = false;


    public static void Reset(string mapPath) {
      Level.GidTextureCache.Clear();
      Level.TeleportLocations = new();
      Level.Dimensions = new(new(0, 0), new(0, 0));
      Level.Gravity = 800;
      Level.Active = true;

      Level.Setup(mapPath);
    }


    private static void Setup(string mapPath) {
      (ZLayers, List<string>)[] LayerProperties = [(ZLayers.Background, new() { "All" }), (ZLayers.Main, new() { "All", "Collidable" }), (ZLayers.Foreground, new() { "All" }), (ZLayers.Main, new() { "All", "Damage" })];
      string[] TileLayers = ["Background", "Terrain", "Foreground", "Enemies"];
      string[] ObjectLayers = ["Interactable", "Interactable Enemies", "Collectable", "Moving Objects"];
      XDocument TmxMap = XDocument.Load(mapPath);
      Dimensions LevelDimensions = new(int.Parse(TmxMap.Root.Attribute("width").Value), int.Parse(TmxMap.Root.Attribute("height").Value));
      string MapDirectory = Path.GetDirectoryName(mapPath);
      byte Index = 0;

      LevelData.Dimensions.Item1 = LevelDimensions;
      LevelData.Dimensions.Item2 = new(LevelDimensions.Width * TILE_SIZE, LevelDimensions.Height * TILE_SIZE);

      foreach (XElement Tileset in TmxMap.Descendants("tileset")) {
        foreach (XElement tile in XDocument.Load(Path.GetFullPath(Path.Combine(MapDirectory, Tileset.Attribute("source").Value))).Root.Elements("tile")) {
          XElement TileImageElement = tile.Element("image");

          if (TileImageElement == null) continue;

          Level.GidTextureCache[int.Parse(Tileset.Attribute("firstgid").Value) + int.Parse(tile.Attribute("id").Value)] = Assets.GetTexture($"Images/TileImages/{Path.GetFileNameWithoutExtension(TileImageElement.Attribute("source").Value)}");
        }
      }

      foreach (XElement TileLayer in TmxMap.Descendants("layer")) {
        if (Index >= LayerProperties.Length) break;

        string[] Gids = TileLayer.Element("data").Value.Trim().Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        (ZLayers, List<string>) LayerProperty = LayerProperties[Index];
        Dimensions LayerDimensions = new(int.Parse(TileLayer.Attribute("width").Value), int.Parse(TileLayer.Attribute("height").Value));

        for (int Y = 0; Y < LayerDimensions.Height; Y++) {
          for (int X = 0; X < LayerDimensions.Width; X++) {
            int Gid = int.Parse(Gids[Y * LayerDimensions.Width + X]);

            if (Gid == 0) continue;

            new Sprite(Level.GidTextureCache.OrderByDescending(kvp => kvp.Key).First(kvp => kvp.Key <= Gid).Value, new(X * TILE_SIZE, Y * TILE_SIZE), LayerProperty.Item2, LayerProperty.Item1);
          }
        }

        Index++;
      }
      
      foreach (XElement ObjectLayer in TmxMap.Descendants("objectgroup")) {
        string LayerName = ObjectLayer.Attribute("name")?.Value ?? "";

        foreach (XElement Object in ObjectLayer.Elements("object")) {
          Vector2 Position = new(int.Parse(Object.Attribute("x").Value), int.Parse(Object.Attribute("y").Value) - TILE_SIZE);
          XAttribute GidAttribute = Object.Attribute("gid");
          Texture2D Image = null;
          string Name = Object.Attribute("name")?.Value ?? "";

          if (GidAttribute != null && Level.GidTextureCache.TryGetValue(int.Parse(GidAttribute.Value), out Texture2D Texture)) {
            Image = Texture;
          }

          switch (LayerName) {
            case "Interactable":
              switch (Name) {
                case "StartBall":
                  new Sprite(Image, Position, new() { "All" }, ZLayers.Main);
                  Globals.Player = new Player(Position);
                  break;
                case "EndBall":

                  break;
              }
              break;
            case "Interactable Enemies":

              break;
            case "Collectable":

              break;
            case "Moving Object":

              break;
          }
        }
      }
    }
  }
}
