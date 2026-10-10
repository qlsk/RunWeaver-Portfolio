using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 미션 클리어 후 등장하는 포탈(공중의 균열). 사용하면 한 층 내려간다. (귀환, 여러 층 하강 포탈은 폐기)
/// PortalSpawner가 Setup으로 문구를 정한 뒤 PlayFadeIn으로 등장시킨다.
/// - 등장 : 빛 점 → 금이 퍼짐 → 찢어지며 열림 (_openFrames). 끝나야 Collider가 켜져 F 상호작용이 가능하다.
/// - 대기 : _idleFrames를 반복한다. 아래의 F 안내 텍스트(Prompt)는 준비되면 항상 보인다.
/// - 들어가기 : 플레이어(그림 복사본)가 균열로 빨려 들어간 뒤 균열이 닫히고(_closeFrames) 한 층 내려간다.
///   빨려 드는 동안 진짜 플레이어는 숨기고 조작을 막는다. 다음 층 화면이 걷히기 시작하면 되돌린다. (PortalPlayerHider)
/// - 위쪽 문구(Label)는 쓰지 않는다. (Setup은 호환용으로 남긴다)
/// 플레이어가 상호작용 대상으로 잡으면 밝기가 천천히 맥동한다.
///
/// 프리팹 구성 (Label과 InteractionGuidePoint는 에디터에서 위치/크기를 바로 조정한다)
/// Portal (이 스크립트, Collider)
///  ├ Visual                : SpriteRenderer. FadeIn / 맥동 크기 변화는 이 오브젝트에만 적용한다.
///  ├ Label                 : TextMeshPro (월드 텍스트). 내용과 색은 종류에 따라 코드에서 바꾼다.
///  └ Prompt                : TextMeshPro. "F - Enter" 안내. 가까이 갔을 때만 보인다.
/// </summary>
[DisallowMultipleComponent]
public class Portal : MonoBehaviour, IInteractable
{
    // 기존 테스트 씬(ReturnConfirmPopupUI, StageInfoPanelUI) 호환용 정적 이벤트
    public static event Action<Portal> OnPlayerEntered;
    public static event Action<Portal> OnPlayerExited;

    private const string InteractionText = "들어가기";
    private const string DefaultLabelText = "하강";

    [Header("References")]
    [Tooltip("포탈 이미지 오브젝트. 크기 연출은 여기에만 적용된다")][SerializeField] private Transform _visual;
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private Collider2D _collider;
    [SerializeField] private TextMeshPro _label;
    [Tooltip("가까이 갔을 때만 보이는 F 안내 텍스트")][SerializeField] private TextMeshPro _prompt;

    [Header("Colors (포탈 / 텍스트)")]
    [SerializeField] private Color _descendColor = Color.white;
    [SerializeField] private Color _descendLabelColor = new(1f, 0.95f, 0.85f, 1f);

    [Header("Fade In (프레임이 없을 때만)")]
    [SerializeField, Min(0f)] private float _fadeInDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float _fadeInStartScale = 0.8f;

    [Header("균열 프레임")]
    [SerializeField] private Sprite[] _openFrames;
    [SerializeField] private Sprite[] _idleFrames;
    [SerializeField] private Sprite[] _closeFrames;
    [SerializeField, Min(1f)] private float _framesPerSecond = 12f;

    [Header("들어가기")]
    [Tooltip("플레이어가 균열 가운데로 빨려 드는 시간")]
    [SerializeField, Min(0.05f)] private float _pullDuration = 0.9f;
    [Tooltip("균열 가운데 (포탈 기준, 바닥에서의 높이)")]
    [SerializeField] private Vector2 _riftCenterOffset = new(0f, 2.1f);
    [SerializeField] private Color _pullTint = new(0.75f, 0.92f, 1f, 1f);

