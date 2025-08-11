# CubeRun-Monogame
**Cube Run** is a 2D platformer built in **C# with Monogame**, ported from an original **Python with Pygame** version. 
This project aims to create a scalable, cross-platform, high-performance game to efficiently handle large maps, numerous tiles, and complex collision systems without bottlenecks.

---

## Current Features
- Input Manager for keyboard and gamepad
- SpriteGroup for entity management, handling collective drawing and updates
- Base Sprite and MovingSprite classes
- Player class with designated stats and death functions
- Base Camera class for future side-scrolling support
- Global constants and configuration via `Globals`
- Asset caching system for faster access (uses Texture2D)
- Timer utility class for event handling

---

## Planned Features
- Side-scrolling Camera implementation, following any given target (default Player)
- Collision detection within SpriteGroup and Player
- Level loading from TMX map files
- Player movement implementation
- Basic Menus and UI elements
- Enemy AI and Classes implementation
- Base AnimatedSprite class
