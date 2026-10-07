using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 UI(ShopUI)를 ShopDataSource와 연결한다.
/// - 상점이 새로 진열되면 첫 칸을 자동으로 선택해 Details에 표시한다.
/// - 선택한 칸을 사면 남은 칸 중 첫 칸을 선택한다. 남은 칸이 없으면 Details를 비운다.
/// - 골드가 부족하면 Reroll도 비활성화하고 글자를 붉게 표시한다.
/// - Leave를 누르면 상점 창을 닫는다. (다음 스테이지 진행은 Left 이벤트를 받는 쪽에서 연결)
/// - 열 때는 페이드 인한다.
/// - 닫을 때는 상점 내용만 페이드 아웃해 검은 화면만 남기고, 창을 끈 뒤 Left를 보낸다. (이때 다음 층이 준비된다)
///   그 뒤 검은 화면을 잠깐 유지했다가 페이드 아웃해 다음 층을 보여준다.
/// 데이터 소스를 비워 두면 씬에서 찾는다.
/// </summary>
[DisallowMultipleComponent]
public class ShopPresenter : MonoBehaviour
{
    [SerializeField] private ShopDataSource _dataSource;
    [SerializeField] private ShopSlotView[] _slots;

    [Header("Details")]
    [SerializeField] private TMP_Text _detailNameText;
    [SerializeField] private TMP_Text _detailDescriptionText;
    [Tooltip("포션 설명 아래에 붙는 체력 변화. {0} 현재 체력, {1} 구매 후 체력, {2} 최대 체력")]
    [SerializeField] private string _potionHealthFormat = "({0}/{2}) -> <color=#7FD38A>({1}/{2})</color>";
    [Tooltip("체력이 가득 찼을 때 포션 설명 아래에 붙는 안내. 이때는 포션을 살 수 없다")]
    [SerializeField] private string _fullHealthText = "<color=#E07A7A>Health is already full.</color>";

    [Header("Reroll")]
    [SerializeField] private Button _rerollButton;
    [SerializeField] private TMP_Text _rerollText;
    [SerializeField] private string _rerollFormat = "Reroll - Cost <{0}>";

    [Header("Leave")]
    [SerializeField] private Button _leaveButton;
    [Tooltip("Leave를 누르면 끌 오브젝트. 비워두면 이 오브젝트를 끈다.")]
    [SerializeField] private GameObject _closeTarget;
    [Tooltip("누르면 Leave와 같이 상점을 나가는 키")]
    [SerializeField] private KeyCode _leaveKey = KeyCode.Escape;

    [Header("페이드")]
    [SerializeField, Min(0f)] private float _fadeInDuration = 0.3f;
    [Tooltip("나갈 때 상점 내용이 사라지는 시간 (검은 화면은 남는다)")]
    [SerializeField, Min(0f)] private float _fadeOutDuration = 0.3f;
    [Tooltip("상점 뒤 검은 화면. 나갈 때 다음 층으로 넘어가는 동안 화면을 가린다")]
    [SerializeField] private Image _blackBackground;
    [Tooltip("다음 층이 준비된 뒤 검은 화면을 유지하는 시간")]
    [SerializeField, Min(0f)] private float _blackHoldDuration = 0.2f;
    [Tooltip("검은 화면이 걷히는 시간")]
    [SerializeField, Min(0f)] private float _blackFadeOutDuration = 0.4f;

    private CanvasGroup _canvasGroup;
    private Coroutine _fadeRoutine;
    private bool _isLeaving;

    [Header("골드")]
    [SerializeField] private GoldHudView _goldView;

    [Header("색")]
    [SerializeField] private Color _insufficientColor = new(0.85f, 0.15f, 0.12f, 1f);

    private Color _normalRerollColor = Color.white;
    private int _selectedIndex = -1;

    /// <summary>Leave로 상점을 나갔을 때</summary>
    public event Action Left;