    [Header("가까이 갔을 때 맥동")]
    [SerializeField, Min(0f)] private float _pulseScale = 0.05f;
    [SerializeField, Range(0f, 1f)] private float _pulseDarken = 0.3f;
    [SerializeField, Min(0f)] private float _pulseSpeed = 2.5f;
    [Tooltip("맥동이 켜지고 꺼질 때 걸리는 시간")][SerializeField, Min(0.01f)] private float _pulseBlendDuration = 0.25f;

    [Header("Gizmo")]
    [SerializeField, FormerlySerializedAs("gizmoColor")] private Color _gizmoColor = new(0f, 1f, 1f, 0.4f);

    private Vector3 _baseScale = Vector3.one;
    private Color _spriteColor = Color.white;
    private Color _labelColor = Color.white;
    private float _alpha = 1f;
    private float _scaleFactor = 1f;
    private bool _isReady;
    private bool _isPlayerInside;
    private bool _isInitialized;
    private Coroutine _fadeRoutine;
    private PlayerInteraction _playerInteraction;
    private bool _isFocused;
    private float _pulseWeight;

    private string _labelText = DefaultLabelText;
    private bool _isEntering;
    private float _idleTime;

    public bool IsReady => _isReady;

    public string InteractionName => InteractionText;
    public Transform InteractionGuidePoint => _prompt != null ? _prompt.transform : transform;

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        ApplySorting();

