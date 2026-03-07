using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Cube_Run_C_.Assets.VisualManager;
using static Cube_Run_C_.Camera;
using static Cube_Run_C_.ConfigManager;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Sprites;
using static Cube_Run_C_.Tools;
using static Cube_Run_C_.Tools.BitMask;
using static Cube_Run_C_.Tools.Engine;
using static Cube_Run_C_.Tools.InputManager;
using static Cube_Run_C_.UI;
using RectangleF = System.Drawing.RectangleF;


namespace Cube_Run_C_ {
  public static class PlatformerPlayer {
    [Flags]
    public enum PlayerStats : ulong {
      Animating = 1ul << 0,
      ReturnMovement = 1ul << 1,
      FallDamageEnabled = 1ul << 2,
      FallDamageCondition = 1ul << 3,
      Shielding = 1ul << 4,
      CanJump = 1ul << 5,
      HorizontalMovement = 1ul << 6,
      VerticalMovement = 1ul << 7,
      Left = 1ul << 8,
      Right = 1ul << 9,
      Top = 1ul << 10,
      Bottom = 1ul << 11,
      StickingCeiling = 1ul << 12,
      Ladder = 1ul << 13,
      Water = 1ul << 14,
      DeepWater = 1ul << 15,
      ThickWater = 1ul << 16,
      Quicksand = 1ul << 17,
      QuicksandDeep = 1ul << 18,
      CheckpointOne = 1ul << 19,
      CheckpointTwo = 1ul << 20,
      CheckpointThree = 1ul << 21,
      LanternEnabled = 1ul << 22,
      Invincibility = 1ul << 23,
      AutoMove = 1ul << 24,
      Flying = 1ul << 25,
      Frozen = 1ul << 26,
      Goggles = 1ul << 27,
      Honey = 1ul << 28,
      Sprint = 1ul << 29,
      Telescope = 1ul << 30,
      Normal = 1ul << 31,
      Small = 1ul << 32,
      Large = 1ul << 33,
      OnWall = 1ul << 34       
    }

    public enum PlayerPowers : byte {
      Invincibility = 0,
      AutoMove = 1,
      Flying = 2,
      Frozen = 3,
      Goggles = 4,
      Honey = 5,
      Sprint = 6,
      Telescope = 7,
      Canceller = 8,
      All = 9,
      None = byte.MaxValue
    }

    public enum PlayerTimers : byte {
      RespawnStatus = 0,
      Invincibility = 1,
      AutoMove = 2,
      Flying = 3,
      Frozen = 4,
      Goggles = 5,
      Honey = 6,
      Sprint = 7,
      Telescope = 8,
      WallJump = 9,
      WallJumpStun = 10,
      SpringMove = 11,
      ShieldKnockback = 12
    }


    public class Player : Sprite {
      private readonly RectangleF[] CollisionRectangles = [RectangleF.Empty, RectangleF.Empty, RectangleF.Empty, RectangleF.Empty];
      private readonly PlayerStats[] CheckpointFlags = [PlayerStats.CheckpointOne, PlayerStats.CheckpointTwo, PlayerStats.CheckpointThree];
      private readonly Texture2D DefaultImage = GetTexture("Images/PlayerImages/Player");
      private MovingSprite Platform;
      private Vector2 RespawnPos;
      public Vector2 Direction = Vector2.Zero;
      public Vector2 Acceleration = Vector2.Zero;
      public Vector2 Velocity = Vector2.Zero;
      private Vector2 InputVector = Vector2.Zero;
      private Animations CurrentAnimation = Animations.PlayerDefault;
      public ulong Stats = 0x0000000000000000;
      public ushort Deaths = 0;
      public ushort Gravity = LevelData.Gravity;
      public float JumpHeight = ConfigManager.Player.JumpHeight;
      public float MovementSpeed = ConfigManager.Player.Speed;
      public float QuicksandTop = 0f;
      public Timer[] Timers = [new(150), null, null, null, null, null, null, null, null, new(200), new(200), new(200), new(250)];


      public Player(Vector2 position) : base(new Animation(AnimationsData[(int)Animations.PlayerDefault]), position, [Groups.All], ZLayers.Player) {
        this.RespawnPos = position;

        for (int Index = 0; Index < PowerDurations.Length; Index++) {
          PlayerPowers CapturedIndex = (PlayerPowers)Index;
          Timers[Index + 1] = new(PowerDurations[Index], () => this.DeactivatePower(CapturedIndex));
        }

        Set(ref this.Stats, (ulong)PlayerStats.HorizontalMovement, true);
        Set(ref this.Stats, (ulong)PlayerStats.CanJump, true);
        Set(ref this.Stats, (ulong)PlayerStats.ReturnMovement, true);

        this.Timers[(byte)PlayerTimers.RespawnStatus].Activate();
      }


