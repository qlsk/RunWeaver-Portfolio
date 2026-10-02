using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보유 룬석 목록 패널. 열기/닫기 버튼과 목록 항목 생성을 담당한다.
/// </summary>
[DisallowMultipleComponent]
public class OwnedRuneStoneListView : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject _listPanel;
    [SerializeField] private Button _openButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private bool _isOpenedOnStart = true;

    [Header("목록")]
    [SerializeField] private Transform _content;
    [SerializeField] private OwnedRuneStoneItemView _itemPrefab;

    private readonly List<OwnedRuneStoneItemView> _items = new();
    private bool _isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (_openButton != null)
        {
            _openButton.onClick.RemoveListener(HandleOpenClicked);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(HandleCloseClicked);
        }
    }

    public void Show(IReadOnlyList<RuneStoneStackData> runeStones)
    {
        Initialize();

        if (_content == null || _itemPrefab == null)
        {
            return;
        }

        int count = runeStones?.Count ?? 0;
        while (_items.Count < count)
        {
            _items.Add(Instantiate(_itemPrefab, _content));
        }

        for (int i = 0; i < _items.Count; i++)
        {
            bool isUsed = i < count;
            _items[i].gameObject.SetActive(isUsed);
            if (isUsed)
            {
                _items[i].Show(runeStones[i]);
            }
        }
    }

    public void SetOpened(bool isOpened)
    {
        if (_listPanel != null)
        {
            _listPanel.SetActive(isOpened);
        }

        if (_openButton != null)
        {
            _openButton.gameObject.SetActive(!isOpened);
        }

        if (_closeButton != null)
        {
            _closeButton.gameObject.SetActive(isOpened);
        }
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        if (_content == null)
        {
            Debug.LogError("[OwnedRuneStoneListView] Content가 할당되지 않았습니다.", this);
        }

        if (_itemPrefab == null)
        {
            Debug.LogError("[OwnedRuneStoneListView] 목록 항목 프리팹이 할당되지 않았습니다.", this);
        }

        if (_openButton != null)
        {
            _openButton.onClick.AddListener(HandleOpenClicked);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(HandleCloseClicked);
        }

        SetOpened(_isOpenedOnStart);
    }

    private void HandleOpenClicked()
    {
        SetOpened(true);
    }

    private void HandleCloseClicked()
    {
        SetOpened(false);
    }
}
