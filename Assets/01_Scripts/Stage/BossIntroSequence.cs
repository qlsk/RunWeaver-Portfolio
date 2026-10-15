using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 등장 연출. StageManager가 보스전을 시작할 때 이 프리팹을 띄우고 Play를 부른다.
/// 1. 화면 위아래에 검은 띠가 들어오고 카메라가 착지 지점으로 옮겨간다.
/// 2. 착지 지점에 보스의 근접 범위 공격 원(TankRange)이 깔리고 충전된다. 가운데 그림자가 점점 커진다.
/// 3. 보스가 팔을 든 자세로 공중에서 떨어진다. 닿기 직전 내리누르는 바람에 흙먼지가 먼저 퍼진다.
/// 4. 내려찍는 순간 연출이 잠깐 멈추고(히트스톱) 화면이 번쩍인 뒤, 원이 터지며 충격파 고리가 퍼지고
///    석화 가시가 안쪽부터 번져 솟고, 바위 기둥이 솟았다 가라앉고, 돌 파편이 튄다. 화면이 확대되며 흔들린다.
/// 5. 내려찍기 동작이 끝나면 진짜 보스를 만든다. 연출이 끝날 때까지 보스는 공격하지 않고 제자리에 있는다.
/// 6. 이름 띠를 보여 준 뒤 카메라가 플레이어에게 돌아가고 띠가 걷히면 보스전이 시작된다.
///
/// 그림은 모두 프리팹에 넣어 둔 이미지를 쓴다. (떨어지는 보스 = BossAttack4~7, 원 = TankRange, 가시 = ReactionFx_Petrify,
/// 바위 = BossStone, 흙먼지·그림자 = MissionFx_SoftGlow, 파편 = MissionFx_AltarCrackShards, 충격파 = TraitFx_Shockwave)
/// 피해나 석화는 걸지 않는 보여주기용이다. 연출 동안 플레이어 입력을 막는다.
/// 게임 시간으로 흐르므로 일시정지 중에는 멈추고, 그동안 화면 띠와 이름도 숨긴다.
/// </summary>
[DisallowMultipleComponent]
public class BossIntroSequence : MonoBehaviour
{
    private const float LetterboxDuration = 0.4f;

    [Header("월드 그림")]
    [Tooltip("월드 연출의 부모. 재생할 때 착지 지점으로 옮긴다")]
    [SerializeField] private Transform _worldRoot;
    [Tooltip("보스의 근접 범위 공격 원 (TankRange)")]
    [SerializeField] private SpriteRenderer _range;
    [Tooltip("떨어지는 보스의 그림자")]
    [SerializeField] private SpriteRenderer _shadow;
    [Tooltip("떨어지고 내려찍는 보스 그림")]
    [SerializeField] private SpriteRenderer _faller;
    [Tooltip("떨어지는 동안의 자세 (팔을 든 BossAttack4)")]
    [SerializeField] private Sprite _fallSprite;
    [Tooltip("내려찍기 동작 (보스 공격 클립과 같은 BossAttack5~7)")]
    [SerializeField] private Sprite[] _slamFrames;
    [Tooltip("원이 터질 때 퍼지는 충격파 고리")]
    [SerializeField] private SpriteRenderer _shockRing;
    [Tooltip("흙먼지 견본 (꺼 둔다)")]
    [SerializeField] private SpriteRenderer _dustTemplate;
    [Tooltip("돌 파편 견본 (꺼 둔다)")]
    [SerializeField] private SpriteRenderer _shardTemplate;
    [Tooltip("돌 파편 그림 (하나씩 무작위로 쓴다)")]
    [SerializeField] private Sprite[] _shardSprites;
    [Tooltip("석화 가시 (석화 반응과 같은 ReactionFx_Petrify)")]
    [SerializeField] private GameObject _spikePrefab;
    [Tooltip("솟아오르는 바위 기둥 (보스 낙석 공격과 같은 BossStone)")]
    [SerializeField] private Sprite _stoneSprite;

    [Header("범위 원 (보스 근접 범위 공격과 같은 값)")]
    [SerializeField, Min(0.1f)] private float _rangeRadius = 7f;
    [SerializeField, Range(0.1f, 1f)] private float _rangeHeightRatio = 0.6f;
    [SerializeField] private Color _rangeColor = new(1f, 1f, 1f, 0.35f);
    [SerializeField, Range(0f, 1f)] private float _rangeChargedAlpha = 0.9f;
    [Tooltip("원이 터지며 사라지는 시간")]
    [SerializeField, Min(0.01f)] private float _rangeBurstDuration = 0.45f;
    [Tooltip("원이 터지며 커지는 비율")]
    [SerializeField, Min(0f)] private float _rangeBurstGrow = 0.08f;

