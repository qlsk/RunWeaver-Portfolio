using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 협회 지침서(일시정지 메뉴에서 여는 도감 화면).
/// 왼쪽 목록은 "기본 조작"(튜토리얼 필수 단계)과 분류별 기록(상황 안내 카드)으로 이루어지고,
/// 아직 보지 못한 기록은 ??? 잠금 항목으로 보인다. 오른쪽에는 고른 항목의 설명과 조작 키를 보여준다.
/// 화면은 열 때마다 목록을 새로 만든다. 열려 있는 동안 Esc는 메뉴 대신 이 화면을 닫는다.
/// </summary>
public class GuidebookPresenter : MonoBehaviour
{
    private const string BasicCategory = "기본 조작";
    private const string LockedLabel = "???";

    [Header("데이터")]
    [SerializeField] private TutorialStepSO[] _steps;
    [SerializeField] private RecordCardSO[] _records;

    [Header("메뉴")]
    [SerializeField] private PauseController _pauseController;
    [Tooltip("일시정지 메뉴 안의 협회 지침서 항목 버튼")]
    [SerializeField] private Button _menuButton;

    [Header("화면")]
    [Tooltip("TutorialGuidebookScreen 프리팹 인스턴스. 처음에는 꺼 둔다")]
    [SerializeField] private GameObject _screen;
    [SerializeField] private GameObject _categoryPrefab;
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private GameObject _lockedItemPrefab;
    [SerializeField] private GameObject _keyRowPrefab;

    private sealed class Entry
    {
        public string Category;
        public string Title;
        public string Body;
        public string Note;
        public bool IsLocked;
        public readonly List<(string Key, string Label)> Keys = new();
        public GameObject ItemObject;
    }

    private readonly List<Entry> _entries = new();
    private Transform _listContent;
    private Transform _keyBox;
    private TMP_Text _countText;
    private TMP_Text _categoryLabel;
    private TMP_Text _titleText;
    private TMP_Text _bodyText;
    private TMP_Text _noteText;
    private Entry _selected;

    private void Awake()
    {
        if (_screen == null)
        {
            Debug.LogError("[GuidebookPresenter] 지침서 화면이 연결되지 않았습니다.", this);
            return;
        }

        _listContent = FindDeep(_screen.transform, "Content");
        _keyBox = FindDeep(_screen.transform, "KeyBox");
        _countText = FindText("LeftColumn", "Count");
        _categoryLabel = FindText("RightColumn", "CategoryLabel");
        _titleText = FindText("RightColumn", "Title");
        _bodyText = FindText("RightColumn", "Body");
        _noteText = FindText("RightColumn", "Footnote");
        _screen.SetActive(false);
    }

    private void OnEnable()
    {
        if (_menuButton != null)
            _menuButton.onClick.AddListener(Open);

        if (_pauseController != null)
            _pauseController.RegisterSubScreenCloseHandler(HandleEscape);
    }

    private void OnDisable()
    {
        if (_menuButton != null)
            _menuButton.onClick.RemoveListener(Open);

        if (_pauseController != null)
            _pauseController.UnregisterSubScreenCloseHandler(HandleEscape);
    }

    private void LateUpdate()
    {
        // 일시정지 메뉴가 닫혀 항목이 숨으면 지침서도 닫는다.
        if (_screen != null && _screen.activeSelf && _menuButton != null && !_menuButton.gameObject.activeInHierarchy)
            _screen.SetActive(false);
    }

    private void Open()
    {
        if (_screen == null || _listContent == null)
            return;

        BuildEntries();
        BuildList();
        _screen.SetActive(true);
        Select(_entries.Find(entry => !entry.IsLocked));
    }

    /// <summary>화면이 열려 있으면 닫고 true를 돌려준다.</summary>
    private bool HandleEscape()
    {
        if (_screen == null || !_screen.activeSelf)
            return false;

        _screen.SetActive(false);
        return true;
    }

    private void BuildEntries()
    {
        _entries.Clear();

        if (_steps != null)
        {
            foreach (TutorialStepSO step in _steps)
            {
                if (step == null)
                    continue;

                var entry = new Entry { Category = BasicCategory, Title = step.Title, Body = step.Body };
                if (step.ActionRow == TutorialActionRowKind.Key && step.KeyLabels != null && step.KeyLabels.Count > 0)
                    entry.Keys.Add((step.KeyLabels[0], step.ActionLabel));

                _entries.Add(entry);
            }
        }

        if (_records == null)
            return;

        foreach (RecordCardSO record in _records)
        {
            if (record == null)
                continue;

            bool isSeen = TutorialProgressStore.IsRecordSeen(record.Id);
            _entries.Add(new Entry
            {
                Category = record.Category,
                Title = isSeen ? record.Title : LockedLabel,
                Body = isSeen ? record.Body : string.Empty,
                Note = isSeen ? record.Footer : string.Empty,
                IsLocked = !isSeen
            });
        }
    }

