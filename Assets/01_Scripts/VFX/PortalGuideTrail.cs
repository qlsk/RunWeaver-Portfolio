using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 포탈이 열려 있는 동안 플레이어 곁에서 빛 무리(반딧불)가 피어올라 포탈 쪽으로 흘러가며 길을 알려준다.
/// - 포탈이 멀면 정해진 길이만큼 날아가다 느려지며 흩어져 사라진다.
/// - 포탈이 가까우면 포탈까지 이어지고, 끝에서 모여 포탈로 빨려 들어간다.
/// - 빛 하나하나는 경로를 반 박자 늦게 따라가므로, 플레이어가 움직이면 무리가 출렁이며 따라온다.
/// 그림은 자식의 빛 견본(PortalGuideFly)을 복제해 쓰고, 다 쓴 빛은 꺼 두었다가 다시 쓴다.
/// 게임 시간(Time.deltaTime)으로 움직이므로 일시정지 중에는 멈춘다.
/// </summary>
[DisallowMultipleComponent]
public class PortalGuideTrail : MonoBehaviour
{
    private const string PlayerTag = "Player";

    [Header("그림")]
    [Tooltip("복제해서 쓸 빛 한 마리 (꺼 둔 자식)")]
    [SerializeField] private PortalGuideFly _flyTemplate;
    [Tooltip("빛이 포탈에 닿을 때 포탈 쪽에 은은하게 모이는 빛 (없어도 된다)")]
    [SerializeField] private SpriteRenderer _arriveGlow;

    [Header("출발점과 도착점")]
    [Tooltip("플레이어 위치에서 빛이 나오는 높이까지의 거리")]
    [SerializeField] private Vector2 _startOffset = new(0f, 0.1f);
    [Tooltip("출발점을 포탈 쪽으로 이만큼 내민다 (빛이 몸을 덮지 않게)")]
    [SerializeField, Min(0f)] private float _startForward = 0.5f;
    [Tooltip("포탈 위치에서 빛이 빨려 들어갈 지점까지의 거리")]
    [SerializeField] private Vector2 _portalOffset = Vector2.zero;

    [Header("흐름")]
    [Tooltip("포탈이 멀 때 빛이 날아가는 최대 거리")]
    [SerializeField, Min(0.1f)] private float _maxLength = 5f;
    [Tooltip("빛이 나아가는 기본 속도 (월드 단위/초)")]
    [SerializeField, Min(0f)] private float _speed = 2.4f;
    [Tooltip("빛마다 다른 속도 배율 (최소, 최대)")]
    [SerializeField] private Vector2 _speedRandom = new(0.75f, 1.2f);
    [Tooltip("출발할 때의 속도 비율. 이 비율에서 시작해 점점 빨라진다")]
    [SerializeField, Range(0f, 1f)] private float _launchSpeedRatio = 0.35f;
    [Tooltip("출발해서 제 속도가 붙기까지 걸리는 시간")]
    [SerializeField, Min(0.01f)] private float _launchDuration = 0.6f;
    [Tooltip("포탈이 멀 때, 끝으로 갈수록 느려지는 정도")]
    [SerializeField, Range(0f, 1f)] private float _endSlowdown = 0.55f;

    [Header("굽이 (경로가 천천히 출렁임)")]
    [SerializeField, Min(0f)] private float _curveAmount = 0.5f;
    [SerializeField, Min(0f)] private float _curveSpeed = 0.45f;
    [SerializeField, Min(0f)] private float _curveAmount2 = 0.25f;
    [SerializeField, Min(0f)] private float _curveSpeed2 = 1.1f;
    [Tooltip("방향이 바뀔 때 굽이가 따라가는 빠르기 (작을수록 천천히 휜다)")]
    [SerializeField, Min(0.01f)] private float _curveFollow = 1.8f;

