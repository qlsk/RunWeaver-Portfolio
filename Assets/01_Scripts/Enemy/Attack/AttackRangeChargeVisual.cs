using UnityEngine;

public sealed class AttackRangeChargeVisual : System.IDisposable
{
    private readonly SpriteRenderer _renderer;
    private readonly Material _material;
    private static readonly int Progress = Shader.PropertyToID("_ChargeProgress");
    private static readonly int BaseAlpha = Shader.PropertyToID("_BaseAlpha");
    private static readonly int ChargedAlpha = Shader.PropertyToID("_ChargedAlpha");

    public AttackRangeChargeVisual(SpriteRenderer renderer, bool radial)
    {
        _renderer = renderer;
        Shader shader = Resources.Load<Shader>("AttackRangeCharge");
        if (shader == null)
        {
            Debug.LogError("AttackRangeCharge shader를 찾을 수 없습니다.", renderer);
            return;
        }

        _material = new Material(shader);
        Bounds bounds = renderer.sprite.bounds;
        _material.SetVector("_SpriteBounds", new Vector4(bounds.min.x, bounds.min.y,
            Mathf.Max(bounds.size.x, 0.0001f), Mathf.Max(bounds.size.y, 0.0001f)));
        _material.SetFloat("_Radial", radial ? 1f : 0f);
        renderer.sharedMaterial = _material;
    }

    public void SetProgress(float progress, Color color, float chargedAlpha)
    {
        if (_material == null) return;
        _material.SetFloat(Progress, Mathf.Clamp01(progress));
        _material.SetFloat(BaseAlpha, color.a);
        _material.SetFloat(ChargedAlpha, Mathf.Max(color.a, chargedAlpha));
        color.a = 1f;
        _renderer.color = color;
    }

    public void Dispose()
    {
        if (_material != null) Object.Destroy(_material);
    }
}
