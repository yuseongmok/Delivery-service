Shader "Mok/GarageDepthText" {
Properties { _MainTex ("Font Atlas", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,1) }
SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" } Pass {
Blend SrcAlpha OneMinusSrcAlpha
ZWrite Off
ZTest LEqual
Cull Off
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
CBUFFER_START(UnityPerMaterial)
float4 _Color;
CBUFFER_END
struct Attributes {float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR;};
struct Varyings {float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR;};
Varyings vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;o.color=v.color*_Color;return o;}
half4 frag(Varyings i):SV_Target {half4 c=i.color;c.a*=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;return c;}
ENDHLSL
} } }
