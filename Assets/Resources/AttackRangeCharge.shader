Shader "RuneWeaver/AttackRangeCharge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ChargeProgress ("Charge Progress", Range(0,1)) = 0
        _BaseAlpha ("Base Alpha", Range(0,1)) = 0.35
        _ChargedAlpha ("Charged Alpha", Range(0,1)) = 0.9
        _Radial ("Radial Fill", Float) = 0
        _SpriteBounds ("Sprite Bounds", Vector) = (0,0,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" "DisableBatching"="True" }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            CGPROGRAM
            #pragma vertex ChargeVert
            #pragma fragment ChargeFrag
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float _ChargeProgress;
            float _BaseAlpha;
            float _ChargedAlpha;
            float _Radial;
            float4 _SpriteBounds;

            struct ChargeOutput
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 position : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ChargeOutput ChargeVert(appdata_t input)
            {
                ChargeOutput output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float4 position = UnityFlipSprite(input.vertex.xyz, _Flip);
                output.vertex = UnityObjectToClipPos(position);
                output.color = input.color * _Color * _RendererColor;
                output.texcoord = input.texcoord;
                output.position = (position.xy - _SpriteBounds.xy) / _SpriteBounds.zw;
                return output;
            }

            fixed4 ChargeFrag(ChargeOutput input) : SV_Target
            {
                float distance = _Radial > 0.5
                    ? length((input.position - 0.5) * 2.0)
                    : input.position.x;
                float filled = _ChargeProgress <= 0.0 ? 0.0
                    : (_ChargeProgress >= 1.0 ? 1.0 : step(distance, _ChargeProgress));
                fixed4 color = SampleSpriteTexture(input.texcoord) * input.color;
                color.a *= lerp(_BaseAlpha, _ChargedAlpha, filled);
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
