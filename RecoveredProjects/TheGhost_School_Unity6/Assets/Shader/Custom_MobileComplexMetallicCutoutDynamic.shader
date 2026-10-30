Shader "Custom/MobileComplexMetallicCutoutDynamic" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_NoiseTex ("Noise Texture", 2D) = "white" {}
		_MinCutoff ("Minimum Cutoff", Range(0, 1)) = 0.5
		_MaxCutoff ("Maximum Cutoff", Range(0, 1)) = 0.5
		_CutoffTest ("Alpha cutoff test", Range(0, 1)) = 0.5
		_Smoothness ("Smoothness", Range(0, 1)) = 0.5
		_Metallic ("Metallic", Range(0, 1)) = 0
		_MetallicMap ("Metallic Map", 2D) = "black" {}
		_NormalMap ("Normal Map", 2D) = "bump" {}
		_Cube ("Cubemap", Cube) = "" {}
		_ReflectionTint ("Cubemap Tint", Vector) = (1,1,1,1)
		_Offset ("Offset", Float) = 0
		[Toggle] _UseMetallicMap ("Use Metallic Map", Float) = 0
		[Toggle] _UseReflection ("Use Reflection", Float) = 0
		[Toggle] _CutoutTest ("Cutoff Test", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy) * _Color;
			}

			ENDHLSL
		}
	}
}