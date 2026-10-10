using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 실패 연출 동안 플레이어 겉모습을 바꾼다. (플레이어 스크립트와 프리팹은 고치지 않는다)
/// 보스 처치 연출에서는 처치된 보스를 복제한 스프라이트에도 같은 방식으로 쓴다.
/// 만들 때 플레이어의 스프라이트들에 연출용 머티리얼을 끼우고, Restore로 원래 머티리얼을 돌려놓는다.
/// 높이 기준은 만들 때의 몸통 스프라이트 범위로 정한다. (연출 중에는 플레이어가 움직이지 않는다)
/// </summary>
public sealed class PlayerFailureFx
{
    private readonly List<SpriteRenderer> _renderers = new();
    private readonly List<Material> _originalMaterials = new();
    private readonly List<Light2D> _lights = new();
    private readonly List<float> _lightIntensities = new();
    private readonly Material _material;

    /// <summary>몸통 스프라이트의 월드 범위</summary>
    public Bounds Bounds { get; }

    /// <summary>플레이어 위에 그릴 연출의 정렬 레이어</summary>
    public int SortingLayerId { get; }

    /// <summary>플레이어의 정렬 순서. 이보다 크면 플레이어 위에 그려진다.</summary>
    public int SortingOrder { get; }

    public PlayerFailureFx(Transform root, Shader shader)
    {
        SpriteRenderer body = root.GetComponent<SpriteRenderer>();
        bool hasBounds = false;
        Bounds bounds = new(root.position, Vector3.one);

        foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            _renderers.Add(renderer);
            _originalMaterials.Add(renderer.sharedMaterial);

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (body != null && body.sprite != null)
        {
            bounds = body.bounds;
        }

        Bounds = bounds;

        SortingGroup group = root.GetComponent<SortingGroup>();
        if (group != null)
        {
            SortingLayerId = group.sortingLayerID;
            SortingOrder = group.sortingOrder;
        }
        else if (body != null)
        {
            SortingLayerId = body.sortingLayerID;
            SortingOrder = body.sortingOrder;
        }

        foreach (Light2D light in root.GetComponentsInChildren<Light2D>())
        {
            _lights.Add(light);
            _lightIntensities.Add(light.intensity);
        }

        _material = FailureSpriteMaterial.Create(shader, false, "PlayerFailure (Instance)");
        if (_material == null)
        {
            return;
        }

        float pixelsPerUnit = body != null && body.sprite != null ? body.sprite.pixelsPerUnit : 100f;
        float pixelSize = Mathf.Abs(root.lossyScale.y) / Mathf.Max(1f, pixelsPerUnit);
        _material.SetVector(FailureSpriteMaterial.BoundsY, new Vector4(bounds.min.y, bounds.max.y, 0f, 0f));
        _material.SetFloat(FailureSpriteMaterial.PixelSize, Mathf.Max(0.001f, pixelSize));

        foreach (SpriteRenderer renderer in _renderers)
        {
            renderer.sharedMaterial = _material;
        }
    }

    /// <summary>장면 밝기에 맞춘 색. 연출 머티리얼은 조명을 받지 않는다.</summary>
    public void SetTint(Color color)
    {
        if (_material != null)
        {
            _material.SetColor(FailureSpriteMaterial.Tint, color);
        }
    }

    public void SetDesaturate(float amount)
    {
        if (_material != null)
        {
            _material.SetFloat(FailureSpriteMaterial.Desaturate, Mathf.Clamp01(amount));
        }
    }

    public void SetFlash(Color color, float amount)
    {
        if (_material == null)
        {
            return;
        }

        _material.SetColor(FailureSpriteMaterial.FlashColor, color);
        _material.SetFloat(FailureSpriteMaterial.Flash, Mathf.Clamp01(amount));
    }

    /// <summary>발부터 위로 물든다. (0 → 1)</summary>
    public void SetStain(float amount, Color stainColor, Color edgeColor)
    {
        if (_material == null)
        {
            return;
        }

        _material.SetFloat(FailureSpriteMaterial.Stain, Mathf.Clamp01(amount));
        _material.SetColor(FailureSpriteMaterial.StainColor, stainColor);
        _material.SetColor(FailureSpriteMaterial.StainEdgeColor, edgeColor);
    }

    /// <summary>아래(fromTop이면 위)부터 사라진다. (0 → 1)</summary>
    public void SetDissolve(float amount, bool fromTop, float noise, Color edgeColor, float edgeWidth)
    {
        if (_material == null)
        {
            return;
        }

        _material.SetFloat(FailureSpriteMaterial.Dissolve, Mathf.Clamp01(amount));
        _material.SetFloat(FailureSpriteMaterial.DissolveFromTop, fromTop ? 1f : 0f);
        _material.SetFloat(FailureSpriteMaterial.DissolveNoise, noise);
        _material.SetColor(FailureSpriteMaterial.EdgeColor, edgeColor);
        _material.SetFloat(FailureSpriteMaterial.EdgeWidth, edgeWidth);
    }

    /// <summary>center에서 radius 안쪽이 color로 밝아진다. (amount 0이면 꺼짐)</summary>
    public void SetInnerGlow(Vector2 center, float radius, float softness, Color color, float amount)
    {
        if (_material == null)
        {
            return;
        }

        _material.SetVector(FailureSpriteMaterial.GlowCenter, new Vector4(center.x, center.y, 0f, 0f));
        _material.SetFloat(FailureSpriteMaterial.GlowRadius, radius);
        _material.SetFloat(FailureSpriteMaterial.GlowSoftness, softness);
        _material.SetColor(FailureSpriteMaterial.GlowColor, color);
        _material.SetFloat(FailureSpriteMaterial.GlowAmount, Mathf.Clamp01(amount));
    }

    /// <summary>사라지는 경계의 대략적인 월드 높이 (입자를 뿌릴 위치)</summary>
    public float GetDissolveFrontY(float amount, bool fromTop)
    {
        float t = Mathf.Clamp01(amount);
        return fromTop
            ? Mathf.Lerp(Bounds.max.y, Bounds.min.y, t)
            : Mathf.Lerp(Bounds.min.y, Bounds.max.y, t);
    }

    /// <summary>플레이어에 붙은 2D 조명 밝기 배율 (1 = 원래 밝기)</summary>
    public void SetLightFactor(float factor)
    {
        for (int i = 0; i < _lights.Count; i++)
        {
            if (_lights[i] != null)
            {
                _lights[i].intensity = _lightIntensities[i] * Mathf.Clamp01(factor);
            }
        }
    }

    /// <summary>원래 머티리얼과 조명 밝기로 되돌리고 연출용 머티리얼을 정리한다.</summary>
    public void Restore()
    {
        for (int i = 0; i < _renderers.Count; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].sharedMaterial = _originalMaterials[i];
            }
        }

        SetLightFactor(1f);

        if (_material != null)
        {
            Object.Destroy(_material);
        }
    }
}
