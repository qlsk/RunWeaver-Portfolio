Shader "RuneWeaver/EnemyBulletContrast"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _CoreColor ("Bright Core", Color) = (0.98,0.98,1,1)
        _OutlineColor ("Inner Outline", Color) = (0.035,0.025,0.055,1)
        _OutlineWidth ("Inner Outline (Screen Pixels)", Range(0,6)) = 2.5
        _OutlineTexels ("Minimum Inner Outline (Texture Pixels)", Range(0,32)) = 16
        _HighlightStrength ("Bright Detail Strength", Range(0,1)) = 0.8
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex SpriteVert
            #pragma fragment ContrastFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            fixed4 _CoreColor;
            fixed4 _OutlineColor;
            float4 _MainTex_TexelSize;
            float _OutlineWidth;
            float _OutlineTexels;
            float _HighlightStrength;

            fixed4 ContrastFrag(v2f input) : SV_Target
            {
                fixed4 source = SampleSpriteTexture(input.texcoord);
                float2 pixelX = ddx(input.texcoord);
                float2 pixelY = ddy(input.texcoord);
                float texelsPerPixel = max(length(pixelX / _MainTex_TexelSize.xy), length(pixelY / _MainTex_TexelSize.xy));
                float width = max(_OutlineWidth, _OutlineTexels / max(texelsPerPixel, 0.0001));
                float2 dx = pixelX * width;
                float2 dy = pixelY * width;
                float innerAlpha = source.a;
                innerAlpha = min(innerAlpha, SampleSpriteTexture(input.texcoord + dx).a);
                innerAlpha = min(innerAlpha, SampleSpriteTexture(input.texcoord - dx).a);
                innerAlpha = min(innerAlpha, SampleSpriteTexture(input.texcoord + dy).a);
                innerAlpha = min(innerAlpha, SampleSpriteTexture(input.texcoord - dy).a);
                innerAlpha = min(innerAlpha, SampleSpriteTexture(input.texcoord + (dx + dy) * 0.7071).a);
                innerAlpha = min(innerAlpha, SampleSpriteTexture(input.texcoord - (dx + dy) * 0.7071).a);
                innerAlpha = min(innerAlpha, SampleSpriteTexture(input.texcoord + (dx - dy) * 0.7071).a);
                innerAlpha = min(innerAlpha, SampleSpriteTexture(input.texcoord - (dx - dy) * 0.7071).a);
                float coreMask = smoothstep(0.65, 0.95, innerAlpha);
                float luminance = dot(source.rgb, float3(0.2126, 0.7152, 0.0722));
                float highlight = smoothstep(0.3, 0.75, luminance) * _HighlightStrength;
                fixed3 core = lerp(source.rgb, _CoreColor.rgb, highlight);
                fixed3 color = lerp(_OutlineColor.rgb, core, coreMask) * input.color.rgb;
                return fixed4(color, source.a * input.color.a);
            }
            ENDCG
        }
    }
}
