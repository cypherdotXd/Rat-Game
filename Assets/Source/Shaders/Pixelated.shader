Shader "Custom/PixelArtEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PixelSize ("Pixel Size", Float) = 1
        _ColorLevels ("Color Levels", Float) = 4
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _PixelSize;
            float _ColorLevels;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // Pixelate
                float2 pixelatedUV = round(IN.uv * _ScreenParams.xy / _PixelSize) * _PixelSize / _ScreenParams.xy;
                
                // Sample the texture
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pixelatedUV);
                
                // Quantize colors
                col = floor(col * _ColorLevels) / (_ColorLevels - 1);
                
                return col;
            }
            ENDHLSL
        }
    }
}