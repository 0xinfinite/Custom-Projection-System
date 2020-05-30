Shader "Unlit/DilationShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
	_TEXEL_DIST("Texel distance", float) = 0.1
		_MAX_STEPS("Max Steps", int)=1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
			float _TEXEL_DIST;
			int _MAX_STEPS;

			uniform half4 _MainTex_TexelSize;


			fixed4 frag(v2f i) : SV_Target
			{
				float2 offsets[8] = {float2(-_TEXEL_DIST, 0),float2(_TEXEL_DIST, 0),
			float2(0, _TEXEL_DIST),float2(0, -_TEXEL_DIST),
			float2(-_TEXEL_DIST, _TEXEL_DIST),float2(_TEXEL_DIST, _TEXEL_DIST),
			float2(_TEXEL_DIST, -_TEXEL_DIST),float2(-_TEXEL_DIST, _TEXEL_DIST)};
			float2 uv = i.uv;

                // sample the texture
                float4 sample = tex2D(_MainTex, uv);
				float4 sampleMax = sample;

				for (int i = 0; i < _MAX_STEPS; ++i) {
					float2 curUV = uv + offsets[i] * _MainTex_TexelSize.xy;
					float4 offsetsample = tex2D(_MainTex, curUV);
					sampleMax = max(offsetsample, sampleMax);
				}

				sample = sampleMax;

                return sample;
            }
            ENDCG
        }
    }
}
