using UnityEngine;

/// <summary>
/// 플레이어 보호막(베리어)을 캐릭터 둘레의 빛나는 구체로 보여준다. (PlayerBarrier 프리팹, 플레이어의 자식)
/// - 보호막이 1개 이상이면 항상 보인다. 남은 개수에 따라 그림(색)이 바뀐다. (기획서 : 1 붉은색, 2 푸른색, 3 노란색, 4 흰색)
/// - 총까지 감싸도록 조준 방향으로 조금 밀려난다.
/// - 평상시 : 안쪽 물결이 천천히 돌고, 구체가 숨 쉬듯 살짝 커졌다 작아진다.
/// - 피격(보호막 1개 소모) : 이전 색의 테두리가 바깥으로 퍼지며 사라지고 빛 파편이 튄다.
///   남은 보호막은 살짝 줄었다가 다시 자리 잡고, 피격 무적 동안 깜빡인다.
/// - 충전(보호막 1개 증가) : 작게 시작해서 커지며 나타난다.
/// Shield.CurrentShield 값의 변화를 보고 연출하므로 Shield 코드에 의존하는 이벤트가 없다.
/// 시간은 게임 시간으로 흐른다. (일시정지하면 멈춘다)
/// </summary>
[DisallowMultipleComponent]
public class PlayerBarrierView : MonoBehaviour
{
    [Header("참조 (비워두면 부모에서 찾는다)")]
    [SerializeField] private Shield _shield;
    [SerializeField] private PlayerHealth _health;
    [SerializeField] private PlayerInputHandler _inputHandler;

    [Header("그림")]
    [SerializeField] private SpriteRenderer _body;
    [SerializeField] private SpriteRenderer _ripple;
    [SerializeField] private SpriteRenderer _breakRing;
    [Tooltip("파편 하나의 모양. 비활성 상태로 두면 실행 시 복제해서 쓴다.")]
    [SerializeField] private SpriteRenderer _shardTemplate;

    [Header("보호막 개수별 모습 (1개, 2개, 3개, 4개 순서)")]
    [Tooltip("구체 그림. 개수가 칸 수보다 많으면 마지막 그림을 쓴다.")]
    [SerializeField] private Sprite[] _stageSprites = new Sprite[4];
    [Tooltip("물결, 깨지는 테두리, 파편에 입히는 색")]
    [SerializeField] private Color[] _stageColors =
    {
        new(1f, 0.28f, 0.22f, 1f),
        new(0.3f, 0.62f, 1f, 1f),
        new(1f, 0.82f, 0.25f, 1f),
        new(0.93f, 0.96f, 1f, 1f)
    };

    [Header("크기와 위치")]
    [Tooltip("구체 반지름 (가로, 세로). 그림은 반지름 1 기준으로 만들어져 있다.")]
    [SerializeField] private Vector2 _radius = new(0.92f, 0.92f);
    [Tooltip("플레이어 기준 구체 중심")]
    [SerializeField] private Vector2 _baseOffset = Vector2.zero;
    [Tooltip("조준 방향으로 밀려나는 거리 (총까지 감싸기 위해)")]
    [SerializeField, Min(0f)] private float _aimOffset = 0.2f;
    [Tooltip("조준 방향을 따라가는 빠르기")]
    [SerializeField, Min(0.1f)] private float _followSpeed = 12f;

    [Header("평상시")]
    [SerializeField, Range(0f, 1f)] private float _bodyAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float _rippleAlpha = 0.5f;
    [Tooltip("물결이 도는 빠르기 (도/초)")]
    [SerializeField] private float _rippleSpeed = 40f;
    [Tooltip("숨쉬기 크기 (비율)")]
    [SerializeField, Range(0f, 0.2f)] private float _pulseAmount = 0.03f;
    [Tooltip("숨쉬기 빠르기 (라디안/초)")]
    [SerializeField, Min(0f)] private float _pulseSpeed = 3f;

    [Header("피격 (보호막 1개 소모)")]
    [Tooltip("이전 색 테두리가 퍼지며 사라지는 시간")]
    [SerializeField, Min(0.01f)] private float _breakDuration = 0.45f;
    [Tooltip("테두리가 퍼지는 정도 (비율)")]
    [SerializeField, Min(0f)] private float _breakGrow = 0.25f;
    [Tooltip("남은 보호막이 줄었다가 다시 자리 잡는 시간")]
    [SerializeField, Min(0.01f)] private float _hitSettleDuration = 0.35f;
    [Tooltip("남은 보호막이 줄어드는 정도 (비율)")]
    [SerializeField, Range(0f, 0.5f)] private float _hitShrink = 0.06f;
    [SerializeField, Min(0)] private int _shardCount = 24;
    [Tooltip("파편이 날아가는 빠르기 (최소, 최대)")]
    [SerializeField] private Vector2 _shardSpeed = new(1.6f, 2.8f);
    [SerializeField, Min(0.01f)] private float _shardLifetime = 0.45f;
    [Tooltip("피격 무적 동안 깜빡일 때의 투명도")]
    [SerializeField, Range(0f, 1f)] private float _blinkAlpha = 0.55f;
    [Tooltip("깜빡이는 간격 (초)")]
    [SerializeField, Min(0.01f)] private float _blinkInterval = 0.05f;
    [Tooltip("PlayerHealth가 없을 때 쓰는 깜빡임 시간 (있으면 피격 무적 시간을 쓴다)")]
    [SerializeField, Min(0f)] private float _fallbackBlinkDuration = 0.5f;