    [Header("무리")]
    [Tooltip("한 번에 피어오르는 빛 개수 (최소, 최대)")]
    [SerializeField] private Vector2Int _burstCount = new(2, 5);
    [Tooltip("다음 무리가 피어오르기까지의 시간 (최소, 최대)")]
    [SerializeField] private Vector2 _burstInterval = new(0.12f, 0.38f);
    [Tooltip("흐름의 폭 (빛들이 좌우로 퍼진 정도)")]
    [SerializeField, Min(0f)] private float _laneWidth = 0.13f;
    [Tooltip("빛이 떠다니며 흔들리는 폭 (최소, 최대)")]
    [SerializeField] private Vector2 _wobbleAmount = new(0.04f, 0.1f);
    [Tooltip("흔들리는 빠르기 (최소, 최대)")]
    [SerializeField] private Vector2 _wobbleSpeed = new(0.5f, 1.6f);
    [Tooltip("경로를 따라붙는 민첩함 (최소, 최대). 작을수록 굼뜨게 따라온다")]
    [SerializeField] private Vector2 _followRate = new(4f, 7f);
    [Tooltip("빛 크기 배율 (최소, 최대)")]
    [SerializeField] private Vector2 _sizeRandom = new(0.55f, 1.2f);
    [Tooltip("포탈이 멀 때, 끝으로 갈수록 넓게 흩어지는 정도")]
    [SerializeField, Min(0f)] private float _endSpread = 1.6f;
    [Tooltip("날아가면서 살짝 떠오르는 높이")]
    [SerializeField] private float _rise = 0.08f;

    [Header("깜빡임")]
    [Tooltip("깜빡이는 빠르기 (최소, 최대)")]
    [SerializeField] private Vector2 _blinkSpeed = new(1.2f, 2.6f);
    [Tooltip("가장 어두울 때의 밝기")]
    [SerializeField, Range(0f, 1f)] private float _blinkMin = 0.55f;

    [Header("꼬리")]
    [Tooltip("꼬리가 남는 시간 (속도 x 시간 = 꼬리 길이)")]
    [SerializeField, Min(0f)] private float _tailDuration = 0.08f;
    [SerializeField, Min(0f)] private float _tailMaxLength = 0.35f;

    [Header("나타남과 사라짐")]
    [Tooltip("빛 하나가 피어오르며 밝아지는 시간")]
    [SerializeField, Min(0.01f)] private float _fadeInDuration = 0.45f;
    [Tooltip("포탈이 멀 때, 끝에서 이 거리 동안 서서히 사라진다")]
    [SerializeField, Min(0.01f)] private float _fadeOutLength = 1.8f;
    [Tooltip("포탈에 닿을 때, 끝에서 이 거리 동안 사라진다")]
    [SerializeField, Min(0.01f)] private float _arriveFadeLength = 0.4f;
    [Tooltip("포탈에 닿을 때, 끝에서 이 거리 동안 한데 모인다")]
    [SerializeField, Min(0.01f)] private float _arriveGatherLength = 0.9f;
    [Tooltip("포탈 문을 닫았거나 플레이어가 포탈에 들어갔을 때 남은 빛이 사라지는 시간")]
    [SerializeField, Min(0.01f)] private float _stopFadeDuration = 0.4f;
    [Tooltip("플레이어가 포탈과 이 거리보다 가까우면 새 빛을 내보내지 않는다")]
    [SerializeField, Min(0f)] private float _hideDistance = 1.2f;

    [Header("포탈 도착 빛")]
    [SerializeField, Min(0f)] private float _arriveGlowPulse = 0.3f;
    [SerializeField, Min(0f)] private float _arriveGlowPulseSpeed = 4f;
    [Tooltip("도착 빛이 켜지고 꺼지는 시간")]
    [SerializeField, Min(0.01f)] private float _arriveGlowFadeTime = 0.3f;

    private sealed class Fly
    {
        public PortalGuideFly View;
        public float Distance;
        public float Age;
        public float SpeedMultiplier;
        public float Side;
        public float Size;
        public float BlinkPhase;
        public float BlinkSpeed;
        public float Follow;
        public Vector3 WobbleSpeed;
        public Vector3 WobblePhase;
        public Vector3 WobbleAmount;
        public Vector2 Position;
    }

