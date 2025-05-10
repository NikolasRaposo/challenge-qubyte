Shader "Custom/DrawLineMask"
{
        Properties
    {
        _MainTex       ("MaskIn",    2D)      = "white" {}
        _StartEnd      ("S/E",        Vector) = (0,0,1,1)
        _Thickness     ("T",          Float ) = 0.01
        _NoiseTex      ("Noise Tex",  2D)      = "white" {}
        _NoiseScale    ("Noise Scale",Float ) = 5
        _NoiseAmp      ("Noise Amp",  Float ) = 0.02
        _LineColor     ("Line Color", Color ) = (1,0,0,1)

        // Bola de Fogo
        _FireballTex   ("Fireball Texture", 2D) = "white" {}
        _FireballPos   ("Fireball Position", Float) = 0.2
        _FireballSize  ("Fireball Size",     Float) = 0.1
        _FireballActive("Fireball Active",   Float) = 1

        // Faíscas
        _SparkTex      ("Spark Texture",    2D) = "white" {}
        _SparkDensity  ("Spark Density",     Float) = 10
        _SparkOffset   ("Spark Offset",      Float) = 0.0
        _SparkActive   ("Spark Active",      Float) = 1
        _SparkSize     ("Spark Size",        Float) = 0.05

        // Explosão
        _ExplosionTex  ("Explosion Texture",2D) = "white" {}
        _ExplosionSize ("Explosion Size",   Float) = 0.2
        _ExplosionActive("Explosion Active",Float) = 1
    }
  SubShader
  {
    Tags { "RenderType"="Opaque" }
    Cull Off
    ZWrite Off
    ZTest Always

    Pass
    {
      HLSLPROGRAM
      #include "UnityCG.cginc"   
      // gera full‐screen triangle via SV_VertexID
      #pragma vertex Vert
      #pragma fragment Frag
      #pragma target 4.5

      sampler2D _MainTex;
      float4   _StartEnd;
      float    _Thickness;
      sampler2D _NoiseTex;
      float    _NoiseScale;
      float    _NoiseAmp;
      float4   _LineColor;

      sampler2D _FireballTex;
      float _FireballPos;
      float _FireballSize;
      float _FireballActive;
      sampler2D _SparkTex;
      float _SparkDensity;
      float _SparkOffset;
      float _SparkActive;
      float _SparkSize;
      sampler2D _ExplosionTex;
      float _ExplosionSize;
      float _ExplosionActive;

      struct v2f
      {
        float2 uv   : TEXCOORD0;
        float4 pos  : SV_POSITION;
      };

      v2f Vert(uint id : SV_VertexID)
      {
        // full‐screen tri uv
        float2 uv = float2((id<<1)&2, id&2);
        v2f o;
        o.uv  = uv;
        o.pos = float4(uv * 2 - 1, 0, 1);
        return o;
      }

      float4 Frag(v2f i) : SV_Target
{
    float2 A = _StartEnd.xy;
    float2 B = _StartEnd.zw;
    float2 p = i.uv;
    float2 v = B - A;
    float len2 = dot(v, v);
    float t = saturate(dot(p - A, v) / len2);
    float2 proj = A + v * t;

    // **Linha Base**
    float d = length(p - proj);
    float maskLine = smoothstep(_Thickness, 0, d);

    // **Noise (Raio)**
    float2 n = normalize(float2(-v.y, v.x));
    float noise = tex2D(_NoiseTex, float2(t * _NoiseScale + _Time.y * 0.1, 0)).r;
    float envelope = sin(t * UNITY_PI);
    float2 displacedUV = p + n * ((noise - 0.5) * 2 * _NoiseAmp * envelope);
    float dNoise = length(displacedUV - proj);
    float maskNoise = smoothstep(_Thickness, 0, dNoise);

    // Combine Linha Base e Noise
    float finalLine = max(maskLine, maskNoise);

    // acumula resultado
    float4 finalCol = float4(0,0,0,0);
    
    // **Noise layer (under line)**
    float4 noiseCol = float4(noise, noise, noise, 1);
    finalCol += noiseCol * maskNoise;

    // **Linha base colorida**
    float4 lineColor = lerp(_LineColor * 0.5, _LineColor, t);
    finalCol += lineColor * maskLine;

    // **Camada: Faíscas**
    if (_SparkActive > 0)
    {
        float2 sparkCenter = A + v * t;
        float2 sparkUV = (p - sparkCenter) / _SparkSize + 0.5;
        float sparkMask = smoothstep(1.0, 0.0, length(sparkUV - 0.5)) * _SparkActive;
        float4 sparkSample = tex2D(_SparkTex, sparkUV);
        finalCol += sparkSample * sparkMask;
    }

    // **Camada: Bola de Fogo**
    if (_FireballActive > 0)
    {
        float2 fireballCenter = A + v * _FireballPos;
        float2 fireballUV = (p - fireballCenter) / _FireballSize + 0.5;
        float fireballMask = smoothstep(1.0, 0.0, length(fireballUV - 0.5)) * _FireballActive;
        float4 fireballSample = tex2D(_FireballTex, fireballUV);
        finalCol += fireballSample * fireballMask;
    }

    // **Camada: Explosão**
    if (_ExplosionActive > 0)
    {
        float2 explosionUV = (p - B) / _ExplosionSize + 0.5;
        float explosionMask = smoothstep(1.0, 0.0, length(explosionUV - 0.5)) * _ExplosionActive;
        float4 explosionSample = tex2D(_ExplosionTex, explosionUV);
        finalCol += explosionSample * explosionMask;
    }

    return float4(finalCol.rgb, 1);
}
        ENDHLSL
        }
    }
    }