      private void PlayAnimation(Animations animation) {
        this.StopAnimation();
        this.CurrentAnimation = animation;
        this.Image = this.Animation.AnimationData.SpriteSheet;
        this.Animation = new(AnimationsData[(int)animation]);
        this.Animation.Play();

        Set(ref this.Stats, (ulong)PlayerStats.Animating, true);
      }

      private void StopAnimation() {
        if (!IsSet(this.Stats, (ulong)PlayerStats.Animating))
          return;

        this.CurrentAnimation = Animations.PlayerDefault;
        this.Animation = new(AnimationsData[(int)Animations.PlayerDefault]);
        this.Animation.AnimationData.SpriteSheet = this.Image;
        this.Animation.Stop();

        Set(ref this.Stats, (ulong)PlayerStats.Animating, false);
      }


      private void DeathConditions() {
        if (this.Velocity.Y >= 900 && IsSet(this.Stats, (byte)PlayerStats.FallDamageEnabled))
          Set(ref this.Stats, (byte)PlayerStats.FallDamageCondition, true);
        if (this.Velocity.Y == 0 && !IsSet(this.Stats, (ulong)PlayerStats.Bottom))
          Set(ref this.Stats, (byte)PlayerStats.FallDamageCondition, false);

        if (IsSet(this.Stats, (ulong)PlayerStats.Bottom) && IsSet(this.Stats, (byte)PlayerStats.FallDamageCondition)) {
          Set(ref this.Stats, (byte)PlayerStats.FallDamageCondition, false);

          if (!IsSet(this.Stats, (ulong)PlayerStats.Invincibility) && !IsSet(this.Stats, (ulong)PlayerStats.Honey))
            this.Death();
        }
      }

      public void Death() {
        if (this.Timers[(byte)PlayerTimers.RespawnStatus].Active || this.Timers[(byte)PlayerTimers.Invincibility].Active)
          return;

        Set(ref this.Stats, (byte)PlayerStats.FallDamageCondition, false);
        
        this.Rect.X = this.RespawnPos.X;
        this.Rect.Y = this.RespawnPos.Y;
        this.StopMovement();

        for (int Index = 0; Index < DestructibleSprites.Count; Index++) {
          Sprite Destructible = DestructibleSprites[Index];

          if (Destructible is PowerUp PowerSprite && IsSet(PowerSprite.Stats, (byte)PowerSpriteFlags.Destroyed))
            PowerSprite.Regenerate();
        }

        this.Timers[(byte)PlayerTimers.RespawnStatus].Activate();

        Lives--;
      }


      private void Input() {
        this.InputVector = Vector2.Zero;

        if (!this.Timers[(byte)PlayerTimers.RespawnStatus].Active && !this.Timers[(byte)PlayerTimers.SpringMove].Active && !this.Timers[(byte)PlayerTimers.ShieldKnockback].Active) {
          if (IsSet(this.Stats, (ulong)PlayerStats.HorizontalMovement)) {
            if (CheckAction(GameAction.MoveRight, false))
              this.InputVector.X++;

            if (CheckAction(GameAction.MoveLeft, false))
              this.InputVector.X--;

            this.Direction.X = this.InputVector.X * (IsSet(this.Stats, (ulong)PlayerStats.Sprint) && IsKeyDown(Keys.LeftShift) ? 2 : 1);
          }

          if (IsSet(this.Stats, (ulong)PlayerStats.VerticalMovement)) {
            if (CheckAction(GameAction.MoveUp, false))
              this.InputVector.Y--;

            if (CheckAction(GameAction.MoveDown, false))
              this.InputVector.Y++;

            this.Direction.Y = this.InputVector.Y;
          }
        }

        if (IsSet(this.Stats, (ulong)PlayerStats.CanJump) && CheckAction(GameAction.Jump, true))
          this.StartJump();

        if (CheckAction(GameAction.MoveDown, false)) {
          if (IsSet(this.Stats, (ulong)PlayerStats.Honey) && IsSet(this.Stats, (ulong)PlayerStats.StickingCeiling)) {
            this.Rect.Y += 2;
            Set(ref this.Stats, (ulong)PlayerStats.StickingCeiling, false);
          }
        }
      }


