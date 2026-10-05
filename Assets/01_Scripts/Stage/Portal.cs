using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 포탈 종류. 귀환 / 1층 하강 / 2~3층 하강(위험)
/// </summary>
public enum PortalType
{
    Return,
    Descend,
    DeepDescend
}

/// <summary>
/// 미션 클리어 후 등장하는 포탈.
/// PortalSpawner가 Setup으로 종류와 층수를 정한 뒤 PlayFadeIn으로 등장시킨다.
/// FadeIn이 끝나야 Collider가 켜져서 F 상호작용(IInteractable)이 가능해진다.
/// 포탈 위에는 종류를 알려주는 텍스트를 항상 띄우고, 아래(InteractionGuidePoint)에는 F 안내가 뜬다.
/// 위험 포탈(DeepDescend)은 붉은색 + 맥동으로 분위기를 준다.
/// </summary>
[DisallowMultipleComponent]
public class Portal : MonoBehaviour, IInteractable
{
    // 기존 테스트 씬(ReturnConfirmPopupUI, StageInfoPanelUI) 호환용 정적 이벤트
    public static event Action<Portal> OnPlayerEntered;
    public static event Action<Portal> OnPlayerExited;

    private const string InteractionText = "Enter";

    [Header("References (비워두면 자동으로 찾는다)")]
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private Collider2D _collider;

    [Header("Label")]
    [SerializeField] private TMP_FontAsset _labelFont;
    [SerializeField, Min(0.1f)] private float _labelFontSize = 5f;
    [SerializeField] private float _labelOffset = 0.25f;
    [SerializeField] private float _guideOffset = 0.35f;
    [SerializeField] private Color _labelOutlineColor = new(0f, 0f, 0f, 0.85f);
    [SerializeField, Range(0f, 1f)] private float _labelOutlineWidth = 0.2f;

    [Header("Colors (포탈 / 텍스트)")]
    [SerializeField] private Color _returnColor = new(0.45f, 0.7f, 1f, 1f);
    [SerializeField] private Color _returnLabelColor = new(0.7f, 0.85f, 1f, 1f);
    [SerializeField] private Color _descendColor = Color.white;
    [SerializeField] private Color _descendLabelColor = new(1f, 0.95f, 0.85f, 1f);
    [SerializeField] private Color _deepColor = new(1f, 0.35f, 0.3f, 1f);
    [SerializeField] private Color _deepLabelColor = new(1f, 0.45f, 0.4f, 1f);

    [Header("Fade In")]
    [SerializeField, Min(0f)] private float _fadeInDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float _fadeInStartScale = 0.8f;

    [Header("위험 포탈 연출")]
    [SerializeField, Min(0.01f)] private float _deepScale = 1.15f;
    [SerializeField, Min(0f)] private float _pulseScale = 0.05f;
    [SerializeField, Range(0f, 1f)] private float _pulseDarken = 0.3f;
    [SerializeField, Min(0f)] private float _pulseSpeed = 2.5f;

    [Header("Gizmo")]
    [SerializeField] private Color gizmoColor = new(0f, 1f, 1f, 0.4f);

    private TextMeshPro _label;
    private Transform _guidePoint;
    private Vector3 _baseScale = Vector3.one;
    private Color _spriteColor = Color.white;
    private Color _labelColor = Color.white;
    private float _alpha = 1f;
    private float _scaleFactor = 1f;
    private bool _isReady;
    private bool _isPlayerInside;
    private bool _isInitialized;
    private Coroutine _fadeRoutine;

    public PortalType Type { get; private set; } = PortalType.Descend;
    public int Floors { get; private set; } = 1;
    public bool IsReady => _isReady;

    public string InteractionName => InteractionText;
    public Transform InteractionGuidePoint => _guidePoint != null ? _guidePoint : transform;

    private void Awake()
    {
        Initialize();
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
            _sprite = GetComponent<SpriteRenderer>();
        }

        if (_collider == null)
        {
            _collider = GetComponent<Collider2D>();
        }

