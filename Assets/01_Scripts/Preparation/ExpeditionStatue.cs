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
/// - 원소 선택을 취소하면 석상은 그대로 있고, 다시 상호작용할 수 있다.
/// - 원소를 고른 뒤 열리는 룬북은 닫기 키(Tab, Esc)로 닫을 수 없고 Confirm해야만 닫힌다.
///   (원소만 장착된 채 룬북을 빠져나가 미션이 시작되지 않는 상태를 막는다)
/// - 씬이 열리면 원정(지갑, 시작 골드)을 시작한다. (플레이어 시작 위치는 PlayerStartPositioner가 정한다)
///   플레이어는 석상 앞 공중에 열린 균열에서 튀어나온다. (PortalArrival)
/// - 1층은 StageManager가 기다리고 있다가(_waitForPreparation) 석상이 무너진 뒤 BeginPreparedRun으로 시작한다.
/// 무너지는 연출은 게임 시간으로 움직인다. (일시정지하면 멈춘다)
/// 무너질 때는 비석 윗부분부터 알갱이로 풀려 모래처럼 떨어지며, 비석이 위에서 아래로 점점 낮아진다.
///
/// 이 클래스는 흐름(상호작용, 붕괴 순서, 중단 처리)만 맡고, 세부는 두 클래스에 맡긴다.
/// - CircleGlow : 바닥 마법진의 근접 발광
/// - StatueCrumbleEffect : 마스크, 알갱이, 먼지로 이루어진 붕괴 연출
/// </summary>
[DisallowMultipleComponent]
public class ExpeditionStatue : MonoBehaviour, IInteractable
{
    private const string PlayerTag = "Player";

    [Header("상호작용")]
    [SerializeField] private string _interactionName = "원소 새기기";
    [SerializeField] private Transform _interactionGuidePoint;
    [Tooltip("씬이 열린 뒤 균열이 열리기까지 (화면이 밝아지는 동안 기다린다)")]
    [SerializeField, Min(0f)] private float _arrivalDelay = 0.5f;

    [Header("UI")]
    [SerializeField] private PopupController _popupController;
    [SerializeField] private MainElementSelectView _elementSelectView;
    [SerializeField] private RuneBookV2Toggle _runeBookToggle;
    [Tooltip("룬북 화면 루트. 비워두면 _runeBookPresenter를 직접 넣는다")]
    [SerializeField] private GameObject _runeBookRoot;
    [SerializeField] private RuneBookV2Presenter _runeBookPresenter;

    [Header("석상")]
    [SerializeField] private SpriteRenderer _statueRenderer;
    [Tooltip("석상과 함께 사라지는 것 (바닥 마법진 등)")]
    [SerializeField] private SpriteRenderer[] _fadeOutRenderers;
    [SerializeField] private Collider2D[] _colliders;

    [Header("마법진 근접 발광")]
    [SerializeField] private SpriteRenderer _circleBaseRenderer;
    [SerializeField] private SpriteRenderer _circleGlowRenderer;
    [SerializeField] private SpriteRenderer _circleAuraRenderer;
    [SerializeField, Min(0f)] private float _glowActivationDistance = 2f;
    [SerializeField, Range(0f, 1f)] private float _glowAlpha = 0.65f;
    [FormerlySerializedAs("_glowFadeDuration")]
    [SerializeField, Min(0.01f)] private float _glowExpandDuration = 2f;

    [Header("무너지기 전 흔들림")]
    [SerializeField, Min(0f)] private float _shakeDuration = 0.7f;
    [SerializeField] private float _shakeAmount = 0.07f;
    [Tooltip("무너지기 직전 석상이 빛나는 색")]
    [SerializeField] private Color _flashColor = new(0.8f, 0.92f, 1f, 1f);

    [Header("알갱이 (위에서부터 풀려 모래처럼 떨어진다)")]
    [Tooltip("비석이 위에서 아래까지 다 풀리는 데 걸리는 시간")]
    [SerializeField, Min(0.1f)] private float _crumbleDuration = 2f;
    [Tooltip("무너지는 동안 떨어지는 알갱이의 총 개수")]
    [SerializeField, Min(0)] private int _grainCount = 1400;
    [Tooltip("알갱이 한 변의 크기 (월드 단위, 최소~최대)")]
    [SerializeField] private Vector2 _grainSize = new(0.025f, 0.055f);
    [SerializeField] private float _grainGravity = 8f;
    [Tooltip("알갱이가 풀릴 때 옆으로 흩어지는 세기")]
    [SerializeField] private float _grainSpread = 0.6f;
    [Tooltip("알갱이가 떨어져 쌓이는 바닥 높이의 흩어짐 (석상 밑동 기준)")]
    [SerializeField] private float _groundJitter = 0.5f;
    [Tooltip("다 무너진 뒤 쌓인 알갱이가 사라지는 시간")]
    [SerializeField, Min(0.01f)] private float _grainFadeDuration = 0.8f;

