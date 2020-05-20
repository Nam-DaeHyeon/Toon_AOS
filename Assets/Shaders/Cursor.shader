// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/Cursor"
{
	/*
	//Texture + Alpha
	Properties{
		_Color("Main Color (A=Opacity)", Color) = (1,1,1,1)
		_MainTex("Base (A=Opacity)", 2D) = ""
	}

		Category{
			Tags {"Queue" = "Transparent" "IgnoreProjector" = "True"}
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			SubShader {Pass {
				GLSLPROGRAM
				varying mediump vec2 uv;

				#ifdef VERTEX
				uniform mediump vec4 _MainTex_ST;
				void main() {
					gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
					uv = gl_MultiTexCoord0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				}
				#endif

				#ifdef FRAGMENT
				uniform lowp sampler2D _MainTex;
				uniform lowp vec4 _Color;
				void main() {
					gl_FragColor = texture2D(_MainTex, uv) * _Color;
				}
				#endif     
				ENDGLSL
			}}

			SubShader {Pass {
				SetTexture[_MainTex] {Combine texture * constant ConstantColor[_Color]}
			}}
	}
	*/
	/*
	//투명하게 만들기
	Properties{
		_Color("Color", Color) = (1,1,1,1)
	}
		SubShader{
			Tags { "RenderType" = "Opaque" }
			LOD 200

			ZWrite On
			ColorMask 0

			Pass{}
	}
		FallBack "Diffuse"
		*/
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Color("PassColor", Color) = (1,1,1,1)
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100


		Pass
		{
			Stencil {
				Ref 0
				Comp Equal
				Pass IncrSat
				Fail IncrSat
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = fixed4(0.0, 0.0, 1.0, 1.0);
				return col;
			}
			ENDCG
		}

		Pass
		{
			Stencil {
				Ref 1
				Comp Less
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = fixed4(0.0, 1.0, 1.0, 1.0);
				//fixed4 col = (_Color.r, _Color.g, _Color.b, _Color.a);
				return col;
			}
				
			ENDCG
		}
	}
}
