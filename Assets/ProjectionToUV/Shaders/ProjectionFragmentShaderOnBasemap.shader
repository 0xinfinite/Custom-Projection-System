// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "UVProjection/ProjectionFragmentShaderOnBasemap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
	_BaseMap("BaseMap", 2D) = "white" {}
[KeywordEnum(Off, On)] _UseNormal("Use Normal Map?", Float) = 0
_BlankColor("Color of outside", Color) = (1,1,1,0)
[KeywordEnum(Off, On)]_CLIPPINGNORMAL("Cliping Normal", Float) = 0
_NormalClip("Normal Clip", Range(-1,1))=-0.5
_Bias("Projection Shadow Bias", Float) = 1.994
[KeywordEnum(Off, On)]_SENDNORMALDOT("Send Normal", Float) = 0
_NormalOffset("Normal Offset", Float) = 0.01
	}
		SubShader
	{
		//Tags { "RenderType" = "Opaque" }
		//Tags{"LightMode" = "_ProjectionPass"}
		//LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature _ _CLIPPINGNORMAL_OFF _CLIPPINGNORMAL_ON
			#pragma shader_feature _ _SENDNORMALDOT_OFF _SENDNORMALDOT_ON
			//#pragma shader_feature _USENORMAL_OFF _USENORMAL_ON

			#include "UnityCG.cginc"
#include "../ShaderLibrary/Projection.hlsl"

		float4x4 _Projection;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD1;
				float4 projUV : TEXCOORD2;
				float3 normal : NORMAL;
            };

			sampler2D _BaseMap;
            sampler2D _MainTex;
            float4 _MainTex_ST;
			//sampler2D _CameraDepthTexture;
			uniform half4 _MainTex_TexelSize;
			float4 _BlankColor;
			float _NormalClip;
			float3 _ProjectorWorldPos;
			sampler2D _DepthMap;
			float _Bias;
			float _NormalOffset;

            v2f vert (appdata v)
            {
                v2f o;
				v.vertex.xyz += v.normal.xyz*_NormalOffset;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);


				//float4 uv = float4(0, 0, 0, 1);
				//uv.xy = float2(1, _ProjectionParams.x)*(v.uv.xy* float2(2, 2) - float2(1, 1));
				


				o.uv = v.uv;//TRANSFORM_TEX(v.uv, _MainTex);


				//float4 projVertex = mul(_Projection, mul(unity_ObjectToWorld, v.vertex));

				o.projUV = projectUV(_Projection, v.vertex);//ComputeScreenPos(projVertex);

				o.normal = UnityObjectToWorldNormal(v.normal);

                return o;
            }

			float4 returnColorAndNormalDot(float4 color, float dot) {
#if _SENDNORMALDOT_ON
				return float4(color.rgb, dot*0.5+0.5);
#endif
				return color;
			}

			float4 frag (v2f i) : SV_Target
            {
				float4 base = tex2D(_BaseMap,i.uv);


				float dot = 1;
#if _CLIPPINGNORMAL_ON
				dot = NormalDotProjector(i.normal, i.worldPos, _ProjectorWorldPos);
				//return dot;
				if (dot < _NormalClip)
				{
					discard;
				}
#endif
				//return dot;

				float2 uv = ProjectionUVToTex2DUV(i.projUV);
				if (ClipBackProjection(i.projUV.z)) {

					discard;
				}
				if (ClipUVBoarder(uv)) {

					discard;
				}

				

# if UNITY_UV_STARTS_AT_TOP
				//if (_MainTex_TexelSize.y < 0)
				uv.y = 1 - uv.y;
# endif

				
				float depthFromPos = DepthFromProjection(i.projUV);
				float depthFromMap = DepthFromDepthmap(_DepthMap, uv, _Bias);
				if (ClipProjectionShadow(depthFromPos, depthFromMap)) {
					discard;
				}



                // sample the texture
				float4 col = tex2D(_MainTex, uv);
				clip(col.a-0.1);
                return returnColorAndNormalDot(col, dot);
            }
            ENDCG
        }
    }
}
