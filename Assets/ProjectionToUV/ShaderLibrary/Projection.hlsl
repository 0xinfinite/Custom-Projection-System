﻿#ifndef CUSTOM_PROJECTION_INCLUDED
#define CUSTOM_PROJECTION_INCLUDED

			float4 UVSpreadOnVertex(float2 inputUV) {
				float4 uv = float4(0, 0, 0, 1);
				uv.xy = float2(1, _ProjectionParams.x)*(inputUV.xy* float2(2, 2) - float2(1, 1));
				return uv;
			}

			float4 projectUV(float4x4 projection, float4 inputVertex) {
				float4 projVertex = mul(projection, mul(unity_ObjectToWorld, inputVertex));

				return ComputeScreenPos(projVertex);
			}
			
			float2 ProjectionUVToTex2DUV(float4 projUV) {
				return projUV.xy / projUV.w;
			}

			bool ClipUVBoarder(float2 uv) {
				if (uv.x < 0 || uv.x>1) return true;
				if (uv.y < 0 || uv.y>1) return true;

				return false;
			}

			bool ClipBackProjection(float4 projUV) {
				if (projUV.z < 0) return true;
				
				return false;
			}

			float NormalDotProjector(float3 normal, float3 worldPos, float3 projectorPos) {
				return dot(normal, normalize(projectorPos- worldPos));

			}

			float DepthFromProjection(float4 projUV) {
				return 1 - projUV.z / projUV.w;
			}

			float DepthFromDepthmap(sampler2D depthMap, float2 projectedUV, float bias) {
				return tex2D(depthMap, projectedUV).r*bias;
			}

			bool ClipProjectionShadow(float depthFromPos, float depthFromMap) {
				if (depthFromPos - depthFromMap < 0) return true;

				return false;
			}


#endif

			//#define CLIP_BACK_PROJECTION if (i.projUV.z < 0) //return _BlankColor;
			//#define CLIP_BOARDER_X if (uv.x < 0 || uv.x>1) //return _BlankColor;
			//#define CLIP_BOARDER_Y if (uv.y < 0 || uv.y>1) //return _BlankColor;
			//#define CLIP_NORMAL_DOT if (dot(i.normal, normalize(i.worldPos - _ProjectorWorldPos)) > _NormalClip) 