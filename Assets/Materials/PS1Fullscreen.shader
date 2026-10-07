Shader "Hidden/PS1Fullscreen"
{
    Properties
    {
        _TargetHeight   ("Internal Height (px)", Float) = 240
        _ColorBits      ("Bits per Channel", Range(2, 8)) = 5
        _DitherStrength ("Dither Strength", Range(0, 2)) = 1
        _Saturation     ("Saturation", Range(0, 2)) = 1.1
        _Contrast       ("Contrast", Range(0.5, 1.5)) = 1.05
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "PS1"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Fornece Vert, Varyings, _BlitTexture e sampler_PointClamp
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _TargetHeight;
            float _ColorBits;
            float _DitherStrength;
            float _Saturation;
            float _Contrast;

            // Matriz de dither 4x4 do PS1 (valores em escala 0..255)
            static const float Dither4x4[16] =
            {
                -4.0,  0.0, -3.0,  1.0,
                 2.0, -2.0,  3.0, -1.0,
                -3.0,  1.0, -4.0,  0.0,
                 3.0, -1.0,  2.0, -2.0
            };

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // 1) Resolução interna baixa, mantendo a proporção da tela
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 internalRes = float2(round(_TargetHeight * aspect), _TargetHeight);

                float2 block = floor(input.texcoord * internalRes);
                float2 uv = (block + 0.5) / internalRes;

                float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rgb;

                // 2) Contraste e saturação leves (o visual "lavado" do CRT)
                col = (col - 0.5) * _Contrast + 0.5;
                float luma = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(luma.xxx, col, _Saturation);
                col = saturate(col);

                // 3) Dithering 4x4 no pixel interno, antes de reduzir a profundidade de cor
                int2 p = int2(block) & 3;
                float d = Dither4x4[p.y * 4 + p.x] * _DitherStrength;

                // 4) Reduz para N bits por canal (5 = 15-bit do PS1)
                float levels = exp2(_ColorBits);
                float stepSize = 256.0 / levels;
                float3 c255 = col * 255.0 + d;
                c255 = floor(c255 / stepSize) * stepSize;
                col = saturate(c255 / (256.0 - stepSize));

                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
