using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

/// <summary>
/// 시작 석상. 전투 씬 1층 가운데에 서 있다. (마을 씬을 대신한다)
/// 흐름 : 상호작용(F) → 주력 원소 선택 → 룬북이 열려 보조 룬 워드 선택 → Confirm
///        → 석상이 흔들리다 무너진다 → 1층 미션 오브젝트가 서서히 나타난다 → 미션 시작 오브젝트로 시작
/// - 원소 선택을 취소하거나 룬북을 Confirm 없이 닫으면 석상은 그대로 있고, 다시 상호작용할 수 있다.
/// - 씬이 열리면 원정(지갑, 시작 골드)을 시작한다. (플레이어 시작 위치는 PlayerStartPositioner가 정한다)
/// - 1층은 StageManager가 기다리고 있다가(_waitForPreparation) 석상이 무너진 뒤 BeginPreparedRun으로 시작한다.
/// 무너지는 연출은 게임 시간으로 움직인다. (일시정지하면 멈춘다)
/// 무너질 때는 비석 윗부분부터 알갱이로 풀려 모래처럼 떨어지며, 비석이 위에서 아래로 점점 낮아진다.
/// </summary>
[DisallowMultipleComponent]
public class ExpeditionStatue : MonoBehaviour, IInteractable
{
    private const string PlayerTag = "Player";
    private static readonly int RevealProgressId = Shader.PropertyToID("_RevealProgress");
    private static readonly int GlowTimeId = Shader.PropertyToID("_GlowTime");

    // 위에서 아래로 풀리는 속도 곡선. 1보다 크면 처음엔 천천히, 갈수록 빨라진다.
    private const float CrumbleCurve = 1.35f;
    private const float MaskMargin = 0.2f;
    private const byte MinGrainAlpha = 80;
    private const int GrainSampleAttempts = 6;
    private const float GrainDrag = 1.2f;
    private const float GrainLifetime = 1000f;

    private static Sprite s_whiteSprite;

    [Header("상호작용")]
    [SerializeField] private string _interactionName = "원소 새기기";
    [SerializeField] private Transform _interactionGuidePoint;

    [Header("UI")]
    [SerializeField] private PopupController _popupController;
    [SerializeField] private MainElementSelectView _elementSelectView;
    [SerializeField] private RuneBookV2Toggle _runeBookToggle;
    [Tooltip("룬북 화면 루트. 비워두면 _runeBookPresenter를 직접 넣는다")] [SerializeField] private GameObject _runeBookRoot;
    [SerializeField] private RuneBookV2Presenter _runeBookPresenter;

    [Header("석상")]
    [SerializeField] private SpriteRenderer _statueRenderer;
    [Tooltip("석상과 함께 사라지는 것 (바닥 마법진 등)")] [SerializeField] private SpriteRenderer[] _fadeOutRenderers;
    [SerializeField] private Collider2D[] _colliders;

    [Header("마법진 근접 발광")]
    [SerializeField] private SpriteRenderer _circleBaseRenderer;
    [SerializeField] private SpriteRenderer _circleGlowRenderer;
    [SerializeField] private SpriteRenderer _circleAuraRenderer;
    [SerializeField, Min(0f)] private float _glowActivationDistance = 2f;
    [SerializeField, Range(0f, 1f)] private float _glowAlpha = 0.65f;
    [FormerlySerializedAs("_glowFadeDuration")] [SerializeField, Min(0.01f)] private float _glowExpandDuration = 2f;

    [Header("무너지기 전 흔들림")]
    [SerializeField, Min(0f)] private float _shakeDuration = 0.7f;
    [SerializeField] private float _shakeAmount = 0.07f;
    [Tooltip("무너지기 직전 석상이 빛나는 색")] [SerializeField] private Color _flashColor = new(0.8f, 0.92f, 1f, 1f);

    [Header("알갱이 (위에서부터 풀려 모래처럼 떨어진다)")]
    [Tooltip("비석이 위에서 아래까지 다 풀리는 데 걸리는 시간")] [SerializeField, Min(0.1f)] private float _crumbleDuration = 2f;
    [Tooltip("무너지는 동안 떨어지는 알갱이의 총 개수")] [SerializeField, Min(0)] private int _grainCount = 1400;
    [Tooltip("알갱이 한 변의 크기 (월드 단위, 최소~최대)")] [SerializeField] private Vector2 _grainSize = new(0.025f, 0.055f);
    [SerializeField] private float _grainGravity = 8f;
    [Tooltip("알갱이가 풀릴 때 옆으로 흩어지는 세기")] [SerializeField] private float _grainSpread = 0.6f;
    [Tooltip("알갱이가 떨어져 쌓이는 바닥 높이의 흩어짐 (석상 밑동 기준)")] [SerializeField] private float _groundJitter = 0.5f;
    [Tooltip("다 무너진 뒤 쌓인 알갱이가 사라지는 시간")] [SerializeField, Min(0.01f)] private float _grainFadeDuration = 0.8f;

