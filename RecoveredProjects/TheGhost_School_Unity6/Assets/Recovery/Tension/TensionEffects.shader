Shader "Hidden/School/TensionEffects" {
 Properties { _MainTex("Source",2D)="white"{} }
 SubShader { Cull Off ZWrite Off ZTest Always
 Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 float _Tension, _NoiseTime;
 fixed4 frag(v2f_img i):SV_Target {
   float2 delta=i.uv-.5;
   float2 shift=delta*dot(delta,delta)*.006*_Tension;
   fixed4 c=tex2D(_MainTex,i.uv);
   c.r=tex2D(_MainTex,saturate(i.uv+shift)).r;
   c.b=tex2D(_MainTex,saturate(i.uv-shift)).b;
   float grey=dot(c.rgb,float3(.2126,.7152,.0722));
   c.rgb=lerp(c.rgb,grey.xxx,_Tension*.22);
   c.rgb*=1-smoothstep(.12,.48,dot(delta,delta))*_Tension*.3;
   float grain=frac(sin(dot(i.uv*_ScreenParams.xy,float2(12.9898,78.233))+floor(_NoiseTime*24))*43758.5453)-.5;
   c.rgb+=grain*.018*_Tension;
   return c;
 }
 ENDCG }
 } Fallback Off
}
