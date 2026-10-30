Shader "Custom/TextMeshPro UI Polychrome" {
	Properties {
		[PerRendererData] _MainTex ("Font Atlas", 2D) = "white" {}
		_FaceColor ("Face Color", Vector) = (1,1,1,1)
		_SDFThreshold ("SDF Threshold", Range(0, 1)) = 0.5
		_SDFSoftness ("SDF Softness", Range(0.1, 3)) = 1
		[Toggle(_POLYCHROME_ON)] _PolychromeEnabled ("Polychrome Effect", Float) = 0
		[Toggle(_POLYCHROME_MASK_ON)] _PolychromeMaskEnabled ("Use Polychrome Mask", Float) = 0
		_PolychromeMask ("Polychrome Mask", 2D) = "white" {}
		[Toggle] _PolychromeMaskInvert ("Invert Polychrome Mask", Float) = 0
		[Toggle] _PolychromeColorOverride ("Polychrome Color Override", Float) = 1
		[Toggle] _PolychromeUsePositionShift ("Use Position Shift", Float) = 1
		_PolychromeStrength ("Polychrome Strength", Range(0, 1)) = 0.85
		_PolychromeScale ("Polychrome Scale", Range(0.1, 20)) = 4
		_PolychromeSpeed ("Polychrome Speed", Range(-5, 5)) = 0.15
		_PolychromeSaturation ("Polychrome Saturation", Range(0, 1)) = 1
		_PolychromeBrightness ("Polychrome Brightness", Range(0, 2)) = 1.1
		_PolychromeNoiseStrength ("Noise Strength", Range(0, 1)) = 0.45
		_PolychromePositionInfluence ("Position Influence", Range(0, 2)) = 0.4
		_PolychromePositionScale ("Position Scale", Range(0, 10)) = 1
		_PolychromeDirection ("Polychrome Direction", Vector) = (0.65,0.35,0,0)
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_ColorMask ("Color Mask", Float) = 15
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy);
			}

			ENDHLSL
		}
	}
}