    [Header("먼지")]
    [SerializeField] private Sprite _dustSprite;
    [SerializeField] private Color _dustColor = new(0.78f, 0.74f, 0.7f, 0.6f);
    [SerializeField, Min(0)] private int _dustCount = 14;
    [SerializeField] private Vector2 _dustSize = new(0.8f, 1.8f);
    [SerializeField, Min(0.1f)] private float _dustLifetime = 1.1f;

    [Header("룬북 Confirm 뒤")]
    [Tooltip("룬북 Confirm 연출(장착 칸이 차례로 빛남)을 보여준 뒤 룬북을 닫기까지 (실제 시간)")] [SerializeField, Min(0f)]
    private float _runeBookCloseDelay = 0.55f;

    [Header("이후")]
    [Tooltip("무너지기 시작하고 1층 미션을 준비하기까지")] [SerializeField, Min(0f)] private float _beginRunDelay = 0.6f;
    [Tooltip("미션 오브젝트가 서서히 나타나는 시간")] [SerializeField, Min(0.01f)] private float _missionFadeDuration = 1.4f;
    [SerializeField, Min(0.01f)] private float _circleFadeDuration = 1f;

    private bool _isChoosingElement;
    private bool _isWaitingRuneConfirm;
    private bool _isCrumbling;
    private bool _runBegun;
    private bool _crumbleFinished;
    private Coroutine _crumbleCoroutine;
    private float[] _circleBaseAlphas;
    private Material _fallbackGrainMaterial;

    // 무너지는 연출이 만든 오브젝트의 루트. 연출이 끊기면 OnDisable에서 함께 치운다.
    private GameObject _effectRoot;
    private Transform _statueTransform;
    private Vector3 _statueBasePosition;
    private Color _statueBaseColor = Color.white;
    private bool _hasStatueBase;

    // 알갱이는 GameObject 대신 ParticleSystem 하나에 담아 직접 움직인다.
    private struct GrainState
    {
        public Vector3 Position;
        public Vector2 Velocity;
        public float GroundY;
        public bool IsResting;
        public Color32 Color;
    }

    private GrainState[] _grains;
    private ParticleSystem.Particle[] _grainParticles;
    private ParticleSystem _grainSystem;
    private int _grainActive;
    private Vector2 _sampleOffset;
    private Transform _player;
    private MaterialPropertyBlock _circleGlowProperties;
    private float _glowProgress;
    private float _glowTime;

    /// <summary>플레이어가 석상과 상호작용해 주력 원소 선택 창을 열었을 때</summary>
    public event Action Interacted;

    public string InteractionName => _interactionName;
    public Transform InteractionGuidePoint => _interactionGuidePoint != null ? _interactionGuidePoint : transform;