    [Header("시간")]
    [Tooltip("카메라가 착지 지점으로 옮겨가는 시간. 끝나면 원이 깔린다")]
    [SerializeField, Min(0f)] private float _panDuration = 0.8f;
    [Tooltip("원이 충전되는 시간. 끝나는 순간 보스가 땅에 닿는다")]
    [SerializeField, Min(0.1f)] private float _chargeDuration = 1.2f;
    [Tooltip("보스가 떨어지는 시간 (충전 마지막 구간)")]
    [SerializeField, Min(0.05f)] private float _fallDuration = 0.35f;
    [Tooltip("보스가 떨어지기 시작하는 높이 (월드 단위)")]
    [SerializeField, Min(0f)] private float _fallHeight = 9.5f;
    [Tooltip("그림자가 나타나는 시점 (땅에 닿기 몇 초 전)")]
    [SerializeField, Min(0f)] private float _shadowLead = 0.9f;
    [Tooltip("내려찍기 동작 한 프레임 시간")]
    [SerializeField, Min(0.01f)] private float _slamFrameTime = 0.0833f;
    [Tooltip("내려찍기 동작 중 땅을 치는 프레임 (0부터)")]
    [SerializeField, Min(0)] private int _impactFrame = 1;
    [Tooltip("땅을 치는 순간 연출이 멈추는 시간")]
    [SerializeField, Min(0f)] private float _hitStopDuration = 0.09f;
    [Tooltip("땅을 친 뒤 이름 띠가 나올 때까지")]
    [SerializeField, Min(0f)] private float _bannerDelay = 0.4f;
    [Tooltip("이름 띠가 떠 있는 시간. 끝나면 카메라가 돌아간다")]
    [SerializeField, Min(0.1f)] private float _bannerHold = 1.5f;
    [Tooltip("카메라가 플레이어에게 돌아가는 시간. 끝나면 보스전이 시작된다")]
    [SerializeField, Min(0f)] private float _returnDuration = 0.8f;

    [Header("카메라")]
    [Tooltip("착지 지점에서 카메라가 비출 곳까지의 거리")]
    [SerializeField] private Vector2 _focusOffset = new(0f, -0.5f);
    [Tooltip("카메라가 옮겨가는 부드러움 (클수록 빠르다)")]
    [SerializeField, Min(0.1f)] private float _cameraSmoothSpeed = 4f;
    [Tooltip("충전 동안 땅이 울리는 흔들림 (월드 단위, 충전이 끝날수록 커진다)")]
    [SerializeField, Min(0f)] private float _chargeShake = 0.04f;
    [Tooltip("땅을 친 순간의 흔들림 (월드 단위)")]
    [SerializeField, Min(0f)] private float _impactShake = 0.35f;
    [Tooltip("흔들림이 잦아드는 시간")]
    [SerializeField, Min(0.01f)] private float _impactShakeDuration = 0.4f;
    [Tooltip("땅을 친 순간 화면이 확대되는 정도 (0.08이면 8%)")]
    [SerializeField, Range(0f, 0.3f)] private float _zoomPunch = 0.08f;
    [SerializeField, Min(0.01f)] private float _zoomPunchDuration = 0.45f;

    [Header("그림자")]
    [Tooltip("착지 지점에서 보스 발밑까지의 거리")]
    [SerializeField] private Vector2 _feetOffset = new(0f, -1.75f);
    [Tooltip("그림자 크기 (처음, 땅에 닿을 때)")]
    [SerializeField] private Vector2 _shadowScale = new(0.8f, 3.2f);
    [Tooltip("그림자 진하기 (처음, 땅에 닿을 때)")]
    [SerializeField] private Vector2 _shadowAlpha = new(0.15f, 0.5f);

    [Header("흙먼지 (땅에 닿기 직전 먼저 퍼진다)")]
    [Tooltip("땅에 닿기 몇 초 전에 퍼지기 시작하는지")]
    [SerializeField, Min(0f)] private float _dustLead = 0.25f;
    [SerializeField, Min(0)] private int _dustCount = 14;
    [Tooltip("발밑에서 퍼지는 거리 (시작, 끝)")]
    [SerializeField] private Vector2 _dustDistance = new(0.5f, 3.6f);
    [Tooltip("세로로 납작한 정도")]
    [SerializeField, Range(0.1f, 1f)] private float _dustFlatten = 0.4f;
    [Tooltip("흙먼지 한 덩이 크기 (시작, 끝)")]
    [SerializeField] private Vector2 _dustScale = new(1.2f, 2.6f);
    [SerializeField, Min(0.05f)] private float _dustLife = 0.75f;

