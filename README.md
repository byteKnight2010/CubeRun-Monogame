# CubeRun-Monogame
**Cube Run** is a 2D platformer built in **C# with Monogame**, ported from an original **Python with Pygame** version. 
This project aims to create a scalable, cross-platform, high-performance game to efficiently handle large maps, numerous tiles, and complex collision systems without bottlenecks. 
I also hope to learn to use C#, Monogame, and the .NET framework through this project, all of which I have never used before. 

---

## Current Features
- Fullscreen feature
- Input Manager for keyboard and gamepad
- Level loading from TMX map files
- Foundation for customizable controls
- SpriteGroup for entity management, handling collective drawing and updates
- Collision detection between Sprites and Player
- Base Sprite, MovingSprite, and Animation classes
- Player class with designated stats and death functions, and movement implementation
- Side-Scrolling Camera utilizing Culling, Spatial-Grid Partitioning, Target (Player-Center), and a "Lock" feature
- Global constants and configuration via `Globals`
- Timer utility class for event handling
- Basic UI with sliders
- Window-sizing options
- Brightness & Sound adjustment options
- Teleport functionality
- Removed TextureCache for rotations
- Debug helper
- Tight optimizations to the camera (ex., bit-shift instead of / 2)

---

## Recently Added Features
- Animation Function
- Enemy AI & Classes Implementation
- Checkpoints
- Coins
- Canons
- Multiple Spawn Locations

---

## Planned Features (By next Update ideally):
- Overworld
- Finish Spring
- Player powers: Invincibility, Sprinting, Honey, Auto-Move
- Upgrade the TMX loader for less reliance on LINQ and improve efficiency
