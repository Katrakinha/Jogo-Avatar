Shader "Custom/LegoMecabricks"
{
    Properties
    {
        [MainColor] _BaseColor("Cor do Plastico (Fundo)", Color) = (1, 0.8, 0.6, 1)
        [MainTexture] _BaseMap("Estampa PNG (Adesivo)", 2D) = "black" {}
        _Smoothness("Brilho do Plastico", Range(0, 1)) = 0.7
        [IntRange] _UVChannel("Canal UV (Teste de 0 a 3)", Range(0, 3)) = 0
        [Toggle] _FlipY("Inverter Vertical (Y)", Float) = 0
        [Toggle] _FlipX("Inverter Horizontal (X)", Float) = 0
        [Toggle] _ClipUV("Cortar Bordas (Modo Clip)", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float2 uv2        : TEXCOORD2;
                float2 uv3        : TEXCOORD3;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                half _Smoothness;
                float _UVChannel;
                float _FlipY;
                float _FlipX;
                float _ClipUV;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);

                // Seleciona entre os 4 canais de UV possíveis do Mecabricks
                float2 chosenUV = IN.uv0;
                if (_UVChannel > 0.5 && _UVChannel < 1.5) chosenUV = IN.uv1;
                else if (_UVChannel >= 1.5 && _UVChannel < 2.5) chosenUV = IN.uv2;
                else if (_UVChannel >= 2.5) chosenUV = IN.uv3;

                if (_FlipX > 0.5) chosenUV.x = 1.0 - chosenUV.x;
                if (_FlipY > 0.5) chosenUV.y = 1.0 - chosenUV.y;

                OUT.uv = TRANSFORM_TEX(chosenUV, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 adesivo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                half dentroDoQuadrado = 1.0;
                if (_ClipUV > 0.5)
                {
                    dentroDoQuadrado = step(0.002, IN.uv.x) * step(IN.uv.x, 0.998) * 
                                       step(0.002, IN.uv.y) * step(IN.uv.y, 0.998);
                }

                half mascara = smoothstep(0.05, 0.95, adesivo.a) * dentroDoQuadrado;
                half3 corAlvo = lerp(_BaseColor.rgb, adesivo.rgb, mascara);

                Light mainLight = GetMainLight();
                half3 normalWS = normalize(IN.normalWS);
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 ambient = SampleSH(normalWS);

                half3 halfDir = normalize(mainLight.direction + viewDirWS);
                half spec = pow(saturate(dot(normalWS, halfDir)), exp2(_Smoothness * 8.0 + 1.0)) * _Smoothness * NdotL;

                half3 corFinal = corAlvo * (ambient + mainLight.color * NdotL) + (mainLight.color * spec * 0.35);
                return half4(corFinal, 1.0);
            }
            ENDHLSL
        }
    }
}