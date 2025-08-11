using System.Numerics;
using Microsoft.Xna.Framework.Graphics;

namespace Cube_Run_C_
{
  internal class Camera : SpriteGroup<Sprite> {
    private GraphicsDevice graphicsDevice;
    private Vector2 offset = new(0, 0);
    public double scaleFactor = 1.0;


    public Camera(GraphicsDevice device) {
      graphicsDevice = device;
    }


    public void updateScaleFactor(double scale) {
      scaleFactor = scale;
    }
  }
}