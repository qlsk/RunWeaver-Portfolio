using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 보상 선택 UI. 카드 3장을 가로로 배치.
///
/// - 동작 방식 2가지:
///   A) 외부 UI 연결: UI팀이 직접 만든 Canvas/카드/버튼을 인스펙터 슬롯에 연결하면 그걸 쓴다.
///      스프라이트 교체, 마우스 배치 모두 에디터에서 자유롭게. 코드 생성은 생략된다.
///   B) 코드 자동 생성: 슬롯이 비어 있으면 코드로 Canvas/카드/버튼을 만든다 (플레이스홀더).
/// - 선택 → 확정 버튼 2단계로 오선택을 방지한다. 선택 없이 닫을 수 없다.
/// - 시간 정지/재개(Time.timeScale)는 RewardController가 맡는다. 이 UI는 표시/선택/확정만 한다.
/// - 입력: 마우스 클릭 + 숫자키 1~3 + 방향키 이동 + Enter/Space 확정.
/// </summary>
[DisallowMultipleComponent]
public class RewardSelectUI : MonoBehaviour
{
    [Header("외부 UI 연결 (UI팀 작업용: 연결하면 코드 생성 생략)")]
    [Tooltip("직접 만든 UI 루트. 평소에는 비활성화로 둔다 (Play 시작 시 자동 숨김)")]
    [SerializeField] private GameObject _externalRoot;
    [Tooltip("보상 제목 텍스트")]
    [SerializeField] private Text _externalTitle;
    [Tooltip("카드 3장. 생성된 선택지 순서대로 표시된다")]
    [SerializeField] private RewardCardView[] _externalCards;
    [Tooltip("확정 버튼")]
    [SerializeField] private Button _externalConfirmButton;
    [Tooltip("확정 버튼 라벨 (선택 사항)")]
    [SerializeField] private Text _externalConfirmLabel;

    [Header("카드 색상 (플레이스홀더)")]
    [SerializeField, FormerlySerializedAs("normalCardColor")] private Color _normalCardColor = new(0.16f, 0.17f, 0.22f, 1f);
    [SerializeField, FormerlySerializedAs("selectedCardColor")] private Color _selectedCardColor = new(0.85f, 0.65f, 0.2f, 1f);
    [SerializeField, FormerlySerializedAs("runePlaceholderColor")] private Color _runePlaceholderColor = new(0.4f, 0.7f, 1f, 1f);
    [SerializeField, FormerlySerializedAs("passivePlaceholderColor")] private Color _passivePlaceholderColor = new(0.55f, 0.9f, 0.55f, 1f);

    [Header("레이아웃 (Play 전 인스펙터에서 조정, 우클릭 → 미리보기 다시 만들기)")]
    [Tooltip("전체 패널 크기")]
    [SerializeField] private Vector2 _panelSize = new(1080f, 680f);
    [Tooltip("카드 1장 크기")]
    [SerializeField] private Vector2 _cardSize = new(300f, 400f);
    [Tooltip("카드 사이 간격")]
    [SerializeField, Min(0f)] private float _cardSpacing = 24f;
    [Tooltip("카드 안 아이콘 크기")]
    [SerializeField, Min(16f)] private float _iconSize = 96f;
    [Tooltip("확정 버튼 크기")]
    [SerializeField] private Vector2 _confirmButtonSize = new(440f, 64f);
    [Tooltip("제목 / 카드이름 / 카드설명 / 확정버튼 글자 크기")]
    [SerializeField, Min(8)] private int _titleFontSize = 30;
    [SerializeField, Min(8)] private int _cardNameFontSize = 22;
    [SerializeField, Min(8)] private int _cardDescFontSize = 16;
    [SerializeField, Min(8)] private int _confirmFontSize = 24;

    /// <summary>확정 버튼을 누르면 발행된다. 닫기/시간복구는 구독자(RewardController)가 한다.</summary>
    public event Action<RewardOption> OnOptionConfirmed;

    public bool IsOpen => _root != null && _root.activeSelf;

    private GameObject _root;
    private GameObject _autoBuiltRoot;
    private Canvas _canvas;
    private Text _titleText;
    private Button _confirmButton;
    private Text _confirmLabel;

    private readonly List<RewardOption> _currentOptions = new();
    private readonly List<Image> _cardBackgrounds = new();
    private readonly List<Button> _cardButtons = new();
    private readonly List<Image> _cardIcons = new();
    private readonly List<Text> _cardNames = new();
    private readonly List<Text> _cardDescs = new();

    private int _selectedIndex = -1;
    private bool _confirmLocked;

