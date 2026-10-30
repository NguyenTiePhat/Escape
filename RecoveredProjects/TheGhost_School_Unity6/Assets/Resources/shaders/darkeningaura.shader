Shader "SineVFX/AuraEffectsPro/DarkeningAura" {
	Properties {
		_MaskDistance ("Mask Distance", Float) = 1
		_MaskMultiply ("Mask Multiply", Range(0, 4)) = 1
		_MaskExp ("Mask Exp", Range(0.2, 10)) = 1
		[Toggle] _Noise01Enabled ("Noise 01 Enabled", Float) = 1
		_Noise01 ("Noise 01", 2D) = "white" {}
		_Noise01Tiling ("Noise 01 Tiling", Float) = 1
		_Noise01ScrollSpeed ("Noise 01 Scroll Speed", Float) = 0.25
		_NoiseDistortionPower ("Noise Distortion Power", Range(0, 10)) = 1
		[Toggle] _NoiseMaskDistortionEnabled ("Noise Mask Distortion Enabled", Float) = 1
		_NoiseMaskDistortion ("Noise Mask Distortion", 2D) = "white" {}
		_NoiseMaskDistortionTiling ("Noise Mask Distortion Tiling", Float) = 0.5
		_NoiseMaskDistortionPower ("Noise Mask Distortion Power", Range(0, 2)) = 0.5
		[HideInInspector] __dirty ("", Float) = 1
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return float4(1.0, 1.0, 1.0, 1.0); // RGBA
			}

			ENDHLSL
		}
	}
	Fallback "Diffuse"
	//CustomEditor "ASEMaterialInspector"
}