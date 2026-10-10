Shader "RuneWeaver/SpriteMagicCircleAura"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _GlowColor ("Aura Color", Color) = (0.1,0.65,1,1)
        _GlowIntensity ("Aura Intensity", Float) = 2
        _AuraWidth ("Aura Width", Range(0.001,0.04)) = 0.012
        _AuraOpacity ("Aura Opacity", Range(0,1)) = 0.35
        _ShimmerAmount ("Aura Movement", Range(0,0.01)) = 0.003
        _ShimmerSpeed ("Aura Movement Speed", Float) = 1.3
        _ShimmerStrength ("Aura Light Variation", Range(0,0.3)) = 0.15
        [PerRendererData] _GlowTime ("Glow Time", Float) = 0
        _LayerRotationSpeeds ("Layer Rotation Speeds (Degrees / Second)", Vector) = (2,-3,1.5,-2)
        [PerRendererData] _RevealProgress ("Reveal Progress", Range(0,1)) = 0
        _RevealFeather ("Reveal Softness", Float) = 0.01
        _RevealStartRadius ("Reveal Start Radius", Float) = 0.38
        _RevealEndRadius ("Reveal End Radius", Float) = 0.72
        _RevealStageRadii ("Inner Circle / Runes / Outer Circle Radii", Vector) = (0.46,0.52,0.55,0)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False" }
        Cull Off
        ZWrite Off
        Blend One One
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex SpriteVert
            #pragma fragment AuraFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            #include "MagicCircleReveal.cginc"

            float4 _GlowColor;
            float _GlowIntensity, _AuraWidth, _AuraOpacity;
            float _ShimmerAmount, _ShimmerSpeed, _ShimmerStrength, _GlowTime;
            float _RevealProgress, _RevealFeather, _RevealStartRadius, _RevealEndRadius;
            float4 _RevealStageRadii;
            float4 _LayerRotationSpeeds;

            float4 SampleRotatingCircle(float2 uv)
            {
                return MagicCircleRotatedTexture(uv, _RevealStageRadii.xyz,
                    _RevealFeather, _LayerRotationSpeeds, _GlowTime);
            }

            float4 AuraFrag(v2f input) : SV_Target
            {
                float2 offset = input.texcoord - float2(0.5, 0.5);
                float radius = length(offset);
                float distanceFromCenter = radius * 1.41421356;
                float angle = atan2(offset.y, offset.x);
                float time = _GlowTime * _ShimmerSpeed;
                float wave = sin(angle * 5.0 + time * 0.8) * sin(distanceFromCenter * 16.0 - time * 1.2);
                float2 uv = input.texcoord + offset / max(radius, 0.00001) * wave * _ShimmerAmount;
                float width = _AuraWidth * (1.0 + wave * 0.15);
                float blur = SampleRotatingCircle(uv + float2(width, 0)).a;
                blur += SampleRotatingCircle(uv - float2(width, 0)).a;
                blur += SampleRotatingCircle(uv + float2(0, width)).a;
                blur += SampleRotatingCircle(uv - float2(0, width)).a;
                blur += SampleRotatingCircle(uv + float2(width, width) * 0.7071).a;
                blur += SampleRotatingCircle(uv + float2(-width, width) * 0.7071).a;
                blur += SampleRotatingCircle(uv + float2(width, -width) * 0.7071).a;
                blur += SampleRotatingCircle(uv - float2(width, width) * 0.7071).a;
                float halo = max(blur * 0.125 - SampleRotatingCircle(input.texcoord).a * 0.65, 0.0);
                float reveal = MagicCircleRevealAlpha(distanceFromCenter, _RevealProgress, _RevealStageRadii.xyz, _RevealFeather);
                float light = 1.0 + sin(angle * 3.0 + distanceFromCenter * 12.0 - time) * _ShimmerStrength;
                float intensity = halo * _AuraOpacity * light * reveal * input.color.a;
                return float4(_GlowColor.rgb * input.color.rgb * _GlowIntensity * intensity, 0);
            }
            ENDCG
        }
    }
}
