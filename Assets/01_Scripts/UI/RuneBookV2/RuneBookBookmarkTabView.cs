using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 새 룬북(RuneBookV2)의 책갈피 탭 (Words / Reactions).
/// 책갈피 클릭 또는 Q(이전 탭) / E(다음 탭)로 바꾼다. 탭마다 켤 오브젝트 목록을 두고, 선택한 탭의 오브젝트만 켠다.
/// 선택한 책갈피는 선택 스프라이트로 바꾸고 더 길게 뺀다. (책갈피의 피벗은 왼쪽 기준)
/// 룬북을 다시 열어도 마지막으로 보던 탭을 유지한다.
/// </summary>
[DisallowMultipleComponent]
public class RuneBookBookmarkTabView : MonoBehaviour
{
    [Serializable]
    private class Tab
    {
        public Button button;
        public Image bookmarkImage;
        public Image icon;
        public TMP_Text label;
        public GameObject[] pages;
        [Tooltip("책갈피(리본) 색. 투명이면 바꾸지 않는다")]
        public Color ribbonColor = Color.clear;
    }

    [SerializeField] private Tab[] _tabs;

    [Header("키")]
    [SerializeField] private KeyCode _previousKey = KeyCode.Q;
    [SerializeField] private KeyCode _nextKey = KeyCode.E;

    [Header("책갈피 표시")]
    [SerializeField] private Sprite _activeSprite;
    [SerializeField] private Sprite _inactiveSprite;
    [Tooltip("선택한 책갈피 너비. 0이면 바꾸지 않는다.")]
    [SerializeField] private float _activeWidth;
    [Tooltip("선택하지 않은 책갈피 너비. 0이면 바꾸지 않는다.")]
    [SerializeField] private float _inactiveWidth;
    [SerializeField] private Color _activeColor = new(0.980f, 0.925f, 0.808f, 1f);
    [SerializeField] private Color _inactiveColor = new(0.745f, 0.627f, 0.549f, 1f);
    [Tooltip("선택하지 않은 리본을 이 색 쪽으로 바랜다")]
    [SerializeField] private Color _fadedRibbonColor = new(0.78f, 0.7f, 0.6f, 1f);
    [SerializeField, Range(0f, 1f)] private float _inactiveRibbonFade = 0.45f;

    private int _currentIndex;
    private bool _isInitialized;

    public int CurrentIndex => _currentIndex;

    private void Awake()
    {
        Initialize();
        Select(_currentIndex);
    }

    private void OnEnable()
    {
        Initialize();
        Select(_currentIndex);
    }

    private void Update()
    {
        if (_tabs == null || _tabs.Length < 2)
        {
            return;
        }

        if (Input.GetKeyDown(_previousKey))
        {
            Select((_currentIndex - 1 + _tabs.Length) % _tabs.Length);
        }
        else if (Input.GetKeyDown(_nextKey))
        {
            Select((_currentIndex + 1) % _tabs.Length);
        }
    }

    private void OnDestroy()
    {
        if (_tabs == null)
        {
            return;
        }

        foreach (Tab tab in _tabs)
        {
            if (tab?.button != null)
            {
                tab.button.onClick.RemoveAllListeners();
            }
        }
    }

    public void Select(int index)
    {
        if (_tabs == null || _tabs.Length == 0)
        {
            return;
        }

        _currentIndex = Mathf.Clamp(index, 0, _tabs.Length - 1);
        for (int i = 0; i < _tabs.Length; i++)
        {
            Tab tab = _tabs[i];
            if (tab == null)
            {
                continue;
            }

            bool isActive = i == _currentIndex;
            if (tab.pages != null)
            {
                foreach (GameObject page in tab.pages)
                {
                    if (page != null)
                    {
                        page.SetActive(isActive);
                    }
                }
            }

            if (tab.bookmarkImage != null)
            {
                Sprite sprite = isActive ? _activeSprite : _inactiveSprite;
                if (sprite != null)
                {
                    tab.bookmarkImage.sprite = sprite;
                }

                if (tab.ribbonColor.a > 0f)
                {
                    tab.bookmarkImage.color = isActive
                        ? tab.ribbonColor
                        : Color.Lerp(tab.ribbonColor, _fadedRibbonColor, _inactiveRibbonFade);
                }

                float width = isActive ? _activeWidth : _inactiveWidth;
                if (width > 0f)
                {
                    RectTransform rect = tab.bookmarkImage.rectTransform;
                    rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                }
            }

            Color color = isActive ? _activeColor : _inactiveColor;
            if (tab.icon != null)
            {
                tab.icon.color = color;
            }

            if (tab.label != null)
            {
                tab.label.color = color;
            }
        }
    }

    private void Initialize()
    {
        if (_isInitialized || _tabs == null)
        {
            return;
        }

        _isInitialized = true;
        for (int i = 0; i < _tabs.Length; i++)
        {
            int index = i;
            if (_tabs[i]?.button != null)
            {
                _tabs[i].button.onClick.AddListener(() => Select(index));
            }
        }
    }
}