      public void ActivatePower(PlayerPowers power) {
        ulong PowerFlag = 1ul << ((int)(power + ConfigManager.Player.PowerBitOffset));

        if (IsSet(this.Stats, PowerFlag))
          return;

        Texture2D NewImage = null;

        switch (power) {
          case PlayerPowers.Invincibility:
            NewImage = GetTexture("Images/PlayerImages/PlayerGold");
            break;
          case PlayerPowers.AutoMove:

            break;
          case PlayerPowers.Flying:

            break;
          case PlayerPowers.Frozen:

            break;
          case PlayerPowers.Goggles:

            break;
          case PlayerPowers.Honey:
            NewImage = GetTexture("Images/PlayerImages/PlayerHoney");
            break;
          case PlayerPowers.Sprint:

            break;
          case PlayerPowers.Telescope:

            break;
        }

        if (NewImage != null)
          this.Animation.AnimationData.SpriteSheet = NewImage;
        

        Set(ref this.Stats, PowerFlag, true);
        this.Timers[(int)power + 1].Activate();
      }

      public void DeactivatePower(PlayerPowers power) {
        if (power == PlayerPowers.All) {
          byte PowerCount = (byte)(ConfigManager.Player.PowerBitEnd - ConfigManager.Player.PowerBitOffset);

          for (byte Index = 0; Index < PowerCount; Index++) {
            this.PowerDeactivate(Index);
          }
        } else {
          this.PowerDeactivate((byte)power);
        }
      }

      private void PowerDeactivate(byte power) {
        if (!IsSet(this.Stats, 1ul << (power + ConfigManager.Player.PowerBitOffset)))
          return;

        Texture2D DefaultedSheet = this.DefaultImage;

        switch (this.CurrentAnimation) {
          case Animations.PlayerTeleportGold:
          case Animations.PlayerTeleportHoney:
            DefaultedSheet = AnimationsData[(int)Animations.PlayerTeleport].SpriteSheet;
            break;
        }

        this.Animation.AnimationData.SpriteSheet = DefaultedSheet;
        this.Image = this.DefaultImage;
        Set(ref this.Stats, 1ul << (power + ConfigManager.Player.PowerBitOffset), false);
      }


      public void MovementChange(PlayerStats change) {
        if (IsSet(this.Stats, (ulong)change))
          return;

        Set(ref this.Stats, (ulong)change, true);

        switch (change) {
          case PlayerStats.Ladder:
            Set(ref this.Stats, (ulong)PlayerStats.HorizontalMovement, true);
            Set(ref this.Stats, (ulong)PlayerStats.VerticalMovement, true);
            break;
        }
      }

      private void MovementReturn() {
        if (!IsSet(this.Stats, (ulong)PlayerStats.ReturnMovement) || !Any(this.Stats, 13, 18))
          return;

        byte AirTouch = 0x00;

        if (SpriteGroups[(int)Groups.Quicksand].SpriteList.Count > 0) {
          if (SpriteGroups[(int)Groups.Quicksand].OverlapsWith(this.Rect) == null) {
            Set(ref this.Stats, (ulong)PlayerStats.Quicksand, false);
            Set(ref this.Stats, (ulong)PlayerStats.QuicksandDeep, false);
            Set(ref AirTouch, 1 << 0, true);
          } else {
            return;
          }
        } else {
          Set(ref AirTouch, 1 << 0, true);  
        }

        if (SpriteGroups[(int)Groups.Water].SpriteList.Count > 0) {
          if (SpriteGroups[(int)Groups.Water].OverlapsWith(this.Rect) == null) {
            Set(ref this.Stats, (ulong)PlayerStats.Water, false);
            Set(ref this.Stats, (ulong)PlayerStats.DeepWater, false);
            Set(ref this.Stats, (ulong)PlayerStats.ThickWater, false);
            Set(ref AirTouch, 1 << 1, true);
          } else {
            return;
          }
        } else {
          Set(ref AirTouch, 1 << 1, true);
        }

        if (IsSet(AirTouch, 1 << 0) && IsSet(AirTouch, 1 << 1)) {
          this.Gravity = LevelData.Gravity;
          this.JumpHeight = ConfigManager.Player.JumpHeight;
          this.MovementSpeed = ConfigManager.Player.Speed;

          Set(ref this.Stats, (ulong)PlayerStats.HorizontalMovement, true);
          Set(ref this.Stats, (ulong)PlayerStats.VerticalMovement, false);
        }
      }

      private void StopMovement() {
        this.Direction = Vector2.Zero;
        this.Velocity = Vector2.Zero;
      }


