Shader "RuneWeaver/BarrierWall"
{
    // 거점 점령 결계 전용. 배경 위에 빛을 더한다. (CaptureZoneBarrier가 만든 그림을 쓴다)
    // 그림 채널 : R = 밝기, G = 바닥에서의 높이(0~1), B = 바닥 테두리 여부, A = 그릴 칸
    // _Reveal(0~1)보다 높은 칸은 그리지 않아 벽이 바닥에서부터 솟아오르게 한다. 솟는 끝은 살짝 밝게 빛난다.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _WallColor ("Wall Color", Color) = (1, 0.82, 0.42, 1)
        _RimColor ("Rim Color", Color) = (1, 0.96, 0.78, 1)
        _Reveal ("Reveal", Range(0, 1)) = 1
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment WallFrag
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"

            fixed4 _WallColor;
            fixed4 _RimColor;
            float _Reveal;

            fixed4 WallFrag(v2f input) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, input.texcoord);
                if (t.a < 0.5 || _Reveal <= 0.001 || t.g > _Reveal)
                {
                    return fixed4(0, 0, 0, 0);
                }

                fixed3 color = lerp(_WallColor.rgb, _RimColor.rgb, t.b);
                // 솟아오르는 끝부분을 살짝 밝게 (다 솟으면 사라진다)
                float edge = saturate(1 - (_Reveal - t.g) * 12) * (1 - _Reveal * _Reveal);
                float alpha = saturate(t.r + edge * 0.6) * input.color.a;
                color = lerp(color, _RimColor.rgb, edge);
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }
}
