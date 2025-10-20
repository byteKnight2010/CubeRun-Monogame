using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using static Cube_Run_C_.Tools;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Camera;
using static Cube_Run_C_.Tools.Assets;
using static Cube_Run_C_.Tools.BitMask;
using RectangleF = System.Drawing.RectangleF;


namespace Cube_Run_C_ {
  public class UI {
    [Flags]
    public enum MenuStatus : byte {
      UpdateScreen = 1 << 0,
      UpdateBrightness = 1 << 1,
      UpdateVolume = 1 << 2,
      DraggingSlider = 1 << 3,
      Fullscreen = 1 << 4,
      Quit = 1 << 5,
      Controls = 1 << 6,
      Active = 1 << 7
    }

    
    public static class PauseMenu {
      public static readonly List<Dimensions> ValidResolutions = GetValidResolutions();
      private static readonly Slider[] Sliders = [null, null, null];
      private static readonly Button[] MenuButtons = [null, null, null];
      private static DisplayText[] DisplayTexts = [DisplayText.Empty, DisplayText.Empty, DisplayText.Empty, DisplayText.Empty, DisplayText.Empty, DisplayText.Empty];
      private static readonly MenuStatus[] SliderFlags = [MenuStatus.UpdateScreen, MenuStatus.UpdateBrightness, MenuStatus.UpdateVolume];
      private static readonly MenuStatus[] ButtonFlags = [MenuStatus.Fullscreen, MenuStatus.Quit, MenuStatus.Controls];
      public static float[] SelectedValues = [0f, 1f, 1f];
      public static SpriteFont Font;
      public static Point StoredMousePosition = Point.Zero;
      private static byte SelectedSlider = 1;
      private static byte SetResolutionIndex;
      public static byte Status = 0b00000000;


      public static void Initialize() {
        int HalfWidth = Camera.HalfScreen.Width;
        (float, float)[] SliderCaps = [(ValidResolutions.Count - 1f, 100f / ValidResolutions.Count), (2.0f, 0.02f), (1f, 0.1f)];

        SelectedValues[0] = SetResolutionIndex;

        for (int Index = 0; Index < 3; Index++) {
          Sliders[Index] = new(Color.DimGray, new(HalfWidth >> 1, 180 + (160 * Index)), [0f, SliderCaps[Index].Item1], SliderCaps[Index].Item2, SelectedValues[Index], HalfWidth);
          MenuButtons[Index] = new(GetTexture("Images/UIImages/MainMenuButton"), new(HalfWidth * (0.5f * (Index + 1)), 620));
        }

        Sliders[0].Selected = true;

        UpdateScale();
      }


      private static List<Dimensions> GetValidResolutions() {
        Dimensions CalculateResolution(ushort width) {
          int RoundedWidth = (width + 7) / 8 * 8;

          return new(RoundedWidth, (int)Math.Ceiling(RoundedWidth / SCREEN_RATIO / 8f) * 8);
        }

        List<Dimensions> GenerateResolutions(float startWidth, float scaleFactor, bool descending) {
          List<Dimensions> Resolutions = new();
          float Width = startWidth;

          while (true) {
            Dimensions Resolution = CalculateResolution((ushort)Width);

            if (descending) {
              if (Resolution.Width < MINIMUM_DIMENSIONS.Width || Resolution.Height < MINIMUM_DIMENSIONS.Height) break;
            } else {
              if (Resolution.Width > MonitorDimensions.Width || Resolution.Height > MonitorDimensions.Height) break;
            }

            Resolutions.Add(Resolution);
            Width *= scaleFactor;
          }

          return Resolutions;
        }


        List<Dimensions> SmallerResolutions = GenerateResolutions(DEFAULT_DIMENSIONS.Width / 1.2f, 1 / 1.2f, true);
        List<Dimensions> LargerResolutions = GenerateResolutions(DEFAULT_DIMENSIONS.Width, 1.2f, false);

        if (LargerResolutions.Count > 0) {
          int LargerResolutionCount = LargerResolutions.Count - 1;
          if (LargerResolutions[LargerResolutionCount].Width == MonitorDimensions.Width) {
            LargerResolutions.RemoveAt(LargerResolutionCount);
          }
        }

        SmallerResolutions.Reverse();
        SetResolutionIndex = (byte)SmallerResolutions.Count;
        SmallerResolutions.AddRange(LargerResolutions);

        return SmallerResolutions;
      }

      private static void NavigateSlider(sbyte direction) {
        SelectedSlider = (byte)(((SelectedSlider - 1 + direction + Sliders.Length) % Sliders.Length) + 1);

        for (int Index = 0; Index < Sliders.Length; Index++) {
          Sliders[Index].Selected = Sliders[Index] == Sliders[SelectedSlider - 1];
        }
      }

