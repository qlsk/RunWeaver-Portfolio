using UnityEngine;

/// <summary>
/// 포탈이 열려 있는 동안 플레이어 주위를 돌면서 포탈 방향을 가리키는 화살표.
/// 방향이 바뀌면 부드럽게 따라 돌고, 포탈 쪽으로 콕콕 찌르듯 앞뒤로 움직인다.
/// 플레이어가 포탈에 가까워지면 서서히 사라진다.
/// 게임 시간(Time.deltaTime)으로 움직이므로 일시정지 중에는 멈춘다.
/// </summary>
[DisallowMultipleComponent]
public class PortalDirectionArrow : MonoBehaviour
{
    private const string PlayerTag = "Player";

    [Header("이미지")]
    [SerializeField] private Sprite _arrowSprite;
    [Tooltip("스프라이트 원본에서 화살 끝이 향하는 각도 (오른쪽 0도, 반시계 방향 +)")]
    [SerializeField] private float _spriteForwardAngle = 112f;
    [SerializeField, Min(0.01f)] private float _scale = 1.6f;
    [Tooltip("다른 오브젝트(YSort 등)보다 위에 그려지도록 높은 값을 쓴다.")]
    [SerializeField] private int _sortingOrder = 30000;

    [Header("위치")]
    [Tooltip("플레이어 위치에서 회전 중심(몸 중심)까지의 오프셋")]
    [SerializeField] private Vector2 _centerOffset = Vector2.zero;
    [Tooltip("회전 중심에서 화살표까지의 거리")]
    [SerializeField, Min(0f)] private float _orbitRadius = 1.1f;
    [Tooltip("방향이 바뀔 때 따라 도는 데 걸리는 시간 (클수록 느긋하게 따라간다)")]
    [SerializeField, Min(0f)] private float _turnSmoothTime = 0.2f;
    [Tooltip("플레이어를 따라오는 데 걸리는 시간 (클수록 살짝 늦게 둥실 따라온다)")]
    [SerializeField, Min(0f)] private float _followSmoothTime = 0.08f;

    [Header("움직임")]
    [Tooltip("포탈 쪽으로 앞뒤로 움직이는 거리")]
    [SerializeField, Min(0f)] private float _bobAmount = 0.1f;
    [SerializeField, Min(0f)] private float _bobSpeed = 4f;

    [Header("표시")]
    [SerializeField, Min(0f)] private float _fadeInDuration = 0.4f;
    [Tooltip("나타날 때 이 크기 비율에서 시작해 원래 크기로 커진다")]
    [SerializeField, Range(0f, 1f)] private float _appearStartScale = 0.6f;
    [Tooltip("투명도가 바뀔 때 걸리는 시간 (거리에 따라 흐려질 때 부드럽게)")]
    [SerializeField, Min(0f)] private float _alphaSmoothTime = 0.15f;
    [Tooltip("포탈과 이 거리보다 가까우면 사라지기 시작한다")]
    [SerializeField, Min(0f)] private float _fadeStartDistance = 3f;
    [Tooltip("포탈과 이 거리보다 가까우면 완전히 숨긴다")]
    [SerializeField, Min(0f)] private float _hideDistance = 1.9f;

    private SpriteRenderer _renderer;
    private Transform _arrow;
    private Transform _player;
    private Portal _portal;
    private float _angle;
    private float _angleVelocity;
    private Vector3 _center;
    private Vector3 _centerVelocity;
    private float _alpha;
    private float _alphaVelocity;
    private bool _hasAngle;
    private float _visibleTime;
    private float _bobTime;

    private void Awake()
    {
        GameObject arrowObject = new GameObject("Arrow");
        _arrow = arrowObject.transform;
        _arrow.SetParent(transform, false);

        _renderer = arrowObject.AddComponent<SpriteRenderer>();
        _renderer.sprite = _arrowSprite;
        _renderer.sortingOrder = _sortingOrder;
        _renderer.enabled = false;
    }