      private void Move(float deltaTime) {
        if (IsSet(this.Stats, (ulong)PlayerStats.AutoMove)) {
          this.Velocity.X += this.Acceleration.X * deltaTime;
          this.Rect.X += this.Velocity.X;
        } else {
          this.Velocity.X = this.Direction.X * this.MovementSpeed * deltaTime;
          this.Rect.X += this.Velocity.X;
        }

        if (this.Platform != null) {
          this.Rect.X += this.Platform.Direction.X * this.Platform.Speed * deltaTime;
          this.Rect.Y += this.Platform.Direction.Y * this.Platform.Speed * deltaTime;
        }

        this.Collision(true);

        if (IsSet(this.Stats, (ulong)PlayerStats.VerticalMovement)) {
          this.Velocity.Y = this.Direction.Y * this.MovementSpeed * deltaTime;
          this.Rect.Y += this.Velocity.Y;
          return;
        }

        if (IsSet(this.Stats, (ulong)PlayerStats.Quicksand) || IsSet(this.Stats, (ulong)PlayerStats.QuicksandDeep)) {
          this.QuicksandDrag(deltaTime);
          this.Rect.Y += this.Velocity.Y * deltaTime;
          return;
        }

        if (IsSet(this.Stats, (ulong)PlayerStats.Honey)) {
          if (IsSet(this.Stats, (ulong)PlayerStats.Top)) {
            Set(ref this.Stats, (ulong)PlayerStats.StickingCeiling, true);
            this.Direction.Y = 0;
            this.Rect.Y -= 1;
          } else if (!this.Timers[(byte)PlayerTimers.WallJump].Active && !IsSet(this.Stats, (ulong)PlayerStats.Bottom) && (IsSet(this.Stats, (ulong)PlayerStats.Left) || IsSet(this.Stats, (ulong)PlayerStats.Right))) {
            this.Direction.Y = 0;
          }
        }

        this.Velocity.Y += this.Gravity * deltaTime;
        this.Velocity.Y = Math.Clamp(this.Velocity.Y, -ConfigManager.Player.TerminalVelocity, ConfigManager.Player.TerminalVelocity);

        this.Rect.Y += this.Velocity.Y * deltaTime;

        this.Collision(false);
      }

      private void StartJump() {
        if (IsSet(this.Stats, (ulong)PlayerStats.Bottom) && !IsSet(this.Stats, (ulong)PlayerStats.Top)) {
          this.Timers[(byte)PlayerTimers.WallJump].Activate();
          this.Direction.Y = 1.0f;

          if (IsSet(this.Stats, (ulong)PlayerStats.Quicksand) || IsSet(this.Stats, (ulong)PlayerStats.QuicksandDeep)) {
            this.Velocity.Y -= this.JumpHeight;
          } else {
            this.Velocity.Y = -this.JumpHeight;
          }
        } else {
          if (IsSet(this.Stats, (ulong)PlayerStats.Quicksand) || IsSet(this.Stats, (ulong)PlayerStats.QuicksandDeep)) {
            this.Velocity.Y -= this.JumpHeight;
          } else if (IsSet(this.Stats, (ulong)PlayerStats.Honey) && !this.Timers[(byte)PlayerTimers.WallJump].Active && (IsSet(this.Stats, (ulong)PlayerStats.Left) || IsSet(this.Stats, (ulong)PlayerStats.Right))) {
            this.Timers[(byte)PlayerTimers.WallJumpStun].Activate();
            this.Direction.Y = 1;
            this.Direction.X = IsSet(this.Stats, (ulong)PlayerStats.Left) ? ConfigManager.Player.WallJumpDirectionFactor : -ConfigManager.Player.WallJumpDirectionFactor;
            this.Velocity.Y = -this.JumpHeight;
          }
        }
      }


      public void ActivateSpring(Directions bounceDirection) {
        switch (bounceDirection) {
          case Directions.Left:
            this.Velocity.X = -(CheckAction(GameAction.MoveLeft, true) ? ConfigManager.Player.AlternativeSpringEffect : ConfigManager.Player.SpringEffect);
            break;
          case Directions.Right:
            this.Velocity.X = CheckAction(GameAction.MoveRight, true) ? ConfigManager.Player.AlternativeSpringEffect : ConfigManager.Player.SpringEffect;
            break;
          case Directions.Up:
            this.Velocity.Y = -(CheckAction(GameAction.MoveUp, true) ? ConfigManager.Player.AlternativeSpringEffect : ConfigManager.Player.SpringEffect);
            break;
          case Directions.Down:
            this.Velocity.Y = CheckAction(GameAction.MoveDown, true) ? ConfigManager.Player.AlternativeSpringEffect : ConfigManager.Player.SpringEffect;
            break;
        }
      }