      private static void UpdateText(int index) {
        string Text = index switch {
          0 => $"Window-Size: ({ValidResolutions[(int)SelectedValues[0]].Width} x {ValidResolutions[(int)SelectedValues[0]].Height})",
          1 => $"Brightness: {SelectedValues[1]:F2}x",
          2 => $"Volume: {SelectedValues[2] * 100:F2}%",
          3 => "Fullscreen",
          4 => "Quit",
          5 => "Controls",
          _ => ""
        };

        Vector2 TextSize = Font.MeasureString(Text);
        int HalfWidth = Camera.HalfScreen.Width;

        if (index < 3) {
          DisplayTexts[index] = new(Text, new(HalfWidth - TextSize.X * Camera.Scale * 0.5f, (Sliders[index].OriginalPosition.Y - TextSize.Y * 1.5f) * Camera.Scale));
        } else {
          Vector2 ButtonCenter = MenuButtons[index - 3].Rect.FCenter();
          DisplayTexts[index] = new(Text, new(ButtonCenter.X - TextSize.X * Camera.Scale * 0.5f, ButtonCenter.Y - TextSize.Y * Camera.Scale * 0.5f));
        }
      }


      public static void Draw(SpriteBatch spriteBatch) {
        if (!IsSet(Status, (byte)MenuStatus.Active))
          return;

        for (int Index = 0; Index < Sliders.Length; Index++) {
          Sliders[Index].Draw(spriteBatch);
          MenuButtons[Index].Draw(spriteBatch);
        }

        for (int Index = 0; Index < DisplayTexts.Length; Index++) {
          spriteBatch.DrawString(Font, DisplayTexts[Index].Text, DisplayTexts[Index].Position, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }
      }

      public static void UpdateScale() {
        for (int Index = 0; Index < Sliders.Length; Index++) {
          Sliders[Index].UpdateScale();
          MenuButtons[Index].UpdateScale();
        }
        for (int Index = 0; Index < DisplayTexts.Length; Index++) {
          UpdateText(Index);
        }
      }

      public static void Update() {
        if (!IsSet(Status, (byte)MenuStatus.Active))
          return;

        for (byte Index = 0; Index < Sliders.Length; Index++) {
          Sliders[Index].Update();

          if (Sliders[Index].Value != SelectedValues[Index]) {
            SelectedValues[Index] = Sliders[Index].Value;
            Set(ref Status, (byte)SliderFlags[Index], true);
            UpdateText(Index);
          }
        }
        for (int Index = 0; Index < MenuButtons.Length; Index++) {
          MenuButtons[Index].Update();

          if (MenuButtons[Index].Clicking) {
            MenuButtons[Index].Clicking = false;
            Set(ref Status, (byte)ButtonFlags[Index], true);
          }
        }

        if (InputManager.IsKeyPressed(Keys.Up) || InputManager.IsButtonPressed(Buttons.DPadUp))
          NavigateSlider(1);
        if (InputManager.IsKeyPressed(Keys.Down) || InputManager.IsButtonPressed(Buttons.DPadDown))
          NavigateSlider(-1);
      }
    }

    public static class FocusOverlay {
      private static Texture2D OverlayPixel;
      private static Color OverlayColor = Color.Black * 0.5f;
      private static Vector2 TextPosition = Vector2.Zero;
      public static SpriteFont Font;
      public const string Text = "Game paused to save resources.";
      public static bool Active = false;


      public static void Initialize() {
        OverlayPixel = new(Assets.GraphicsDevice, 1, 1);
        OverlayPixel.SetData([Color.Black]);
        UpdateScale();
      }


      public static void UpdateScale() {
        Vector2 TextSize = Font.MeasureString(Text);
        TextPosition = new((ScreenDimensions.Width >> 1) - TextSize.X * 0.5f, (ScreenDimensions.Height >> 1) - TextSize.Y * 0.5f);
      }


      public static void Draw(SpriteBatch spriteBatch) {
        if (!Active)
          return;

        spriteBatch.Draw(OverlayPixel, ScreenRect, OverlayColor);
        spriteBatch.DrawString(Font, Text, TextPosition, Color.White);
      }
    }


    public class Slider {
      private Texture2D BackgroundTexture;
      private Texture2D SliderTexture;
      private readonly Rectangle OriginalBackgroundRect;
      private Rectangle BackgroundRect;
      private RectangleF OriginalSliderRect;
      private RectangleF SliderRect;
      public readonly Vector2 OriginalPosition;
      public Vector2 Position;
      private float[] ValueBounds;
      private readonly float OriginalWidth;
      private float Width;
      private float CurrentValue;
      public float Value {
        get { return this.CurrentValue; }
        set {
          this.CurrentValue = MathHelper.Clamp(value, this.ValueBounds[0], this.ValueBounds[1]);
          this.UpdateSliderPosition();
        }
      }
      private readonly float Increment;
      private bool Dragging = false;
      public bool Selected = false;