    private void Awake()
    {
        // Play 진입 시에는 인스펙터 최신값으로 항상 새로 만든다 (에디터 미리보기 잔재 무시).
        ClearBuilt();
        EnsureBuilt();
        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsOpen || _confirmLocked)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SelectSlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SelectSlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SelectSlot(2);
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) SelectSlot((_selectedIndex < 0 ? 0 : _selectedIndex + 2) % 3);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) SelectSlot((_selectedIndex < 0 ? 0 : _selectedIndex + 1) % 3);
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)) ConfirmSelected();
    }

    /// <summary>
    /// 선택지를 세팅하고 연다. UI가 열려 있는 동안 선택지는 유지된다.
    /// </summary>
    public void Open(IReadOnlyList<RewardOption> options)
    {
        EnsureBuilt();

        if (options == null || options.Count != 3)
        {
            Debug.LogWarning("[RewardSelectUI] 선택지는 항상 3개여야 합니다.", this);
            return;
        }

        if (_cardNames.Count < 3 || _cardDescs.Count < 3 || _cardIcons.Count < 3 || _cardBackgrounds.Count < 3)
        {
            Debug.LogError($"[RewardSelectUI] 카드 UI가 완성되지 않았습니다 (names={_cardNames.Count}, descs={_cardDescs.Count}, icons={_cardIcons.Count}, bgs={_cardBackgrounds.Count}). 외부 UI 연결 시 카드 3장 슬롯을 확인하세요.", this);
            return;
        }

        _currentOptions.Clear();
        _currentOptions.AddRange(options);
        _selectedIndex = -1;
        _confirmLocked = false;

        for (int i = 0; i < 3; i++)
        {
            RewardOption option = _currentOptions[i];
            _cardNames[i].text = option.title;
            _cardDescs[i].text = option.description;

            if (option.icon != null)
            {
                _cardIcons[i].sprite = option.icon;
                _cardIcons[i].color = Color.white;
            }
            else
            {
                _cardIcons[i].sprite = null;
                _cardIcons[i].color = option.type == RewardType.RuneStone ? _runePlaceholderColor : _passivePlaceholderColor;
            }

            _cardBackgrounds[i].color = _normalCardColor;
        }

        _titleText.text = "보상을 선택하세요 (반드시 1개)";
        RefreshConfirmButton();
        _root.SetActive(true);
    }

    public void Close()
    {
        _selectedIndex = -1;
        _confirmLocked = false;
        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    public void SelectSlot(int index)
    {
        if (!IsOpen || index < 0 || index >= _currentOptions.Count)
        {
            return;
        }

        _selectedIndex = index;
        for (int i = 0; i < _cardBackgrounds.Count; i++)
        {
            _cardBackgrounds[i].color = i == _selectedIndex ? _selectedCardColor : _normalCardColor;
        }

        Debug.Log($"[RewardSelectUI] 카드 선택: {index} ({_currentOptions[index].title})", this);
        RefreshConfirmButton();

        // 게임패드/키보드 포커스도 따라가게 한다.
        if (index < _cardButtons.Count && _cardButtons[index] != null)
        {
            EventSystem current = EventSystem.current;
            if (current != null)
            {
                current.SetSelectedGameObject(_cardButtons[index].gameObject);
            }
        }
    }

    public void ConfirmSelected()
    {
        if (!IsOpen || _confirmLocked)
        {
            return;
        }

        if (_selectedIndex < 0 || _selectedIndex >= _currentOptions.Count)
        {
            return;
        }

        _confirmLocked = true;
        RewardOption option = _currentOptions[_selectedIndex];
        Debug.Log($"[RewardSelectUI] 보상 확정: {option.title}");
        OnOptionConfirmed?.Invoke(option);
    }

    private void RefreshConfirmButton()
    {
        if (_confirmButton != null)
        {
            _confirmButton.interactable = _selectedIndex >= 0;
        }

        if (_confirmLabel != null)
        {
            _confirmLabel.text = _selectedIndex >= 0
                ? $"확정: {_currentOptions[_selectedIndex].title}"
                : "카드를 선택하세요";
        }
    }

    // ── UI 바인딩 (외부 연결 우선, 없으면 코드 생성) ──

    /// <summary>
    /// 씬에서 비활성화 상태로 시작하면 Awake가 돌지 않으므로, 사용 직전에 보장한다.
    /// </summary>
    private void EnsureBuilt()
    {
        if (IsBound())
        {
            return;
        }

        ClearAutoBuilt();

        // 외부 UI가 연결되어 있으면 그걸 쓴다.
        if (TryBindExternal())
        {
            return;
        }

        try
        {
            BuildUI();
            _autoBuiltRoot = _root;
            Debug.Log($"[RewardSelectUI] 코드 생성 UI 완료 (카드 {_cardBackgrounds.Count}장)", this);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RewardSelectUI] UI 생성 실패: {e}", this);
            ClearAutoBuilt();
        }
    }

    private bool IsBound()
    {
        return _root != null && _cardBackgrounds.Count >= 3;
    }

    /// <summary>
    /// 인스펙터에 연결된 외부 UI를 바인딩한다. 실패하면 false (코드 생성으로 대체).
    /// 버튼 OnClick은 코드가 연결하므로 에디터에서 비워둔다.
    /// </summary>
    private bool TryBindExternal()
    {
        if (_externalRoot == null)
        {
            return false;
        }

        if (_externalTitle == null || _externalConfirmButton == null)
        {
            Debug.LogError("[RewardSelectUI] 외부 UI의 Title/ConfirmButton이 비어 있습니다.", this);
            return false;
        }

        if (_externalCards == null || _externalCards.Length != 3)
        {
            Debug.LogError("[RewardSelectUI] 외부 UI의 카드는 정확히 3장이어야 합니다.", this);
            return false;
        }

        for (int i = 0; i < 3; i++)
        {
            if (!_externalCards[i].IsValid)
            {
                Debug.LogError($"[RewardSelectUI] 외부 UI 카드 {i}의 슬롯(Background/Button/Icon/Name/Desc)이 비어 있습니다.", this);
                return false;
            }
        }

        EnsureEventSystem();

        _root = _externalRoot;
        _titleText = _externalTitle;
        _confirmButton = _externalConfirmButton;
        _confirmLabel = _externalConfirmLabel;

        for (int i = 0; i < 3; i++)
        {
            RewardCardView card = _externalCards[i];
            _cardBackgrounds.Add(card.Background);
            _cardButtons.Add(card.Button);
            _cardIcons.Add(card.Icon);
            _cardNames.Add(card.Name);
            _cardDescs.Add(card.Desc);

            int captured = i;
            card.Button.onClick.RemoveAllListeners();
            card.Button.onClick.AddListener(() => SelectSlot(captured));
        }

        _confirmButton.onClick.RemoveAllListeners();
        _confirmButton.onClick.AddListener(ConfirmSelected);

        Debug.Log("[RewardSelectUI] 외부 연결 UI 사용", this);
        return true;
    }

    /// <summary>
    /// 코드로 만든 UI만 파괴한다. 외부 연결 UI는 절대 건드리지 않는다.
    /// </summary>
    private void ClearAutoBuilt()
    {
        if (_autoBuiltRoot != null)
        {
            DestroyTarget(_autoBuiltRoot);
            _autoBuiltRoot = null;
        }

        // 외부 바인딩 잔재도 함께 비운다 (EnsureBuilt에서 다시 바인딩됨). 외부 오브젝트는 파괴하지 않는다.
        _root = null;
        _canvas = null;
        _titleText = null;
        _confirmButton = null;
        _confirmLabel = null;
        _currentOptions.Clear();
        _cardBackgrounds.Clear();
        _cardButtons.Clear();
        _cardIcons.Clear();
        _cardNames.Clear();
        _cardDescs.Clear();
        _selectedIndex = -1;
        _confirmLocked = false;
    }

    private void ClearBuilt()
    {
        ClearAutoBuilt();
    }

    private void DestroyTarget(GameObject target)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(target);
        }
        else
        {
            Destroy(target);
        }
