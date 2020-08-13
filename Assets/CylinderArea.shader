// From https://forum.unity.com/threads/cull-rendering-between-camera-and-stencil-mask.544280/
Shader "Custom/CylinderArea"
{
	Properties
	{
		[HDR] _Color("Main Color", Color) = (1,1,1,1)
		_CylinderCenter("Cylinder Center", Vector) = (0,0,0,0)
		_CylinderRadius("Cylinder Radius", Float) = 3

		
		_ShadowPow("Shadow Strength", Range(0, 1)) = 0.5
		_LerpValue("Shadow Lerp Corretion", Range(0, 1)) = 0.2

		[Header(Stencil)]
		_Stencil("Stencil ID [0;255]", Float) = 0
		_ReadMask("ReadMask [0;255]", Int) = 255
		_WriteMask("WriteMask [0;255]", Int) = 255
		[Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comparison", Int) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilOp("Stencil Operation", Int) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilFail("Stencil Fail", Int) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilZFail("Stencil ZFail", Int) = 0

		[Header(Rendering)]
		[Enum(UnityEngine.Rendering.CullMode)] _Culling("Culling", Int) = 2
		[Enum(Off,0,On,1)] _ZWrite("ZWrite", Int) = 1
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Int) = 4
		_Offset("Offset", Float) = 0
		[Enum(None,0,Alpha,1,Red,8,Green,4,Blue,2,RGB,14,RGBA,15)] _ColorMask("Writing Color Mask", Int) = 15
	}

		SubShader
		{
			Stencil
			{
				Ref[_Stencil]
				ReadMask[_ReadMask]
				WriteMask[_WriteMask]
				Comp[_StencilComp]
				Pass[_StencilOp]
				Fail[_StencilFail]
				ZFail[_StencilZFail]
			}

			Tags{ "Queue" = "Geometry" "RenderType" = "Opaque" }
			LOD 200
			Cull[_Culling]
			Offset[_Offset],[_Offset]
			ZWrite[_ZWrite]
			ZTest[_ZTest]
			ColorMask[_ColorMask]

			CGPROGRAM
			#pragma surface surf Standard

			struct Input {
				 float3 worldPos;
			 };
			 half4 _Color;
			 float4 _CylinderCenter;
			float _CylinderRadius;
			float _ShadowPow;
			float _LerpValue;
			float squaredHorizontalDistance(float3 a, float3 b) {
				float3 ab = b - a;
				return ab.x * ab.x + ab.z * ab.z;
			}
			 void surf(Input IN, inout SurfaceOutputStandard o)
			 {
				 float squaredDistance = squaredHorizontalDistance(_CylinderCenter.xyz, IN.worldPos);
				 if (squaredDistance > _CylinderRadius)
				 {
					 o.Albedo = _Color * _ShadowPow;
				 }
				 else
				 {
					 o.Albedo = _Color * max(_ShadowPow, (1 - squaredDistance * _LerpValue));
				 }
			 }

			ENDCG

				// shadow caster pass
				Pass{
					Name "Caster"
					Tags{ "LightMode" = "ShadowCaster" }
					Cull[_Culling]
					Offset[_Offset],[_Offset]
					ZWrite[_ZWrite]
					ZTest[_ZTest]
					ColorMask[_ColorMask]

					CGPROGRAM
					#pragma vertex vert
					#pragma fragment frag
					#pragma target 2.0
					#pragma multi_compile_shadowcaster
					#pragma multi_compile_instancing // allow instanced shadow pass for most of the shaders
					#include "UnityCG.cginc"

					float4 _CylinderCenter;
					float _CylinderRadius;

					float squaredHorizontalDistance(float3 a, float3 b) {
						float3 ab = b - a;
						return ab.x * ab.x + ab.z * ab.z;
					}


					struct v2f {
						V2F_SHADOW_CASTER;
						float3 worldPos : TEXCOORD0;
						UNITY_VERTEX_OUTPUT_STEREO
					};

					v2f vert(appdata_base v)
					{
						v2f o;
						UNITY_SETUP_INSTANCE_ID(v);
						o.worldPos = mul(unity_ObjectToWorld, v.vertex);
						UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
						TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);
						return o;
					}

					uniform sampler2D _MainTex;
					uniform half _Cutoff;

					float4 frag(v2f IN) : SV_Target
					{
						float squaredDistance = squaredHorizontalDistance(_CylinderCenter.xyz, IN.worldPos);
						if (squaredDistance > _CylinderRadius)
						{
							discard;
						}
						SHADOW_CASTER_FRAGMENT(IN)
					}
					ENDCG
				}
		}
}