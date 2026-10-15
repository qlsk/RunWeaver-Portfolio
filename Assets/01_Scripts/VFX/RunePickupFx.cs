using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class RunePickupFx : MonoBehaviour
{
    [SerializeField] private Sprite _glowSprite;
    [SerializeField] private Material _effectMaterial;
    [SerializeField] private Color _color = new Color(0.55f, 0.85f, 1f, 0.35f);
    [SerializeField, Min(0.01f)] private float _glowDiameter = 0.65f;
    [SerializeField, Min(0.01f)] private float _trailTime = 0.25f;
    [SerializeField, Min(0.001f)] private float _trailWidth = 0.12f;

    private SpriteRenderer _source;
    private SpriteRenderer _glow;
    private TrailRenderer _trail;

    private void Awake()
    {
        _source = GetComponent<SpriteRenderer>();
        if (_glowSprite == null || _effectMaterial == null) return;

        var glowObject = new GameObject("Rune Glow");
        glowObject.layer = gameObject.layer;
        glowObject.transform.SetParent(transform, false);
        _glow = glowObject.AddComponent<SpriteRenderer>();
        _glow.sprite = _glowSprite;
        _glow.sharedMaterial = _effectMaterial;
        _glow.color = _color;

        var trailObject = new GameObject("Rune Trail");
        trailObject.layer = gameObject.layer;
        trailObject.transform.SetParent(transform, false);
        _trail = trailObject.AddComponent<TrailRenderer>();
        _trail.sharedMaterial = _effectMaterial;
        _trail.time = _trailTime;
        _trail.minVertexDistance = 0.02f;
        _trail.widthMultiplier = _trailWidth;
        _trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        _trail.startColor = _color;
        _trail.endColor = new Color(_color.r, _color.g, _color.b, 0f);
        _trail.numCapVertices = 4;
        _trail.numCornerVertices = 3;
        _trail.textureMode = LineTextureMode.Stretch;
        _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _trail.receiveShadows = false;
        _trail.Clear();
    }

    private void OnEnable()
    {
        if (_glow != null) _glow.enabled = true;
        if (_trail == null) return;
        _trail.enabled = true;
        _trail.Clear();
        _trail.emitting = true;
    }

    private void OnDisable()
    {
        if (_glow != null) _glow.enabled = false;
        if (_trail == null) return;
        _trail.emitting = false;
        _trail.Clear();
        _trail.enabled = false;
    }

    private void LateUpdate()
    {
        if (_glow == null || _trail == null) return;
        Vector3 scale = transform.lossyScale;
        Vector3 size = _glowSprite.bounds.size;
        _glow.transform.localScale = new Vector3(
            _glowDiameter / Mathf.Max(0.001f, Mathf.Abs(scale.x) * size.x),
            _glowDiameter / Mathf.Max(0.001f, Mathf.Abs(scale.y) * size.y), 1f);
        _glow.sortingLayerID = _source.sortingLayerID;
        _glow.sortingOrder = _source.sortingOrder - 1;
        _trail.sortingLayerID = _source.sortingLayerID;
        _trail.sortingOrder = _source.sortingOrder - 2;
    }
}