        // 플레이어가 이 포탈을 가장 가까운 상호작용 대상으로 잡았을 때만 F 안내를 띄운다.
        _playerInteraction = FindAnyObjectByType<PlayerInteraction>();
        if (_playerInteraction != null)
        {
            _playerInteraction.CurrentInteractableChanged += HandleCurrentInteractableChanged;
        }
    }

    private void HandleCurrentInteractableChanged(IInteractable interactable)
    {
        _isFocused = ReferenceEquals(interactable, this);
        RefreshPrompt();
    }

    private void RefreshPrompt()
    {
        if (_prompt != null)
        {
            // 준비되면 항상 보이고, 들어가기 시작하면 끈다.
            _prompt.enabled = _isReady && !_isEntering;
        }
    }

    private bool HasFrames => _idleFrames != null && _idleFrames.Length > 0;

    /// <summary>
    /// 씬의 YSort(10000 - 바닥 y * 100)와 같은 기준으로 균열과 안내 글자의 정렬 순서를 정한다.
    /// 포탈은 움직이지 않으므로 한 번만 정한다.
    /// </summary>
    private void ApplySorting()
    {
        int order = PortalArrival.RiftSortingOrder(transform.position.y);
        if (_sprite != null)
        {
            _sprite.sortingOrder = order;
        }

        if (_prompt != null)
        {
            _prompt.sortingOrder = order + 1;
        }
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        if (_sprite == null)
        {
            _sprite = GetComponentInChildren<SpriteRenderer>();
        }

        if (_visual == null && _sprite != null)
        {
            _visual = _sprite.transform;
        }

        if (_collider == null)
        {
            _collider = GetComponent<Collider2D>();
        }

        if (_label == null)
        {
            _label = GetComponentInChildren<TextMeshPro>();
        }

        // 위쪽 문구는 쓰지 않는다.
        if (_label != null && _label != _prompt)
        {
            _label.gameObject.SetActive(false);
        }

        if (_visual != null)
        {
            _baseScale = _visual.localScale;
        }

        RefreshPrompt();

        ApplyType();
    }

    /// <summary>
    /// 포탈 위에 띄울 문구를 정한다. (예: Descend, Face the Boss)
    /// </summary>
    public void Setup(string label)
    {
        Initialize();
        _labelText = string.IsNullOrEmpty(label) ? DefaultLabelText : label;
        ApplyType();
    }

    /// <summary>
    /// 투명한 상태에서 서서히 나타난다. 끝나면 상호작용이 가능해진다.
    /// </summary>
    public void PlayFadeIn()
    {
        Initialize();

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }

        ApplySorting();
        _fadeRoutine = StartCoroutine(HasFrames ? OpenRoutine() : FadeInRoutine());
    }

    public void Interact()
    {
        if (!_isReady || _isEntering)
        {
            return;
        }

        StageManager stageManager = StageManager.Instance;
        if (stageManager == null)
        {
            Debug.LogWarning("[Portal] StageManager가 없어 포탈을 사용할 수 없습니다.", this);
            return;
        }

        if (!stageManager.CanUsePortal)
        {
            return;
        }

        // 연타로 두 번 들어가지 않도록 바로 막는다.
        _isReady = false;
        _isEntering = true;
        RefreshPrompt();
        SetColliderEnabled(false);

        if (!HasFrames)
        {
            stageManager.UsePortal(this);
            return;
        }

        StartCoroutine(EnterRoutine());
    }

    // ───────────────────────── 균열 프레임 ─────────────────────────

    private IEnumerator OpenRoutine()
    {
        _isReady = false;
        RefreshPrompt();
        SetColliderEnabled(false);
        _alpha = 1f;
        _scaleFactor = 1f;
        ApplyVisual(0f);

        yield return PlayFrames(_openFrames);

        _fadeRoutine = null;
        _idleTime = 0f;
        _isReady = true;
        SetColliderEnabled(true);
        RefreshPrompt();
    }

    private IEnumerator PlayFrames(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0 || _sprite == null)
        {
            yield break;
        }

        float elapsed = 0f;
        float duration = frames.Length / _framesPerSecond;
        while (elapsed < duration)
        {
            int index = Mathf.Min(frames.Length - 1, (int)(elapsed * _framesPerSecond));
            _sprite.sprite = frames[index];
            elapsed += Time.deltaTime;
            yield return null;
        }

        _sprite.sprite = frames[frames.Length - 1];
    }

    private void UpdateIdleFrame()
    {
        if (!HasFrames || _sprite == null)
        {
            return;
        }

        _idleTime += Time.deltaTime;
        _sprite.sprite = _idleFrames[(int)(_idleTime * _framesPerSecond) % _idleFrames.Length];
    }

    /// <summary>플레이어가 빨려 들어간 뒤 균열이 닫히고 한 층 내려간다.</summary>
    private IEnumerator EnterRoutine()
    {
        Vector3 center = transform.position + (Vector3)_riftCenterOffset;
        PortalPlayerHider hider = PortalPlayerHider.Hide(GameObject.FindGameObjectWithTag("Player"));
        List<SpriteRenderer> ghosts = hider != null
            ? hider.CreateGhosts(_sprite != null ? _sprite.sortingOrder + 1 : int.MinValue)
            : new List<SpriteRenderer>();
        Vector3[] starts = new Vector3[ghosts.Count];
        Vector3[] scales = new Vector3[ghosts.Count];
        Color[] colors = new Color[ghosts.Count];
        for (int i = 0; i < ghosts.Count; i++)
        {
            starts[i] = ghosts[i].transform.position;
            scales[i] = ghosts[i].transform.localScale;
            colors[i] = ghosts[i].color;
        }

        // 1. 플레이어가 푸른빛으로 물들며 작아져 균열 가운데로 빨려 든다.
        float elapsed = 0f;
        while (elapsed < _pullDuration)
        {
            elapsed += Time.deltaTime;
            UpdateIdleFrame();
            float t = Mathf.Clamp01(elapsed / _pullDuration);
            float eased = t * t * (3f - 2f * t);
            for (int i = 0; i < ghosts.Count; i++)
            {
                if (ghosts[i] == null)
                {
                    continue;
                }

                Transform ghost = ghosts[i].transform;
                ghost.position = Vector3.Lerp(starts[i], center, eased);
                ghost.localScale = new Vector3(scales[i].x * Mathf.Lerp(1f, 0.15f, eased),
                    scales[i].y * Mathf.Lerp(1f, 0.4f, eased), scales[i].z);
                Color color = Color.Lerp(colors[i], _pullTint, Mathf.Clamp01(eased * 1.3f));
                color.a = colors[i].a * (1f - Mathf.Clamp01((t - 0.8f) / 0.2f));
                ghosts[i].color = color;
            }

            yield return null;
        }

        foreach (SpriteRenderer ghost in ghosts)
        {
            if (ghost != null)
            {
                Destroy(ghost.gameObject);
            }
        }

        // 2. 균열이 하얗게 봉합되며 닫힌다.
        yield return PlayFrames(_closeFrames);
        if (_sprite != null)
        {
            _sprite.enabled = false;
        }

        // 3. 한 층 내려간다. (포탈은 StageManager가 정리한다)
        StageManager stageManager = StageManager.Instance;
        if (stageManager != null)
        {
            stageManager.UsePortal(this);
        }
    }

    private IEnumerator FadeInRoutine()
    {
        _isReady = false;
        RefreshPrompt();
        SetColliderEnabled(false);

        float elapsed = 0f;
        while (elapsed < _fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _fadeInDuration);
            float eased = 1f - (1f - t) * (1f - t);
            _alpha = eased;
            _scaleFactor = Mathf.Lerp(_fadeInStartScale, 1f, eased);
            ApplyVisual(0f);
            yield return null;
        }

        _alpha = 1f;
        _scaleFactor = 1f;
        ApplyVisual(0f);

        _fadeRoutine = null;
        _isReady = true;
        SetColliderEnabled(true);
        RefreshPrompt();
    }

    private void Update()
    {
        if (_fadeRoutine != null || _isEntering)
        {
            return;
        }

        if (_isReady)
        {
            UpdateIdleFrame();
        }

        // 가까이 간 포탈만 맥동한다. 켜지고 꺼질 때는 부드럽게 섞는다.
        float target = _isFocused && _isReady ? 1f : 0f;
        _pulseWeight = Mathf.MoveTowards(_pulseWeight, target, Time.deltaTime / _pulseBlendDuration);
        if (_pulseWeight <= 0f && target <= 0f)
        {
            return;
        }

        float wave = (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f;
        ApplyVisual(wave * _pulseWeight, _pulseWeight);
    }

    private void ApplyType()
    {
        _spriteColor = _descendColor;
        _labelColor = _descendLabelColor;

        if (_label != null)
        {
            _label.text = _labelText;
        }

        ApplyVisual(0f);
    }

    /// <param name="pulse">0~1. 현재 맥동 크기 (맥동하지 않으면 0)</param>
    /// <param name="pulseWeight">0~1. 맥동이 켜진 정도 (밝기 변화 폭에 사용)</param>
    private void ApplyVisual(float pulse, float pulseWeight = 0f)
    {
        float pulseScale = 1f + _pulseScale * pulse;

        if (_visual != null)
        {
            _visual.localScale = _baseScale * (pulseScale * _scaleFactor);
        }

        if (_sprite != null)
        {
            float brightness = 1f - _pulseDarken * (pulseWeight - pulse);
            Color color = _spriteColor * brightness;
            color.a = _spriteColor.a * _alpha;
            _sprite.color = color;
        }

        if (_label != null)
        {
            Color labelColor = _labelColor;
            labelColor.a *= _alpha;
            _label.color = labelColor;
        }
    }

    private void SetColliderEnabled(bool isEnabled)
    {
        if (_collider != null)
        {
            _collider.enabled = isEnabled;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _isPlayerInside = true;
            OnPlayerEntered?.Invoke(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _isPlayerInside = false;
            OnPlayerExited?.Invoke(this);
        }
    }

    private void OnDestroy()
    {
        if (_playerInteraction != null)
        {
            _playerInteraction.CurrentInteractableChanged -= HandleCurrentInteractableChanged;
        }

        // 포탈이 파괴될 때 플레이어가 안에 있었다면 이탈 이벤트를 보내 기존 UI를 닫아준다.
        if (_isPlayerInside)
        {
            _isPlayerInside = false;
            OnPlayerExited?.Invoke(this);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}