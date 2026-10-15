Shader "RuneWeaver/SpriteRadialGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowIntensity ("Glow Intensity", Float) = 2
        [PerRendererData] _GlowTime ("Glow Time", Float) = 0
        _LayerRotationSpeeds ("Layer Rotation Speeds (Degrees / Second)", Vector) = (2,-3,1.5,-2)
        [PerRendererData] _RevealProgress ("Reveal Progress", Range(0,1)) = 0
        _RevealFeather ("Reveal Edge Softness", Range(0.001,0.2)) = 0.01
        _RevealStartRadius ("Reveal Start Radius", Range(0,1)) = 0.38
        _RevealEndRadius ("Reveal End Radius", Range(0,1)) = 0.72
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
        Blend One OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex SpriteVert
            #pragma fragment RadialGlowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            #include "MagicCircleReveal.cginc"

            float4 _GlowColor;
            float _GlowIntensity;
            float _GlowTime;
            float4 _LayerRotationSpeeds;
            float _RevealProgress;
            float _RevealFeather;
            float _RevealStartRadius;
            float _RevealEndRadius;
            float4 _RevealStageRadii;

            float4 RadialGlowFrag(v2f input) : SV_Target
            {
                float2 offset = input.texcoord - float2(0.5, 0.5);
                float radialDistance = length(offset);
                float distanceFromCenter = radialDistance * 1.41421356;
                float reveal = MagicCircleRevealAlpha(distanceFromCenter, _RevealProgress, _RevealStageRadii.xyz, _RevealFeather);

                float4 color = MagicCircleRotatedTexture(input.texcoord, _RevealStageRadii.xyz,
                    _RevealFeather, _LayerRotationSpeeds, _GlowTime) * input.color;
                color.a *= reveal;
                color.rgb *= _GlowColor.rgb * _GlowIntensity * color.a;
                return color;
            }
            ENDCG
        }
    }
}
