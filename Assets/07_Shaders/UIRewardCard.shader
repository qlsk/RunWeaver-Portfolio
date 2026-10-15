Shader "RuneWeaver/UIRewardCard"
{
    // 보상 카드 연출용 UI 셰이더. UI/Default에 두 가지를 더했다.
    // - 모서리부터 그려지기 (_CornerReveal 0 → 1)
    // - 아래부터 타 들어가기 (_Burn 0 → 1). 타는 경계에 _BurnEdgeColor 빛을 더한다.
    // 카드 영역(_CardRect)은 루트 캔버스 공간 기준이며 RewardCardPresentation이 매 프레임 넣는다.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _NoiseTex ("Burn Noise", 2D) = "gray" {}
        _NoiseScale ("Burn Noise Scale", Float) = 1.4
        _CardRect ("Card Rect (x, y, width, height)", Vector) = (0,0,1,1)
        _CornerReveal ("Corner Reveal", Range(0, 1)) = 1
        _RevealSoftness ("Reveal Softness (px)", Float) = 30
        _Burn ("Burn", Range(0, 1)) = 0
        _BurnEdgeColor ("Burn Edge Color", Color) = (1, 0.55, 0.2, 1)
        _BurnEdgeWidth ("Burn Edge Width", Float) = 0.05

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 canvasPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _NoiseScale;
            float4 _CardRect;
            float _CornerReveal;
            float _RevealSoftness;
            float _Burn;
            fixed4 _BurnEdgeColor;
            float _BurnEdgeWidth;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.canvasPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                float2 size = max(_CardRect.zw, float2(1, 1));
                float2 local = IN.canvasPosition.xy - _CardRect.xy;
                float2 uv = local / size;

                // 모서리부터 그려지기: 네 모서리 중 가장 가까운 곳까지의 거리로 드러낸다.
                if (_CornerReveal < 1)
                {
                    float d0 = length(local);
                    float d1 = length(local - float2(size.x, 0));
                    float d2 = length(local - float2(0, size.y));
                    float d3 = length(local - size);
                    float corner = min(min(d0, d1), min(d2, d3));
                    float radius = _CornerReveal * length(size) * 0.55;
                    color.a *= saturate((radius - corner) / max(_RevealSoftness, 0.001));
                }

                // 아래부터 타 들어가기: 노이즈 + 높이로 경계를 만든다.
                if (_Burn > 0)
                {
                    float2 noiseUV = float2(uv.x * size.x / size.y, uv.y) * _NoiseScale;
                    float value = tex2D(_NoiseTex, noiseUV).r * 0.6 + uv.y * 0.9;
                    float threshold = _Burn * 1.9 - 0.3;
                    float keep = saturate((value - threshold) / 0.06);
                    float edge = saturate(1 - abs(value - threshold) / max(_BurnEdgeWidth, 0.001));
                    color.rgb = lerp(color.rgb, _BurnEdgeColor.rgb, edge * _BurnEdgeColor.a);
                    color.a *= keep;
                }

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.canvasPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
