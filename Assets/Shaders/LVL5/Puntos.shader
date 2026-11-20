Shader "Custom/Puntos"
{
    // "Dithered Transparency": transparencia por tramado (Bayer 4x4). El objeto se
    // ve semitransparente usando un patrón de puntos en vez de alpha blending.
    // El alpha de _BaseColor controla cuántos píxeles sobreviven (menor alpha =
    // más transparente). Pensado para la Pistola cuando no se puede usar.
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            // Matriz de Bayer 4x4 (tramado ordenado).
            static const float bayer4[16] =
            {
                0.0/16.0, 8.0/16.0, 2.0/16.0, 10.0/16.0,
                12.0/16.0, 4.0/16.0, 14.0/16.0, 6.0/16.0,
                3.0/16.0, 11.0/16.0, 1.0/16.0, 9.0/16.0,
                15.0/16.0, 7.0/16.0, 13.0/16.0, 5.0/16.0
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                int x = (int)IN.positionHCS.x & 3;
                int y = (int)IN.positionHCS.y & 3;
                float threshold = bayer4[y * 4 + x];

                half4 col = _BaseColor;
                // Descartar píxeles según el tramado (dithered transparency).
                clip(col.a - threshold);
                return col;
            }
            ENDHLSL
        }
    }
    Fallback "Sprites/Default"
}
