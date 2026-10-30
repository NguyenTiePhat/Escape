Shader "Custom/UnlitTransparentCutout" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Tint ("Tint", Vector) = (1,1,1,1)
		[Toggle(USE_PARTICLE_COLOR_ONLY)] _UseParticleColorOnly ("Use Particle Color Only", Float) = 0
		[Toggle(SMOOTH_ALPHA)] _SmoothAlpha ("Smooth Alpha", Float) = 0
		[KeywordEnum(Alpha, Red)] _AlphaMode ("Texture Alpha Mode", Float) = 0
		_ClipAmount ("Clip Amount", Float) = 0.1
		_Transparency ("Transparency", Float) = 1
		_ZOffset ("ZOffset", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _SourceBlend ("Source Blend", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DestBlend ("Dest Blend", Float) = 10
		[Toggle(USE_FOG)] _UseFog ("Use Fog", Float) = 1
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