    private void Awake()
    {
        // 원정을 시작한다. (지갑과 시작 골드. 이미 원정 중이면 아무것도 하지 않는다)
        RunFlow.StartExpedition();
        InitializeCircleGlow();
        SetCircleGlowProgress(0f);

        if (_runeBookPresenter == null && _runeBookRoot != null)
        {
            _runeBookPresenter = _runeBookRoot.GetComponentInChildren<RuneBookV2Presenter>(true);
        }
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);
        if (player != null)
        {
            _player = player.transform;
        }
    }

    private void Update()
    {
        if (_circleGlowRenderer == null || _isCrumbling)
        {
            return;
        }

        bool isNearby = _player != null &&
                        ((Vector2)(_player.position - _circleGlowRenderer.transform.position)).sqrMagnitude <=
                        _glowActivationDistance * _glowActivationDistance;
        float targetProgress = isNearby ? 1f : 0f;
        _glowTime += Time.deltaTime;
        float expandSpeed = 1f / Mathf.Max(0.01f, _glowExpandDuration);
        SetCircleGlowProgress(Mathf.MoveTowards(_glowProgress, targetProgress, expandSpeed * Time.deltaTime));
    }

    private void SetCircleGlowProgress(float progress)
    {
        _glowProgress = progress;
        if (_circleGlowRenderer == null)
        {
            return;
        }

        _circleGlowRenderer.GetPropertyBlock(_circleGlowProperties);
        _circleGlowProperties.SetFloat(RevealProgressId, progress);
        _circleGlowProperties.SetFloat(GlowTimeId, _glowTime);
        _circleGlowRenderer.SetPropertyBlock(_circleGlowProperties);
        Color color = _circleGlowRenderer.color;
        color.a = progress > 0f ? _glowAlpha : 0f;
        _circleGlowRenderer.color = color;
        if (_circleAuraRenderer != null)
        {
            _circleAuraRenderer.SetPropertyBlock(_circleGlowProperties);
            Color auraColor = _circleAuraRenderer.color;
            auraColor.a = color.a;
            _circleAuraRenderer.color = auraColor;
        }
    }

    private void InitializeCircleGlow()
    {
        if (_fadeOutRenderers != null)
        {
            foreach (SpriteRenderer circle in _fadeOutRenderers)
            {
                if (circle == null)
                {
                    continue;
                }

                if (_circleBaseRenderer == null && circle.gameObject.name == "Base")
                {
                    _circleBaseRenderer = circle;
                }

                if (_circleGlowRenderer == null && circle.gameObject.name == "Glow")
                {
                    _circleGlowRenderer = circle;
                }
            }
        }

        if (_circleGlowRenderer != null)
        {
            _circleGlowProperties = new MaterialPropertyBlock();
            if (_circleAuraRenderer == null)
            {
                Transform aura = _circleGlowRenderer.transform.Find("MagicCircleAura");
                if (aura != null) _circleAuraRenderer = aura.GetComponent<SpriteRenderer>();
            }
        }

        if (_circleBaseRenderer != null && _circleGlowRenderer != null)
        {
            _circleGlowRenderer.sortingLayerID = _circleBaseRenderer.sortingLayerID;
            _circleGlowRenderer.sortingOrder = _circleBaseRenderer.sortingOrder - 1;
        }
        if (_circleGlowRenderer != null && _circleAuraRenderer != null)
        {
            _circleAuraRenderer.sortingLayerID = _circleGlowRenderer.sortingLayerID;
            _circleAuraRenderer.sortingOrder = _circleGlowRenderer.sortingOrder - 1;
        }
    }

    private void OnEnable()
    {
        if (_elementSelectView != null)
        {
            _elementSelectView.Confirmed += HandleElementConfirmed;
            _elementSelectView.Cancelled += HandleElementCancelled;
        }

        if (_runeBookPresenter != null)
        {
            _runeBookPresenter.Confirmed += HandleRunesConfirmed;
        }
    }

    private void OnDisable()
    {
        AbortCrumble();
        SetCircleGlowProgress(0f);

        if (_elementSelectView != null)
        {
            _elementSelectView.Confirmed -= HandleElementConfirmed;
            _elementSelectView.Cancelled -= HandleElementCancelled;
        }

        if (_runeBookPresenter != null)
        {
            _runeBookPresenter.Confirmed -= HandleRunesConfirmed;
        }
    }

    public void Interact()
    {
        if (_isCrumbling || _isChoosingElement || PauseController.IsGamePaused)
        {
            return;
        }

        if (_popupController == null || _elementSelectView == null)
        {
            Debug.LogError("[ExpeditionStatue] PopupController 또는 주력 원소 선택 창이 할당되지 않았습니다.", this);
            return;
        }

        if (_popupController.IsOpen || (_runeBookToggle != null && _runeBookToggle.IsOpen))
        {
            return;
        }

        _isChoosingElement = true;
        _popupController.OpenPopup(_elementSelectView.gameObject);
        Interacted?.Invoke();
    }

    private void HandleElementConfirmed(ReactionElement element)
    {
        if (!_isChoosingElement)
        {
            return;
        }

        // 원소는 MainElementSelectView가 이미 장착했다. 이어서 보조 룬 워드를 고르는 룬북을 연다.
        _isChoosingElement = false;
        _popupController.ClosePopup();

        _isWaitingRuneConfirm = true;
        if (_runeBookPresenter != null)
        {
            _runeBookPresenter.SetViewOnly(false);
        }

        if (_runeBookToggle != null)
        {
            _runeBookToggle.Open();
        }
    }

    private void HandleElementCancelled()
    {
        if (!_isChoosingElement)
        {
            return;
        }

        _isChoosingElement = false;
        _popupController.ClosePopup();
    }

    private void HandleRunesConfirmed()
    {
        if (!_isWaitingRuneConfirm || _isCrumbling)
        {
            return;
        }

        _isWaitingRuneConfirm = false;
        _isCrumbling = true;
        _crumbleCoroutine = StartCoroutine(CloseRuneBookThenCrumble());
    }

    /// <summary>룬북의 Confirm 연출을 잠깐 보여준 뒤 닫고 석상을 무너뜨린다. (룬북이 열린 동안 게임이 멈추므로 실제 시간)</summary>
    private IEnumerator CloseRuneBookThenCrumble()
    {
        yield return new WaitForSecondsRealtime(_runeBookCloseDelay);
        if (_runeBookToggle != null)
        {
            _runeBookToggle.Close();
        }

        yield return CrumbleRoutine();
    }

    // ───────────────────────── 무너짐 ─────────────────────────

    private IEnumerator CrumbleRoutine()
    {
        _isCrumbling = true;
        foreach (Collider2D target in _colliders)
        {
            if (target != null)
            {
                target.enabled = false;
            }
        }

        // 1. 점점 세게 흔들리며 빛난다.
        Transform statue = _statueRenderer != null ? _statueRenderer.transform : transform;
        Color baseColor = _statueRenderer != null ? _statueRenderer.color : Color.white;
        _statueTransform = statue;
        _statueBasePosition = statue.localPosition;
        _statueBaseColor = baseColor;
        _hasStatueBase = true;
        float elapsed = 0f;
        while (elapsed < _shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _shakeDuration);
            float amount = _shakeAmount * t;
            statue.localPosition = _statueBasePosition +
                                   new Vector3(Random.Range(-amount, amount), Random.Range(-amount, amount) * 0.4f, 0f);
            if (_statueRenderer != null)
            {
                _statueRenderer.color = Color.Lerp(baseColor, _flashColor, t * t);
            }

            yield return null;
        }

        statue.localPosition = _statueBasePosition;

        // 2. 비석은 위에서부터 알갱이로 풀리며 낮아진다.
        //    스프라이트 마스크를 위에서 아래로 내려 윗부분을 지우고, 지워진 자리에서 비석 색 알갱이가 모래처럼 떨어진다.
        Bounds bounds = _statueRenderer != null
            ? _statueRenderer.bounds
            : new Bounds(transform.position, new Vector3(2f, 3f, 0f));
        _effectRoot = new GameObject("StatueCrumbleEffects");
        SpriteMask mask = CreateCrumbleMask(bounds);
        if (_statueRenderer != null)
        {
            _statueRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        bool hasPixels = TryReadStatuePixels(out Color32[] pixels, out int pixelWidth, out int pixelHeight);
        CreateGrainSystem();
        List<Particle> dusts = new();
        float[] circleAlphas = new float[_fadeOutRenderers?.Length ?? 0];
        for (int i = 0; i < circleAlphas.Length; i++)
        {
            circleAlphas[i] = _fadeOutRenderers[i] != null ? _fadeOutRenderers[i].color.a : 0f;
        }

        _circleBaseAlphas = circleAlphas;

        // 3. 풀리는 동안 1층을 준비하고, 미션 오브젝트를 서서히 보여준다.
        //    먼지는 풀리는 마지막까지 만들어지므로, 그 먼지의 수명이 끝날 때까지 기다린다.
        bool hasBegun = false;
        float total = Mathf.Max(_crumbleDuration + _grainFadeDuration, _crumbleDuration + _dustLifetime,
            _circleFadeDuration);
        float previousProgress = 0f;
        float grainBudget = 0f;
        float dustBudget = 0f;
        elapsed = 0f;
        while (elapsed < total)
        {
            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;

            float progress = Mathf.Pow(Mathf.Clamp01(elapsed / _crumbleDuration), CrumbleCurve);
            if (progress > previousProgress)
            {
                float step = progress - previousProgress;
                float previousY = Mathf.Lerp(bounds.max.y, bounds.min.y, previousProgress);
                float cutY = Mathf.Lerp(bounds.max.y, bounds.min.y, progress);

                grainBudget += _grainCount * step;
                int grainsToSpawn = (int)grainBudget;
                grainBudget -= grainsToSpawn;
                SpawnGrains(grainsToSpawn, bounds, cutY, previousY, hasPixels, pixels, pixelWidth, pixelHeight,
                    baseColor);

                if (_dustSprite != null)
                {
                    dustBudget += _dustCount * step;
                    int dustToSpawn = (int)dustBudget;
                    dustBudget -= dustToSpawn;
                    for (int i = 0; i < dustToSpawn; i++)
                    {
                        // 절반은 풀리는 경계에서, 절반은 알갱이가 쌓이는 밑동에서 인다.
                        float dustY = i % 2 == 0
                            ? Random.Range(cutY, previousY)
                            : bounds.min.y + Random.Range(0f, 0.35f);
                        dusts.Add(CreateDust(bounds, dustY));
                    }
                }

                UpdateCrumbleMask(mask, bounds, cutY);
                previousProgress = progress;
            }

            // 빛나던 석상 색은 풀리기 시작하면 돌 색으로 돌아온다.
            if (_statueRenderer != null && progress < 1f)
            {
                float colorT = Mathf.Clamp01(elapsed / (_crumbleDuration * 0.25f));
                _statueRenderer.color = Color.Lerp(_flashColor, baseColor, colorT);
            }

            float fade = Mathf.Clamp01((elapsed - _crumbleDuration) / _grainFadeDuration);
            UpdateGrains(deltaTime, fade);
            UpdateDust(dusts, deltaTime);

            float circleT = Mathf.Clamp01(elapsed / _circleFadeDuration);
            for (int i = 0; i < circleAlphas.Length; i++)
            {
                SpriteRenderer circle = _fadeOutRenderers[i];
                if (circle != null)
                {
                    Color color = circle.color;
                    color.a = circleAlphas[i] * (1f - circleT);
                    circle.color = color;
                }
            }

            if (!hasBegun && elapsed >= _beginRunDelay)
            {
                hasBegun = true;
                BeginRun();
            }

            yield return null;
        }

        if (!hasBegun)
        {
            BeginRun();
        }

        if (_statueRenderer != null)
        {
            _statueRenderer.enabled = false;
        }

        DestroyEffects();
        _crumbleFinished = true;

        // 미션 오브젝트가 다 나타난 뒤 석상을 치운다.
        yield return new WaitForSeconds(_missionFadeDuration);
        Destroy(gameObject);
    }

    /// <summary>무너지는 연출이 만든 마스크, 알갱이, 먼지를 모두 치운다.</summary>
    private void DestroyEffects()
    {
        if (_effectRoot != null)
        {
            Destroy(_effectRoot);
        }

        if (_fallbackGrainMaterial != null)
        {
            Destroy(_fallbackGrainMaterial);
        }

        _fallbackGrainMaterial = null;
        _effectRoot = null;
        _grainSystem = null;
        _grains = null;
        _grainParticles = null;
        _grainActive = 0;
    }

    /// <summary>
    /// 연출이 끊겼을 때(석상이 꺼지거나 파괴될 때) 만든 오브젝트를 치운다.
    /// 1층이 시작되기 전이면 석상을 원래대로 돌려 다시 상호작용할 수 있게 한다.
    /// </summary>
    private void AbortCrumble()
    {
        // 컴포넌트만 꺼진 경우에는 코루틴이 계속 돌기 때문에, 이미 치운 오브젝트에 접근하지 않도록 먼저 멈춘다.
        if (_crumbleCoroutine != null)
        {
            StopCoroutine(_crumbleCoroutine);
            _crumbleCoroutine = null;
        }

        DestroyEffects();
        if (!_isCrumbling || _crumbleFinished)
        {
            return;
        }

        if (_runBegun)
        {
            // 1층이 이미 시작됐으면 석상은 사라진 채로 둔다. (마스크를 지웠으니 반쯤 보이는 상태로 남지 않게 숨긴다)
            if (_statueRenderer != null)
            {
                _statueRenderer.enabled = false;
            }

            // 원래대로 사라지는 중이던 바닥 마법진이 반투명하게 남지 않게 마저 지운다.
            if (_fadeOutRenderers != null)
            {
                foreach (SpriteRenderer circle in _fadeOutRenderers)
                {
                    if (circle != null)
                    {
                        SetAlpha(circle, 0f);
                    }
                }
            }

            return;
        }

        if (_hasStatueBase && _statueTransform != null)
        {
            _statueTransform.localPosition = _statueBasePosition;
        }

        if (_statueRenderer != null)
        {
            _statueRenderer.maskInteraction = SpriteMaskInteraction.None;
            _statueRenderer.enabled = true;
            if (_hasStatueBase)
            {
                _statueRenderer.color = _statueBaseColor;
            }
        }

        // 연출이 시작되면서 줄어든 마법진 알파를 되돌린다. (근접 발광은 SetCircleGlowProgress가 다시 맞춘다)
        if (_circleBaseAlphas != null && _fadeOutRenderers != null)
        {
            for (int i = 0; i < _fadeOutRenderers.Length && i < _circleBaseAlphas.Length; i++)
            {
                if (_fadeOutRenderers[i] != null)
                {
                    SetAlpha(_fadeOutRenderers[i], _circleBaseAlphas[i]);
                }
            }
        }

        _circleBaseAlphas = null;

        foreach (Collider2D target in _colliders)
        {
            if (target != null)
            {
                target.enabled = true;
            }
        }

        _isCrumbling = false;
        _isWaitingRuneConfirm = false;
        _isChoosingElement = false;
    }

    private void BeginRun()
    {
        StageManager stageManager = StageManager.Instance;
        if (stageManager == null)
        {
            Debug.LogWarning("[ExpeditionStatue] StageManager가 없어 1층을 시작할 수 없습니다.", this);
            return;
        }

        _runBegun = true;
        stageManager.BeginPreparedRun();
        StartCoroutine(FadeInMissionObjectsRoutine());
    }

    /// <summary>1층 미션 오브젝트와 시작 오브젝트를 투명한 상태에서 서서히 보여준다.</summary>
    private IEnumerator FadeInMissionObjectsRoutine()
    {
        List<SpriteRenderer> renderers = new();
        MissionManager missionManager = MissionManager.Instance;
        if (missionManager != null && missionManager.CurrentMissionObject != null)
        {
            renderers.AddRange(missionManager.CurrentMissionObject.GetComponentsInChildren<SpriteRenderer>(true));
        }

        foreach (MissionStartObject startObject in FindObjectsByType<MissionStartObject>(FindObjectsSortMode.None))
        {
            renderers.AddRange(startObject.GetComponentsInChildren<SpriteRenderer>(true));
        }

        float[] alphas = new float[renderers.Count];
        for (int i = 0; i < renderers.Count; i++)
        {
            alphas[i] = renderers[i].color.a;
            SetAlpha(renderers[i], 0f);
        }

        float elapsed = 0f;
        while (elapsed < _missionFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _missionFadeDuration));
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    SetAlpha(renderers[i], alphas[i] * t);
                }
            }

            yield return null;
        }

        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null)
            {
                SetAlpha(renderers[i], alphas[i]);
            }
        }
    }

    private static void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }

    // ───────────────────────── 알갱이와 먼지 ─────────────────────────

    /// <summary>먼지 하나. (알갱이는 GrainState로 따로 다룬다)</summary>
    private class Particle
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Vector2 Velocity;
        public float Age;
        public float Life;
        public Vector3 BaseScale;
    }

    private int SortingLayerId => _statueRenderer != null ? _statueRenderer.sortingLayerID : 0;
    private int SortingOrder => _statueRenderer != null ? _statueRenderer.sortingOrder : 0;

    private static Sprite GetWhiteSprite()
    {
        if (s_whiteSprite == null)
        {
            Texture2D white = Texture2D.whiteTexture;
            s_whiteSprite = Sprite.Create(white, new Rect(0f, 0f, white.width, white.height), new Vector2(0.5f, 0.5f),
                white.width);
        }

        return s_whiteSprite;
    }

    /// <summary>비석 윗부분을 가리는 마스크를 만든다. 마스크 안쪽(아래쪽)만 비석이 보인다.</summary>
    private SpriteMask CreateCrumbleMask(Bounds bounds)
    {
        var go = new GameObject("StatueCrumbleMask");
        go.transform.SetParent(_effectRoot.transform, false);
        SpriteMask mask = go.AddComponent<SpriteMask>();
        mask.sprite = GetWhiteSprite();
        UpdateCrumbleMask(mask, bounds, bounds.max.y);
        return mask;
    }

    /// <summary>마스크의 윗변을 cutY로 맞춘다. 아랫변은 비석 밑동보다 조금 아래에 둔다.</summary>
    private void UpdateCrumbleMask(SpriteMask mask, Bounds bounds, float cutY)
    {
        float bottom = bounds.min.y - MaskMargin;
        float height = Mathf.Max(0.0001f, cutY - bottom);
        mask.transform.position = new Vector3(bounds.center.x, (cutY + bottom) * 0.5f, transform.position.z);
        mask.transform.localScale = new Vector3(bounds.size.x + MaskMargin * 2f, height, 1f);
    }

    /// <summary>알갱이를 그릴 ParticleSystem을 만든다. 생성과 움직임은 코드가 직접 맡고, 시스템은 그리기만 한다.</summary>
    private void CreateGrainSystem()
    {
        int capacity = Mathf.Max(1, _grainCount);
        _grains = new GrainState[capacity];
        _grainParticles = new ParticleSystem.Particle[capacity];
        _grainActive = 0;

        var go = new GameObject("StatueGrains");
        go.transform.SetParent(_effectRoot.transform, false);
        _grainSystem = go.AddComponent<ParticleSystem>();
        _grainSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = _grainSystem.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = capacity;
        main.gravityModifier = 0f;
        ParticleSystem.EmissionModule emission = _grainSystem.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = _grainSystem.shape;
        shape.enabled = false;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (_statueRenderer != null && _statueRenderer.sharedMaterial != null)
        {
            renderer.sharedMaterial = _statueRenderer.sharedMaterial;
        }
        else
        {
            // 석상 머티리얼이 없을 때만 직접 만들고, DestroyEffects에서 함께 치운다.
            _fallbackGrainMaterial = new Material(Shader.Find("Sprites/Default"));
            renderer.sharedMaterial = _fallbackGrainMaterial;
        }

        renderer.sortingLayerID = SortingLayerId;
        renderer.sortingOrder = SortingOrder + 1;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        _grainSystem.Play();
    }

    /// <summary>석상 스프라이트의 색을 읽어 온다. (텍스처가 읽기 불가여도 GPU를 거쳐 한 번 복사한다)</summary>
    private bool TryReadStatuePixels(out Color32[] pixels, out int width, out int height)
    {
        pixels = null;
        width = 0;
        height = 0;
        if (_statueRenderer == null || _statueRenderer.sprite == null)
        {
            return false;
        }

        Sprite sprite = _statueRenderer.sprite;
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = null;
        Texture2D readable = null;
        try
        {
            if (sprite.packed && sprite.packingRotation != SpritePackingRotation.None)
            {
                Debug.LogWarning("[ExpeditionStatue] 회전되어 패킹된 스프라이트는 색을 읽지 못해 알갱이를 단색으로 만듭니다.", this);
                return false;
            }

            Texture2D texture = sprite.texture;
            Rect rect = sprite.textureRect;
            _sampleOffset = sprite.textureRectOffset;
            target = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            Graphics.Blit(texture, target);
            RenderTexture.active = target;
            readable = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(rect.x, rect.y, rect.width, rect.height), 0, 0);
            pixels = readable.GetPixels32();
            width = readable.width;
            height = readable.height;
            return true;
        }
        catch (Exception exception)
        {
            pixels = null;
            Debug.LogWarning($"[ExpeditionStatue] 석상 색을 읽지 못해 알갱이를 단색으로 만듭니다. {exception.Message}", this);
            return false;
        }
        finally
        {
            RenderTexture.active = previous;
            if (target != null)
            {
                RenderTexture.ReleaseTemporary(target);
            }

            if (readable != null)
            {
                Destroy(readable);
            }
        }
    }

    /// <summary>월드 좌표에 해당하는 석상 스프라이트의 색을 구한다. 투명한 곳이면 false.</summary>
    private bool TrySampleStatueColor(Vector3 worldPosition, Color32[] pixels, int width, int height, out Color32 color)
    {
        color = default;
        Sprite sprite = _statueRenderer.sprite;
        Vector3 local = _statueRenderer.transform.InverseTransformPoint(worldPosition);
        if (_statueRenderer.flipX)
        {
            local.x = -local.x;
        }

        if (_statueRenderer.flipY)
        {
            local.y = -local.y;
        }

        // 읽어 온 배열은 textureRect 영역이므로, 원래 스프라이트 기준 좌표에서 오프셋을 뺀다.
        int x = Mathf.FloorToInt(local.x * sprite.pixelsPerUnit + sprite.pivot.x - _sampleOffset.x);
        int y = Mathf.FloorToInt(local.y * sprite.pixelsPerUnit + sprite.pivot.y - _sampleOffset.y);
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return false;
        }

        color = pixels[y * width + x];
        return color.a >= MinGrainAlpha;
    }

    /// <summary>풀려 나간 띠(lowerY~upperY)에서 알갱이를 만든다. 알갱이 색은 그 자리의 비석 색이다.</summary>
    private void SpawnGrains(int count, Bounds bounds, float lowerY, float upperY, bool hasPixels, Color32[] pixels,
        int pixelWidth, int pixelHeight, Color baseColor)
    {
        if (_grains == null)
        {
            return;
        }

        float z = transform.position.z;
        for (int i = 0; i < count && _grainActive < _grains.Length; i++)
        {
            Vector3 position = default;
            Color32 color = baseColor;
            bool found = false;
            for (int attempt = 0; attempt < GrainSampleAttempts && !found; attempt++)
            {
                position = new Vector3(Random.Range(bounds.min.x, bounds.max.x), Random.Range(lowerY, upperY), z);
                if (!hasPixels)
                {
                    found = true;
                }
                else if (TrySampleStatueColor(position, pixels, pixelWidth, pixelHeight, out Color32 sampled))
                {
                    color = (Color)sampled * baseColor;
                    found = true;
                }
            }

            if (!found)
            {
                continue;
            }

            color.a = 255;
            float groundY = bounds.min.y + Random.Range(0f, _groundJitter);
            int index = _grainActive++;
            _grains[index] = new GrainState
            {
                Position = position,
                Velocity = new Vector2(Random.Range(-1f, 1f) * _grainSpread, Random.Range(-0.3f, 0.1f)),
                // 생성된 자리보다 높은 곳에는 쌓이지 않는다. (알갱이가 위로 튀어 오르지 않게)
                GroundY = Mathf.Min(groundY, position.y),
                Color = color,
            };

            _grainParticles[index] = new ParticleSystem.Particle
            {
                position = position,
                startColor = color,
                startSize = Random.Range(_grainSize.x, _grainSize.y),
                startLifetime = GrainLifetime,
                remainingLifetime = GrainLifetime,
            };
        }
    }

    /// <summary>알갱이를 모래처럼 떨어뜨린다. 바닥에 닿으면 쌓여 있다가 fade가 올라가면 사라진다.</summary>
    private void UpdateGrains(float deltaTime, float fade)
    {
        if (_grainSystem == null || _grainActive == 0)
        {
            return;
        }

        byte alpha = (byte)Mathf.RoundToInt(255f * (1f - fade));
        for (int i = 0; i < _grainActive; i++)
        {
            ref GrainState grain = ref _grains[i];
            if (!grain.IsResting)
            {
                grain.Velocity.y -= _grainGravity * deltaTime;
                grain.Velocity.x *= Mathf.Exp(-GrainDrag * deltaTime);
                grain.Position += (Vector3)(grain.Velocity * deltaTime);
                if (grain.Position.y <= grain.GroundY)
                {
                    grain.Position.y = grain.GroundY;
                    grain.IsResting = true;
                }

                _grainParticles[i].position = grain.Position;
            }

            _grainParticles[i].remainingLifetime = GrainLifetime;
            if (fade > 0f)
            {
                _grainParticles[i].startColor = new Color32(grain.Color.r, grain.Color.g, grain.Color.b, alpha);
            }
        }

        _grainSystem.SetParticles(_grainParticles, _grainActive);
    }

    private Particle CreateDust(Bounds bounds, float y)
    {
        var go = new GameObject("StatueDust");
        go.transform.SetParent(_effectRoot.transform, false);
        float x = Mathf.Lerp(bounds.min.x, bounds.max.x, Random.value);
        go.transform.position = new Vector3(x, y, transform.position.z);
        float size = Random.Range(_dustSize.x, _dustSize.y);
        go.transform.localScale = Vector3.one * size;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = _dustSprite;
        renderer.sortingLayerID = SortingLayerId;
        renderer.sortingOrder = SortingOrder + 2;
        renderer.color = new Color(_dustColor.r, _dustColor.g, _dustColor.b, 0f);

        float side = (x - bounds.center.x) / Mathf.Max(0.01f, bounds.extents.x);
        return new Particle
        {
            Transform = go.transform,
            Renderer = renderer,
            Velocity = new Vector2(side * Random.Range(0.8f, 2f), Random.Range(0.2f, 0.9f)),
            Life = _dustLifetime * Random.Range(0.7f, 1f),
            BaseScale = go.transform.localScale,
        };
    }

    private void UpdateDust(List<Particle> dusts, float deltaTime)
    {
        foreach (Particle dust in dusts)
        {
            if (dust.Transform == null)
            {
                continue;
            }

            dust.Age += deltaTime;
            float t = Mathf.Clamp01(dust.Age / dust.Life);
            dust.Velocity *= Mathf.Exp(-2.5f * deltaTime);
            dust.Transform.position += (Vector3)(dust.Velocity * deltaTime);
            dust.Transform.localScale = dust.BaseScale * Mathf.Lerp(0.7f, 1.6f, 1f - (1f - t) * (1f - t));

            Color color = _dustColor;
            color.a *= Mathf.Min(1f, t / 0.15f) * (1f - t);
            dust.Renderer.color = color;
        }
    }
}
