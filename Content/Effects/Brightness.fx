sampler TextureSampler : register(s0);


float3 InnerColor = float3(1.0, 1.0, 1.0); 
float3 MiddleColor = float3(0.3, 0.5, 1.0);
float3 OuterColor = float3(0.5, 0.3, 0.6); 
float3 FarColor = float3(0.0f, 0.0f, 0.0f);
float3 BeyondColor = float3(0.0, 0.0, 0.0);

float2 PlayerScreenPosition;
float2 ScreenSize;  

float InnerRadius;   
float MiddleRadius;
float OuterRadius;
float FarRadius;

float InnerBrightness;
float MiddleBrightness;
float OuterBrightness;
float FarBrightness;
float BeyondBrightness;

float PixelSize = 8.0f;

float Brightness;
bool LanternEnabled;


float4 MainPS(float4 color : COLOR0, float2 TexCoord : TEXCOORD0) : COLOR0 {
  float4 TexColor = tex2D(TextureSampler, TexCoord);
  float3 ColorTint = float3(1.0f, 1.0f, 1.0f);
  float LightMultiplier = Brightness;
  
  if (LanternEnabled) {
    float2 PixelatedCoord = floor(TexCoord * ScreenSize / PixelSize) * PixelSize + (PixelSize * 0.5f);
    float Distance = distance(PixelatedCoord, PlayerScreenPosition);
    float LanternMultiplier;
    
    if (Distance < InnerRadius) {
      ColorTint = InnerColor;
      LanternMultiplier = InnerBrightness;
    } else if (Distance < MiddleRadius) {
      float Normalized = (Distance - InnerRadius) / (MiddleRadius - InnerRadius);
      ColorTint = lerp(InnerColor, MiddleColor, Normalized);
      LanternMultiplier = lerp(InnerBrightness, MiddleBrightness, Normalized);
    } else if (Distance < OuterRadius) {
      float Normalized = (Distance - MiddleRadius) / (OuterRadius - MiddleRadius);
      ColorTint = lerp(MiddleColor, OuterColor, Normalized);
      LanternMultiplier = lerp(MiddleBrightness, OuterBrightness, Normalized);
    } else if (Distance < FarRadius) {
      float Normalized = (Distance - OuterRadius) / (FarRadius - OuterRadius);
      ColorTint = lerp(OuterColor, FarColor, Normalized);
      LanternMultiplier = lerp(OuterBrightness, FarBrightness, Normalized);
    } else {
      float Normalized = saturate((Distance - OuterRadius) / 200.0f);
      ColorTint = lerp(OuterColor, BeyondColor, Normalized);
      LanternMultiplier = lerp(OuterBrightness, BeyondBrightness, Normalized);
    }
        
    LightMultiplier *= LanternMultiplier;
  }
  
  return TexColor * float4(ColorTint * LightMultiplier, 1.0f);
}


technique Technique1 {
  pass Pass1 {
    PixelShader = compile ps_2_0  MainPS();
  }
}