    [Header("충격파 고리")]
    [Tooltip("원 반지름 대비 크기 (시작, 끝)")]
    [SerializeField] private Vector2 _shockRingSize = new(0.2f, 1.25f);
    [SerializeField, Min(0.05f)] private float _shockRingDuration = 0.5f;

    [Header("석화 가시")]
    [Tooltip("가시를 놓을 고리들. x = 원 반지름 대비 거리, y = 개수, z = 각도 어긋남 (0~1)")]
    [SerializeField] private Vector3[] _spikeRings = { new(0.5f, 5f, 0.1f), new(0.86f, 9f, 0.55f) };
    [Tooltip("가시 크기 (안쪽, 바깥쪽)")]
    [SerializeField] private Vector2 _spikeScale = new(1.6f, 1.3f);
    [Tooltip("안쪽에서 바깥쪽 끝까지 번지는 시간")]
    [SerializeField, Min(0f)] private float _spikeSpreadTime = 0.2f;
    [SerializeField, Min(0.1f)] private float _spikeLifetime = 1.2f;
    [SerializeField] private int _spikeSortingOrder = -10;

    [Header("바위 기둥")]
    [Tooltip("기둥 위치. 원 반지름 대비 (x, y)")]
    [SerializeField] private Vector2[] _stonePoints = { new(-0.55f, -0.5f), new(0.6f, -0.42f), new(-0.72f, 0.35f), new(0.5f, 0.55f) };
    [Tooltip("기둥 높이 (월드 단위, 번갈아 쓴다)")]
    [SerializeField] private Vector2 _stoneHeight = new(2.6f, 3.1f);
    [Tooltip("기둥마다 늦게 솟는 시간")]
    [SerializeField, Min(0f)] private float _stoneStagger = 0.05f;
    [SerializeField, Min(0.01f)] private float _stoneRise = 0.12f;
    [SerializeField, Min(0f)] private float _stoneHold = 0.6f;
    [SerializeField, Min(0.01f)] private float _stoneSink = 0.3f;

    [Header("돌 파편")]
    [SerializeField, Min(0)] private int _shardCount = 18;
    [Tooltip("옆으로 튀는 속도 (최소, 최대)")]
    [SerializeField] private Vector2 _shardSpeed = new(3f, 6.5f);
    [Tooltip("위로 튀는 속도 (최소, 최대)")]
    [SerializeField] private Vector2 _shardLift = new(4f, 7f);
    [SerializeField, Min(0f)] private float _shardGravity = 16f;
    [SerializeField, Min(0.05f)] private float _shardLife = 0.9f;

    [Header("화면")]
    [Tooltip("화면 연출 전체 (일시정지 중에는 숨긴다)")]
    [SerializeField] private CanvasGroup _screen;
    [SerializeField] private RectTransform _letterboxTop;
    [SerializeField] private RectTransform _letterboxBottom;
    [Tooltip("검은 띠 높이 (1080 기준 픽셀)")]
    [SerializeField, Min(0f)] private float _letterboxHeight = 108f;
    [Tooltip("땅을 치는 순간의 섬광")]
    [SerializeField] private Image _flash;
    [SerializeField, Range(0f, 1f)] private float _flashAlpha = 0.55f;
    [SerializeField, Min(0.01f)] private float _flashFade = 0.2f;
    [Tooltip("이름 띠")]
    [SerializeField] private CanvasGroup _banner;
    [Tooltip("이름 띠 위아래 선 (가운데에서 양옆으로 펼쳐진다)")]
    [SerializeField] private RectTransform[] _bannerLines;
    [SerializeField] private TMP_Text _bossNameText;
    [SerializeField] private string _bossName = "이끼 바위 거상";
    [SerializeField, Min(0.01f)] private float _bannerFade = 0.35f;

    private sealed class Puff
    {
        public SpriteRenderer Renderer;
        public Vector2 Direction;
        public float Age;
    }

    private sealed class Shard
    {
        public SpriteRenderer Renderer;
        public Vector2 Velocity;
        public float Lift;
        public float Spin;
        public float Age;
    }

    private sealed class Stone
    {
        public RockEruptionVisual Visual;
        public float Delay;
    }

    private sealed class Spike
    {
        public GameObject Instance;
        public float Delay;
        public bool Started;
    }

    private readonly List<Puff> _puffs = new();
    private readonly List<Shard> _shards = new();
    private readonly List<Stone> _stones = new();
    private readonly List<Spike> _spikes = new();

