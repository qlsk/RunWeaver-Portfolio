using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

/// <summary>
/// 미션 클리어 후 등장하는 포탈(공중의 균열). 사용하면 한 층 내려간다. (귀환, 여러 층 하강 포탈은 폐기)
/// PortalSpawner가 Setup으로 문구를 정한 뒤 PlayFadeIn으로 등장시킨다.
/// - 등장 : 빛 점 → 금이 퍼짐 → 찢어지며 열림 (Animator의 Open 상태). 끝나야 Collider가 켜져 F 상호작용이 가능하다.
/// - 대기 : Animator의 Idle 상태가 반복된다. F 안내는 InteractionGuideView가 InteractionGuidePoint 위치에 띄운다.
/// - 들어가기 : 플레이어는 그 자리에 둔 채 조작만 막고 화면이 어두워지며 한 층 내려간다. (PortalArrival, PortalPlayerHider)
/// - 균열 그림은 Visual의 Animator(컨트롤러 : Open → Idle → Close)가 바꾼다. 보스 포탈은 _bossController로 바꿔 끼운다.
///   바위 문이 솟고 가라앉는 것, 들어갈 때의 반짝임, 가까이 갔을 때의 맥동도 같은 컨트롤러의 다른 레이어 클립이 한다.
/// - 위쪽 문구(Label)는 쓰지 않는다. (Setup은 호환용으로 남긴다)
/// 플레이어가 상호작용 대상으로 잡으면 밝기가 천천히 맥동한다. (Animator의 "Focus" 값)
///
/// 프리팹 구성 (Label과 InteractionGuidePoint는 에디터에서 위치/크기를 바로 조정한다)
/// Portal (이 스크립트, Collider)
///  ├ Visual                : SpriteRenderer, SortingGroup, Animator. 자식 Dust(흙먼지), Flash(반짝임)
///  ├ Label                 : TextMeshPro (월드 텍스트). 내용과 색은 종류에 따라 코드에서 바꾼다.
///  └ InteractionGuidePoint : 빈 Transform. InteractionGuideView가 F 안내를 띄울 위치.
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
    [Tooltip("InteractionGuideView가 F 안내를 띄울 위치")][SerializeField] private Transform _interactionGuidePoint;

    [Header("Colors (포탈 / 텍스트)")]
    [SerializeField] private Color _descendColor = Color.white;
    [SerializeField] private Color _descendLabelColor = new(1f, 0.95f, 0.85f, 1f);

    [Header("Fade In (프레임이 없을 때만)")]
    [SerializeField, Min(0f)] private float _fadeInDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float _fadeInStartScale = 0.8f;

    [Header("균열 애니메이션")]
    [Tooltip("Visual의 Animator. 비워두면 Visual에서 찾는다. 컨트롤러가 없으면 페이드 인으로 등장한다")]
    [SerializeField] private Animator _animator;
    [Tooltip("보스 층으로 내려가는 포탈에 쓸 컨트롤러 (PortalGateBoss). 비워두면 기본 컨트롤러를 쓴다")]
    [SerializeField] private RuntimeAnimatorController _bossController;

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

    public bool IsReady => _isReady;

    public string InteractionName => InteractionText;
    public Transform InteractionGuidePoint => _interactionGuidePoint != null ? _interactionGuidePoint : transform;

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
    }

    private bool HasAnimation => _animator != null && _animator.isActiveAndEnabled && _animator.runtimeAnimatorController != null;

    /// <summary>
    /// 씬의 YSort(10000 - 바닥 y * 100)와 같은 기준으로 균열의 정렬 순서를 정한다.
    /// 포탈은 움직이지 않으므로 한 번만 정한다.
    /// </summary>
    private void ApplySorting()
    {
        int order = PortalArrival.RiftSortingOrder(transform.position.y);
        SortingGroup group = _visual != null ? _visual.GetComponent<SortingGroup>() : null;
        if (group != null)
        {
            group.sortingOrder = order;
        }
        else if (_sprite != null)
        {
            _sprite.sortingOrder = order;
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

        if (_animator == null)
        {
            _animator = _visual != null ? _visual.GetComponent<Animator>() : GetComponentInChildren<Animator>();
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
        if (_label != null)
        {
            _label.gameObject.SetActive(false);
        }

        if (_visual != null)
        {
            _baseScale = _visual.localScale;
        }

        ApplyType();
    }

    /// <summary>
    /// 포탈 위에 띄울 문구를 정한다. (예: Descend, Face the Boss)
    /// </summary>
    /// <param name="isBoss">보스 층으로 내려가는 포탈이면 true. 보스 컨트롤러가 있으면 보스 균열 그림으로 바꾼다.</param>
    public void Setup(string label, bool isBoss = false)
    {
        Initialize();
        _labelText = string.IsNullOrEmpty(label) ? DefaultLabelText : label;
        if (isBoss && _bossController != null && _animator != null)
        {
            _animator.runtimeAnimatorController = _bossController;
        }

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
        _fadeRoutine = StartCoroutine(HasAnimation ? OpenRoutine() : FadeInRoutine());
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
        SetColliderEnabled(false);
        Sfx.Play(Sfx.PortalMove);

        if (!HasAnimation)
        {
            stageManager.UsePortal(this);
            return;
        }

        EnterWithFade(stageManager);
    }

    // ───────────────────────── 균열 애니메이션 ─────────────────────────

    private IEnumerator OpenRoutine()
    {
        _isReady = false;
        SetColliderEnabled(false);
        _alpha = 1f;
        _scaleFactor = 1f;

        // 바위 문이 바닥에서 솟아오르며 나타나는 동안 소용돌이가 열리기 시작한다. (Gate 레이어의 Rise 클립)
        PortalRiftAnimation.PlayLayer(_animator, PortalRiftAnimation.GateLayer, PortalRiftAnimation.RiseState);

        // Open이 끝나면 Animator가 자동으로 Idle(반복)로 넘어간다.
        yield return PortalRiftAnimation.PlayOpen(_animator);

        _fadeRoutine = null;
        _isReady = true;
        SetColliderEnabled(true);
    }

    /// <summary>
    /// 페이드 방식 : 플레이어는 그 자리에 둔 채 조작만 막고 바로 층을 옮긴다. (화면은 StageManager가 검게 덮는다)
    /// 포탈은 층 이동과 함께 정리되므로, 같은 모양의 균열을 남겨 화면이 다 덮일 때까지 일렁이게 한다.
    /// 다음 층에서는 PortalPlayerHider가 화면이 걷힐 때 도착 연출(열린 균열 앞에 선 플레이어)을 시작한다.
    /// </summary>
    private void EnterWithFade(StageManager stageManager)
    {
        PortalPlayerHider.Hide(GameObject.FindGameObjectWithTag("Player"), true, true);
        // 남겨 둔 균열의 소용돌이가 한 번 밝게 부풀었다 가라앉으며 화면이 어두워진다.
        PortalArrival.LingerExitRift(_animator, transform.position, PortalArrival.RiftSortingOrder(transform.position.y));
        if (_visual != null)
        {
            _visual.gameObject.SetActive(false);
        }

        stageManager.UsePortal(this);
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
        if (_fadeRoutine != null || _isEntering)
        {
            return;
        }

        // 가까이 간 포탈만 맥동한다. 켜지고 꺼질 때는 부드럽게 섞는다.
        float target = _isFocused && _isReady ? 1f : 0f;
        _pulseWeight = Mathf.MoveTowards(_pulseWeight, target, Time.deltaTime / _pulseBlendDuration);
        if (HasAnimation)
        {
            // 맥동 모양은 Focus 레이어의 클립이 정한다.
            _animator.SetFloat(PortalRiftAnimation.FocusHash, _pulseWeight);
            return;
        }

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