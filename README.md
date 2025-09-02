# CubeRun-Monogame
**Cube Run** is a 2D platformer built in **C# with Monogame**, ported from an original **Python with Pygame** version. 
This project aims to create a scalable, cross-platform, high-performance game to efficiently handle large maps, numerous tiles, and complex collision systems without bottlenecks.

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
- Asset caching system for faster access (uses Texture2D)
- Timer utility class for event handling

---

## Planned Features (By next Update ideally):
- Basic Menus and UI elements
- Enemy AI and Classes implementation
- Overworld
- Finish Spring and Teleport functionality
- Player powers: Invincibility, Sprinting, Honey, Auto-Move
- More window-sizing options
- FPS options, Cap at 144
