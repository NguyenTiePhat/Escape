Shader "Hidden/TheGhost/PickupOutline"
{
    Properties { _OutlinePixels ("Width in pixels", Float) = 2.5 }
    SubShader
    {
        // Mark the visible item surface so expansion cannot paint over the model itself.
        Pass
        {
            Cull Back ZWrite Off ZTest LEqual ColorMask 0
            Stencil { Ref 64 ReadMask 64 WriteMask 64 Comp Always Pass Replace }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 vert(float4 vertex : POSITION) : SV_POSITION { return UnityObjectToClipPos(vertex); }
            fixed4 frag() : SV_Target { return 0; }
            ENDCG
        }
        Pass
        {
            Cull Front ZWrite Off ZTest LEqual
            Stencil { Ref 64 ReadMask 64 WriteMask 0 Comp NotEqual Pass Keep }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _OutlinePixels;
            struct Input { float4 vertex : POSITION; float3 normal : NORMAL; };
            float4 vert(Input v) : SV_POSITION
            {
                float4 clip = UnityObjectToClipPos(v.vertex);
                float3 normalVS = mul((float3x3)UNITY_MATRIX_V, UnityObjectToWorldNormal(v.normal));
                float2 direction = mul((float2x2)UNITY_MATRIX_P, normalVS.xy);
                direction /= max(length(direction), 0.0001);
                clip.xy += direction * (_OutlinePixels * 2.0 / _ScreenParams.xy) * clip.w;
                return clip;
            }
            fixed4 frag() : SV_Target { return fixed4(1, 1, 1, 1); }
            ENDCG
        }
    }
    Fallback Off
}
