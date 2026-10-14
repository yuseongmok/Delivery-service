Shader "Mok/NavigationFlatColor" {
Properties { _BaseColor ("Map Color", Color) = (1,1,1,1) }
SubShader { Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" } Pass {
ZWrite On ZTest LEqual Cull Off
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;
CBUFFER_END
struct A{float4 positionOS:POSITION;};struct V{float4 positionCS:SV_POSITION;};
V vert(A v){V o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);return o;}
half4 frag(V i):SV_Target{return _BaseColor;}
ENDHLSL
} } }
