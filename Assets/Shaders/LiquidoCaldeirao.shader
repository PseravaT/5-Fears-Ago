Shader "Custom/LiquidoCaldeirao"
{
    Properties
    {
        [Header(Cores)]
        _CorEscura ("Cor Escura", Color) = (0.30, 0.02, 0.04, 1)
        _CorClara ("Cor Clara", Color) = (0.85, 0.12, 0.15, 1)
        _Brilho ("Brilho (multiplicador)", Range(0, 3)) = 1

        [Header(Ruido da Superficie)]
        _EscalaRuido ("Escala do Ruido (manchas por unidade)", Float) = 3
        _VelocidadeA ("Velocidade Ruido A (X, Z por segundo)", Vector) = (0.05, 0.03, 0, 0)
        _VelocidadeB ("Velocidade Ruido B (X, Z por segundo)", Vector) = (-0.04, 0.02, 0, 0)
        _Contraste ("Contraste", Range(0.5, 4)) = 1.6
        _Passos ("Passos do Posterize", Range(2, 12)) = 5

        [Header(Ondulacao dos Vertices)]
        _AmplitudeOnda ("Amplitude (unidades)", Range(0, 0.05)) = 0.015
        _FrequenciaOnda ("Frequencia (ondas por unidade)", Float) = 6
        _VelocidadeOnda ("Velocidade (ciclos por segundo x 2pi)", Float) = 1.5
        _ReflexoOnda ("Brilho nas Cristas", Range(0, 1)) = 0.25

        [Header(Borda e Espuma)]
        _RaioDisco ("Raio do Disco (espaco do objeto)", Float) = 1
        _CorEspuma ("Cor da Espuma", Color) = (1, 0.55, 0.55, 1)
        _LarguraEspuma ("Largura da Espuma", Range(0.01, 0.5)) = 0.12
        _ForcaEspuma ("Forca da Espuma", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CorEscura;
                half4 _CorClara;
                half _Brilho;

                float _EscalaRuido;
                float4 _VelocidadeA;
                float4 _VelocidadeB;
                float _Contraste;
                float _Passos;

                float _AmplitudeOnda;
                float _FrequenciaOnda;
                float _VelocidadeOnda;
                float _ReflexoOnda;

                float _RaioDisco;
                half4 _CorEspuma;
                float _LarguraEspuma;
                float _ForcaEspuma;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float raioNorm : TEXCOORD1;
                float onda : TEXCOORD2;
            };

            // Hash e value noise 2D (sem textura)
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 pos = input.positionOS.xyz;

                // Distancia ao centro (0 no meio, 1 na borda do disco)
                float raio = length(pos.xz) / max(_RaioDisco, 0.0001);

                // Duas ondas cruzadas, _Time.y esta em segundos
                float t = _Time.y * _VelocidadeOnda;
                float onda = sin(t + pos.x * _FrequenciaOnda)
                           + sin(t * 0.73 + pos.z * _FrequenciaOnda * 0.83 + 1.7);
                onda *= 0.5;

                // Amplitude cai perto da borda para o liquido nao atravessar a parede do caldeirao
                float mascaraBorda = 1.0 - smoothstep(0.75, 1.0, raio);
                pos.y += onda * _AmplitudeOnda * mascaraBorda;

                output.positionWS = TransformObjectToWorld(pos);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.raioNorm = raio;
                output.onda = onda;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Ruido por posicao no mundo: sem listras radiais do UV em leque do disco
                float2 p = input.positionWS.xz * _EscalaRuido;

                float n1 = ValueNoise(p + _Time.y * _VelocidadeA.xy);
                float n2 = ValueNoise(p * 1.7 + _Time.y * _VelocidadeB.xy + 13.7);
                float n = (n1 + n2) * 0.5;

                // Realce de contraste em torno do meio e brilho nas cristas das ondas
                n = saturate((n - 0.5) * _Contraste + 0.5);
                n = saturate(n + input.onda * _ReflexoOnda);

                // Posterize (visual PS1)
                float passos = max(_Passos, 2.0);
                n = floor(n * passos) / (passos - 1.0);
                n = saturate(n);

                half3 cor = lerp(_CorEscura.rgb, _CorClara.rgb, n);

                // Espuma na borda
                float espuma = smoothstep(1.0 - _LarguraEspuma, 1.0, input.raioNorm);
                cor = lerp(cor, _CorEspuma.rgb, espuma * _ForcaEspuma);

                return half4(cor * _Brilho, 1.0);
            }
            ENDHLSL
        }
    }
}
