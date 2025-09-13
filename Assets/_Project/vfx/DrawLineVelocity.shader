Shader "Custom/DrawLineVelocity"
{
  Properties
  {
    _StartEnd      ("S/E",       Vector) = (0,0,1,1)
    _PrevStartEnd  ("Prev S/E",  Vector) = (0,0,1,1)
    _DeltaTime     ("Delta Time",Float)  = 0.016
  }
  SubShader
  {
    // ESSAS TAGS GARANTEM QUE O UNITY Saiba onde e quando usar
    Tags { "RenderType"="Opaque" "Queue"="Overlay" }
    LOD 100

    Pass
    {
      HLSLPROGRAM
      #include "UnityCG.cginc"
      #pragma vertex Vert
      #pragma fragment FragVel
      #pragma target 4.5

      struct v2f { float2 uv: TEXCOORD0; float4 pos: SV_POSITION; };
      v2f Vert(uint id:SV_VertexID)
      {
        float2 uv = float2((id<<1)&2, id&2);
        v2f o;
        o.uv  = uv;
        o.pos = float4(uv * 2 - 1, 0, 1);
        return o;
      }

      float4 _StartEnd, _PrevStartEnd;
      float  _DeltaTime;

      float4 FragVel(v2f i):SV_Target
      {
        float2 A   = _StartEnd.xy;
        float2 B   = _StartEnd.zw;
        float2 v   = B - A;
        float  t   = saturate(dot(i.uv - A, v)/dot(v,v));
        float2 cur = A + v * t;

        float2 A0   = _PrevStartEnd.xy;
        float2 B0   = _PrevStartEnd.zw;
        float2 v0   = B0 - A0;
        float  t0   = saturate(dot(i.uv - A0, v0)/dot(v0,v0));
        float2 prev = A0 + v0 * t0;

        float2 vel = (cur - prev) / _DeltaTime;

        float mask = 1;
        float2 enc = vel * 0.5 + 0.5;
        return float4(enc, mask, 1);
      }
      ENDHLSL
    }
  }
  Fallback "Diffuse"
}