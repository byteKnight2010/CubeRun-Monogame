# CubeRun-Monogame
**Cube Run** is a 2D platformer built in **C# with Monogame**, ported from an original **Python with Pygame** version. 
This project aims to create a scalable, cross-platform, high-performance game to efficiently handle large maps, numerous tiles, and complex collision systems without bottlenecks. 
I also hope to learn to use C#, Monogame, and the .NET framework through this project, all of which I have never used before. 

---

New Features:
- Activator & Activator-Blocks, acting as toggleable switches
- Temporary Game-Over foundation, currently terminates Game
- Configured TmxMap Builder to count Sprites and store in `SpriteCount`
- Outlined Player methods, variables, getters/setters, and Sprite class into a separate *.cs* file

Optimizations:
- Reduced Camera allocations by pre-allocating `CountsBuffer` and clearing upon Reset
- Reduced Camera allocations by allocating `SortedSpritesBuffer` only at the beginning of each level

Fixes:
- Resolved scaling issue in Letterboxing
- Template implementation inconsistencies: all TMX maps now use Object Templates

---

## Planned Features/Fixes (By next Update):
- Decide final tile-size, and if smaller: Render to small target and scale-up each draw frame
- Add large/small powerups
