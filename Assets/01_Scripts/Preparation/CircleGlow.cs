using UnityEngine;

/// <summary>
/// 시작 석상 바닥 마법진의 근접 발광. 플레이어가 가까이 오면 발광(Glow)과 오라(Aura)가 퍼지듯 나타나고,
/// 멀어지면 천천히 사라진다. (순수 C# 클래스. ExpeditionStatue가 만들어서 매 프레임 Tick을 부른다)
/// Base / Glow 렌더러는 지정하지 않으면 석상과 함께 사라지는 렌더러 중 이름이 "Base" / "Glow"인 것을 찾고,
/// Aura는 Glow 아래의 "MagicCircleAura"를 찾는다.
/// </summary>
public sealed class CircleGlow
{
    private static readonly int RevealProgressId = Shader.PropertyToID("_RevealProgress");
    private static readonly int GlowTimeId = Shader.PropertyToID("_GlowTime");

    private readonly SpriteRenderer _glowRenderer;
    private readonly SpriteRenderer _auraRenderer;
    private readonly MaterialPropertyBlock _properties;

    // 인스펙터에서 바꾼 값을 바로 따라갈 수 있게, ExpeditionStatue가 매 프레임 Configure로 갱신한다.
    private float _activationDistance;
    private float _glowAlpha;
    private float _expandDuration;

    private float _progress;
    private float _time;

    public CircleGlow(SpriteRenderer[] candidates, SpriteRenderer baseRenderer, SpriteRenderer glowRenderer,
        SpriteRenderer auraRenderer)
    {
        if (candidates != null)
        {
            foreach (SpriteRenderer circle in candidates)
            {
                if (circle == null)
                {
                    continue;
                }

                if (baseRenderer == null && circle.gameObject.name == "Base")
                {
                    baseRenderer = circle;
                }

                if (glowRenderer == null && circle.gameObject.name == "Glow")
                {
                    glowRenderer = circle;
                }
            }
        }

        _glowRenderer = glowRenderer;
        if (_glowRenderer != null)
        {
            _properties = new MaterialPropertyBlock();
            if (auraRenderer == null)
            {
                Transform aura = _glowRenderer.transform.Find("MagicCircleAura");
                if (aura != null)
                {
                    auraRenderer = aura.GetComponent<SpriteRenderer>();
                }
            }
        }

        _auraRenderer = auraRenderer;

        // 발광은 마법진 바로 아래, 오라는 그 아래에 그려서 겹쳐 보이게 한다.
        if (baseRenderer != null && _glowRenderer != null)
        {
            _glowRenderer.sortingLayerID = baseRenderer.sortingLayerID;
            _glowRenderer.sortingOrder = baseRenderer.sortingOrder - 1;
        }

        if (_glowRenderer != null && _auraRenderer != null)
        {
            _auraRenderer.sortingLayerID = _glowRenderer.sortingLayerID;
            _auraRenderer.sortingOrder = _glowRenderer.sortingOrder - 1;
        }
    }

    /// <summary>발광 렌더러가 있어 동작할 수 있는지</summary>
    public bool IsAvailable => _glowRenderer != null;

    /// <summary>발광 설정을 정한다. 플레이 중에 인스펙터 값을 바꿔도 따라가도록 자주 불러도 된다.</summary>
    /// <param name="activationDistance">이 거리 안에 플레이어가 들어오면 발광이 퍼진다</param>
    /// <param name="glowAlpha">발광이 퍼졌을 때의 투명도</param>
    /// <param name="expandDuration">발광이 완전히 퍼지는 데 걸리는 시간</param>
    public void Configure(float activationDistance, float glowAlpha, float expandDuration)
    {
        _activationDistance = activationDistance;
        _glowAlpha = glowAlpha;
        _expandDuration = expandDuration;
    }

    /// <summary>플레이어와의 거리에 맞춰 발광을 퍼뜨리거나 거둔다.</summary>
    public void Tick(Transform player, float deltaTime)
    {
        if (_glowRenderer == null)
        {
            return;
        }

        bool isNearby = player != null &&
                        ((Vector2)(player.position - _glowRenderer.transform.position)).sqrMagnitude <=
                        _activationDistance * _activationDistance;
        float target = isNearby ? 1f : 0f;
        _time += deltaTime;
        float expandSpeed = 1f / Mathf.Max(0.01f, _expandDuration);
        SetProgress(Mathf.MoveTowards(_progress, target, expandSpeed * deltaTime));
    }

    /// <summary>발광이 퍼진 정도(0~1)를 정하고 렌더러에 반영한다.</summary>
    public void SetProgress(float progress)
    {
        _progress = progress;
        if (_glowRenderer == null)
        {
            return;
        }

        _glowRenderer.GetPropertyBlock(_properties);
        _properties.SetFloat(RevealProgressId, progress);
        _properties.SetFloat(GlowTimeId, _time);
        _glowRenderer.SetPropertyBlock(_properties);
        Color color = _glowRenderer.color;
        color.a = progress > 0f ? _glowAlpha : 0f;
        _glowRenderer.color = color;
        if (_auraRenderer != null)
        {
            _auraRenderer.SetPropertyBlock(_properties);
            Color auraColor = _auraRenderer.color;
            auraColor.a = color.a;
            _auraRenderer.color = auraColor;
        }
    }
}