    [Header("충전 (보호막 1개 증가)")]
    [SerializeField, Min(0.01f)] private float _gainDuration = 0.5f;
    [Tooltip("나타나기 시작할 때의 크기 (비율)")]
    [SerializeField, Range(0f, 1f)] private float _gainStartScale = 0.7f;

    private struct Shard
    {
        public SpriteRenderer Renderer;
        public Vector3 Velocity;
        public float Age;
        public Color Color;
    }

    private Shard[] _shards;
    private int _count;
    private Vector3 _offset;
    private float _time;
    private float _breakTimer = -1f;
    private float _settleTimer = -1f;
    private float _blinkTimer = -1f;
    private float _gainTimer = -1f;
    private Color _breakColor;
    private Vector3 _breakCenter;

    private void Awake()
    {
        if (_shield == null) _shield = GetComponentInParent<Shield>();
        if (_health == null) _health = GetComponentInParent<PlayerHealth>();
        if (_inputHandler == null) _inputHandler = GetComponentInParent<PlayerInputHandler>();

        CreateShards();
        _count = _shield != null ? _shield.CurrentShield : 0;
        _offset = _baseOffset;
        SetVisible(_breakRing, false);
        Refresh(0f);
    }

    private void LateUpdate()
    {
        float deltaTime = Time.deltaTime;
        _time += deltaTime;

        int count = _shield != null ? _shield.CurrentShield : 0;
        if (_health != null && _health.IsDead)
        {
            count = 0;
        }

        if (count < _count)
        {
            OnBroken(_count);
        }
        else if (count > _count)
        {
            OnGained();
        }

        _count = count;
        Refresh(deltaTime);
    }

    // ───────────────────────── 상태 변화 ─────────────────────────

    private void OnBroken(int previousCount)
    {
        _breakColor = GetStageColor(previousCount);
        _breakCenter = _body != null ? _body.transform.position : transform.position;
        _breakTimer = 0f;
        _settleTimer = 0f;
        _blinkTimer = 0f;
        _gainTimer = -1f;
        EmitShards(_breakColor);
    }

    private void OnGained()
    {
        _gainTimer = 0f;
    }

    // ───────────────────────── 매 프레임 ─────────────────────────

    private void Refresh(float deltaTime)
    {
        UpdateOffset(deltaTime);
        Vector3 center = transform.parent != null
            ? transform.parent.position + _offset
            : transform.position;

        // 본체와 물결
        bool visible = _count > 0;
        float scale = 1f + Mathf.Sin(_time * _pulseSpeed) * _pulseAmount;
        float alpha = 1f;

        if (_settleTimer >= 0f)
        {
            _settleTimer += deltaTime;
            float t = Mathf.Clamp01(_settleTimer / _hitSettleDuration);
            scale *= 1f - _hitShrink * (1f - t) + 0.02f * Mathf.Sin(t * Mathf.PI);
            if (t >= 1f) _settleTimer = -1f;
        }

        if (_gainTimer >= 0f)
        {
            _gainTimer += deltaTime;
            float t = Mathf.Clamp01(_gainTimer / _gainDuration);
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);
            scale *= Mathf.Lerp(_gainStartScale, 1f, eased);
            alpha *= t;
            if (t >= 1f) _gainTimer = -1f;
        }

        if (_blinkTimer >= 0f)
        {
            _blinkTimer += deltaTime;
            float duration = _health != null ? _health.InvincibilityDuration : _fallbackBlinkDuration;
            if (_blinkTimer >= duration)
            {
                _blinkTimer = -1f;
            }
            else if (Mathf.FloorToInt(_blinkTimer / _blinkInterval) % 2 == 1)
            {
                alpha *= _blinkAlpha;
            }
        }

        Vector3 size = new(_radius.x * scale, _radius.y * scale, 1f);
        Color stageColor = GetStageColor(_count);

        if (_body != null)
        {
            SetVisible(_body, visible);
            if (visible)
            {
                _body.sprite = GetStageSprite(_count);
                _body.transform.SetPositionAndRotation(center, Quaternion.identity);
                _body.transform.localScale = size;
                _body.color = new Color(1f, 1f, 1f, _bodyAlpha * alpha);
            }
        }

