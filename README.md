# CubeRun-Monogame
**Cube Run** is a 2D platformer built in **C# with Monogame**, ported from an original **Python with Pygame** version. 
This project aims to create a scalable, cross-platform, high-performance game to efficiently handle large maps, numerous tiles, and complex collision systems without bottlenecks. 
I also hope to learn to use C#, Monogame, and the .NET framework through this project, all of which I have never used before. 

---

The new update has brought tremendous new features:

- The *Camera* system has been optimized with a Matrix to handle screen offset and scale rather than applying to each sprite. 
- Incorporated Multithreading for the *SaveSystem*, Screenshots, and LevelLoading
- Outlined *SoundManager* and *Assets* classes into seperate file for convenience
- Added screenshots with the hotkey F2, and Fullscreen functionality with the F11 hotkey
- Added *Config* Binary system for easier global variable assignment
- Added *Bestiary* functionality for future dictionary implementation
- Added *Quicksand* using linear interpolation for pseudo-accurate sinking
- Added Spring functionality that has a stronger force with matched input
- Added *BasicSprite* into *Camera* and Mapsystem for lightweight sprites with no: Animations, Rotations, DrawOffsets
- Added *ExitWindow*, *SaveWindow*, *EndLevelScreen*, *TmxLoadingScreen*, and *TitleScreen* UI elements
- Added *SoundManager* as an efficient way to cache and handle Sounds, SoundEffects, and Music

And optimizations:

- The *Camera* system has been optimized with a Matrix to handle screen offset and scale rather than applying to each sprite.
- A global Matrix has been added for other elements independent of the Camera, to avoid scaling each sprite.
- The *Maps* system has been entirely reworked to use a custom *MapContentPipeline* to process and load TmxMaps.
- A single List<Sprite> pool is now used for all collisions in *Maps*
- A lightweight *SoundData* struct for SoundEffects, while *SoundEffectInstances* make use of *SoundEffectData*, which includes a loop option
- Bitmasking in all regions possible, including: Sprites, UI, and Camera
- *MethodImpl(MethodImplOptions.AggressiveInlining)*'d getters/setters and conversion functions (eg *ToVector2()*)
- Added *BVector*, which makes use of bytes instead of floats for simple direction Vectors
  
---

## Planned Features (By next Update ideally):
- Overworld
- Finish Spring
- Player powers: Invincibility, Sprinting, Honey, Auto-Move
- Upgrade the TMX loader for less reliance on LINQ and improve efficiency