    [Header("먼지")]
    [SerializeField] private Sprite _dustSprite;
    [SerializeField] private Color _dustColor = new(0.78f, 0.74f, 0.7f, 0.6f);
    [SerializeField, Min(0)] private int _dustCount = 14;
    [SerializeField] private Vector2 _dustSize = new(0.8f, 1.8f);
    [SerializeField, Min(0.1f)] private float _dustLifetime = 1.1f;

    [Header("룬북 Confirm 뒤")]
    [Tooltip("룬북 Confirm 연출(장착 칸이 차례로 빛남)을 보여준 뒤 룬북을 닫기까지 (실제 시간)")]
    [SerializeField, Min(0f)] private float _runeBookCloseDelay = 0.55f;

    [Header("이후")]
    [SerializeField, Min(0f)] private float _beginMissionDelay = 0.6f;
    [Tooltip("미션 오브젝트가 서서히 나타나는 시간")]
    [SerializeField, Min(0.01f)] private float _missionFadeDuration = 1.4f;
    [SerializeField, Min(0.01f)] private float _circleFadeDuration = 1f;

    /// <summary>흔들리기 직전 석상의 상태. 연출이 시작 전에 끊기면 이 상태로 되돌린다.</summary>
    private readonly struct StatueSnapshot
    {
        public readonly Transform Transform;
        public readonly Vector3 Position;
        public readonly Color Color;

        public StatueSnapshot(Transform transform, Vector3 position, Color color)
        {
            Transform = transform;
            Position = position;
            Color = color;
        }
    }

    // 상호작용 흐름. 서로 독립이라 하나의 열거형으로 합치지 않는다.
    private bool _isChoosingElement;
    private bool _isWaitingRuneConfirm;

    // 붕괴 흐름
    private bool _isCrumbling;
    private bool _runBegun;
    private bool _crumbleFinished;
    private Coroutine _crumbleCoroutine;
    private StatueCrumbleEffect _crumbleEffect;
    private StatueSnapshot? _statueSnapshot;
    private float[] _circleBaseAlphas;

    private Transform _player;
    private CircleGlow _circleGlow;

    /// <summary>플레이어가 석상과 상호작용해 주력 원소 선택 창을 열었을 때</summary>
    public event Action Interacted;

    public string InteractionName => _interactionName;
    public Transform InteractionGuidePoint => _interactionGuidePoint != null ? _interactionGuidePoint : transform;

    // 도메인 리로드 등으로 사라져도 다시 만들 수 있게 필요할 때 만든다.
    // 접근할 때마다 인스펙터의 발광 값을 다시 넘겨서, 플레이 중에 바꾼 값도 바로 반영된다.
    private CircleGlow Glow
    {
        get
        {
            _circleGlow ??= new CircleGlow(_fadeOutRenderers, _circleBaseRenderer, _circleGlowRenderer,
                _circleAuraRenderer);
            _circleGlow.Configure(_glowActivationDistance, _glowAlpha, _glowExpandDuration);
            return _circleGlow;
        }
    }

