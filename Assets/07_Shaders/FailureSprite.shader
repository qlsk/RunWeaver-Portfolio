// 실패 연출(사망, 탐사 포기, 임무 실패)에서 플레이어와 연출 스프라이트에 쓰는 셰이더.
// 조명을 받지 않는다. (연출 중에는 _Tint로 장면 밝기를 맞춘다)
// - 번쩍임 : _FlashColor / _Flash
// - 물듦 : 발부터 _StainColor로 물든다. 경계는 _StainEdgeColor로 빛난다. (_Stain 0 → 1)
// - 사라짐 : 아래(또는 위)부터 픽셀 단위 노이즈로 사라진다. 경계는 _EdgeColor로 빛난다. (_Dissolve 0 → 1)
// - 드러남 : _RevealCenter에서 _RevealRadius 안쪽만 보인다. (바닥 줄기가 퍼지는 연출)
// 높이 기준(_BoundsY)과 픽셀 크기(_PixelSize)는 월드 좌표로 스크립트가 넣어 준다.
Shader "RuneWeaver/FailureSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Desaturate ("Desaturate", Range(0, 1)) = 0
        _FlashColor ("Flash Color", Color) = (1, 1, 1, 1)
        _Flash ("Flash", Range(0, 1)) = 0
        _StainColor ("Stain Color", Color) = (0.2, 0.06, 0.26, 1)
        _StainEdgeColor ("Stain Edge Color", Color) = (0.75, 0.3, 0.95, 1)
        _Stain ("Stain", Range(0, 1)) = 0
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _DissolveFromTop ("Dissolve From Top", Float) = 0
        _DissolveNoise ("Dissolve Noise", Range(0, 1)) = 0.3
        _EdgeColor ("Edge Color", Color) = (1, 1, 1, 1)
        _EdgeWidth ("Edge Width", Range(0, 0.3)) = 0.06
        _BoundsY ("Bounds Y (min, max)", Vector) = (0, 1, 0, 0)
        _PixelSize ("Pixel Size", Float) = 0.02
        _RevealCenter ("Reveal Center (xy) / Axis Scale (zw)", Vector) = (0, 0, 1, 1)
        _RevealRadius ("Reveal Radius", Float) = 100000
        _RevealSoftness ("Reveal Softness", Float) = 0.3
        _Alpha ("Alpha", Range(0, 1)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);

    CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        half4 _Tint;
        half _Desaturate;
        half4 _FlashColor;
        half _Flash;
        half4 _StainColor;
        half4 _StainEdgeColor;
        half _Stain;
        half _Dissolve;
        half _DissolveFromTop;
        half _DissolveNoise;
        half4 _EdgeColor;
        half _EdgeWidth;
        float4 _BoundsY;
        float _PixelSize;
        float4 _RevealCenter;
        float _RevealRadius;
        float _RevealSoftness;
        half _Alpha;
    CBUFFER_END

    struct Attributes
    {
        float3 positionOS : POSITION;
        float4 color : COLOR;
        float2 uv : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        half4 color : COLOR;
        float2 uv : TEXCOORD0;
        float3 positionWS : TEXCOORD1;
    };

    float Hash21(float2 p)
    {
        p = frac(p * float2(123.34, 456.21));
        p += dot(p, p + 45.32);
        return frac(p.x * p.y);
    }

    Varyings Vert(Attributes input)
    {
        Varyings output;
        output.positionWS = TransformObjectToWorld(input.positionOS);
        output.positionCS = TransformWorldToHClip(output.positionWS);
        output.uv = TRANSFORM_TEX(input.uv, _MainTex);
        output.color = input.color;
        return output;
    }

    half4 Frag(Varyings input) : SV_Target
    {
        half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;

        // 픽셀 아트에 맞춰 월드 좌표를 픽셀 칸으로 나눈 노이즈
        float2 cell = floor(input.positionWS.xy / max(_PixelSize, 1e-4));
        float fine = Hash21(cell);
        float coarse = Hash21(floor(cell / 3.0));
        float grain = lerp(fine, coarse, 0.5);
        float height = saturate((input.positionWS.y - _BoundsY.x) / max(_BoundsY.y - _BoundsY.x, 1e-4));

        half luma = dot(color.rgb, half3(0.299, 0.587, 0.114));
        color.rgb = lerp(color.rgb, luma.xxx, _Desaturate);
        color.rgb *= _Tint.rgb;

        // 물듦 (발부터 위로)
        float stainFront = _Stain * 1.25 - 0.1;
        float stain = saturate((stainFront - height + (grain - 0.5) * 0.15) / 0.06);
        color.rgb = lerp(color.rgb, _StainColor.rgb * (0.55 + luma * 0.9), stain);
        float stainActive = step(0.001, _Stain) * step(_Stain, 0.999);
        color.rgb += _StainEdgeColor.rgb * (stain * (1.0 - stain) * 4.0) * 0.8 * stainActive;

        // 번쩍임
        color.rgb = lerp(color.rgb, _FlashColor.rgb, _Flash);

        // 사라짐
        float dissolveHeight = lerp(height, 1.0 - height, _DissolveFromTop);
        float front = lerp(-_DissolveNoise - _EdgeWidth - 0.01, 1.0 + _DissolveNoise, _Dissolve);
        float dist = dissolveHeight + (grain - 0.5) * _DissolveNoise * 2.0 - front;
        color.a *= step(0.0, dist);
        float edge = (1.0 - saturate(dist / max(_EdgeWidth, 1e-4))) * step(0.0001, _Dissolve);
        color.rgb = lerp(color.rgb, _EdgeColor.rgb, edge);

        // 드러남 (중심에서 바깥으로)
        float2 offset = (input.positionWS.xy - _RevealCenter.xy) * _RevealCenter.zw;
        float radius = _RevealRadius * (0.85 + 0.3 * coarse);
        color.a *= saturate((radius - length(offset)) / max(_RevealSoftness, 1e-4));

        color.a *= _Alpha;
        return color;
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend [_SrcBlend] [_DstBlend]
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }

    Fallback Off
}