      private void QuicksandDrag(float deltaTime) {
        float Depth = Quicksand.GetDepth(this.Rect);
        float Drag = MathHelper.Lerp(Gameplay.QuicksandDrag, Gameplay.QuicksandDeepDrag, Depth);
        float DampFactor = MathF.Exp(-Drag * deltaTime);
        float SandGravity = Gravity * MathHelper.Lerp(1.0f, 0.1f, Depth * Depth);

        this.Velocity *= DampFactor;

        this.Velocity.Y += SandGravity * deltaTime;
        this.Velocity.Y = MathF.Min(this.Velocity.Y, Gravity / Drag);
      }


      private void CheckContact() {
        float QuarterHeight = this.Rect.Height * 0.25f;

        Set(ref this.Stats, (ulong)PlayerStats.Left, false);
        Set(ref this.Stats, (ulong)PlayerStats.Right, false);
        Set(ref this.Stats, (ulong)PlayerStats.Top, false);
        Set(ref this.Stats, (ulong)PlayerStats.Bottom, false);

        this.CollisionRectangles[0] = new(this.Rect.X - 2, this.Rect.Y + QuarterHeight, 2, this.Rect.Height);
        this.CollisionRectangles[1] = new(this.Rect.Right, this.Rect.Y + QuarterHeight, 2, this.Rect.Height * 0.5f);
        this.CollisionRectangles[2] = new(this.Rect.X, this.Rect.Y - QuarterHeight, this.Rect.Width, QuarterHeight);
        this.CollisionRectangles[3] = new(this.Rect.X, this.Rect.Bottom, this.Rect.Width, QuarterHeight);
        this.Platform = null;

        for (int I = 0; I < this.CollisionRectangles.Length; I++) {
          Sprite WallCollision = SpriteGroups[(int)Groups.Collidable].OverlapsWith(this.CollisionRectangles[I]);
          bool SemiCollision = false;

          if (WallCollision == null) {
            List<Sprite> SemiCollidables = SpriteGroups[(int)Groups.SemiCollidable].SpriteList;

            for (int Index = 0; Index < SemiCollidables.Count; Index++) {

            }
          } else if (I == 3) {
            if (WallCollision is MovingSprite MovingSprite && WallCollision.Rect != WallCollision.OldRect)
              this.Platform = MovingSprite;
          }
          bool WallContact = WallCollision != null || SemiCollision;

          Set(ref this.Stats, (ulong)(1ul << (I + 8)), WallContact);

          if (WallContact)
            Set(ref this.Stats, (ulong)PlayerStats.OnWall, true);
        }
      }

      private void CollisionPosition(float position, bool horizontal) {
        if (horizontal) {
          this.Rect.X = position;
          this.Direction.X = 0;
          this.Velocity.X = 0;
        } else {
          this.Rect.Y = position;
          this.Direction.Y = 0;
          this.Velocity.Y = 0;
        }
      }

      private void HandleWallCollision(Sprite wall, Directions direction) {
        RectangleF WallRect = wall.Rect;
        
        switch (direction) {
          case Directions.Left:
            this.CollisionPosition(WallRect.Right, true);
            break;
          case Directions.Right:
            this.CollisionPosition(WallRect.X - this.Rect.Width, true);
            break;
          case Directions.Up:
            this.CollisionPosition(WallRect.Bottom, false);
            Set(ref this.Stats, (ulong)PlayerStats.Top, true);
            break;
          case Directions.Down:
            this.CollisionPosition(WallRect.Y - this.Rect.Height, false);
            Set(ref this.Stats, (ulong)PlayerStats.Bottom, true);

            if (wall is MovingSprite MovingSprite)
              this.Platform = MovingSprite;
            break;
          default:
            this.Rect.TopLeft(wall.Rect.TopLeft());
            this.Direction = Vector2.Zero;
            break;
        }
      }