    private Func<GameObject> _spawnBoss;
    private Action _onFinished;
    private AttackRangeChargeVisual _rangeVisual;
    private CameraFollow _cameraFollow;
    private FailureCameraFx _cameraFx;
    private BossPatternController _bossPattern;
    private EnemyMotor _bossMotor;
    private Vector3 _landing;
    private Vector3 _rangeBaseScale;
    private Color _shadowColor = Color.black;
    private Color _dustColor = Color.white;
    private Color _shockColor = Color.white;
    private Color _flashColor = Color.white;
    private bool _isPlaying;
    private bool _isInputLocked;
    private bool _hasImpacted;
    private bool _hasSpawnedBoss;
    private bool _hasSpawnedDust;
    private bool _hasReturned;
    private float _time;
    private float _hitStopRemaining;
    private float _impactAge = -1f;

    private float LandTime => _panDuration + _chargeDuration;
    private float ImpactTime => LandTime + _slamFrameTime * _impactFrame;
    private float SlamEndTime => LandTime + _slamFrameTime * Mathf.Max(1, _slamFrames != null ? _slamFrames.Length : 1);
    private float BannerStart => ImpactTime + _bannerDelay;
    private float ReturnStart => BannerStart + _bannerHold;
    private float EndTime => ReturnStart + _returnDuration;

    private void Awake()
    {
        if (_shadow != null)
        {
            _shadowColor = _shadow.color;
        }

        if (_dustTemplate != null)
        {
            _dustColor = _dustTemplate.color;
            _dustTemplate.gameObject.SetActive(false);
        }

        if (_shardTemplate != null)
        {
            _shardTemplate.gameObject.SetActive(false);
        }

        if (_shockRing != null)
        {
            _shockColor = _shockRing.color;
        }

        if (_flash != null)
        {
            _flashColor = _flash.color;
        }

        if (_range != null)
        {
            _rangeBaseScale = _range.transform.localScale;
        }

        HideAll();
    }

    /// <summary>
    /// 연출을 시작한다. 내려찍기 동작이 끝나면 spawnBoss로 진짜 보스를 만들고,
    /// 연출이 끝나면 보스를 풀어 주고 onFinished를 부른 뒤 스스로 사라진다.
    /// </summary>
    public void Play(Vector3 landing, Func<GameObject> spawnBoss, Action onFinished = null)
    {
        if (_isPlaying)
        {
            return;
        }

        _isPlaying = true;
        _landing = landing;
        _spawnBoss = spawnBoss;
        _onFinished = onFinished;
        _time = 0f;

        if (_worldRoot != null)
        {
            _worldRoot.position = new Vector3(landing.x, landing.y, _worldRoot.position.z);
        }

        if (_bossNameText != null)
        {
            _bossNameText.text = _bossName;
        }

        PlayerInputHandler.LockForCutscene();
        _isInputLocked = true;

        _cameraFollow = FindAnyObjectByType<CameraFollow>();
        if (_cameraFollow != null)
        {
            _cameraFollow.SetFocusOverride(landing + (Vector3)_focusOffset, _cameraSmoothSpeed);
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            _cameraFx = camera.GetComponent<FailureCameraFx>();
            if (_cameraFx == null)
            {
                _cameraFx = camera.gameObject.AddComponent<FailureCameraFx>();
            }
        }

        if (_range != null)
        {
            SetRangeSize(1f);
            _rangeVisual = new AttackRangeChargeVisual(_range, true);
        }

        Evaluate();
    }

    private void Update()
    {
        if (!_isPlaying)
        {
            return;
        }

        // 런이 끝나면(탐사 포기 등) 바로 정리한다.
        if (RunFlow.HasEnded)
        {
            Finish();
            return;
        }

        bool isPaused = PauseController.IsGamePaused;
        if (_screen != null)
        {
            _screen.alpha = isPaused ? 0f : 1f;
        }

        float deltaTime = Time.deltaTime;
        if (_hitStopRemaining > 0f)
        {
            _hitStopRemaining -= deltaTime;
            if (_hitStopRemaining <= 0f)
            {
                Impact();
            }
        }
        else
        {
            float previous = _time;
            _time += deltaTime;
            if (!_hasImpacted && previous < ImpactTime && _time >= ImpactTime)
            {
                _time = ImpactTime;
                if (_hitStopDuration > 0f)
                {
                    _hitStopRemaining = _hitStopDuration;
                }
                else
                {
                    Impact();
                }
            }
        }

        if (_impactAge >= 0f && _hitStopRemaining <= 0f)
        {
            _impactAge += deltaTime;
        }

        Evaluate();
        UpdateEffects(_hitStopRemaining > 0f ? 0f : deltaTime);

        if (_cameraFx != null)
        {
            _cameraFx.Shake = isPaused ? 0f : CurrentShake();
            _cameraFx.Zoom = CurrentZoom();
        }

        if (_time >= EndTime)
        {
            Finish();
        }
    }

