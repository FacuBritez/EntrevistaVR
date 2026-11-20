Shader "Custom/DesaturateStencil"
{
    // Shader de pantalla completa: convierte a blanco y negro (con tinte azulado)
    // SOLO los píxeles que NO están marcados con stencil = 1.
    // Los objetos "resaltados" escriben stencil = 1 y quedan a todo color.
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Desaturate"
            ZWrite Off
            ZTest Always
            Cull Off

            // Solo desaturar donde NO esté marcado (stencil != 1).
            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Keep
                Fail Keep
                ZFail Keep
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                float4 color = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, IN.uv);

                // Luma (blanco y negro)
                float luma = dot(color.rgb, float3(0.299, 0.587, 0.114));

                // Tinte azulado sutil: realza el canal azul.
                float3 resultado = luma * float3(0.72, 0.80, 0.98);

                return float4(resultado, color.a);
            }
            ENDHLSL
        }
    }
}
