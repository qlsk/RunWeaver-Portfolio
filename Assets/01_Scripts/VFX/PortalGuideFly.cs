using UnityEngine;

/// <summary>
/// 포탈 길잡이 빛(PortalGuideTrail) 한 마리의 그림.
/// 번짐(Glow), 심지(Core), 꼬리(Tail) 이미지를 들고 있고, 프리팹에서 정한 색에 투명도만 곱해 쓴다.
/// 색이나 크기는 프리팹의 각 SpriteRenderer에서 바꾸면 된다.
/// </summary>
[DisallowMultipleComponent]
public class PortalGuideFly : MonoBehaviour
{
    [Tooltip("넓게 퍼지는 빛 번짐")]
    [SerializeField] private SpriteRenderer _glow;
    [Tooltip("가운데 밝은 심지")]
    [SerializeField] private SpriteRenderer _core;
    [Tooltip("움직이는 방향 뒤로 남는 옅은 꼬리. 이미지는 오른쪽(+X)으로 길쭉해야 한다")]
    [SerializeField] private SpriteRenderer _tail;

    private Color _glowColor;
    private Color _coreColor;
    private Color _tailColor;
    private float _tailSpriteLength = 1f;
    private bool _cached;

    private void Awake()
    {
        Cache();
    }

    /// <summary>전체 투명도(0~1)를 정한다. 프리팹에서 정한 각 이미지의 투명도에 곱해진다.</summary>
    public void SetAlpha(float alpha)
    {
        Cache();
        Apply(_glow, _glowColor, alpha);
        Apply(_core, _coreColor, alpha);
        Apply(_tail, _tailColor, alpha);
    }

    /// <summary>
    /// 꼬리를 움직이는 방향 뒤쪽으로 늘인다.
    /// velocity : 이번 프레임 속도 (월드 단위/초), duration : 꼬리가 남는 시간, maxLength : 최대 길이
    /// </summary>
    public void SetTail(Vector2 velocity, float duration, float maxLength)
    {
        Cache();
        if (_tail == null)
        {
            return;
        }

        float speed = velocity.magnitude;
        float length = Mathf.Min(speed * duration, maxLength);
        if (length <= 0.001f)
        {
            _tail.enabled = false;
            return;
        }

        _tail.enabled = true;
        Vector2 direction = velocity / speed;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Transform tailTransform = _tail.transform;
        tailTransform.SetPositionAndRotation(transform.position - (Vector3)(direction * (length * 0.5f)),
            Quaternion.Euler(0f, 0f, angle));

        // 몸 크기 배율과 상관없이 길이를 맞춘다.
        float parentScale = Mathf.Max(0.0001f, transform.lossyScale.x);
        Vector3 scale = tailTransform.localScale;
        scale.x = length / (_tailSpriteLength * parentScale);
        tailTransform.localScale = scale;
    }

    private void Cache()
    {
        if (_cached)
        {
            return;
        }

        _cached = true;
        _glowColor = _glow != null ? _glow.color : Color.white;
        _coreColor = _core != null ? _core.color : Color.white;
        _tailColor = _tail != null ? _tail.color : Color.white;
        if (_tail != null && _tail.sprite != null)
        {
            _tailSpriteLength = Mathf.Max(0.0001f, _tail.sprite.bounds.size.x);
        }
    }

    private static void Apply(SpriteRenderer renderer, Color baseColor, float alpha)
    {
        if (renderer == null)
        {
            return;
        }

        baseColor.a *= alpha;
        renderer.color = baseColor;
    }
}
