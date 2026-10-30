Shader "Custom/SearchItemsAura" {
	Properties {
		_FinalPower ("Final Power", Range(0, 10)) = 2
		[Toggle] _MaskConstantThickness ("Mask Constant Thickness", Float) = 0
		_MaskThickness ("Mask Thickness", Float) = 1
		_MaskDistance ("Mask Distance", Float) = 1
		_MaskMultiply ("Mask Multiply", Range(0, 4)) = 1
		_MaskExp ("Mask Exp", Range(0.2, 10)) = 1
		[Toggle] _MaskTextureEnabled ("Mask Texture Enabled", Float) = 1
		_MaskTexture ("Mask Texture", 2D) = "white" {}
		_Ramp ("Ramp", 2D) = "white" {}
		_RampColorTint ("Ramp Color Tint", Vector) = (1,1,1,1)
		_RampMultiplyTiling ("Ramp Multiply Tiling", Float) = 1
		_NoiseDistortionPower ("Noise Distortion Power", Range(0, 10)) = 1
		_Noise01 ("Noise 01", 2D) = "white" {}
		_Noise01Tiling ("Noise 01 Tiling", Float) = 1
		_Noise01ScrollSpeed ("Noise 01 Scroll Speed", Float) = 0.25
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
}