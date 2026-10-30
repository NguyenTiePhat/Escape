Shader "Custom/OutlineEnemy" {
	Properties {
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMask ("ZTestMask", Float) = 0
		_OutlineColor ("Outline Color", Vector) = (1,1,1,1)
		_OutlineWidth ("Outline Width", Range(0, 10)) = 2
		_NoiseStrength ("Noise Strength", Range(0, 1)) = 0.2
		_NoiseTex ("Noise Texture", 2D) = "white" {}
		_NoiseTexScale ("Noise Texture Scale", Float) = 1
		_NoiseTexSpeed ("Noise Scroll Speed", Vector) = (0.1,0.1,0,0)
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