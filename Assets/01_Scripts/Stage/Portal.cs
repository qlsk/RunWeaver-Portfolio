using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 미션 클리어 후 등장하는 포탈. 사용하면 한 층 내려간다. (귀환, 여러 층 하강 포탈은 폐기)
/// PortalSpawner가 Setup으로 문구를 정한 뒤 PlayFadeIn으로 등장시킨다.
/// FadeIn이 끝나야 Collider가 켜져서 F 상호작용(IInteractable)이 가능해진다.
/// 포탈 위에는 문구(Label)를 항상 띄우고,
/// 플레이어가 이 포탈을 상호작용 대상으로 잡으면 아래에 F 안내 텍스트(Prompt)를 띄운다.
/// 플레이어가 상호작용 대상으로 잡으면 크기와 밝기가 천천히 맥동한다.
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

    private const string InteractionText = "Enter";
    private const string DefaultLabelText = "Descend";

    [Header("References")]
    [Tooltip("포탈 이미지 오브젝트. 크기 연출은 여기에만 적용된다")][SerializeField] private Transform _visual;
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private Collider2D _collider;
    [SerializeField] private TextMeshPro _label;
    [Tooltip("가까이 갔을 때만 보이는 F 안내 텍스트")][SerializeField] private TextMeshPro _prompt;

    [Header("Colors (포탈 / 텍스트)")]
    [SerializeField] private Color _descendColor = Color.white;
    [SerializeField] private Color _descendLabelColor = new(1f, 0.95f, 0.85f, 1f);

    [Header("Fade In")]
    [SerializeField, Min(0f)] private float _fadeInDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float _fadeInStartScale = 0.8f;

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

    public bool IsReady => _isReady;

    public string InteractionName => InteractionText;
    public Transform InteractionGuidePoint => _prompt != null ? _prompt.transform : transform;

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
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
            _prompt.enabled = _isFocused && _isReady;
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

        _fadeRoutine = StartCoroutine(FadeInRoutine());
    }

    public void Interact()
    {
        if (!_isReady)
        {
            return;
        }

        if (StageManager.Instance == null)
        {
            Debug.LogWarning("[Portal] StageManager가 없어 포탈을 사용할 수 없습니다.", this);
            return;
        }

        // 연타로 두 번 들어가지 않도록 바로 막는다.
        _isReady = false;
        RefreshPrompt();
        StageManager.Instance.UsePortal(this);
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
        if (_fadeRoutine != null)
        {
            return;
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