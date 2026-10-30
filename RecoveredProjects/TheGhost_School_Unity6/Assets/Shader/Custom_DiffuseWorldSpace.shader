Shader "Custom/DiffuseWorldSpace" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Color ("Tint", Vector) = (1,1,1,1)
		[HDR] _EmissionColor ("Color", Vector) = (0,0,0,1)
		_BlinkingSpeed ("Blinking Speed", Float) = 1
		[Toggle] _Blinking ("Blinking", Float) = 0
		_WorldTiling ("World Tiling", Float) = 1
		_WorldOffset ("World Offset (XYZ)", Vector) = (0,0,0,0)
		_TriplanarSharpness ("Triplanar Sharpness", Range(1, 16)) = 4
		_Offset ("Offset", Float) = 0
		[Enum(Back,0,Front,1,Off,2)] _Cull ("Cull Mode", Float) = 2
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
	Fallback "Diffuse"
}