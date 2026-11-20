Shader "DeliveryService/Yun/FadedBoxText"
{
    Properties { _MainTex ("Font atlas", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite Off
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct Input { float4 position : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Output { float4 position : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Output Vert(Input v)
            {
                Output o; o.position = TransformObjectToHClip(v.position.xyz); o.uv = v.uv; o.color = v.color; return o;
            }
            half4 Frag(Output i) : SV_Target
            {
                return half4(i.color.rgb, i.color.a * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a);
            }
            ENDHLSL
        }
    }
}
