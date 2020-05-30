// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "UVProjection/UVProjectionShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
[KeywordEnum(Off, On)] _UseNormal("Use Normal Map?", Float) = 0
_BlankColor("Color of outside", Color) = (1,1,1,0)
[KeywordEnum(Off, On)]_CLIPPINGNORMAL("Cliping Normal", Float) = 0
_NormalClip("Normal Clip", Range(-1,1))=-0.5
_Bias("Projection Shadow Bias", Float) = 1.994
_TempValue("Temp", Float) = 0
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
                //float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				float4 projUV : TEXCOORD1;
				float3 normal : NORMAL;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			//sampler2D _CameraDepthTexture;
			uniform half4 _MainTex_TexelSize;
			float4 _BlankColor;
			float _NormalClip;
			float3 _ProjectorWorldPos;
			sampler2D _DepthMap;
			float _Bias;

			float _TempValue;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);


				//float4 uv = float4(0, 0, 0, 1);
				//uv.xy = float2(1, _ProjectionParams.x)*(v.uv.xy* float2(2, 2) - float2(1, 1));
				o.vertex = UVSpreadOnVertex(v.uv);//uv;


				//o.uv = TRANSFORM_TEX(v.uv, _MainTex);


				//float4 projVertex = mul(_Projection, mul(unity_ObjectToWorld, v.vertex));

				o.projUV = projectUV(_Projection, v.vertex);//ComputeScreenPos(projVertex);

				o.normal = UnityObjectToWorldNormal(v.normal);

                return o;
            }

			float4 frag (v2f i) : SV_Target
            {
				float2 uv = ProjectionUVToTex2DUV(i.projUV);//i.projUV.xy / i.projUV.w;
				if (ClipBackProjection(i.projUV.z)) { return _BlankColor; }
				if (ClipUVBoarder(uv)) return _BlankColor;
				//if (uv.y < 0 || uv.y>1) return _BlankColor;

				#if _CLIPPINGNORMAL_ON
				if (NormalDotProjector(i.normal, i.worldPos, _ProjectorWorldPos)< _NormalClip) return _BlankColor;
				#endif

# if UNITY_UV_STARTS_AT_TOP
				//if (_MainTex_TexelSize.y < 0)
				uv.y = 1 - uv.y;
# endif

				//return dot(i.normal, normalize(i.worldPos - _ProjectorWorldPos));
				//return tex2D(_DepthMap, uv).r;
				
				float depthFromPos = DepthFromProjection(i.projUV);
				
				float depthFromMap = DepthFromDepthmap(_DepthMap, uv, _Bias);
				//return depth;
				//return depthFromPos - depthFromMap;
				//return (i.projUV.z-depth)*_TempValue;
				if (ClipProjectionShadow(depthFromPos, depthFromMap)) { return _BlankColor; }



                // sample the texture
				float4 col = tex2D(_MainTex, uv);

                return col;
            }
            ENDCG
        }
    }
}