        if (_ripple != null)
        {
            SetVisible(_ripple, visible);
            if (visible)
            {
                _ripple.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, 0f, -_time * _rippleSpeed));
                _ripple.transform.localScale = size;
                Color color = stageColor;
                color.a = _rippleAlpha * alpha;
                _ripple.color = color;
            }
        }

        UpdateBreakRing(center, deltaTime);
        UpdateShards(deltaTime);
    }

    private void UpdateOffset(float deltaTime)
    {
        Vector3 target = _baseOffset;
        if (_inputHandler != null && transform.parent != null)
        {
            Vector2 toMouse = _inputHandler.MousePosition - (Vector2)transform.parent.position;
            if (toMouse.sqrMagnitude > 0.0001f)
            {
                target += (Vector3)(toMouse.normalized * _aimOffset);
            }
        }

        _offset = deltaTime > 0f
            ? Vector3.Lerp(_offset, target, 1f - Mathf.Exp(-_followSpeed * deltaTime))
            : target;
    }

    private void UpdateBreakRing(Vector3 center, float deltaTime)
    {
        if (_breakRing == null)
        {
            return;
        }

        if (_breakTimer < 0f)
        {
            SetVisible(_breakRing, false);
            return;
        }

        _breakTimer += deltaTime;
        float t = Mathf.Clamp01(_breakTimer / _breakDuration);
        float grow = 1f + _breakGrow * t;
        SetVisible(_breakRing, true);
        _breakRing.transform.SetPositionAndRotation(center, Quaternion.identity);
        _breakRing.transform.localScale = new Vector3(_radius.x * grow, _radius.y * grow, 1f);
        Color color = Color.Lerp(Color.white, _breakColor, t);
        color.a = Mathf.Pow(1f - t, 1.5f);
        _breakRing.color = color;
        if (t >= 1f)
        {
            _breakTimer = -1f;
            SetVisible(_breakRing, false);
        }
    }

    // ───────────────────────── 파편 ─────────────────────────

    private void CreateShards()
    {
        if (_shardTemplate == null || _shardCount <= 0)
        {
            _shards = System.Array.Empty<Shard>();
            return;
        }

        _shardTemplate.gameObject.SetActive(false);
        _shards = new Shard[_shardCount];
        for (int i = 0; i < _shardCount; i++)
        {
            SpriteRenderer shard = Instantiate(_shardTemplate, _shardTemplate.transform.parent);
            shard.name = $"{_shardTemplate.name} ({i + 1})";
            shard.gameObject.SetActive(false);
            _shards[i] = new Shard { Renderer = shard, Age = -1f };
        }
    }

    private void EmitShards(Color color)
    {
        for (int i = 0; i < _shards.Length; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 direction = new(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            Vector3 edge = new(direction.x * _radius.x, direction.y * _radius.y, 0f);
            Shard shard = _shards[i];
            shard.Age = 0f;
            shard.Velocity = direction * Random.Range(_shardSpeed.x, _shardSpeed.y);
            shard.Color = color;
            shard.Renderer.transform.position = _breakCenter + edge;
            shard.Renderer.gameObject.SetActive(true);
            _shards[i] = shard;
        }
    }

    private void UpdateShards(float deltaTime)
    {
        for (int i = 0; i < _shards.Length; i++)
        {
            Shard shard = _shards[i];
            if (shard.Age < 0f)
            {
                continue;
            }

            shard.Age += deltaTime;
            float t = shard.Age / _shardLifetime;
            if (t >= 1f)
            {
                shard.Age = -1f;
                shard.Renderer.gameObject.SetActive(false);
            }
            else
            {
                shard.Renderer.transform.position += shard.Velocity * deltaTime;
                shard.Velocity *= Mathf.Exp(-3f * deltaTime);
                Color color = shard.Color;
                color.a = 1f - t;
                shard.Renderer.color = color;
            }

            _shards[i] = shard;
        }
    }

    // ───────────────────────── 도움 함수 ─────────────────────────

    private Sprite GetStageSprite(int count)
    {
        if (_stageSprites == null || _stageSprites.Length == 0 || count <= 0)
        {
            return null;
        }

        return _stageSprites[Mathf.Min(count, _stageSprites.Length) - 1];
    }

    private Color GetStageColor(int count)
    {
        if (_stageColors == null || _stageColors.Length == 0 || count <= 0)
        {
            return Color.white;
        }

        return _stageColors[Mathf.Min(count, _stageColors.Length) - 1];
    }

    private static void SetVisible(SpriteRenderer renderer, bool visible)
    {
        if (renderer != null && renderer.enabled != visible)
        {
            renderer.enabled = visible;
        }
    }
}