    // ───────────────────────── 시간에 따른 상태 ─────────────────────────

    private void Evaluate()
    {
        float t = _time;
        Vector3 feet = _landing + (Vector3)_feetOffset;

        // 검은 띠 : 처음에 들어오고 카메라가 돌아갈 때 걷힌다.
        float letterbox = Mathf.Min(Segment(t, 0f, LetterboxDuration), 1f - Segment(t, ReturnStart, ReturnStart + LetterboxDuration));
        SetLetterbox(EaseOut(letterbox));

        // 범위 원 : 충전 → 땅을 치면 터지며 사라진다.
        if (_range != null && _rangeVisual != null)
        {
            if (!_hasImpacted)
            {
                bool visible = t >= _panDuration;
                _range.enabled = visible;
                if (visible)
                {
                    SetRangeSize(1f);
                    _rangeVisual.SetProgress(Segment(t, _panDuration, LandTime), _rangeColor, _rangeChargedAlpha);
                }
            }
            else
            {
                float burst = Mathf.Clamp01(_impactAge / _rangeBurstDuration);
                _range.enabled = burst < 1f;
                SetRangeSize(1f + _rangeBurstGrow * EaseOut(burst));
                Color color = _rangeColor;
                color.a *= 1f - burst;
                _rangeVisual.SetProgress(1f, color, _rangeChargedAlpha * (1f - burst));
            }
        }

        // 그림자 : 떨어지는 동안 커지고 진해진다. 땅에 닿으면 보스 그림에 맡긴다.
        if (_shadow != null)
        {
            float shadow = Segment(t, LandTime - _shadowLead, LandTime);
            bool visible = t >= LandTime - _shadowLead && t < SlamEndTime;
            _shadow.enabled = visible;
            if (visible)
            {
                float k = t >= LandTime ? 1f : shadow * shadow;
                _shadow.transform.position = new Vector3(feet.x, feet.y, _shadow.transform.position.z);
                float scale = Mathf.Lerp(_shadowScale.x, _shadowScale.y, k);
                _shadow.transform.localScale = new Vector3(scale, scale * 0.28f, 1f);
                Color color = _shadowColor;
                color.a = Mathf.Lerp(_shadowAlpha.x, _shadowAlpha.y, k);
                _shadow.color = color;
            }
        }

        // 떨어지는 보스 → 내려찍기 동작. 동작이 끝나면 진짜 보스로 바꾼다.
        if (_faller != null)
        {
            float fallStart = LandTime - _fallDuration;
            if (t < fallStart || _hasSpawnedBoss)
            {
                _faller.enabled = false;
            }
            else if (t < LandTime)
            {
                float fall = Segment(t, fallStart, LandTime);
                _faller.enabled = true;
                _faller.sprite = _fallSprite;
                _faller.transform.position = _landing + Vector3.up * (_fallHeight * (1f - fall * fall));
            }
            else
            {
                _faller.enabled = true;
                _faller.transform.position = _landing;
                if (_slamFrames != null && _slamFrames.Length > 0)
                {
                    int index = Mathf.Clamp(Mathf.FloorToInt((t - LandTime) / _slamFrameTime), 0, _slamFrames.Length - 1);
                    _faller.sprite = _slamFrames[index];
                }
            }
        }

        if (!_hasSpawnedDust && t >= LandTime - _dustLead)
        {
            _hasSpawnedDust = true;
            SpawnDust(feet);
        }

        if (!_hasSpawnedBoss && _hasImpacted && t >= SlamEndTime)
        {
            _hasSpawnedBoss = true;
            SpawnBoss();
        }

        // 섬광 : 히트스톱 동안 켜 두고 풀리면 빠르게 사라진다.
        if (_flash != null)
        {
            float alpha = _hitStopRemaining > 0f ? _flashAlpha
                : _impactAge >= 0f ? _flashAlpha * (1f - Mathf.Clamp01(_impactAge / _flashFade)) : 0f;
            _flash.enabled = alpha > 0f;
            Color color = _flashColor;
            color.a = alpha;
            _flash.color = color;
        }

        // 이름 띠
        if (_banner != null)
        {
            float alpha = Mathf.Min(Segment(t, BannerStart, BannerStart + _bannerFade), 1f - Segment(t, ReturnStart - _bannerFade, ReturnStart));
            _banner.alpha = alpha;
            _banner.gameObject.SetActive(alpha > 0f);
            float open = EaseOut(Segment(t, BannerStart, BannerStart + _bannerFade * 1.5f));
            if (_bannerLines != null)
            {
                foreach (RectTransform line in _bannerLines)
                {
                    if (line != null)
                    {
                        line.localScale = new Vector3(open, 1f, 1f);
                    }
                }
            }
        }

        if (!_hasReturned && t >= ReturnStart)
        {
            _hasReturned = true;
            if (_cameraFollow != null)
            {
                _cameraFollow.ClearFocusOverride(_returnDuration);
            }
        }
    }

