Shader "Custom/CRTFilterURP"
{
    Properties
    {
        [Header(Tela)]
        _Curvature ("Curvatura", Range(0, 0.5)) = 0.12
        _Vignette ("Vinheta", Range(0, 1)) = 0.35

        [Header(Scanlines e mascara)]
        _ScanlineCount ("Qtd de scanlines", Float) = 240
        _ScanlineIntensity ("Intensidade scanlines", Range(0, 1)) = 0.25
        _MaskIntensity ("Mascara RGB (fosforo)", Range(0, 1)) = 0.12

        [Header(Cor)]
        _RGBShift ("Aberracao cromatica", Range(0, 0.01)) = 0.0015
        _Desaturation ("Dessaturacao (opaco)", Range(0, 1)) = 0.3
        _Contrast ("Contraste", Range(0.5, 1.5)) = 0.85
        _BlackLift ("Preto levantado (opaco)", Range(0, 0.3)) = 0.08
        _Tint ("Cor da tela", Color) = (0.92, 1.0, 0.95, 1)
        _HighlightStart ("Protecao de luzes (inicio)", Range(0, 1)) = 0.25
        _HighlightEnd ("Protecao de luzes (fim)", Range(0, 1)) = 0.7

        [Header(Dano)]
        _DamageTint ("Forca do tom vermelho", Range(0, 1)) = 0.75
        _PulseAmount ("Forca da pulsacao", Range(0, 0.6)) = 0.25
        _EdgeSize ("Tamanho da vinheta de dano", Range(0.15, 1.5)) = 0.5
        _EdgeRough ("Giz da vinheta de dano", Range(0, 1)) = 0.6
        [HideInInspector] _DamageLevel ("Nivel de dano (script)", Range(0, 1)) = 0
        [HideInInspector] _PulsePhase ("Fase da pulsacao (script)", Float) = 0
        [HideInInspector] _HitFlash ("Flash de impacto (script)", Range(0, 1)) = 0
        [HideInInspector] _DamageColor ("Cor do estagio (script)", Color) = (0, 0, 0, 0)
        [HideInInspector] _DamageDarkness ("Escurecimento final (script)", Range(0, 1)) = 0

        // Veias: valores enviados pelo DamageSystem (Vein Stages, um conjunto por dano)
        [HideInInspector] _VeinColor ("Cor das veias (script)", Color) = (0.6, 0.03, 0.05, 1)
        [HideInInspector] _VeinDensity ("Densidade (script)", Float) = 12
        [HideInInspector] _VeinWidth ("Espessura (script)", Float) = 0.06
        [HideInInspector] _VeinReach ("Alcance (script)", Float) = 0
        [HideInInspector] _VeinFps ("Quadros por segundo (script)", Float) = 10
        [HideInInspector] _VeinFrames ("Quadros no ciclo (script)", Float) = 4
        [HideInInspector] _VeinBoil ("Tremida (script)", Float) = 0.8
        [HideInInspector] _VeinOutlineColor ("Cor do contorno (script)", Color) = (0.03, 0.0, 0.01, 1)
        [HideInInspector] _VeinOutline ("Espessura do contorno (script)", Float) = 0
        [HideInInspector] _VeinOutlineJitter ("Rabisco do contorno (script)", Float) = 0.7
        [HideInInspector] _BrushColor ("Cor dos rabiscos (script)", Color) = (0.02, 0.02, 0.02, 1)
        [HideInInspector] _BrushReach ("Alcance dos rabiscos (script)", Float) = 0
        [HideInInspector] _BrushThickness ("Espessura dos rabiscos (script)", Float) = 0.35
        [HideInInspector] _BrushDensity ("Densidade dos rabiscos (script)", Float) = 7
        [HideInInspector] _BrushRough ("Aspereza dos rabiscos (script)", Float) = 0.6
        [HideInInspector] _VeinRough ("Aspereza das veias (script)", Float) = 0.5
        [HideInInspector] _VeinOutlineRough ("Aspereza do contorno (script)", Float) = 0.5

        [Header(Animacao)]
        _NoiseAmount ("Ruido", Range(0, 0.3)) = 0.06
        _FlickerAmount ("Cintilacao", Range(0, 0.1)) = 0.02
        _FlickerSpeed ("Velocidade cintilacao (Hz)", Float) = 8
        _RollAmount ("Faixa rolante", Range(0, 0.3)) = 0.06
        _RollSpeed ("Faixa rolante (telas/seg)", Float) = 0.15
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "CRTFilterURP"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Curvature, _Vignette;
                float _ScanlineCount, _ScanlineIntensity, _MaskIntensity;
                float _RGBShift, _Desaturation, _Contrast, _BlackLift;
                float4 _Tint;
                float _HighlightStart, _HighlightEnd;
                float _DamageTint, _PulseAmount, _DamageLevel, _PulsePhase, _HitFlash;
                float _EdgeSize;
                float _EdgeRough;
                float4 _DamageColor;
                float _DamageDarkness;
                float4 _VeinColor;
                float _VeinDensity, _VeinWidth, _VeinReach;
                float _VeinFps, _VeinFrames, _VeinBoil;
                float4 _VeinOutlineColor;
                float _VeinOutline, _VeinOutlineJitter;
                float4 _BrushColor;
                float _BrushReach, _BrushThickness, _BrushDensity, _BrushRough;
                float _VeinRough, _VeinOutlineRough;
                float _NoiseAmount, _FlickerAmount, _FlickerSpeed;
                float _RollAmount, _RollSpeed;
            CBUFFER_END

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Batimento duplo (lub-dub). phase = ciclos acumulados pelo script
            float heartbeat(float phase)
            {
                float p = frac(phase);
                float x1 = (p - 0.08) / 0.05;
                float x2 = (p - 0.26) / 0.06;
                return saturate(exp(-x1 * x1) + 0.6 * exp(-x2 * x2));
            }

            // Textura de giz/carvao: erode as bordas com ruido por pixel (0 = traco liso)
            float chalk(float m, float g, float rough)
            {
                return smoothstep(0.2, 0.55, saturate(m * 1.25 - g * rough * 0.8));
            }

            // Uma camada de veias saindo de UMA borda.
            // d = distancia da borda, s = posicao ao longo dela (ambos em alturas de tela)
            float2 veinLayer(float d, float s, float density, float reach, float level, float seed, float f)
            {
                float cells = s * density;
                float id = floor(cells);
                float r  = hash(float2(id, seed));
                float r2 = hash(float2(id + 31.7, seed));

                // Sorteios que mudam a cada quadro do ciclo (f = indice do quadro)
                float fv  = hash(float2(id * 1.7 + f * 13.1, seed + 5.0));
                float fv2 = hash(float2(id + f * 7.7, seed + 9.0));

                // cada veia tem seu proprio tamanho; a ponta treme entre quadros
                float len = reach * level * (0.3 + 0.7 * r) * (1.0 + (fv - 0.5) * 0.35 * _VeinBoil);
                if (d >= len + _VeinOutline) return float2(0.0, 0.0);

                // Ondulacao + quebras em zigue-zague, ancorada na borda (ramp)
                float ramp = saturate(d * 8.0);
                float wob = (sin(d * 20.0 + (r + fv * _VeinBoil) * 6.28318) * 0.15
                           + (abs(frac(d * 9.0 + r2 + fv2 * _VeinBoil * 0.5) - 0.5) - 0.25) * 0.3) * ramp;
                float x = frac(cells) - 0.5 - wob;

                // Afina ate a ponta
                float t = saturate(d / max(len, 0.0001));
                float width = _VeinWidth * (1.0 - t) * (1.0 + (fv2 - 0.5) * 0.4 * _VeinBoil);

                // Nucleo da veia
                float core = (d < len) ? 1.0 - smoothstep(width * 0.5, width + 0.01, abs(x)) : 0.0;

                // Contorno: traco proprio, mais largo e desalinhado do nucleo (efeito de rabisco).
                // O desvio muda a cada quadro (f), entao o traco "treme" em volta da veia.
                float scribble = sin(d * 38.0 + r * 20.0 + f * 4.1) * 0.10 + (fv - 0.5) * 0.06;
                float xo = x - scribble * _VeinOutlineJitter * ramp;
                float wo = width + _VeinOutline;
                float outer = 1.0 - smoothstep(wo * 0.5, wo + 0.01, abs(xo));

                // giz/carvao: o pincel "falha" em pontos ao longo do traco (diferente a cada quadro)
                float gap  = step(0.75, hash(float2(floor(d * 45.0 + r  * 30.0), seed + f * 3.0)));
                float gap2 = step(0.75, hash(float2(floor(d * 45.0 + r2 * 30.0), seed + f * 3.0 + 1.0)));
                core  *= 1.0 - _VeinRough * 0.6 * gap;
                outer *= 1.0 - _VeinOutlineRough * 0.6 * gap2;

                return float2(core, outer);   // x = nucleo, y = contorno
            }

            float2 veins(float2 uv, float level)
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float dL = uv.x * aspect;
                float dR = (1.0 - uv.x) * aspect;
                float dB = uv.y;
                float dT = 1.0 - uv.y;
                float sV = uv.y;            // ao longo das bordas esquerda/direita
                float sH = uv.x * aspect;   // ao longo das bordas de cima/baixo

                // Quadro atual do ciclo: troca _VeinFps vezes por segundo
                float f = fmod(floor(_Time.y * _VeinFps), max(1.0, floor(_VeinFrames)));

                float2 m = float2(0.0, 0.0);
                // camada principal
                m = max(m, veinLayer(dL, sV, _VeinDensity, _VeinReach, level, 1.0, f));
                m = max(m, veinLayer(dR, sV, _VeinDensity, _VeinReach, level, 2.0, f));
                m = max(m, veinLayer(dB, sH, _VeinDensity, _VeinReach, level, 3.0, f));
                m = max(m, veinLayer(dT, sH, _VeinDensity, _VeinReach, level, 4.0, f));
                // camada secundaria: mais fina e mais curta
                m = max(m, veinLayer(dL, sV, _VeinDensity * 1.9, _VeinReach * 0.55, level, 11.0, f));
                m = max(m, veinLayer(dR, sV, _VeinDensity * 1.9, _VeinReach * 0.55, level, 12.0, f));
                m = max(m, veinLayer(dB, sH, _VeinDensity * 1.9, _VeinReach * 0.55, level, 13.0, f));
                m = max(m, veinLayer(dT, sH, _VeinDensity * 1.9, _VeinReach * 0.55, level, 14.0, f));

                // giz/carvao: bordas esfareladas (ruido por pixel, muda a cada quadro)
                float2 px = floor(uv * _ScreenParams.xy / 1.5);
                float g1 = hash(px + f * 7.3 + 11.0);
                float g2 = hash(px.yx + f * 5.1 + 29.0);
                m.x = chalk(m.x, g1, _VeinRough);
                m.y = chalk(m.y, g2, _VeinOutlineRough);
                return m;
            }

            // Rabiscos grossos de giz/carvao saindo das bordas (massa de tracos, estilo desenho a mao).
            // Cada celula testa tambem as vizinhas, porque os tracos sao largos e inclinados rumo ao centro.
            // center/halfSpan: centro da tela ao longo desta borda e distancia ate ele (abre os tracos em leque)
            float brushLayer(float d, float s, float density, float reach, float thick, float seed, float f,
                             float center, float halfSpan)
            {
                float cells = s * density;
                float id = floor(cells);
                float m = 0.0;

                [unroll]
                for (int i = -1; i <= 1; i++)
                {
                    float cid = id + i;
                    float r  = hash(float2(cid, seed));
                    float r2 = hash(float2(cid + 17.3, seed));
                    float fv = hash(float2(cid * 1.7 + f * 13.1, seed + 5.0));   // muda a cada quadro

                    float len = reach * (0.35 + 0.65 * r) * (1.0 + (fv - 0.5) * 0.35 * _VeinBoil);
                    if (d < len)
                    {
                        float t = d / max(len, 0.0001);

                        // leque: os tracos convergem pro centro da tela, com desvio proprio
                        float sCell = (cid + 0.5) / density;
                        float fan = (center - sCell) / halfSpan * 0.5;
                        float slope = (r2 - 0.5) * 0.5 + fan;
                        float wob = sin(d * 14.0 + (r + fv * _VeinBoil) * 6.28318) * 0.12;
                        float shift = slope * d * density + wob;
                        float dist = abs(cells - (cid + 0.5 + shift));

                        float w = thick * (1.0 - 0.3 * t) * (0.6 + 0.8 * r);      // largura quase constante
                        float stroke = 1.0 - smoothstep(w * 0.6, w, dist);

                        // listras de pincel seco ao longo do traco
                        float u = cells - shift;
                        float streak = hash(float2(floor(u * 12.0 + r * 50.0), seed + 3.0 + f));
                        stroke *= lerp(1.0, 0.55 + 0.45 * streak, _BrushRough);

                        // ponta esfiapada: cada fiapo termina num ponto diferente
                        float tipEnd = 0.7 + 0.3 * streak;
                        stroke *= 1.0 - smoothstep(tipEnd - 0.08, tipEnd, t);

                        m = max(m, stroke);
                    }
                }
                return m;
            }

            float brushes(float2 uv, float reach)
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float dL = uv.x * aspect;
                float dR = (1.0 - uv.x) * aspect;
                float dB = uv.y;
                float dT = 1.0 - uv.y;
                float sV = uv.y;
                float sH = uv.x * aspect;

                // mesmo quadro das veias: rabiscos e veias tremem juntos
                float f = fmod(floor(_Time.y * _VeinFps), max(1.0, floor(_VeinFrames)));

                float m = 0.0;
                float halfV = aspect * 0.5;   // da borda esquerda/direita ate o centro (em alturas de tela)

                // tracos grossos
                m = max(m, brushLayer(dL, sV, _BrushDensity, reach, _BrushThickness, 21.0, f, 0.5, halfV));
                m = max(m, brushLayer(dR, sV, _BrushDensity, reach, _BrushThickness, 22.0, f, 0.5, halfV));
                m = max(m, brushLayer(dB, sH, _BrushDensity, reach, _BrushThickness, 23.0, f, halfV, 0.5));
                m = max(m, brushLayer(dT, sH, _BrushDensity, reach, _BrushThickness, 24.0, f, halfV, 0.5));
                // fios mais finos e curtos entre os tracos grossos
                m = max(m, brushLayer(dL, sV, _BrushDensity * 2.3, reach * 0.65, _BrushThickness * 0.55, 31.0, f, 0.5, halfV));
                m = max(m, brushLayer(dR, sV, _BrushDensity * 2.3, reach * 0.65, _BrushThickness * 0.55, 32.0, f, 0.5, halfV));
                m = max(m, brushLayer(dB, sH, _BrushDensity * 2.3, reach * 0.65, _BrushThickness * 0.55, 33.0, f, halfV, 0.5));
                m = max(m, brushLayer(dT, sH, _BrushDensity * 2.3, reach * 0.65, _BrushThickness * 0.55, 34.0, f, halfV, 0.5));

                // borda esfarelada de giz/carvao: ruido por pixel que muda a cada quadro
                float2 px = floor(uv * _ScreenParams.xy / 1.5);
                float grain = hash(px + f * 7.3);
                m = saturate(m * 1.25 - grain * _BrushRough * 0.8);
                return smoothstep(0.2, 0.55, m);
            }

            float2 curve(float2 uv)
            {
                uv = uv * 2.0 - 1.0;
                float2 offset = abs(uv.yx) * _Curvature;
                uv += uv * offset * offset * 4.0;
                return uv * 0.5 + 0.5;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // Tremor da imagem no impacto
                float2 jit = float2(hash(float2(_Time.y, 1.0)), hash(float2(_Time.y, 2.0))) - 0.5;
                float2 uv = curve(input.texcoord + jit * 0.012 * _HitFlash);

                // Fora da tela curva = borda preta
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    return float4(0, 0, 0, 1);

                // Aberracao cromatica
                float shift = _RGBShift + _HitFlash * 0.008;
                float3 col;
                col.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(shift, 0)).r;
                col.g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).g;
                col.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(shift, 0)).b;

                // Scanlines
                float scan = sin(uv.y * _ScanlineCount * 6.28318) * 0.5 + 0.5;
                col *= 1.0 - _ScanlineIntensity * scan;

                // Mascara de fosforo (listras RGB por pixel de tela)
                float px = fmod(floor(uv.x * _ScreenParams.x), 3.0);
                float3 mask = float3(px == 0.0, px == 1.0, px == 2.0);
                col *= lerp(1.0, 0.6 + mask * 0.8, _MaskIntensity);

                // Faixa rolante
                float roll = frac(uv.y - _Time.y * _RollSpeed);
                col *= 1.0 + _RollAmount * smoothstep(0.9, 1.0, roll);

                // Visual opaco: dessatura, reduz contraste, levanta pretos
                // Luzes (pixels claros) ficam protegidas do efeito opaco
                float luma = dot(col, float3(0.299, 0.587, 0.114));
                float maxc = max(col.r, max(col.g, col.b));
                float bright = max(luma, maxc * 0.8);
                float lightMask = smoothstep(_HighlightStart, _HighlightEnd, bright);
                float shadowMask = 1.0 - lightMask;

                col = lerp(col, luma.xxx, _Desaturation * shadowMask);
                float3 contrasted = (col - 0.5) * _Contrast + 0.5;
                col = lerp(contrasted, col, lightMask);
                col += _BlackLift * (1.0 - smoothstep(0.0, 0.5, luma));
                col *= lerp(_Tint.rgb, 1.0.xxx, lightMask);

                // Ruido e cintilacao
                float n = hash(uv * _ScreenParams.xy + frac(_Time.y) * 100.0);
                col += (n - 0.5) * _NoiseAmount * shadowMask;
                col *= 1.0 + sin(_Time.y * _FlickerSpeed * 6.28318) * _FlickerAmount;

                // Dano: tom vermelho persistente + pulsacao no ritmo do batimento
                float beat = heartbeat(_PulsePhase);
                float lumaD = dot(col, float3(0.299, 0.587, 0.114));
                float3 stageGrade = lumaD.xxx * _DamageColor.rgb * 1.5;
                col = lerp(col, stageGrade, _DamageColor.a * _DamageTint);
                col *= 1.0 + beat * _PulseAmount * _DamageLevel;

                // Vinheta
                float2 v = uv * (1.0 - uv.yx);
                float vig = saturate(v.x * v.y * 16.0);
                col *= pow(vig, _Vignette);

                // Borda vermelha pulsante
                float edge = pow(1.0 - vig, 1.0 / max(_EdgeSize, 0.01));
                float edgeAmount = saturate(_DamageLevel * (0.5 + 0.5 * beat) + _HitFlash);
                float3 stageEdge = lerp(float3(0.75, 0.02, 0.02), saturate(_DamageColor.rgb * 1.3), _DamageColor.a);
                float3 edgeColor = lerp(stageEdge, float3(0.75, 0.02, 0.02), _HitFlash);

                // Textura de giz/carvao na vinheta: raios irregulares saindo do centro + grao (muda a cada quadro)
                float edgeA;
                if (_EdgeRough > 0.001)
                {
                    float fe = fmod(floor(_Time.y * _VeinFps), max(1.0, floor(_VeinFrames)));
                    float ang = atan2(uv.y - 0.5, (uv.x - 0.5) * (_ScreenParams.x / _ScreenParams.y));
                    float sk = hash(float2(floor(ang * 14.0), fe * 3.0 + 5.0));
                    float edgeMask = edge * lerp(1.0, 0.6 + 0.8 * sk, _EdgeRough);   // alcance irregular (media 1)
                    float soft = saturate(edgeMask * edgeAmount * 1.5);
                    float ge = hash(floor(uv * _ScreenParams.xy / 1.5) + fe * 7.3 + 41.0);
                    // granulado: densidade = intensidade. O fade em soft ~ 0 impede grao onde nao ha vinheta
                    float grain = smoothstep(ge - 0.2, ge + 0.2, soft) * smoothstep(0.0, 0.08, soft);
                    grain = max(grain, smoothstep(0.9, 1.0, soft));
                    edgeA = lerp(soft, grain, _EdgeRough);
                }
                else
                {
                    edgeA = saturate(edge * edgeAmount * 1.5);
                }
                col = lerp(col, edgeColor, edgeA);

                // Veias que crescem das bordas conforme o dano (pulsam com o batimento)
                float veinLevel = saturate(_DamageLevel * 20.0) * (1.0 + _HitFlash * 0.12);
                // Rabiscos grossos (giz/carvao) por baixo das veias
                float brushReach = _BrushReach * veinLevel;
                if (brushReach > 0.001)
                {
                    float bm = brushes(uv, brushReach);
                    col = lerp(col, _BrushColor.rgb, bm * _BrushColor.a);
                }

                float2 vein = veins(uv, veinLevel);   // x = nucleo, y = contorno
                float outlineOn = step(0.001, _VeinOutline);
                col = lerp(col, _VeinOutlineColor.rgb, vein.y * outlineOn);              // contorno por baixo
                col = lerp(col, _VeinColor.rgb * (0.85 + 0.5 * beat), vein.x);           // veia por cima

                // Flash de impacto
                col += float3(0.5, 0.0, 0.0) * _HitFlash;
                col.gb *= 1.0 - 0.6 * _HitFlash;

                col *= 1.0 - _DamageDarkness;

                return float4(saturate(col), 1.0);
            }
            ENDHLSL
        }
    }
}
