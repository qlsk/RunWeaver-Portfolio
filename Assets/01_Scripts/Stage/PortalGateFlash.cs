using UnityEngine;

/// <summary>들어갈 때 소용돌이 반짝임. 원본 그림을 따라 바꾸며 밝은 빛을 겹친다.</summary>
public class PortalGateFlash : MonoBehaviour
{
    private const float Duration = 0.45f;
    private const float MaxAlpha = 0.5f;
    private const float Swell = 0.03f;
    private static readonly Color FlashColor = new(0.8f, 0.68f, 1f, 1f);
    private static Material s_unlitMaterial;

    private SpriteRenderer _source;
    private SpriteRenderer _overlay;
    private Vector3 _sourceScale;
    private float _elapsed;

    public void Initialize(SpriteRenderer source)
    {
        _source = source;
        _sourceScale = source.transform.localScale;
        _overlay = gameObject.AddComponent<SpriteRenderer>();
        _overlay.sharedMaterial = GetUnlitMaterial() ?? source.sharedMaterial;
        _overlay.sortingLayerID = source.sortingLayerID;
        _overlay.sortingOrder = source.sortingOrder + 1;
        _overlay.spriteSortPoint = source.spriteSortPoint;
        Sync(0f);
    }

    private void LateUpdate()
    {
        if (_source == null)
        {
            Destroy(gameObject);
            return;
        }

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / Duration);
        // 빠르게 밝아졌다가 천천히 가라앉는다.
        float wave = t < 0.25f ? PortalGateFx.Smooth(t / 0.25f) : 1f - PortalGateFx.Smooth((t - 0.25f) / 0.75f);
        Sync(wave);
        if (t >= 1f)
        {
            _source.transform.localScale = _sourceScale;
            Destroy(gameObject);
        }
    }

    private void Sync(float wave)
    {
        _overlay.sprite = _source.sprite;
        _overlay.flipX = _source.flipX;
        Color color = FlashColor;
        color.a = MaxAlpha * wave * _source.color.a;
        _overlay.color = color;
        _source.transform.localScale = _sourceScale * (1f + Swell * wave);
    }

    private static Material GetUnlitMaterial()
    {
        if (s_unlitMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader != null)
            {
                s_unlitMaterial = new Material(shader) { name = "PortalGateFlash (Unlit)" };
            }
        }

        return s_unlitMaterial;
    }
}
