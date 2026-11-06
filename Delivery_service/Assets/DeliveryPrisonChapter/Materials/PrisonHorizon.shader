Shader "DeliveryPrison/HorizonSky" {
 Properties { _Zenith("Zenith",Color)=(.23,.40,.58,1) _Horizon("Horizon",Color)=(.68,.76,.78,1) }
 SubShader { Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"} Cull Off ZWrite Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f {float4 pos:SV_POSITION; float3 dir:TEXCOORD0;};
 float4 _Zenith,_Horizon;
 v2f vert(float4 vertex:POSITION){v2f o;o.pos=UnityObjectToClipPos(vertex);o.dir=vertex.xyz;return o;}
 fixed4 frag(v2f i):SV_Target {float h=saturate(normalize(i.dir).y);return lerp(_Horizon,_Zenith,pow(h,.55));}
 ENDCG }
 } Fallback Off
}
