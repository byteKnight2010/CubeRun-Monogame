using System.Collections.Generic;
using System.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Cube_Run_C_.Globals;


namespace Cube_Run_C_ {
  public class SpriteGroup<T> where T : Sprite {
    private List<T> sprites = new();


    public void Add(T sprite) {
      sprites.Add(sprite);
    }

    public void Clear() {
      sprites.Clear();
    }


    public void Update(float deltaTime) {
      foreach (var sprite in sprites) {
        sprite.Update(deltaTime);
      }
    }


    public IEnumerable<T> Sprites => sprites;
  }


  public class Sprite {
    public Texture2D Image;
    public RectangleF Rect;
    public Z_LAYERS z = 0;


    public Sprite(Texture2D texture, PointF position, string[] groups, Z_LAYERS z) {
      this.Image = texture;
      this.Rect = new RectangleF(position.X, position.Y, texture.Width, texture.Height);
      this.z = z;

      foreach (string group in groups) {
        spriteGroups[group].Add(this);
      }
    }

    public virtual void Update(float deltaTime) {

    }
  }

  public class MovingSprite : Sprite {
    public Vector2 Direction;
    public uint[] Speed;


    public MovingSprite(Texture2D texture, PointF position, Vector2 direction, uint[] speed, Vector2 dimensions, string[] groups, Z_LAYERS z) : base(texture, position, groups, z) {
      this.Direction = direction;
      this.Speed = speed;

      if (this.Direction != Vector2.Zero)
        this.Direction.Normalize();
    }


    public override void Update(float deltaTime) {
      this.Rect.X += this.Direction.X * this.Speed[0] * deltaTime;
      this.Rect.Y += this.Direction.Y * this.Speed[1] * deltaTime;
    }
  }


  public class Player : Sprite {
    // Stats
    public Dictionary<string, bool> ActivatedPowers = new() {
      ["Invincibility"] = false,
      ["Auto-Move"] = false,
      ["Flying"] = false,
      ["Frozen"] = false,
      ["Goggles"] = false,
      ["Honey"] = false,
      ["Telescope"] = false
    };
    private bool[] ActivatedCheckpoints = new bool[3];
    private PointF RespawnPos;
    public char Size = 'N';
    public char Environment = 'A';
    public ushort Deaths = 0;
    public bool FallDamageEnabled = false;
    private bool FallDamageCondition = false;
    // Movement
    public Dictionary<char, bool> MovementAbility = new() {
      ['X'] = true,
      ['Y'] = false
    };
    public Vector2 Direction = new(0, 0);
    private const byte SPRINT_MULTIPLIER = 2;
    private ushort vy;
    public ushort JumpHeight = 600;
    public ushort MovementSpeed = 300;
    public bool CanJump = true;
    private bool Jumping = false;
    // Collision
    private Dictionary<char, bool> OnSurface = new() {
      ['L'] = false,
      ['R'] = false,
      ['U'] = false,
      ['D'] = false
    };
    private bool Platform;
    private bool StickingCeiling = false;
    // Timers
    public Dictionary<string, Timer> PowerTimers = new() {
      ["Invincibility"] = new(0),
      ["Auto-Move"] = new(0),
      ["Flying"] = new(0),
      ["Frozen"] = new(0),
      ["Goggles"] = new(0),
      ["Honey"] = new(0),
      ["Telescope"] = new(0)
    };
    private Timer WallJumpTimer = new(200);
    private Timer WallJumpStunTimer = new(100);
    private Timer DeathMoveTimer = new(100);
    private Timer InvincibiltyFrames = new(200);


    public Player(PointF position) : base(Assets.GetTexture("images/playerImages/player"), position, ["All"], Z_LAYERS.Player) {
      this.RespawnPos = position;
    }


    public void DeathConditions() {
      if (this.vy >= 900 && this.FallDamageEnabled)
        this.FallDamageCondition = true;
      if (this.vy == 0 && !this.OnSurface['D'])
        this.FallDamageCondition = false;

      if (this.OnSurface['D'] && this.FallDamageCondition) {
        this.FallDamageCondition = false;

        if (!this.ActivatedPowers["Invincibility"] && !this.ActivatedPowers["Honey"])
          this.Death();
      }
    }

    public void Death() {
      if (this.InvincibiltyFrames.Active || this.PowerTimers["Invincibility"].Active)
        return;

      this.FallDamageCondition = false;
      this.Rect.X = this.RespawnPos.X;
      this.Rect.Y = this.RespawnPos.Y;
      this.Direction = new(0, 0);

      this.InvincibiltyFrames.Activate(CurrentGameTime);
      this.DeathMoveTimer.Activate(CurrentGameTime);

      PlayerData.Lives -= 1;
    }

  }
}