    private readonly List<Fly> _flies = new();
    private readonly Stack<PortalGuideFly> _pool = new();

    private Transform _player;
    private Portal _portal;

    private Vector2 _start;
    private Vector2 _end;
    private Vector2 _control;
    private bool _hasPath;
    private float _pathLength;
    private float _reach;
    private bool _arrive;
    private float _time;
    private float _nextBurstTime;
    private float _stopFade = 1f;
    private float _arriveGlowAlpha;
    private Color _arriveGlowColor = Color.white;
    private Vector3 _arriveGlowScale = Vector3.one;
    private float _z;

    private void Awake()
    {
        if (_flyTemplate != null)
        {
            _flyTemplate.gameObject.SetActive(false);
        }

        if (_arriveGlow != null)
        {
            _arriveGlowColor = _arriveGlow.color;
            _arriveGlowScale = _arriveGlow.transform.localScale;
            _arriveGlow.enabled = false;
        }
    }

    private void LateUpdate()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
        {
            return;
        }

        _time += deltaTime;

        bool hasTargets = TryGetTargets(out Vector3 playerPosition, out Vector3 portalPosition);
        bool emitting = false;
        if (hasTargets)
        {
            _z = playerPosition.z;
            UpdatePath(playerPosition, portalPosition, deltaTime);
            emitting = Vector2.Distance(playerPosition, portalPosition) > _hideDistance;
        }

        // 내보내는 중이면 다시 밝게, 멈췄으면 남은 빛을 서서히 지운다.
        _stopFade = emitting
            ? 1f
            : Mathf.MoveTowards(_stopFade, 0f, deltaTime / _stopFadeDuration);

        if (emitting && _time >= _nextBurstTime)
        {
            EmitBurst();
        }

        if (_hasPath)
        {
            UpdateFlies(deltaTime);
        }

        if (!emitting && _stopFade <= 0f)
        {
            ReleaseAll();
            if (!hasTargets)
            {
                _hasPath = false;
            }
        }