        _baseScale = transform.localScale;
        CreateLabel();
        CreateGuidePoint();
        ApplyType();
    }

    /// <summary>
    /// 포탈 종류와 내려갈 층수를 정한다. (귀환 포탈은 floors를 무시한다)
    /// </summary>
    public void Setup(PortalType type, int floors)
    {
        Initialize();
        Type = type;
        Floors = type == PortalType.Return ? 0 : Mathf.Max(1, floors);
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
        StageManager.Instance.UsePortal(this);
    }

    private IEnumerator FadeInRoutine()
    {
        _isReady = false;
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
    }

    private void Update()
    {
        if (Type != PortalType.DeepDescend || _fadeRoutine != null)
        {
            return;
        }

        // 위험 포탈: 크기와 밝기가 천천히 커졌다 작아진다.
        float wave = (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f;
        ApplyVisual(wave);
    }

    private void ApplyType()
    {
        switch (Type)
        {
            case PortalType.Return:
                _spriteColor = _returnColor;
                _labelColor = _returnLabelColor;
                break;
            case PortalType.DeepDescend:
                _spriteColor = _deepColor;
                _labelColor = _deepLabelColor;
                break;
            default:
                _spriteColor = _descendColor;
                _labelColor = _descendLabelColor;
                break;
        }

        if (_label != null)
        {
            _label.text = GetLabelText();
        }

        ApplyVisual(0f);
    }

    private string GetLabelText()
    {
        if (Type == PortalType.Return)
        {
            return "Return to Town";
        }

        return Floors == 1 ? "Descend 1 Floor" : $"Descend {Floors} Floors";
    }

    /// <param name="pulse">0~1. 위험 포탈 맥동 정도 (다른 포탈은 0)</param>
    private void ApplyVisual(float pulse)
    {
        bool isDeep = Type == PortalType.DeepDescend;
        float typeScale = isDeep ? _deepScale : 1f;
        float pulseScale = isDeep ? 1f + _pulseScale * pulse : 1f;
        transform.localScale = _baseScale * (typeScale * pulseScale * _scaleFactor);

        if (_sprite != null)
        {
            float brightness = isDeep ? 1f - _pulseDarken * (1f - pulse) : 1f;
            Color color = _spriteColor * brightness;
            color.a = _spriteColor.a * _alpha;
            _sprite.color = color;
        }

        if (_label != null)
        {
            Color labelColor = _labelColor;
            labelColor.a *= _alpha;
            _label.color = labelColor;

            // 포탈 크기가 바뀌어도 글자 크기는 그대로 보이게 한다.
            float scale = transform.localScale.x;
            _label.transform.localScale = scale > 0.0001f ? Vector3.one / scale : Vector3.one;
        }
    }

    private void CreateLabel()
    {
        GameObject labelObject = new("Label");
        labelObject.transform.SetParent(transform, false);

        _label = labelObject.AddComponent<TextMeshPro>();
        if (_labelFont != null)
        {
            _label.font = _labelFont;
        }

        _label.fontSize = _labelFontSize;
        _label.alignment = TextAlignmentOptions.Bottom;
        _label.textWrappingMode = TextWrappingModes.NoWrap;
        _label.rectTransform.sizeDelta = new Vector2(12f, 2f);
        _label.rectTransform.pivot = new Vector2(0.5f, 0f);
        _label.outlineColor = _labelOutlineColor;
        _label.outlineWidth = _labelOutlineWidth;

        // 포탈 스프라이트 위쪽 가장자리 바로 위
        float top = _sprite != null && _sprite.sprite != null ? _sprite.sprite.bounds.max.y : 1f;
        labelObject.transform.localPosition = new Vector3(0f, top + _labelOffset / Mathf.Max(0.0001f, _baseScale.y), 0f);

        if (_sprite != null && labelObject.TryGetComponent(out MeshRenderer meshRenderer))
        {
            meshRenderer.sortingLayerID = _sprite.sortingLayerID;
            meshRenderer.sortingOrder = _sprite.sortingOrder + 1;
        }
    }

    private void CreateGuidePoint()
    {
        GameObject guideObject = new("InteractionGuidePoint");
        guideObject.transform.SetParent(transform, false);

        // 포탈 스프라이트 아래쪽 가장자리 바로 아래 (F - Enter가 뜨는 위치)
        float bottom = _sprite != null && _sprite.sprite != null ? _sprite.sprite.bounds.min.y : -1f;
        guideObject.transform.localPosition = new Vector3(0f, bottom - _guideOffset / Mathf.Max(0.0001f, _baseScale.y), 0f);
        _guidePoint = guideObject.transform;
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
        // 포탈이 파괴될 때 플레이어가 안에 있었다면 이탈 이벤트를 보내 기존 UI를 닫아준다.
        if (_isPlayerInside)
        {
            _isPlayerInside = false;
            OnPlayerExited?.Invoke(this);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}
