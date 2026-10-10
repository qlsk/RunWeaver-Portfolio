using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 시작 석상. 전투 씬 1층 가운데에 서 있다. (마을 씬을 대신한다)
/// 흐름 : 상호작용(F) → 주력 원소 선택 → 룬북이 열려 보조 룬 워드 선택 → Confirm
///        → 석상이 흔들리다 무너진다 → 1층 미션 오브젝트가 서서히 나타난다 → 미션 시작 오브젝트로 시작
/// - 원소 선택을 취소하거나 룬북을 Confirm 없이 닫으면 석상은 그대로 있고, 다시 상호작용할 수 있다.
/// - 씬이 열리면 원정(지갑, 시작 골드)을 시작하고 플레이어를 석상 앞으로 옮긴다.
///   플레이어는 석상 앞 공중에 열린 균열에서 튀어나온다. (PortalArrival)
/// - 1층은 StageManager가 기다리고 있다가(_waitForPreparation) 석상이 무너진 뒤 BeginPreparedRun으로 시작한다.
/// 무너지는 연출은 게임 시간으로 움직인다. (일시정지하면 멈춘다)
/// </summary>
[DisallowMultipleComponent]
public class ExpeditionStatue : MonoBehaviour, IInteractable
{
    private const string PlayerTag = "Player";
    private static readonly int RevealProgressId = Shader.PropertyToID("_RevealProgress");
    private static readonly int GlowTimeId = Shader.PropertyToID("_GlowTime");

    [Header("상호작용")]
    [SerializeField] private string _interactionName = "원소 새기기";
    [SerializeField] private Transform _interactionGuidePoint;
    [Tooltip("씬이 열릴 때 플레이어를 놓을 위치 (석상 기준)")]
    [SerializeField] private Vector3 _playerStartOffset = new(0f, -3f, 0f);
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

    [Header("조각")]
    [SerializeField, Min(1)] private int _fragmentColumns = 5;
    [SerializeField, Min(1)] private int _fragmentRows = 8;
    [SerializeField, Min(0.1f)] private float _fragmentLifetime = 1.6f;
    [SerializeField] private float _gravity = 14f;
    [Tooltip("조각이 옆으로 퍼지는 세기")]
    [SerializeField] private float _spread = 2.4f;
    [Tooltip("조각이 처음 위로 튀는 세기 (최대)")]
    [SerializeField] private float _pop = 2.5f;
    [Tooltip("조각이 떨어져 멈추는 바닥 높이의 흩어짐 (석상 밑동 기준)")]
    [SerializeField] private float _groundJitter = 0.5f;

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
    [Tooltip("무너지기 시작하고 1층 미션을 준비하기까지")]
    [SerializeField, Min(0f)] private float _beginRunDelay = 0.6f;
    [Tooltip("미션 오브젝트가 서서히 나타나는 시간")]
    [SerializeField, Min(0.01f)] private float _missionFadeDuration = 1.4f;
    [SerializeField, Min(0.01f)] private float _circleFadeDuration = 1f;

    private bool _isChoosingElement;
    private bool _isWaitingRuneConfirm;
    private bool _isCrumbling;
    private Transform _player;
    private MaterialPropertyBlock _circleGlowProperties;
    private float _glowProgress;
    private float _glowTime;

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
            player.transform.position = transform.position + _playerStartOffset;
            PortalArrival.PlayFor(player, _arrivalDelay);
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
        StartCoroutine(CloseRuneBookThenCrumble());
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
        Vector3 basePosition = statue.localPosition;
        Color baseColor = _statueRenderer != null ? _statueRenderer.color : Color.white;
        float elapsed = 0f;
        while (elapsed < _shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _shakeDuration);
            float amount = _shakeAmount * t;
            statue.localPosition = basePosition + new Vector3(Random.Range(-amount, amount), Random.Range(-amount, amount) * 0.4f, 0f);
            if (_statueRenderer != null)
            {
                _statueRenderer.color = Color.Lerp(baseColor, _flashColor, t * t);
            }

