using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 룬북의 탭(Rune Words / Reactions) 전환. 탭 버튼 클릭 또는 Q(이전 탭) / E(다음 탭)로 바꾼다.
/// 탭마다 켤 오브젝트 목록을 두고, 선택한 탭의 오브젝트만 켠다.
/// 선택한 탭은 선택 스프라이트로 바꾸고 조금 더 높게 올린다. (탭의 피벗은 아래쪽 기준)
/// 룬북을 다시 열어도 마지막으로 보던 탭을 유지한다.
/// </summary>
[DisallowMultipleComponent]
public class RuneBookTabView : MonoBehaviour
{
    [Serializable]
    private class Tab
    {
        public Button button;
        public Image buttonImage;
        public TMP_Text label;
        public GameObject[] pages;
    }

    [SerializeField] private Tab[] _tabs;

    [Header("키")]
    [SerializeField] private KeyCode _previousKey = KeyCode.Q;
    [SerializeField] private KeyCode _nextKey = KeyCode.E;

    [Header("탭 표시")]
    [SerializeField] private Sprite _activeSprite;
    [SerializeField] private Sprite _inactiveSprite;
    [SerializeField] private Color _activeLabelColor = new(0.957f, 0.886f, 0.769f, 1f);
    [SerializeField] private Color _inactiveLabelColor = new(0.745f, 0.627f, 0.549f, 1f);
    [Tooltip("선택한 탭 높이. 0이면 바꾸지 않는다.")]
    [SerializeField] private float _activeHeight;
    [Tooltip("선택하지 않은 탭 높이. 0이면 바꾸지 않는다.")]
    [SerializeField] private float _inactiveHeight;

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

            if (tab.buttonImage != null)
            {
                Sprite sprite = isActive ? _activeSprite : _inactiveSprite;
                if (sprite != null)
                {
                    tab.buttonImage.sprite = sprite;
                }

                float height = isActive ? _activeHeight : _inactiveHeight;
                if (height > 0f)
                {
                    RectTransform rect = tab.buttonImage.rectTransform;
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
                }
            }

            if (tab.label != null)
            {
                tab.label.color = isActive ? _activeLabelColor : _inactiveLabelColor;
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
