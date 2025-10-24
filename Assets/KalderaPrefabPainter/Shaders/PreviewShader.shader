Shader "Kaldera/Preview" {
	Properties{
		_Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
	}

		CGINCLUDE

#include "UnityCG.cginc"
	fixed4 _Color;

	struct v2f {
		half4 pos : SV_POSITION;
		half2 uv : TEXCOORD0;
		fixed4 vertexColor : COLOR;
	};

	v2f vert(appdata_full v) {
		v2f o;

		o.pos = UnityObjectToClipPos(v.vertex);
		o.vertexColor = v.color * _Color;

		return o;
	}

	fixed4 frag(v2f i) : COLOR{
		return i.vertexColor * _Color;
	}

		ENDCG

		SubShader {
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		Cull Back
		Lighting Off
		ZWrite Off
		ZTest Always
		Fog{ Mode Off }
		Blend SrcAlpha OneMinusSrcAlpha

		Pass{

			CGPROGRAM

#pragma vertex vert
#pragma fragment frag
#pragma fragmentoption ARB_precision_hint_fastest

			ENDCG

		}

	}
	FallBack Off
}