      private void Collision(bool horizontal) {
        for (int Index = 0; Index < SpriteGroups[(int)Groups.Collidable].SpriteList.Count; Index++) {
          Sprite Wall = SpriteGroups[(int)Groups.Collidable].SpriteList[Index];

          if (!Wall.Rect.IntersectsWith(this.Rect))
            continue;

          this.HandleWallCollision(Wall, CollisionDirection(this, Wall, horizontal ? Directions.Horizontal : Directions.Vertical));
        }
        for (int Index = 0; Index < SpriteGroups[(int)Groups.SemiCollidable].SpriteList.Count; Index++) {
          Spring SemiWall = (Spring)SpriteGroups[(int)Groups.SemiCollidable].SpriteList[Index];

          if (!SemiWall.Rect.IntersectsWith(this.Rect))
            continue;

          Directions Direction = CollisionDirection(this, SemiWall, horizontal ? Directions.Horizontal : Directions.Vertical);

          if (IsSet(SemiWall.Stats, (byte)DirectionToFlag(Direction)))
            this.HandleWallCollision(SemiWall, Direction);
        }
      }


      public void Lantern(bool active) {
        Shader.SetVariable("LanternEnabled", active);
        Set(ref this.Stats, (ulong)PlayerStats.LanternEnabled, active);
      }

      public void ActivateCheckpoint(Vector2 position, byte index) {
        this.RespawnPos = position;

        for (int Index = 0; Index < this.CheckpointFlags.Length; Index++) {
          Set(ref this.Stats, (ulong)this.CheckpointFlags[index], Index == index);
        }
      }

      public void Teleport(Teleporter teleportPortal) {
        Animations TeleportAnimation = Animations.PlayerTeleport;

        if (IsSet(this.Stats, (ulong)PlayerStats.Invincibility)) {
          TeleportAnimation = Animations.PlayerTeleportGold;
        } else if (IsSet(this.Stats, (ulong)PlayerStats.Honey)) {
          TeleportAnimation = Animations.PlayerTeleportHoney;
        }

        this.PlayAnimation(TeleportAnimation);
        this.Rect.TopLeft(PlatformerLevel.TeleportLocations[teleportPortal.ID]);
        this.StopMovement();
      }


      private void KeepInScreen() {
        if (this.Rect.X < 0) {
          this.CollisionPosition(0, true);
        } else if (this.Rect.Right > LevelData.Dimensions.PixelWidth) {
          this.CollisionPosition( LevelData.Dimensions.PixelWidth - this.Rect.Width, true);
        }

        if (this.Rect.Y < 0) {
          this.CollisionPosition(0, false);
        } else if (this.Rect.Bottom >  LevelData.Dimensions.PixelHeight) {
          this.CollisionPosition( LevelData.Dimensions.PixelHeight - this.Rect.Height, false);
        }
      }

      private void UpdateTimers() {
        for (int Index = 0; Index < this.Timers.Length; Index++) {
          this.Timers[Index].Update();
        }
      }

      public override void Update(float deltaTime) {
        this.OldRect = this.Rect;

        this.UpdateTimers();
        this.Input();
        this.MovementReturn();
        this.Move(deltaTime);
        this.KeepInScreen();
        this.CheckContact();
        this.DeathConditions();

        if (this.Rect.TopLeft() != this.OldRect.TopLeft())
          UpdateSpritePosition(this);
        
        if (IsSet(this.Stats, (ulong)PlayerStats.Animating)) {
          base.Update(deltaTime);

          if (!IsSet(this.Animation.AnimationData.Stats, (byte)AnimationFlags.Playing))
            this.StopAnimation();
        }
      }
    }


    public static uint[] PowerDurations = [5000, 5000, 5000, 5000, 50, 50, 50, 50];
    public static ushort[] Status = [5, 0, 0];
    public static byte CurrentWorld = 1;
    public static byte CurrentLevel = 1;
    

    public static ushort Lives {
      get => Status[(int)PlayerDisplayElements.Lives]; 
      set {
        if (value == 0) {
          GameOver();
        } else {
          Status[(int)PlayerDisplayElements.Lives] = value;
          PlayerDisplay.UpdateLives();
        }
      }
    }

    public static ushort Coins {
      get => Status[(int)PlayerDisplayElements.Coins]; 
      set {
        Status[(int)PlayerDisplayElements.Coins] = value;
        PlayerDisplay.UpdateScore();
      }
    }

    public static byte KeyCoins {
      get => (byte)Status[(int)PlayerDisplayElements.KeyCoins];
      set {
        Status[(int)PlayerDisplayElements.KeyCoins] = value;
        PlayerDisplay.UpdateKeyCoins();
      }
    }
  
  
    public static void GameOver() {
      Console.WriteLine("GAME OVER!");
      Set(ref GlobalStats, (ushort)GlobalFlags.ForceExit, true);
    }
  }
}