    private void Awake()
    {
        // 원정을 시작한다. (지갑과 시작 골드. 이미 원정 중이면 아무것도 하지 않는다)
        RunFlow.StartExpedition();
        Glow.SetProgress(0f);

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
            PortalArrival.PlayFor(player, _arrivalDelay);
        }
    }

    private void Update()
    {
        if (_isCrumbling)
        {
            return;
        }

        CircleGlow glow = Glow;
        if (glow.IsAvailable)
        {
            glow.Tick(_player, Time.deltaTime);
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
        if (_runeBookToggle != null)
        {
            _runeBookToggle.IsCloseLocked = false;
        }
        _circleGlow?.SetProgress(0f);

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

    // ───────────────────────── 상호작용 ─────────────────────────

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
            // Confirm 전에는 룬북을 빠져나갈 수 없게 한다.
            _runeBookToggle.IsCloseLocked = true;
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
            _runeBookToggle.IsCloseLocked = false;
            _runeBookToggle.Close();
        }

        yield return CrumbleRoutine();
    }

    // ───────────────────────── 무너짐 ─────────────────────────

    private IEnumerator CrumbleRoutine()
    {
        _isCrumbling = true;
        SetCollidersEnabled(false);

        // 1. 점점 세게 흔들리며 빛난다.
        Transform statue = _statueRenderer != null ? _statueRenderer.transform : transform;
        Color baseColor = _statueRenderer != null ? _statueRenderer.color : Color.white;
        _statueSnapshot = new StatueSnapshot(statue, statue.localPosition, baseColor);
        yield return ShakeRoutine(statue, statue.localPosition, baseColor);

        // 2. 비석이 위에서부터 알갱이로 풀리며 낮아진다. (세부는 StatueCrumbleEffect)
        _crumbleEffect = new StatueCrumbleEffect(BuildCrumbleSettings(), _statueRenderer, transform);
        _crumbleEffect.Begin(baseColor, _flashColor);
        float[] circleAlphas = CaptureCircleAlphas();
        _circleBaseAlphas = circleAlphas;

        // 3. 풀리는 동안 바닥 마법진을 지우고, 1층을 준비하고, 미션 오브젝트를 서서히 보여준다.
        bool hasBegun = false;
        float collapsedElapsed = 0f;
        float total = Mathf.Max(_crumbleEffect.Duration, _circleFadeDuration + _beginMissionDelay, _circleFadeDuration);
        float elapsed = 0f;

        while (elapsed < total)
        {
            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;

            _crumbleEffect.Tick(elapsed, deltaTime);
            FadeCircles(circleAlphas, elapsed);

            if (!hasBegun && _crumbleEffect.IsCollapsed)
            {
                collapsedElapsed += deltaTime;

                if (collapsedElapsed >= _beginMissionDelay)
                {
                    hasBegun = true;
                    BeginRun();
                }
            }

            yield return null;
        }

        if (!hasBegun)
        {
            BeginRun();
        }

        _crumbleEffect.Finish();
        _crumbleEffect = null;
        _crumbleFinished = true;

        // 미션 오브젝트가 다 나타난 뒤 석상을 치운다.
        yield return new WaitForSeconds(_missionFadeDuration);
        Destroy(gameObject);
    }

    /// <summary>석상이 점점 세게 흔들리며 빛난다.</summary>
    private IEnumerator ShakeRoutine(Transform statue, Vector3 basePosition, Color baseColor)
    {
        float elapsed = 0f;
        while (elapsed < _shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _shakeDuration);
            float amount = _shakeAmount * t;
            statue.localPosition = basePosition +
                                   new Vector3(Random.Range(-amount, amount), Random.Range(-amount, amount) * 0.4f, 0f);
            if (_statueRenderer != null)
            {
                _statueRenderer.color = Color.Lerp(baseColor, _flashColor, t * t);
            }

            yield return null;
        }

        statue.localPosition = basePosition;
    }

    private StatueCrumbleEffect.Settings BuildCrumbleSettings()
    {
        return new StatueCrumbleEffect.Settings
        {
            Duration = _crumbleDuration,
            GrainCount = _grainCount,
            GrainSize = _grainSize,
            GrainGravity = _grainGravity,
            GrainSpread = _grainSpread,
            GroundJitter = _groundJitter,
            GrainFadeDuration = _grainFadeDuration,
            DustSprite = _dustSprite,
            DustColor = _dustColor,
            DustCount = _dustCount,
            DustSize = _dustSize,
            DustLifetime = _dustLifetime,
        };
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        foreach (Collider2D target in _colliders)
        {
            if (target != null)
            {
                target.enabled = isEnabled;
            }
        }
    }

    /// <summary>석상과 함께 사라지는 렌더러들의 지금 알파를 기록한다.</summary>
    private float[] CaptureCircleAlphas()
    {
        float[] alphas = new float[_fadeOutRenderers?.Length ?? 0];
        for (int i = 0; i < alphas.Length; i++)
        {
            alphas[i] = _fadeOutRenderers[i] != null ? _fadeOutRenderers[i].color.a : 0f;
        }

        return alphas;
    }

    /// <summary>바닥 마법진 등을 원래 알파에서 서서히 지운다.</summary>
    private void FadeCircles(float[] baseAlphas, float elapsed)
    {
        float circleT = Mathf.Clamp01(elapsed / _circleFadeDuration);
        for (int i = 0; i < baseAlphas.Length; i++)
        {
            SpriteRenderer circle = _fadeOutRenderers[i];
            if (circle != null)
            {
                SetAlpha(circle, baseAlphas[i] * (1f - circleT));
            }
        }
    }

    // ───────────────────────── 중단 처리 ─────────────────────────

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

        _crumbleEffect?.Cleanup();
        _crumbleEffect = null;
        if (!_isCrumbling || _crumbleFinished)
        {
            return;
        }

        if (_runBegun)
        {
            HideStatueLeftovers();
            return;
        }

        RestoreStatue();
        _isCrumbling = false;
        _isWaitingRuneConfirm = false;
        _isChoosingElement = false;
    }

    /// <summary>1층이 이미 시작됐으면 석상은 사라진 채로 둔다. 마스크를 지웠으니 반쯤 보이거나 반투명하게 남지 않게 숨긴다.</summary>
    private void HideStatueLeftovers()
    {
        if (_statueRenderer != null)
        {
            _statueRenderer.enabled = false;
        }

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
    }

    /// <summary>1층이 시작되기 전에 끊겼을 때 석상, 마법진, 콜라이더를 흔들리기 전 상태로 돌린다.</summary>
    private void RestoreStatue()
    {
        if (_statueSnapshot.HasValue && _statueSnapshot.Value.Transform != null)
        {
            _statueSnapshot.Value.Transform.localPosition = _statueSnapshot.Value.Position;
        }

        if (_statueRenderer != null)
        {
            _statueRenderer.maskInteraction = SpriteMaskInteraction.None;
            _statueRenderer.enabled = true;
            if (_statueSnapshot.HasValue)
            {
                _statueRenderer.color = _statueSnapshot.Value.Color;
            }
        }

        // 연출이 시작되면서 줄어든 마법진 알파를 되돌린다. (근접 발광은 CircleGlow가 다시 맞춘다)
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
        SetCollidersEnabled(true);
    }

    // ───────────────────────── 1층 시작 ─────────────────────────

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
}