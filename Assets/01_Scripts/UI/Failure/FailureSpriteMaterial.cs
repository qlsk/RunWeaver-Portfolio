using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 실패 연출 셰이더(RuneWeaver/FailureSprite)의 속성 이름과 머티리얼 생성.
/// 머티리얼은 실행 중에 만들고, 만든 쪽이 Destroy로 정리한다.
/// </summary>
public static class FailureSpriteMaterial
{
    public static readonly int Tint = Shader.PropertyToID("_Tint");
    public static readonly int Desaturate = Shader.PropertyToID("_Desaturate");
    public static readonly int FlashColor = Shader.PropertyToID("_FlashColor");
    public static readonly int Flash = Shader.PropertyToID("_Flash");
    public static readonly int StainColor = Shader.PropertyToID("_StainColor");
    public static readonly int StainEdgeColor = Shader.PropertyToID("_StainEdgeColor");
    public static readonly int Stain = Shader.PropertyToID("_Stain");
    public static readonly int Dissolve = Shader.PropertyToID("_Dissolve");
    public static readonly int DissolveFromTop = Shader.PropertyToID("_DissolveFromTop");
    public static readonly int DissolveNoise = Shader.PropertyToID("_DissolveNoise");
    public static readonly int EdgeColor = Shader.PropertyToID("_EdgeColor");
    public static readonly int EdgeWidth = Shader.PropertyToID("_EdgeWidth");
    public static readonly int BoundsY = Shader.PropertyToID("_BoundsY");
    public static readonly int PixelSize = Shader.PropertyToID("_PixelSize");
    public static readonly int RevealCenter = Shader.PropertyToID("_RevealCenter");
    public static readonly int RevealRadius = Shader.PropertyToID("_RevealRadius");
    public static readonly int RevealSoftness = Shader.PropertyToID("_RevealSoftness");
    public static readonly int GlowColor = Shader.PropertyToID("_GlowColor");
    public static readonly int GlowCenter = Shader.PropertyToID("_GlowCenter");
    public static readonly int GlowRadius = Shader.PropertyToID("_GlowRadius");
    public static readonly int GlowSoftness = Shader.PropertyToID("_GlowSoftness");
    public static readonly int GlowAmount = Shader.PropertyToID("_GlowAmount");
    public static readonly int Alpha = Shader.PropertyToID("_Alpha");

    private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");

    /// <summary>
    /// 셰이더로 머티리얼을 만든다. additive가 true면 빛처럼 더해서 그린다. 셰이더가 없으면 null
    /// </summary>
    public static Material Create(Shader shader, bool additive, string name)
    {
        if (shader == null)
        {
            return null;
        }

        Material material = new(shader) { name = name };
        material.SetFloat(SrcBlend, (float)BlendMode.SrcAlpha);
        material.SetFloat(DstBlend, (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        return material;
    }
}