    private void LateUpdate()
    {
        if (!TryGetTargets(out Vector3 playerPosition, out Vector3 portalPosition))
        {
            Hide();
            return;
        }

        float deltaTime = Time.deltaTime;
        _visibleTime += deltaTime;
        _bobTime += deltaTime;

        Vector3 targetCenter = playerPosition + (Vector3)_centerOffset;
        if (!_hasAngle)
        {
            // 처음 나타날 때는 바로 제자리에 놓는다.
            _center = targetCenter;
            _centerVelocity = Vector3.zero;
        }
        else
        {
            _center = Vector3.SmoothDamp(_center, targetCenter, ref _centerVelocity, _followSmoothTime,
                Mathf.Infinity, deltaTime);
        }

        Vector2 toPortal = portalPosition - _center;
        float distance = toPortal.magnitude;
        float targetAngle = Mathf.Atan2(toPortal.y, toPortal.x) * Mathf.Rad2Deg;

        if (!_hasAngle)
        {
            _angle = targetAngle;
            _angleVelocity = 0f;
            _alpha = 0f;
            _alphaVelocity = 0f;
            _hasAngle = true;
        }
        else
        {
            _angle = Mathf.SmoothDampAngle(_angle, targetAngle, ref _angleVelocity, _turnSmoothTime,
                Mathf.Infinity, deltaTime);
        }

        _alpha = Mathf.SmoothDamp(_alpha, GetAlpha(distance), ref _alphaVelocity, _alphaSmoothTime,
            Mathf.Infinity, deltaTime);
        if (_alpha <= 0.001f)
        {
            _renderer.enabled = false;
            return;
        }

        // 사인 곡선으로 앞뒤로 둥실 움직인다.
        float bob = _bobAmount * (0.5f - 0.5f * Mathf.Cos(_bobTime * _bobSpeed));
        float radius = _orbitRadius + bob;
        float radian = _angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0f) * radius;

        _arrow.position = new Vector3(_center.x + offset.x, _center.y + offset.y, playerPosition.z);
        _arrow.rotation = Quaternion.Euler(0f, 0f, _angle - _spriteForwardAngle);
        _arrow.localScale = Vector3.one * (_scale * GetAppearScale());

        Color color = _renderer.color;
        color.a = _alpha;
        _renderer.color = color;
        _renderer.enabled = true;
    }

    /// <summary>나타날 때 작게 시작해 살짝 튕기듯 원래 크기로 커진다.</summary>
    private float GetAppearScale()
    {
        if (_fadeInDuration <= 0f)
        {
            return 1f;
        }

        float t = Mathf.Clamp01(_visibleTime / _fadeInDuration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        return Mathf.Lerp(_appearStartScale, 1f, eased);
    }

    private float GetAlpha(float distance)
    {
        float fadeIn = _fadeInDuration > 0f ? Mathf.Clamp01(_visibleTime / _fadeInDuration) : 1f;

        float near = 1f;
        if (_fadeStartDistance > _hideDistance)
        {
            near = Mathf.Clamp01((distance - _hideDistance) / (_fadeStartDistance - _hideDistance));
        }
        else if (distance <= _hideDistance)
        {
            near = 0f;
        }

        return fadeIn * near;
    }

    private bool TryGetTargets(out Vector3 playerPosition, out Vector3 portalPosition)
    {
        playerPosition = Vector3.zero;
        portalPosition = Vector3.zero;

        StageManager stageManager = StageManager.Instance;
        if (stageManager == null || !stageManager.IsPortalOpen)
        {
            _portal = null;
            return false;
        }

        if (_portal == null)
        {
            _portal = FindAnyObjectByType<Portal>();
        }

        if (_player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(PlayerTag);
            _player = playerObject != null ? playerObject.transform : null;
        }

        if (_portal == null || _player == null)
        {
            return false;
        }

        playerPosition = _player.position;
        portalPosition = _portal.transform.position;
        return true;
    }

    private void Hide()
    {
        if (_renderer != null)
        {
            _renderer.enabled = false;
        }

        _hasAngle = false;
        _visibleTime = 0f;
        _bobTime = 0f;
    }
}