    private void Awake()
    {
        if (_dataSource == null)
        {
            _dataSource = FindAnyObjectByType<ShopDataSource>();
        }

        if (_dataSource == null)
        {
            Debug.LogError("[ShopPresenter] ShopDataSource를 찾을 수 없습니다.", this);
            enabled = false;
            return;
        }

        if (_rerollText != null)
        {
            _normalRerollColor = _rerollText.color;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
            {
                _slots[i].Clicked += HandleSlotClicked;
                _slots[i].BuyClicked += HandleBuyClicked;
            }
        }

        if (_rerollButton != null)
        {
            _rerollButton.onClick.AddListener(HandleRerollClicked);
        }

        if (_leaveButton != null)
        {
            _leaveButton.onClick.AddListener(HandleLeaveClicked);
        }

        _dataSource.Changed += Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDestroy()
    {
        if (_dataSource != null)
        {
            _dataSource.Changed -= Refresh;
        }

        foreach (ShopSlotView slot in _slots)
        {
            if (slot != null)
            {
                slot.Clicked -= HandleSlotClicked;
                slot.BuyClicked -= HandleBuyClicked;
            }
        }

        if (_rerollButton != null)
        {
            _rerollButton.onClick.RemoveListener(HandleRerollClicked);
        }

        if (_leaveButton != null)
        {
            _leaveButton.onClick.RemoveListener(HandleLeaveClicked);
        }
    }

    /// <summary>상점을 새로 연다. (창을 켜고 리롤 비용을 초기화한 뒤 새로 진열)</summary>
    public void OpenShop()
    {
        GameObject target = _closeTarget != null ? _closeTarget : gameObject;
        target.SetActive(true);
        _selectedIndex = -1;
        _isLeaving = false;
        _dataSource.Open();
        StartFade(0f, 1f, _fadeInDuration, null);
    }

    private void Refresh()
    {
        if (_dataSource == null)
        {
            return;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
            {
                continue;
            }

            ShopOfferData offer = _dataSource.GetOffer(i);
            if (offer != null)
            {
                _slots[i].Show(offer, _dataSource.CanAfford(offer.Price), _dataSource.CanPurchase(offer));
            }
            else
            {
                _slots[i].ShowSold();
            }
        }

        if (_dataSource.GetOffer(_selectedIndex) == null)
        {
            _selectedIndex = FindFirstOfferIndex();
        }

        ShowDetails();
        RefreshReroll();

        if (_goldView != null)
        {
            _goldView.SetAmount(_dataSource.Gold);
        }
    }

    private void RefreshReroll()
    {
        bool canReroll = _dataSource.CanReroll();
        if (_rerollButton != null)
        {
            _rerollButton.interactable = canReroll;
        }

        if (_rerollText != null)
        {
            _rerollText.text = string.Format(_rerollFormat,
                _dataSource.RerollCost.ToString("#,0", CultureInfo.InvariantCulture));
            _rerollText.color = canReroll ? _normalRerollColor : _insufficientColor;
        }
    }

    private void ShowDetails()
    {
        ShopOfferData offer = _dataSource.GetOffer(_selectedIndex);
        if (_detailNameText != null)
        {
            _detailNameText.text = offer != null ? offer.DisplayName : string.Empty;
        }

        if (_detailDescriptionText != null)
        {
            _detailDescriptionText.text = offer != null ? offer.Description + GetPotionHealthText(offer) : string.Empty;
        }
    }

    /// <summary>포션이면 "(현재/최대) -> (구매 후/최대)" 줄을 만든다. 체력을 알 수 없으면 빈 문자열.</summary>
    private string GetPotionHealthText(ShopOfferData offer)
    {
        if (offer.Type != ShopItemType.Potion || offer.HealRatio <= 0f ||
            !_dataSource.TryGetPlayerHealth(out float current, out float max))
        {
            return string.Empty;
        }

        if (current >= max)
        {
            return "\n\n" + _fullHealthText;
        }

        float after = Mathf.Min(max, current + max * offer.HealRatio);
        return "\n\n" + string.Format(CultureInfo.InvariantCulture, _potionHealthFormat,
            Mathf.RoundToInt(current), Mathf.RoundToInt(after), Mathf.RoundToInt(max));
    }

    private int FindFirstOfferIndex()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_dataSource.GetOffer(i) != null)
            {
                return i;
            }
        }

        return -1;
    }

    private void HandleSlotClicked(ShopSlotView slot)
    {
        int index = Array.IndexOf(_slots, slot);
        if (index < 0 || _dataSource.GetOffer(index) == null)
        {
            return;
        }

        _selectedIndex = index;
        ShowDetails();
    }

    private void HandleBuyClicked(ShopSlotView slot)
    {
        int index = Array.IndexOf(_slots, slot);
        if (index >= 0)
        {
            _dataSource.TryBuy(index);
        }
    }

    private void HandleRerollClicked()
    {
        if (_dataSource.TryReroll())
        {
            // 리롤하면 첫 칸을 다시 선택한다.
            _selectedIndex = FindFirstOfferIndex();
            ShowDetails();
        }
    }

    private void Update()
    {
        // 상점이 열려 있는 동안 Esc로도 나갈 수 있다. (일시정지 메뉴는 팝업이 열린 동안 열리지 않는다)
        GameObject target = _closeTarget != null ? _closeTarget : gameObject;
        if (target.activeInHierarchy && !_isLeaving && Input.GetKeyDown(_leaveKey))
        {
            HandleLeaveClicked();
        }
    }

    private void HandleLeaveClicked()
    {
        if (_isLeaving)
        {
            return;
        }

        _isLeaving = true;
        Debug.Log("[ShopPresenter] Leave shop", this);

        if (_blackBackground == null)
        {
            // 검은 화면이 없으면 상점 전체를 페이드 아웃한다.
            StartFade(GetCanvasGroup().alpha, 0f, _fadeOutDuration, FinishLeave);
            return;
        }

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }

        GetCanvasGroup().interactable = false;
        _fadeRoutine = StartCoroutine(FadeOutContentRoutine());
    }

    /// <summary>검은 화면을 뺀 상점 내용만 페이드 아웃한다.</summary>
    private IEnumerator FadeOutContentRoutine()
    {
        Transform target = (_closeTarget != null ? _closeTarget : gameObject).transform;
        var contentGroups = new System.Collections.Generic.List<CanvasGroup>();
        foreach (Transform child in target)
        {
            if (child == _blackBackground.transform)
            {
                continue;
            }

            CanvasGroup group = child.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = child.gameObject.AddComponent<CanvasGroup>();
            }

            contentGroups.Add(group);
        }

        float elapsed = 0f;
        while (elapsed < _fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / _fadeOutDuration);
            foreach (CanvasGroup group in contentGroups)
            {
                group.alpha = alpha;
            }

            yield return null;
        }

        // 다음에 열 때를 위해 되돌려 둔다. (창은 곧 꺼진다)
        foreach (CanvasGroup group in contentGroups)
        {
            group.alpha = 1f;
        }

        _fadeRoutine = null;
        FinishLeaveWithBlackScreen();
    }

    /// <summary>
    /// 검은 화면을 상점 밖으로 잠시 옮겨 남겨 두고 창을 끈 뒤 Left를 보낸다. (다음 층 준비)
    /// 검은 화면은 잠깐 유지했다가 걷히고, 다시 상점 안으로 돌아간다.
    /// </summary>
    private void FinishLeaveWithBlackScreen()
    {
        GameObject target = _closeTarget != null ? _closeTarget : gameObject;
        Transform black = _blackBackground.transform;
        black.SetParent(target.transform.parent, true);
        black.SetSiblingIndex(target.transform.GetSiblingIndex() + 1);

        FinishLeave();

        // 상점 창이 꺼져도 돌도록 검은 화면 자신에서 코루틴을 돌린다.
        _blackBackground.StartCoroutine(FadeOutBlackRoutine(target.transform));
    }

    private IEnumerator FadeOutBlackRoutine(Transform shopRoot)
    {
        Color color = _blackBackground.color;
        float startAlpha = color.a;

        float wait = 0f;
        while (wait < _blackHoldDuration)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        float elapsed = 0f;
        while (elapsed < _blackFadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / _blackFadeOutDuration));
            _blackBackground.color = color;
            yield return null;
        }

        // 상점 맨 뒤로 되돌리고 색을 원래대로 한다.
        Transform black = _blackBackground.transform;
        black.SetParent(shopRoot, true);
        black.SetSiblingIndex(0);
        color.a = startAlpha;
        _blackBackground.color = color;
    }

    /// <summary>창을 끄고 Left를 보낸다.</summary>
    private void FinishLeave()
    {
        GameObject target = _closeTarget != null ? _closeTarget : gameObject;
        target.SetActive(false);
        _isLeaving = false;
        SafeEventNotifier.Notify(Left);
    }

    private CanvasGroup GetCanvasGroup()
    {
        if (_canvasGroup == null)
        {
            GameObject target = _closeTarget != null ? _closeTarget : gameObject;
            _canvasGroup = target.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = target.AddComponent<CanvasGroup>();
            }
        }

        return _canvasGroup;
    }

    private void StartFade(float from, float to, float duration, Action onFinished)
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }

        _fadeRoutine = StartCoroutine(FadeRoutine(from, to, duration, onFinished));
    }

    /// <summary>게임이 멈춰 있어도 진행되도록 실제 시간으로 잰다. 페이드 중에는 클릭을 받지 않는다.</summary>
    private IEnumerator FadeRoutine(float from, float to, float duration, Action onFinished)
    {
        CanvasGroup canvasGroup = GetCanvasGroup();
        canvasGroup.interactable = false;
        canvasGroup.alpha = from;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        canvasGroup.alpha = to;
        canvasGroup.interactable = to > 0f;
        _fadeRoutine = null;
        onFinished?.Invoke();
    }
}
