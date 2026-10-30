Shader "Recovery/OpaqueTwoSided" {
 Properties { _MainTex("Albedo",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _BumpMap("Normal",2D)="bump"{} _BumpScale("Normal strength",Float)=1 }
 SubShader { Tags {"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 sampler2D _MainTex; sampler2D _BumpMap; fixed4 _Color; float _BumpScale;
 struct Input {float2 uv_MainTex;float2 uv_BumpMap;};
 void surf(Input i,inout SurfaceOutputStandard o){o.Albedo=tex2D(_MainTex,i.uv_MainTex).rgb*_Color.rgb;o.Normal=UnpackScaleNormal(tex2D(_BumpMap,i.uv_BumpMap),_BumpScale);o.Smoothness=.1;o.Alpha=1;}
 ENDCG
 } Fallback "Diffuse"
}
