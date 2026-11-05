Shader "DeliveryPrison/WorldLabel" {
 Properties { _MainTex ("Font",2D)="white"{} _Color("Color",Color)=(1,1,1,1) }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"} Pass { Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float4 _Color;
 struct A {float4 pos:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 V vert(A i){V o;o.pos=TransformObjectToHClip(i.pos.xyz);o.uv=i.uv;o.color=i.color;return o;} half4 frag(V i):SV_Target {return half4(i.color.rgb*_Color.rgb,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a*i.color.a*_Color.a);}
 ENDHLSL
 } }
}
