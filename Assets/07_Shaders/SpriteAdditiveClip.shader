Shader "RuneWeaver/SpriteAdditiveClip"
{
    // SpriteAdditive에 "월드 높이(Y) 기준으로 위나 아래만 그리기"를 더한 셰이더.
    // 캐릭터를 가로로 감싸는 고리를 앞쪽(아래 절반)과 뒤쪽(위 절반)으로 나눠 그릴 때 쓴다.
    // _ClipY, _ClipDir는 스크립트가 MaterialPropertyBlock으로 넣는다.
    //   _ClipDir = -1 : _ClipY보다 아래만 그린다 (앞쪽)
    //   _ClipDir = +1 : _ClipY보다 위만 그린다 (뒤쪽)
    //   _ClipDir =  0 : 자르지 않는다
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        [PerRendererData] _ClipY ("Clip Y (World)", Float) = 0
        [PerRendererData] _ClipDir ("Clip Direction", Float) = 0
        _ClipSoftness ("Clip Softness", Float) = 0.02
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex ClipVert
            #pragma fragment ClipFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float _ClipY;
            float _ClipDir;
            float _ClipSoftness;

            struct v2fClip
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float worldY : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2fClip ClipVert(appdata_t IN)
            {
                v2fClip OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 local = UnityFlipSprite(IN.vertex, _Flip);
                OUT.worldY = mul(unity_ObjectToWorld, local).y;
                OUT.vertex = UnityObjectToClipPos(local);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;
                return OUT;
            }

            fixed4 ClipFrag(v2fClip IN) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(IN.texcoord) * IN.color;
                if (_ClipDir != 0)
                {
                    float side = (IN.worldY - _ClipY) * _ClipDir;
                    color.a *= saturate(side / max(_ClipSoftness, 0.0001) + 0.5);
                }
                return color;
            }
            ENDCG
        }
    }
}