      public Slider(Color backgroundColor, Vector2 position, float[] valueBounds, float increment, float startValue, float width) {
        this.BackgroundTexture = new(Camera.Graphics.GraphicsDevice, 1, 1);
        this.BackgroundTexture.SetData([backgroundColor]);
        this.OriginalPosition = position;
        this.Position = position;
        this.ValueBounds = valueBounds;
        this.OriginalWidth = width;
        this.Width = width;
        this.CurrentValue = startValue;
        this.Increment = Math.Max(1, increment);

        this.UpdateSliderTexture();

        this.OriginalBackgroundRect = new((int)position.X, (int)(position.Y + this.SliderTexture.Height * 0.5f - (this.SliderTexture.Height * 0.25f)), (int)width, (int)(this.SliderTexture.Height * 0.5f));
        this.BackgroundRect = this.OriginalBackgroundRect;

        this.UpdateSliderPosition();
      }


      public void Draw(SpriteBatch spriteBatch) {
        spriteBatch.Draw(this.BackgroundTexture, this.BackgroundRect, Color.White);
        spriteBatch.Draw(this.SliderTexture, this.SliderRect.TopLeft(), null, Color.White, 0f, Vector2.Zero, Camera.Scale, SpriteEffects.None, 0f);
      }


      private void UpdateSliderTexture() {
        if (this.Value == this.ValueBounds[0]) {
          this.SliderTexture = GetTexture("Images/UIImages/SliderLeft");
        } else if (this.Value == this.ValueBounds[1]) {
          this.SliderTexture = GetTexture("Images/UIImages/SliderRight");
        } else {
          this.SliderTexture = GetTexture("Images/UIImages/SliderCenter");
        }
      }

      private void UpdateSliderPosition() {
        this.OriginalSliderRect = this.SliderTexture.GetRectangleF(new(this.Position.X + ((this.Value - this.ValueBounds[0]) / (this.ValueBounds[1] - this.ValueBounds[0])) * this.Width - (this.SliderTexture.Width >> 1), this.Position.Y));
        this.SliderRect = this.OriginalSliderRect;
      }

      public void UpdateScale() {
        this.BackgroundRect = this.OriginalBackgroundRect.Scaled(Camera.Scale);

        this.Position = this.OriginalPosition * Camera.Scale;
        this.Width = this.OriginalWidth * Camera.Scale;

        this.SliderRect = new(this.Position.X + (this.Value - this.ValueBounds[0]) / (this.ValueBounds[1] - this.ValueBounds[0]) * this.Width - this.SliderTexture.Width * Camera.Scale * 0.5f, this.Position.Y, this.SliderTexture.Width * Camera.Scale, this.SliderTexture.Height * Camera.Scale);
      }

      public void Update() {
        Vector2 MousePosition = InputManager.MousePosition().ToVector2();

        if (!IsSet(PauseMenu.Status, (byte)MenuStatus.DraggingSlider) && !this.Dragging && InputManager.IsMouseButtonDown(MouseButton.Left) && this.SliderRect.Contains(MousePosition.ToPointF())) {
          Set(ref PauseMenu.Status, (byte)MenuStatus.DraggingSlider, true);
          this.Dragging = true;
        }
        if (InputManager.IsMouseButtonReleased(MouseButton.Left)) {
          Set(ref PauseMenu.Status, (byte)MenuStatus.DraggingSlider, false);
          this.Dragging = false;
        }

        if (this.Dragging) {
          this.Value = this.ValueBounds[0] + (MousePosition.X - this.Position.X) / this.Width * (this.ValueBounds[1] - this.ValueBounds[0]);
          this.UpdateSliderTexture();
        }

        if (this.Selected) {
          if (InputManager.IsKeyPressed(Keys.Left)) {
            this.Value = Math.Max(this.ValueBounds[0], this.Value - this.Increment);
            this.UpdateSliderTexture();
          }

          if (InputManager.IsKeyPressed(Keys.Right)) {
            this.Value = Math.Min(this.ValueBounds[1], this.Value + this.Increment);
            this.UpdateSliderTexture();
          }
        }
      }
    }

    public class Button {
      private Texture2D Texture;
      private Rectangle OriginalRect;
      public Rectangle Rect;
      public bool Clicking = false;
      public bool Selected = false;


      public Button(Texture2D image, Vector2 position) {
        this.Texture = image;
        this.OriginalRect = new((int)(position.X - (this.Texture.Width >> 1)), (int)(position.Y), this.Texture.Width, this.Texture.Height);
        this.Rect = this.OriginalRect;
      }


      public void Draw(SpriteBatch spriteBatch) => spriteBatch.Draw(this.Texture, this.Rect, Color.White);

      public void UpdateScale() => this.Rect = this.OriginalRect.Scaled(Camera.Scale);
      
      public void Update() {
        if ((this.Rect.Contains(InputManager.MousePosition()) && InputManager.IsMouseButtonPressed(MouseButton.Left)) || (this.Selected && InputManager.IsKeyPressed(Keys.Enter))) {
          if (InputManager.IsMouseButtonPressed(MouseButton.Left))
            this.Clicking = true;
        } else {
          this.Clicking = false;
        }
      }
    }
  }
}