#else
        Destroy(target);
#endif
    }

    /// <summary>
    /// [우클릭 메뉴] Play 없이 레이아웃 확인용. 외부 UI 연결 시 그 UI로, 아니면 코드 생성 UI로 미리보기.
    /// 더미 3장으로 표시되며 실제 보상은 아니다.
    /// </summary>
    [ContextMenu("미리보기 다시 만들기")]
    private void RebuildPreview()
    {
        ClearAutoBuilt();
        EnsureBuilt();

        List<RewardOption> preview = new()
        {
            RewardOption.ForRuneStone(5, null),
            new RewardOption { type = RewardType.Passive, title = "데미지 +5%", description = "데미지 +5%" },
            new RewardOption { type = RewardType.Passive, title = "이동속도 +5%", description = "이동속도 +5%" }
        };
        // 미리보기도 실전처럼 섞는다 (더미 순서 고정 방지).
        for (int i = preview.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (preview[i], preview[j]) = (preview[j], preview[i]);
        }
        Open(preview);
    }

    private void BuildUI()
    {
        EnsureEventSystem();

        _root = new GameObject("RewardSelectUI_Root");
        _root.transform.SetParent(transform, false);

        _canvas = _root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        _root.AddComponent<GraphicRaycaster>();

        Image dim = CreateImage(_root.transform, "Dim", new Color(0f, 0f, 0f, 0.7f));
        StretchFull(dim.rectTransform);

        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(_root.transform, false);
        Image panel = panelObj.AddComponent<Image>();
        panel.color = new Color(0.08f, 0.09f, 0.13f, 0.98f);
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = _panelSize;
        panelRect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup panelLayout = panelObj.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(32, 32, 24, 24);
        panelLayout.spacing = 16f;
        panelLayout.childAlignment = TextAnchor.MiddleCenter;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = false;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        // 내용이 패널보다 길어지면(한글 줄바꿈 등) 패널이 자동으로 늘어나 겹치지 않게 한다.
        ContentSizeFitter panelFitter = panelObj.AddComponent<ContentSizeFitter>();
        panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _titleText = CreateText(panelObj.transform, "Title", _titleFontSize, TextAnchor.MiddleCenter);
        _titleText.text = "보상을 선택하세요";

        GameObject cardsObj = new GameObject("Cards");
        cardsObj.transform.SetParent(panelObj.transform, false);
        HorizontalLayoutGroup cardsLayout = cardsObj.AddComponent<HorizontalLayoutGroup>();
        cardsLayout.spacing = _cardSpacing;
        cardsLayout.childAlignment = TextAnchor.MiddleCenter;
        cardsLayout.childControlWidth = false;
        cardsLayout.childControlHeight = false;
        cardsLayout.childForceExpandWidth = false;
        cardsLayout.childForceExpandHeight = false;
        LayoutElement cardsSize = cardsObj.AddComponent<LayoutElement>();
        cardsSize.minHeight = _cardSize.y;
        cardsSize.flexibleHeight = 1f;

        for (int i = 0; i < 3; i++)
        {
            BuildCard(cardsObj.transform, i);
        }

        GameObject confirmObj = new GameObject("ConfirmButton");
        confirmObj.transform.SetParent(panelObj.transform, false);
        _confirmButton = confirmObj.AddComponent<Button>();
        Image confirmImage = confirmObj.AddComponent<Image>();
        confirmImage.color = new Color(0.9f, 0.7f, 0.25f, 1f);
        RectTransform confirmRect = confirmImage.rectTransform;
        confirmRect.sizeDelta = _confirmButtonSize;
        LayoutElement confirmSize = confirmObj.AddComponent<LayoutElement>();
        confirmSize.minWidth = _confirmButtonSize.x;
        confirmSize.minHeight = _confirmButtonSize.y;

        _confirmLabel = CreateText(confirmObj.transform, "Label", _confirmFontSize, TextAnchor.MiddleCenter);
        _confirmLabel.color = Color.black;
        StretchFull(_confirmLabel.rectTransform);

        ColorBlock colors = _confirmButton.colors;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        _confirmButton.colors = colors;
        _confirmButton.onClick.AddListener(ConfirmSelected);
    }

    private void BuildCard(Transform parent, int index)
    {
        GameObject cardObj = new GameObject($"Card_{index}");
        cardObj.transform.SetParent(parent, false);

        Image bg = cardObj.AddComponent<Image>();
        bg.color = _normalCardColor;
        LayoutElement size = cardObj.AddComponent<LayoutElement>();
        size.minWidth = _cardSize.x;
        size.minHeight = _cardSize.y;
        size.preferredWidth = _cardSize.x;
        size.preferredHeight = _cardSize.y;

        Button button = cardObj.AddComponent<Button>();
        int captured = index;
        button.onClick.AddListener(() => SelectSlot(captured));

        VerticalLayoutGroup layout = cardObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(cardObj.transform, false);
        Image icon = iconObj.AddComponent<Image>();
        icon.color = _passivePlaceholderColor;
        LayoutElement iconSize = iconObj.AddComponent<LayoutElement>();
        iconSize.minWidth = _iconSize;
        iconSize.minHeight = _iconSize;
        iconSize.preferredWidth = _iconSize;
        iconSize.preferredHeight = _iconSize;

        Text nameText = CreateText(cardObj.transform, "Name", _cardNameFontSize, TextAnchor.MiddleCenter);
        Text descText = CreateText(cardObj.transform, "Desc", _cardDescFontSize, TextAnchor.UpperCenter);
        descText.lineSpacing = 1.2f;

        LayoutElement nameSize = nameText.gameObject.AddComponent<LayoutElement>();
        nameSize.minHeight = 36f;
        LayoutElement descSize = descText.gameObject.AddComponent<LayoutElement>();
        descSize.minHeight = 140f;
        descSize.flexibleHeight = 1f;

        _cardBackgrounds.Add(bg);
        _cardButtons.Add(button);
        _cardIcons.Add(icon);
        _cardNames.Add(nameText);
        _cardDescs.Add(descText);
    }

    private Image CreateImage(Transform parent, string objectName, Color color)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private Text CreateText(Transform parent, string objectName, int fontSize, TextAnchor alignment)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = GetDefaultFont();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.supportRichText = false;
        return text;
    }

    private void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private Font GetDefaultFont()
    {
        // Arial.ttf 조회는 현 Unity 버전에서 예외를 던지므로 LegacyRuntime.ttf만 쓴다.
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