            yield return null;
        }

        statue.localPosition = basePosition;

        // 2. 조각으로 부서지고 먼지가 인다.
        List<Fragment> fragments = SpawnFragments();
        List<Fragment> dusts = SpawnDust();
        if (_statueRenderer != null)
        {
            _statueRenderer.enabled = false;
        }

        float[] circleAlphas = new float[_fadeOutRenderers?.Length ?? 0];
        for (int i = 0; i < circleAlphas.Length; i++)
        {
            circleAlphas[i] = _fadeOutRenderers[i] != null ? _fadeOutRenderers[i].color.a : 0f;
        }

        // 3. 조각이 떨어지는 동안 1층을 준비하고, 미션 오브젝트를 서서히 보여준다.
        bool hasBegun = false;
        float total = Mathf.Max(_fragmentLifetime, _dustLifetime, _circleFadeDuration);
        elapsed = 0f;
        while (elapsed < total)
        {
            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;

            UpdateFragments(fragments, deltaTime, elapsed, _fragmentLifetime);
            UpdateDust(dusts, deltaTime, elapsed);

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

        foreach (Fragment fragment in fragments) DestroyFragment(fragment);
        foreach (Fragment dust in dusts) DestroyFragment(dust);

        // 미션 오브젝트가 다 나타난 뒤 석상을 치운다.
        yield return new WaitForSeconds(_missionFadeDuration);
        Destroy(gameObject);
    }

    private void BeginRun()
    {
        StageManager stageManager = StageManager.Instance;
        if (stageManager == null)
        {
            Debug.LogWarning("[ExpeditionStatue] StageManager가 없어 1층을 시작할 수 없습니다.", this);
            return;
        }

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

    // ───────────────────────── 조각과 먼지 ─────────────────────────

    private class Fragment
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Vector2 Velocity;
        public float Spin;
        public float GroundY;
        public bool IsResting;
        public float Life;
        public Vector3 BaseScale;
    }

    /// <summary>석상 그림을 격자로 잘라 조각을 만든다. 조각은 석상 그림의 같은 부분을 그대로 쓴다.</summary>
    private List<Fragment> SpawnFragments()
    {
        List<Fragment> fragments = new();
        if (_statueRenderer == null || _statueRenderer.sprite == null)
        {
            return fragments;
        }

        Sprite sprite = _statueRenderer.sprite;
        Rect source = sprite.textureRect;
        Bounds bounds = _statueRenderer.bounds;
        Vector3 scale = _statueRenderer.transform.lossyScale;
        float cellWidth = source.width / _fragmentColumns;
        float cellHeight = source.height / _fragmentRows;
        float bottom = bounds.min.y;

        for (int row = 0; row < _fragmentRows; row++)
        {
            for (int column = 0; column < _fragmentColumns; column++)
            {
                Rect rect = new(source.x + column * cellWidth, source.y + row * cellHeight, cellWidth, cellHeight);
                Sprite piece = Sprite.Create(sprite.texture, rect, new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);

                var go = new GameObject("StatueFragment");
                go.transform.position = new Vector3(
                    Mathf.Lerp(bounds.min.x, bounds.max.x, (column + 0.5f) / _fragmentColumns),
                    Mathf.Lerp(bounds.min.y, bounds.max.y, (row + 0.5f) / _fragmentRows),
                    transform.position.z);
                go.transform.localScale = scale;

                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = piece;
                renderer.sharedMaterial = _statueRenderer.sharedMaterial;
                renderer.sortingLayerID = _statueRenderer.sortingLayerID;
                renderer.sortingOrder = _statueRenderer.sortingOrder;
                renderer.color = _flashColor;

                // 가운데에서 먼 조각일수록 옆으로 멀리, 위쪽 조각일수록 높이 튄다.
                float side = (column + 0.5f) / _fragmentColumns * 2f - 1f;
                float height = (row + 0.5f) / _fragmentRows;
                fragments.Add(new Fragment
                {
                    Transform = go.transform,
                    Renderer = renderer,
                    Velocity = new Vector2(side * _spread + Random.Range(-0.6f, 0.6f), Random.Range(0.2f, 1f) * _pop * height),
                    Spin = Random.Range(-260f, 260f),
                    GroundY = bottom + Random.Range(0f, _groundJitter) + cellHeight / sprite.pixelsPerUnit * scale.y * 0.4f,
                    Life = _fragmentLifetime * Random.Range(0.85f, 1f),
                    BaseScale = scale,
                });
            }
        }

        return fragments;
    }

    private void UpdateFragments(List<Fragment> fragments, float deltaTime, float elapsed, float lifetime)
    {
        foreach (Fragment fragment in fragments)
        {
            if (fragment.Transform == null)
            {
                continue;
            }

            if (!fragment.IsResting)
            {
                fragment.Velocity.y -= _gravity * deltaTime;
                Vector3 position = fragment.Transform.position + (Vector3)(fragment.Velocity * deltaTime);
                if (fragment.Velocity.y < 0f && position.y <= fragment.GroundY)
                {
                    // 바닥에 닿으면 한 번 작게 튀고 멈춘다.
                    position.y = fragment.GroundY;
                    if (Mathf.Abs(fragment.Velocity.y) > 2f)
                    {
                        fragment.Velocity = new Vector2(fragment.Velocity.x * 0.4f, -fragment.Velocity.y * 0.25f);
                        fragment.Spin *= 0.4f;
                    }
                    else
                    {
                        fragment.IsResting = true;
                    }
                }

                fragment.Transform.position = position;
                fragment.Transform.Rotate(0f, 0f, fragment.Spin * deltaTime);
            }

            // 빛이 빠지며 돌 색으로 돌아오고, 끝에는 작아지며 사라진다.
            float t = Mathf.Clamp01(elapsed / fragment.Life);
            Color color = Color.Lerp(_flashColor, Color.white, Mathf.Clamp01(t * 4f));
            color.a = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;
            fragment.Renderer.color = color;
            if (t > 0.65f)
            {
                fragment.Transform.localScale = fragment.BaseScale * Mathf.Lerp(1f, 0.6f, (t - 0.65f) / 0.35f);
            }
        }
    }

    private List<Fragment> SpawnDust()
    {
        List<Fragment> dusts = new();
        if (_dustSprite == null || _statueRenderer == null)
        {
            return dusts;
        }

        Bounds bounds = _statueRenderer.bounds;
        for (int i = 0; i < _dustCount; i++)
        {
            var go = new GameObject("StatueDust");
            float x = Mathf.Lerp(bounds.min.x, bounds.max.x, Random.value);
            float y = bounds.min.y + Random.Range(0f, bounds.size.y * 0.35f);
            go.transform.position = new Vector3(x, y, transform.position.z);
            float size = Random.Range(_dustSize.x, _dustSize.y);
            go.transform.localScale = Vector3.one * size;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _dustSprite;
            renderer.sortingLayerID = _statueRenderer.sortingLayerID;
            renderer.sortingOrder = _statueRenderer.sortingOrder + 1;
            renderer.color = new Color(_dustColor.r, _dustColor.g, _dustColor.b, 0f);

            float side = (x - bounds.center.x) / Mathf.Max(0.01f, bounds.extents.x);
            dusts.Add(new Fragment
            {
                Transform = go.transform,
                Renderer = renderer,
                Velocity = new Vector2(side * Random.Range(0.8f, 2f), Random.Range(0.2f, 0.9f)),
                Life = _dustLifetime * Random.Range(0.7f, 1f),
                BaseScale = go.transform.localScale,
            });
        }

        return dusts;
    }

    private void UpdateDust(List<Fragment> dusts, float deltaTime, float elapsed)
    {
        foreach (Fragment dust in dusts)
        {
            if (dust.Transform == null)
            {
                continue;
            }

            float t = Mathf.Clamp01(elapsed / dust.Life);
            dust.Velocity *= Mathf.Exp(-2.5f * deltaTime);
            dust.Transform.position += (Vector3)(dust.Velocity * deltaTime);
            dust.Transform.localScale = dust.BaseScale * Mathf.Lerp(0.7f, 1.6f, 1f - (1f - t) * (1f - t));

            Color color = _dustColor;
            color.a *= Mathf.Min(1f, t / 0.15f) * (1f - t);
            dust.Renderer.color = color;
        }
    }

    private static void DestroyFragment(Fragment fragment)
    {
        if (fragment.Transform != null)
        {
            Destroy(fragment.Transform.gameObject);
        }
    }
}
