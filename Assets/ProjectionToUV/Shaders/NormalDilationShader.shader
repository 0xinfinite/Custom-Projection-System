Shader "Unlit/NormalDilationShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
	_NormalDotThresholdMin("Normal Dot Threshold Min", float) = 0.08
		_NormalDotThresholdMax("Normal Dot Threshold Max", float) = 0.1
	_TEXEL_DIST("Texel distance", float) = 0.1
		_MAX_STEPS("Max Steps", int)=1
		_DIST_PERSTEP("Distance Per Steps", float)=1
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
#include "../ShaderLibrary/Projection.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				//float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				//float3 normal : NORMAL;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float3 _ProjectorWorldPos;
			float _NormalDotThresholdMin;
			float _NormalDotThresholdMax;
			float _DIST_PERSTEP;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				//o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }
			float _TEXEL_DIST;
			int _MAX_STEPS;

			uniform half4 _MainTex_TexelSize;

			float Map(float x, float in_min, float in_max, float out_min, float out_max) {
				return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
			}


			fixed4 frag(v2f i) : SV_Target
			{ 
				float2 uv = i.uv;
				float4 sample = tex2D(_MainTex, uv);
				//sample.a*0.5+0.5;
				//float dot = NormalDotProjector(/*i.normal*/, i.vertex, _ProjectorWorldPos);
				if (sample.a > _NormalDotThresholdMax|| sample.a< _NormalDotThresholdMin) return sample;

				float2 offsets[8] = {float2(-_TEXEL_DIST, 0),float2(_TEXEL_DIST, 0),
			float2(0, _TEXEL_DIST),float2(0, -_TEXEL_DIST),
			float2(-_TEXEL_DIST, _TEXEL_DIST),float2(_TEXEL_DIST, _TEXEL_DIST),
			float2(_TEXEL_DIST, -_TEXEL_DIST),float2(-_TEXEL_DIST, _TEXEL_DIST)};
			

                // sample the texture
				float4 sampleMax = sample;

				for (int i = 0; i < _MAX_STEPS; ++i) {
					float2 curUV = uv + offsets[i]* (_MAX_STEPS*_DIST_PERSTEP) * _MainTex_TexelSize.xy;
					float4 offsetsample = tex2D(_MainTex, curUV);
					sampleMax = max(offsetsample, sampleMax);
				}

				sample = sampleMax; //lerp( sampleMax, sample,Map( abs((_NormalDotThresholdMax- _NormalDotThresholdMin)*0.5-(sample.a- _NormalDotThresholdMin))
					//,0, _NormalDotThresholdMax - _NormalDotThresholdMin,0,1));

                return sample;
            }
            ENDCG
        }
    }
}
