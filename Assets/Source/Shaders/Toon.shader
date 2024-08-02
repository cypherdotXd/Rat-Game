
Shader "Unlit/Toon"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalRenderPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 lightAmount : TEXCOORD3;

            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.positionWS = TransformObjectToWorld(v.vertex.xyz);
                o.vertex = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normal.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                Light light = GetMainLight();
                VertexNormalInputs positions = GetVertexNormalInputs(v.vertex);
                o.lightAmount = LightingLambert(light.color, light.direction, positions.normalWS.xyz);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                
                // sample the texture
                i.normalWS = normalize(i.normalWS);
                
                half4 col = tex2D(_MainTex, i.uv);
                
                //InputData lightingInput = (InputData)0;
                //SurfaceData surfaceInput = (SurfaceData)0;
                //UniversalFragmentBlingPhong(, col)
                float3 lightDir = float3(0.5,1,0.5);
                float light = dot(i.normalWS, lightDir);

                //light = step(0.5, light);
                int steps = 8;
                i.lightAmount = 1 - floor(i.lightAmount * steps) / steps;
                i.lightAmount = max(i.lightAmount, 0.4);

                return col * i.lightAmount.x;
                //return col;
            }
            ENDHLSL
        }
    }
}