        UpdateArriveGlow(emitting && _arrive, deltaTime);
    }

    private void UpdatePath(Vector3 playerPosition, Vector3 portalPosition, float deltaTime)
    {
        Vector2 start = (Vector2)playerPosition + _startOffset;
        Vector2 end = (Vector2)portalPosition + _portalOffset;
        Vector2 delta = end - start;
        float length = delta.magnitude;
        if (length < 0.001f)
        {
            return;
        }

        Vector2 direction = delta / length;
        Vector2 normal = new(-direction.y, direction.x);

        // 굽이: 느리게 바뀌는 두 물결을 겹친다. 경로가 짧으면 굽이도 줄인다.
        float curve = _curveAmount * Mathf.Sin(_time * _curveSpeed)
                      + _curveAmount2 * Mathf.Sin(_time * _curveSpeed2 + 1f);
        Vector2 wantControl = (start + end) * 0.5f + normal * (curve * Mathf.Min(1f, length / 4f));

        if (!_hasPath)
        {
            _control = wantControl;
            _hasPath = true;
        }
        else
        {
            // 방향이 바뀌어도 휙 꺾이지 않고 천천히 휘어 따라간다.
            _control += (wantControl - _control) * (1f - Mathf.Exp(-deltaTime * _curveFollow));
        }

        _start = start + direction * _startForward;
        _end = end;
        _pathLength = Mathf.Max(0.001f, Vector2.Distance(_start, _end));
        _arrive = _pathLength <= _maxLength;
        _reach = Mathf.Min(_pathLength, _maxLength);
    }

    private void EmitBurst()
    {
        int count = Random.Range(Mathf.Min(_burstCount.x, _burstCount.y), Mathf.Max(_burstCount.x, _burstCount.y) + 1);
        for (int i = 0; i < count; i++)
        {
            Spawn();
        }

        _nextBurstTime = _time + RandomRange(_burstInterval);
    }

    private void Spawn()
    {
        PortalGuideFly view = Take();
        if (view == null)
        {
            return;
        }

        Fly fly = new()
        {
            View = view,
            SpeedMultiplier = RandomRange(_speedRandom),
            Side = Gaussian() * _laneWidth,
            Size = RandomRange(_sizeRandom),
            BlinkPhase = Random.Range(0f, Mathf.PI * 2f),
            BlinkSpeed = RandomRange(_blinkSpeed),
            Follow = RandomRange(_followRate),
            WobbleSpeed = new Vector3(RandomRange(_wobbleSpeed), RandomRange(_wobbleSpeed), RandomRange(_wobbleSpeed)),
            WobblePhase = new Vector3(Random.Range(0f, 6.28f), Random.Range(0f, 6.28f), Random.Range(0f, 6.28f)),
            WobbleAmount = new Vector3(RandomRange(_wobbleAmount), RandomRange(_wobbleAmount), RandomRange(_wobbleAmount)),
            Position = _start + new Vector2(Gaussian(), Gaussian()) * 0.12f
        };

        view.transform.localScale = Vector3.one * fly.Size;
        view.transform.position = new Vector3(fly.Position.x, fly.Position.y, _z);
        view.SetAlpha(0f);
        view.gameObject.SetActive(true);
        _flies.Add(fly);
    }

    private void UpdateFlies(float deltaTime)
    {
        for (int i = _flies.Count - 1; i >= 0; i--)
        {
            Fly fly = _flies[i];
            fly.Age += deltaTime;

            float progress = _reach > 0f ? fly.Distance / _reach : 1f;

            // 출발할 땐 천천히 떠오르다 속도가 붙고, 포탈이 멀면 끝에서 느려진다.
            float launch = Mathf.Lerp(_launchSpeedRatio, 1f, SmoothStep(fly.Age / _launchDuration));
            float slow = _arrive ? 1f : 1f - _endSlowdown * SmoothStep((progress - 0.6f) / 0.4f);
            fly.Distance += _speed * fly.SpeedMultiplier * launch * slow * deltaTime;

            if (fly.Distance >= _reach)
            {
                Release(i);
                continue;
            }

            float t = fly.Distance / _pathLength;
            Vector2 point = Bezier(t);
            Vector2 tangent = BezierTangent(t);
            Vector2 normal = new(-tangent.y, tangent.x);

            // 여러 주기의 흔들림을 겹쳐, 같은 물결이 반복되지 않게 한다.
            float age = fly.Age;
            float drift = fly.WobbleAmount.x * Mathf.Sin(fly.WobbleSpeed.x * age * 2.2f + fly.WobblePhase.x)
                          + fly.WobbleAmount.y * Mathf.Sin(fly.WobbleSpeed.y * age * 2.2f + fly.WobblePhase.y)
                          + fly.WobbleAmount.z * Mathf.Sin(fly.WobbleSpeed.z * age * 2.2f + fly.WobblePhase.z);
            float along = (fly.WobbleAmount.x * Mathf.Cos(fly.WobbleSpeed.x * age * 1.7f + fly.WobblePhase.x * 1.3f)
                           + fly.WobbleAmount.y * Mathf.Cos(fly.WobbleSpeed.y * age * 1.7f + fly.WobblePhase.y * 1.3f)
                           + fly.WobbleAmount.z * Mathf.Cos(fly.WobbleSpeed.z * age * 1.7f + fly.WobblePhase.z * 1.3f))
                          * 0.6f;

            float side = fly.Side;
            if (_arrive)
            {
                // 포탈에 가까워지면 한데 모여 빨려 들어간다.
                float gather = SmoothStep((_pathLength - fly.Distance) / _arriveGatherLength);
                side *= gather;
                drift *= gather;
                along *= gather;
            }
            else
            {
                // 끝으로 갈수록 넓게 흩어진다.
                side *= 1f + _endSpread * SmoothStep((progress - 0.55f) / 0.45f);
            }

            Vector2 ideal = point + normal * (side + drift) + tangent * along
                            + Vector2.up * (_rise * SmoothStep(progress));

            // 실제 위치는 이상 위치를 살짝 늦게 따라간다.
            Vector2 previous = fly.Position;
            fly.Position += (ideal - fly.Position) * (1f - Mathf.Exp(-deltaTime * fly.Follow));
            Vector2 velocity = (fly.Position - previous) / deltaTime;

            float fadeIn = SmoothStep(fly.Age / _fadeInDuration);
            float fadeOut = SmoothStep((_reach - fly.Distance) / (_arrive ? _arriveFadeLength : _fadeOutLength));
            float wave = 0.5f + 0.5f * Mathf.Sin(fly.BlinkPhase + fly.Age * fly.BlinkSpeed * Mathf.PI);
            float blink = Mathf.Lerp(_blinkMin, 1f, wave * wave);

            Transform flyTransform = fly.View.transform;
            flyTransform.position = new Vector3(fly.Position.x, fly.Position.y, _z);
            fly.View.SetAlpha(fadeIn * fadeOut * blink * _stopFade);
            fly.View.SetTail(velocity, _tailDuration, _tailMaxLength);
        }
    }

    private void UpdateArriveGlow(bool show, float deltaTime)
    {
        if (_arriveGlow == null)
        {
            return;
        }

        _arriveGlowAlpha = Mathf.MoveTowards(_arriveGlowAlpha, show ? 1f : 0f, deltaTime / _arriveGlowFadeTime);
        if (_arriveGlowAlpha <= 0f || !_hasPath)
        {
            _arriveGlow.enabled = false;
            return;
        }

        float pulse = 1f + _arriveGlowPulse * Mathf.Sin(_time * _arriveGlowPulseSpeed);
        _arriveGlow.enabled = true;
        _arriveGlow.transform.position = new Vector3(_end.x, _end.y, _z);
        _arriveGlow.transform.localScale = _arriveGlowScale * (0.9f + 0.1f * pulse);
        Color color = _arriveGlowColor;
        color.a *= _arriveGlowAlpha * pulse;
        _arriveGlow.color = color;
    }

    private Vector2 Bezier(float t)
    {
        float u = 1f - t;
        return u * u * _start + 2f * u * t * _control + t * t * _end;
    }

    private Vector2 BezierTangent(float t)
    {
        Vector2 tangent = 2f * (1f - t) * (_control - _start) + 2f * t * (_end - _control);
        float length = tangent.magnitude;
        return length > 0.0001f ? tangent / length : Vector2.right;
    }

    private PortalGuideFly Take()
    {
        while (_pool.Count > 0)
        {
            PortalGuideFly pooled = _pool.Pop();
            if (pooled != null)
            {
                return pooled;
            }
        }

        if (_flyTemplate == null)
        {
            return null;
        }

        PortalGuideFly created = Instantiate(_flyTemplate, transform);
        created.name = _flyTemplate.name;
        return created;
    }

    private void Release(int index)
    {
        PortalGuideFly view = _flies[index].View;
        _flies.RemoveAt(index);
        if (view != null)
        {
            view.gameObject.SetActive(false);
            _pool.Push(view);
        }
    }

    private void ReleaseAll()
    {
        for (int i = _flies.Count - 1; i >= 0; i--)
        {
            Release(i);
        }
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

        if (_portal == null || _player == null || !_player.gameObject.activeInHierarchy)
        {
            return false;
        }

        playerPosition = _player.position;
        portalPosition = _portal.transform.position;
        return true;
    }

    private static float SmoothStep(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static float RandomRange(Vector2 range)
    {
        return Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
    }

    private static float Gaussian()
    {
        float u1 = Mathf.Max(0.0001f, Random.value);
        float u2 = Random.value;
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
    }
}