    private float CurrentShake()
    {
        if (_hitStopRemaining > 0f)
        {
            return _impactShake * 0.6f;
        }

        if (_impactAge >= 0f)
        {
            float k = 1f - Mathf.Clamp01(_impactAge / _impactShakeDuration);
            if (k > 0f)
            {
                return _impactShake * k * k;
            }
        }

        return !_hasImpacted ? _chargeShake * Segment(_time, _panDuration, LandTime) : 0f;
    }

    private float CurrentZoom()
    {
        if (_impactAge < 0f)
        {
            return 1f;
        }

        float k = Mathf.Clamp01(_impactAge / _zoomPunchDuration);
        return 1f - _zoomPunch * Mathf.Sin(k * Mathf.PI);
    }

    // ───────────────────────── 땅을 치는 순간 ─────────────────────────

    private void Impact()
    {
        if (_hasImpacted)
        {
            return;
        }

        _hasImpacted = true;
        _hitStopRemaining = 0f;
        _impactAge = 0f;

        Vector3 feet = _landing + (Vector3)_feetOffset;
        float radiusX = _rangeRadius;
        float radiusY = _rangeRadius * _rangeHeightRatio;

        if (_shockRing != null)
        {
            _shockRing.transform.position = new Vector3(_landing.x, _landing.y, _shockRing.transform.position.z);
        }

        // 석화 가시 : 안쪽 고리부터 바깥쪽으로 번진다.
        if (_spikePrefab != null && _spikeRings != null)
        {
            float outer = 0.01f;
            foreach (Vector3 ring in _spikeRings)
            {
                outer = Mathf.Max(outer, ring.x);
            }

            foreach (Vector3 ring in _spikeRings)
            {
                int count = Mathf.Max(0, Mathf.RoundToInt(ring.y));
                float scale = Mathf.Lerp(_spikeScale.x, _spikeScale.y, ring.x / outer);
                for (int i = 0; i < count; i++)
                {
                    float angle = (i + ring.z) / count * Mathf.PI * 2f;
                    Vector3 position = _landing + new Vector3(Mathf.Cos(angle) * radiusX * ring.x, Mathf.Sin(angle) * radiusY * ring.x, 0f);
                    GameObject spike = Instantiate(_spikePrefab, position, _spikePrefab.transform.rotation, _worldRoot);
                    spike.transform.localScale = _spikePrefab.transform.localScale * scale;
                    foreach (SpriteRenderer renderer in spike.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        renderer.sortingLayerID = 0;
                        renderer.sortingOrder = _spikeSortingOrder;
                    }

                    spike.SetActive(false);
                    _spikes.Add(new Spike { Instance = spike, Delay = _spikeSpreadTime * ring.x / outer });
                }
            }
        }

        // 바위 기둥 : 낙석 공격처럼 땅 속에서 솟았다 가라앉는다.
        if (_stoneSprite != null && _stonePoints != null)
        {
            for (int i = 0; i < _stonePoints.Length; i++)
            {
                Vector2 point = _stonePoints[i];
                Vector3 center = _landing + new Vector3(point.x * radiusX, point.y * radiusY, 0f);
                SpriteRenderer rock = new GameObject("IntroStone").AddComponent<SpriteRenderer>();
                rock.sprite = _stoneSprite;
                rock.flipX = i % 2 == 1;
                RockEruptionVisual visual = new GameObject("IntroStoneEruption").AddComponent<RockEruptionVisual>();
                visual.transform.SetParent(_worldRoot, true);
                visual.Initialize(rock, center, i % 2 == 0 ? _stoneHeight.x : _stoneHeight.y);
                visual.gameObject.SetActive(false);
                _stones.Add(new Stone { Visual = visual, Delay = _stoneStagger * (i + 1) });
            }
        }

        // 돌 파편
        if (_shardTemplate != null && _shardSprites != null && _shardSprites.Length > 0)
        {
            for (int i = 0; i < _shardCount; i++)
            {
                SpriteRenderer renderer = Instantiate(_shardTemplate, _worldRoot);
                renderer.gameObject.SetActive(true);
                renderer.sprite = _shardSprites[UnityEngine.Random.Range(0, _shardSprites.Length)];
                renderer.transform.position = new Vector3(feet.x, feet.y, renderer.transform.position.z);
                float angle = (i + UnityEngine.Random.value * 0.6f) / Mathf.Max(1, _shardCount) * Mathf.PI * 2f;
                float speed = UnityEngine.Random.Range(_shardSpeed.x, _shardSpeed.y);
                _shards.Add(new Shard
                {
                    Renderer = renderer,
                    Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * _rangeHeightRatio) * speed,
                    Lift = UnityEngine.Random.Range(_shardLift.x, _shardLift.y),
                    Spin = UnityEngine.Random.Range(-540f, 540f)
                });
            }
        }
    }

    private void SpawnDust(Vector3 feet)
    {
        if (_dustTemplate == null)
        {
            return;
        }

        for (int i = 0; i < _dustCount; i++)
        {
            SpriteRenderer renderer = Instantiate(_dustTemplate, _worldRoot);
            renderer.gameObject.SetActive(true);
            renderer.transform.position = new Vector3(feet.x, feet.y, renderer.transform.position.z);
            float angle = (i + UnityEngine.Random.value * 0.5f) / Mathf.Max(1, _dustCount) * Mathf.PI * 2f;
            _puffs.Add(new Puff
            {
                Renderer = renderer,
                Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * _dustFlatten) * UnityEngine.Random.Range(0.85f, 1.15f)
            });
        }
    }

    private void SpawnBoss()
    {
        GameObject boss = _spawnBoss?.Invoke();
        if (boss == null)
        {
            return;
        }

        // 연출이 끝날 때까지 공격하지 않고 제자리에 있게 한다.
        _bossPattern = boss.GetComponentInChildren<BossPatternController>();
        if (_bossPattern != null)
        {
            _bossPattern.enabled = false;
        }

        _bossMotor = boss.GetComponentInChildren<EnemyMotor>();
        if (_bossMotor != null)
        {
            _bossMotor.AcquirePositionLock(this);
        }
    }

    // ───────────────────────── 이펙트 갱신 ─────────────────────────

    private void UpdateEffects(float deltaTime)
    {
        Vector3 feet = _landing + (Vector3)_feetOffset;

        for (int i = _puffs.Count - 1; i >= 0; i--)
        {
            Puff puff = _puffs[i];
            puff.Age += deltaTime;
            float k = puff.Age / _dustLife;
            if (k >= 1f || puff.Renderer == null)
            {
                if (puff.Renderer != null)
                {
                    Destroy(puff.Renderer.gameObject);
                }

                _puffs.RemoveAt(i);
                continue;
            }

            float spread = EaseOut(k);
            Vector2 offset = puff.Direction * Mathf.Lerp(_dustDistance.x, _dustDistance.y, spread);
            puff.Renderer.transform.position = new Vector3(feet.x + offset.x, feet.y + offset.y, puff.Renderer.transform.position.z);
            float scale = Mathf.Lerp(_dustScale.x, _dustScale.y, spread);
            puff.Renderer.transform.localScale = new Vector3(scale, scale * 0.5f, 1f);
            Color color = _dustColor;
            color.a *= Mathf.Clamp01(k / 0.15f) * (1f - k);
            puff.Renderer.color = color;
        }

        if (_shockRing != null)
        {
            float k = _impactAge >= 0f ? _impactAge / _shockRingDuration : 1f;
            _shockRing.enabled = k < 1f;
            if (k < 1f)
            {
                float size = Mathf.Lerp(_shockRingSize.x, _shockRingSize.y, EaseOut(k));
                float spriteWidth = _shockRing.sprite != null ? Mathf.Max(0.01f, _shockRing.sprite.bounds.size.x) : 1f;
                float scaleX = _rangeRadius * 2f * size / spriteWidth;
                _shockRing.transform.localScale = new Vector3(scaleX, scaleX * _rangeHeightRatio, 1f);
                Color color = _shockColor;
                color.a *= 1f - k;
                _shockRing.color = color;
            }
        }

        if (_impactAge < 0f)
        {
            return;
        }

        for (int i = _spikes.Count - 1; i >= 0; i--)
        {
            Spike spike = _spikes[i];
            if (spike.Instance == null)
            {
                _spikes.RemoveAt(i);
                continue;
            }

            if (!spike.Started && _impactAge >= spike.Delay)
            {
                spike.Started = true;
                spike.Instance.SetActive(true);
            }

            if (_impactAge >= spike.Delay + _spikeLifetime)
            {
                Destroy(spike.Instance);
                _spikes.RemoveAt(i);
            }
        }

        for (int i = _stones.Count - 1; i >= 0; i--)
        {
            Stone stone = _stones[i];
            if (stone.Visual == null)
            {
                _stones.RemoveAt(i);
                continue;
            }

            float elapsed = _impactAge - stone.Delay;
            if (elapsed < 0f)
            {
                continue;
            }

            if (elapsed >= _stoneRise + _stoneHold + _stoneSink)
            {
                Destroy(stone.Visual.gameObject);
                _stones.RemoveAt(i);
                continue;
            }

            if (!stone.Visual.gameObject.activeSelf)
            {
                stone.Visual.gameObject.SetActive(true);
            }

            stone.Visual.Animate(elapsed, _stoneRise, _stoneHold, _stoneSink);
        }

        for (int i = _shards.Count - 1; i >= 0; i--)
        {
            Shard shard = _shards[i];
            shard.Age += deltaTime;
            if (shard.Age >= _shardLife || shard.Renderer == null)
            {
                if (shard.Renderer != null)
                {
                    Destroy(shard.Renderer.gameObject);
                }

                _shards.RemoveAt(i);
                continue;
            }

            // 바닥 기준으로 옆으로 퍼지면서 포물선으로 튀었다가 떨어진다.
            float age = shard.Age;
            float height = Mathf.Max(0f, shard.Lift * age - 0.5f * _shardGravity * age * age);
            Vector2 ground = shard.Velocity * age;
            shard.Renderer.transform.position = new Vector3(feet.x + ground.x, feet.y + ground.y + height, shard.Renderer.transform.position.z);
            shard.Renderer.transform.rotation = Quaternion.Euler(0f, 0f, shard.Spin * age);
            Color color = shard.Renderer.color;
            color.a = 1f - Mathf.Clamp01((age - _shardLife * 0.6f) / (_shardLife * 0.4f));
            shard.Renderer.color = color;
        }
    }

    // ───────────────────────── 정리 ─────────────────────────

    private void Finish()
    {
        if (!_isPlaying)
        {
            return;
        }

        _isPlaying = false;
        ReleaseAll();

        Action callback = _onFinished;
        _onFinished = null;
        callback?.Invoke();
        Destroy(gameObject);
    }

    private void ReleaseAll()
    {
        if (_isInputLocked)
        {
            _isInputLocked = false;
            PlayerInputHandler.UnlockForCutscene();
        }

        if (_bossPattern != null)
        {
            _bossPattern.enabled = true;
            _bossPattern = null;
        }

        if (_bossMotor != null)
        {
            _bossMotor.ReleasePositionLock(this);
            _bossMotor = null;
        }

        if (_cameraFollow != null)
        {
            if (!_hasReturned)
            {
                _cameraFollow.ClearFocusOverride(_returnDuration);
            }

            _cameraFollow = null;
        }

        if (_cameraFx != null)
        {
            _cameraFx.Remove();
            _cameraFx = null;
        }

        _rangeVisual?.Dispose();
        _rangeVisual = null;
    }

    private void OnDestroy()
    {
        // 씬이 바뀌는 등 중간에 사라져도 입력·보스·카메라를 풀어 준다.
        if (_isPlaying)
        {
            _isPlaying = false;
            ReleaseAll();
        }
    }

    private void HideAll()
    {
        if (_range != null) _range.enabled = false;
        if (_shadow != null) _shadow.enabled = false;
        if (_faller != null) _faller.enabled = false;
        if (_shockRing != null) _shockRing.enabled = false;
        if (_flash != null) _flash.enabled = false;
        if (_banner != null)
        {
            _banner.alpha = 0f;
            _banner.gameObject.SetActive(false);
        }

        SetLetterbox(0f);
    }

    private void SetLetterbox(float amount)
    {
        float height = _letterboxHeight * amount;
        if (_letterboxTop != null)
        {
            _letterboxTop.sizeDelta = new Vector2(_letterboxTop.sizeDelta.x, height);
        }

        if (_letterboxBottom != null)
        {
            _letterboxBottom.sizeDelta = new Vector2(_letterboxBottom.sizeDelta.x, height);
        }
    }

    /// <summary>원 그림을 반지름·세로 비율에 맞춘다. (scale = 1이면 원래 크기)</summary>
    private void SetRangeSize(float scale)
    {
        if (_range == null || _range.sprite == null)
        {
            return;
        }

        Vector2 size = _range.sprite.bounds.size;
        float x = _rangeRadius * 2f / Mathf.Max(0.01f, size.x) * scale;
        float y = _rangeRadius * 2f * _rangeHeightRatio / Mathf.Max(0.01f, size.y) * scale;
        _range.transform.localScale = new Vector3(x, y, _rangeBaseScale.z == 0f ? 1f : _rangeBaseScale.z);
        _range.transform.position = new Vector3(_landing.x, _landing.y, _range.transform.position.z);
    }

    private static float Segment(float t, float from, float to)
    {
        return to <= from ? (t >= to ? 1f : 0f) : Mathf.Clamp01((t - from) / (to - from));
    }

    private static float EaseOut(float t)
    {
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }
}
