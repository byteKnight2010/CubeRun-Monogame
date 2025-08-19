using System.Numerics;
using Microsoft.Xna.Framework.Graphics;

namespace Cube_Run_C_
{
  internal class Camera : SpriteGroup<Sprite> {
    private GraphicsDevice GraphicsDevice;
    private Vector2 offset = new(0, 0);
    public float scaleFactor = Globals.ScaleFactor;


    public Camera(GraphicsDevice device) {
      this.GraphicsDevice = device;
    }


    public void updateScaleFactor(float scale) {
      scaleFactor = scale;
      Globals.ScaleFactor = scale;
    }
  }
}