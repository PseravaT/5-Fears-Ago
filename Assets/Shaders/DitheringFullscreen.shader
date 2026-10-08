Shader "Hidden/DitheringFullscreen"
{
    Properties
    {
        _PixelSize ("Pixelate (px)", Float) = 3
        _Intensity ("Dither Intensity", Range(0, 1)) = 0.73
        _Levels    ("Color Levels", Float) = 8
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "Dithering"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Fornece Vert, Varyings, _BlitTexture e sampler_PointClamp
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _PixelSize;
            float _Intensity;
            float _Levels;

            // Matriz de Bayer 2x2 (valores 0..3)
            static const float Bayer2x2[4] = { 0.0, 2.0, 3.0, 1.0 };

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 res = _ScreenParams.xy;

                // 1) Pixelate: agrupa pixels em blocos de _PixelSize
                float2 block = floor(input.texcoord * res / _PixelSize);
                float2 uv = (block + 0.5) * _PixelSize / res;

                float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rgb;

                // 2) Limiar de Bayer, centrado em zero (-0.5 .. +0.5)
                int2 p = int2(block) & 1;
                float threshold = (Bayer2x2[p.y * 2 + p.x] + 0.5) / 4.0 - 0.5;

                // 3) Soma o limiar (em unidades de "degrau") e quantiza
                float steps = max(_Levels - 1.0, 1.0);
                col = floor(col * steps + threshold * _Intensity + 0.5) / steps;

                return float4(saturate(col), 1.0);
            }
            ENDHLSL
        }
    }
}