    private void BuildList()
    {
        // 프리팹에 들어 있는 견본 항목을 모두 지우고 새로 만든다.
        for (int i = _listContent.childCount - 1; i >= 0; i--)
        {
            GameObject old = _listContent.GetChild(i).gameObject;
            old.SetActive(false);
            Destroy(old);
        }

        string lastCategory = null;
        foreach (Entry entry in _entries)
        {
            if (entry.Category != lastCategory)
            {
                lastCategory = entry.Category;
                GameObject category = Instantiate(_categoryPrefab, _listContent);
                SetLabel(category.transform, entry.Category);
            }

            GameObject item = Instantiate(entry.IsLocked ? _lockedItemPrefab : _itemPrefab, _listContent);
            entry.ItemObject = item;
            if (!entry.IsLocked)
                SetLabel(FindDeep(item.transform, "Label"), entry.Title);

            // 견본 항목의 "미발견" 개수 표시는 쓰지 않는다.
            Transform count = FindDeep(item.transform, "Count");
            if (count != null)
                count.gameObject.SetActive(false);

            Entry captured = entry;
            if (item.TryGetComponent(out Button button))
                button.onClick.AddListener(() => Select(captured));
        }

        if (_countText != null)
            _countText.text = $"기록 {CountSeenRecords()} / {(_records != null ? _records.Length : 0)}";
    }

    private void Select(Entry entry)
    {
        _selected = entry;

        foreach (Entry other in _entries)
        {
            bool isSelected = other == entry;
            SetActive(FindDeep(other.ItemObject.transform, "SelectedBg"), isSelected);
            SetActive(FindDeep(other.ItemObject.transform, "SelectedBorder"), isSelected);
        }

        if (entry == null)
            return;

        SetText(_categoryLabel, entry.Category);
        SetText(_titleText, entry.IsLocked ? LockedLabel : entry.Title);
        SetText(_bodyText, entry.IsLocked ? "아직 만나지 못한 기록이다." : entry.Body);
        SetText(_noteText, entry.Note);

        BuildKeyRows(entry);
    }

    private void BuildKeyRows(Entry entry)
    {
        if (_keyBox == null)
            return;

        for (int i = _keyBox.childCount - 1; i >= 0; i--)
        {
            GameObject old = _keyBox.GetChild(i).gameObject;
            old.SetActive(false);
            Destroy(old);
        }

        _keyBox.gameObject.SetActive(entry.Keys.Count > 0);
        foreach ((string key, string label) in entry.Keys)
        {
            GameObject row = Instantiate(_keyRowPrefab, _keyBox);
            SetLabel(FindDeep(row.transform, "KeyText"), key);
            SetLabel(FindDeep(row.transform, "Label"), label);
            SetActive(FindDeep(row.transform, "Tag"), false);
        }
    }

    private int CountSeenRecords()
    {
        int seen = 0;
        if (_records == null)
            return seen;

        foreach (RecordCardSO record in _records)
        {
            if (record != null && TutorialProgressStore.IsRecordSeen(record.Id))
                seen++;
        }

        return seen;
    }

    private TMP_Text FindText(string parentName, string childName)
    {
        Transform parent = FindDeep(_screen.transform, parentName);
        Transform child = parent != null ? FindDeep(parent, childName) : null;
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static void SetLabel(Transform target, string text)
    {
        if (target == null)
            return;

        if (target.TryGetComponent(out TMP_Text label))
        {
            label.text = text;
            return;
        }

        // 분류 프리팹처럼 글자가 자식에 있는 경우
        TMP_Text child = target.GetComponentInChildren<TMP_Text>(true);
        if (child != null)
            child.text = text;
    }

    private static void SetText(TMP_Text label, string text)
    {
        if (label == null)
            return;

        label.gameObject.SetActive(!string.IsNullOrEmpty(text));
        label.text = text;
    }

    private static void SetActive(Transform target, bool isActive)
    {
        if (target != null)
            target.gameObject.SetActive(isActive);
    }

    private static Transform FindDeep(Transform parent, string childName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }

        return null;
    }
}
