using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 오른쪽 위의 메뉴 안내 (클릭은 되지 않는 표시). [룬북 아이콘][TAB]  [일시정지 아이콘][ESC]
/// - 초반 층에서는 또렷하고, _fadeFromDepth 층부터는 옅게 보인다.
/// - 새로 장착할 수 있게 된 룬 워드가 생기면(룬 문자가 모이거나 완성품을 얻어서) 룬북 아이콘에 하늘색 점과 푸른빛이 켜진다.
///   룬북을 열면 꺼지고, 그 뒤 또 새로 장착 가능한 룬 워드가 생기면 다시 켜진다.
/// - 이미지는 Resources/HudHints에서 불러와 전용 캔버스를 만든다. 씬이나 프리팹을 고치지 않아도 된다.
/// RuneBookV2Toggle이 만든다. (룬북을 쓸 수 있는 전투 씬에서만 보인다)
/// </summary>
[DisallowMultipleComponent]
public class HudMenuHints : MonoBehaviour
{
    private const string Folder = "HudHints/";
    private const float CheckInterval = 0.5f;

    [SerializeField] private int _fadeFromDepth = 3;
    [SerializeField, Range(0f, 1f)] private float _fadedAlpha = 0.45f;
    [SerializeField] private Vector2 _margin = new(24f, 22f);
    [SerializeField] private float _iconSize = 58f;
    [SerializeField] private float _keyHeight = 36f;
    [SerializeField] private float _groupGap = 26f;
    [SerializeField] private Color _notifyColor = new(0.5f, 0.82f, 1f, 1f);
    [Tooltip("일시정지, 룬북 같은 팝업보다 아래에 그린다")]
    [SerializeField] private int _canvasSortingOrder = -5;

    private RuneBookV2Toggle _toggle;
    private GameObject _canvasObject;
    private CanvasGroup _group;
    private Image _bookIcon;
    private Image _dot;
    private Image _glow;
    private readonly HashSet<string> _seen = new();
    private readonly HashSet<string> _affordable = new();
    private readonly Dictionary<char, int> _cost = new();
    private bool _hasNotification;
    private bool _wasOpen;
    private float _nextCheck;
    private float _notifyTime;
    private bool _primed;

    public static HudMenuHints Create(RuneBookV2Toggle toggle)
    {
        HudMenuHints hints = toggle.gameObject.GetComponent<HudMenuHints>();
        if (hints == null)
        {
            hints = toggle.gameObject.AddComponent<HudMenuHints>();
        }

        hints._toggle = toggle;
        hints.Build();
        return hints;
    }

    private void Build()
    {
        if (_canvasObject != null)
        {
            return;
        }

        _canvasObject = new GameObject("HudMenuHints", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(CanvasGroup));
        var canvas = _canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = _canvasSortingOrder;
        var scaler = _canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _group = _canvasObject.GetComponent<CanvasGroup>();
        _group.blocksRaycasts = false;
        _group.interactable = false;

        // 오른쪽에서 왼쪽으로 : [일시정지][ESC]  [룬북][TAB]
        float x = -_margin.x;
        x = AddKey(x, "HudHint_KeyEsc");
        x = AddIcon(x - 6f, "HudHint_Pause", out _);
        x -= _groupGap;
        x = AddKey(x, "HudHint_KeyTab");
        AddIcon(x - 6f, "HudHint_RuneBook", out _bookIcon);

        // 알림 : 아이콘 뒤 빛과 오른쪽 위 점
        if (_bookIcon != null)
        {
            _glow = CreateImage("Glow", "HudHint_Glow", _bookIcon.rectTransform, _iconSize * 1.9f);
            _glow.rectTransform.SetAsFirstSibling();
            _glow.transform.SetParent(_bookIcon.transform.parent, true);
            _glow.transform.SetSiblingIndex(_bookIcon.transform.GetSiblingIndex());
            _dot = CreateImage("Dot", "HudHint_Dot", _bookIcon.rectTransform, 20f);
            _dot.rectTransform.anchorMin = _dot.rectTransform.anchorMax = new Vector2(0.86f, 0.86f);
            _dot.rectTransform.anchoredPosition = Vector2.zero;
        }

        SetNotification(false);
    }

    private float AddKey(float right, string sprite)
    {
        Image image = CreateImage(sprite, sprite, (RectTransform)_canvasObject.transform, 0f);
        if (image == null)
        {
            return right;
        }

        Vector2 native = image.sprite.rect.size;
        float width = native.x * (_keyHeight / native.y);
        Place(image.rectTransform, right, width, _keyHeight, _iconSize);
        return right - width;
    }

    private float AddIcon(float right, string sprite, out Image image)
    {
        image = CreateImage(sprite, sprite, (RectTransform)_canvasObject.transform, 0f);
        if (image == null)
        {
            return right;
        }

        Place(image.rectTransform, right, _iconSize, _iconSize, _iconSize);
        return right - _iconSize;
    }

    /// <summary>오른쪽 위 기준으로 놓는다. 줄 높이(rowHeight)의 가운데에 세로 정렬한다.</summary>
    private void Place(RectTransform rect, float right, float width, float height, float rowHeight)
    {
        rect.anchorMin = rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(right, -_margin.y - rowHeight * 0.5f);
    }

    private static Image CreateImage(string name, string sprite, RectTransform parent, float size)
    {
        Sprite loaded = Resources.Load<Sprite>(Folder + sprite);
        if (loaded == null)
        {
            return null;
        }

        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        if (size > 0f)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = Vector2.zero;
        }

        var image = go.GetComponent<Image>();
        image.sprite = loaded;
        image.raycastTarget = false;
        return image;
    }

    private void Update()
    {
        if (_group == null)
        {
            return;
        }

        StageManager stage = StageManager.Instance;
        int depth = stage != null ? stage.CurrentDepth : 1;
        float baseAlpha = depth >= _fadeFromDepth ? _fadedAlpha : 1f;

        bool isOpen = _toggle != null && _toggle.IsOpen;
        if (Time.unscaledTime >= _nextCheck)
        {
            _nextCheck = Time.unscaledTime + CheckInterval;
            RefreshAffordable();
        }

        // 룬북을 열면 지금 장착 가능한 것들을 확인한 것으로 보고 알림을 끈다.
        if (isOpen)
        {
            _seen.UnionWith(_affordable);
        }

        bool notify = false;
        foreach (string id in _affordable)
        {
            if (!_seen.Contains(id))
            {
                notify = true;
                break;
            }
        }

        if (notify != _hasNotification)
        {
            SetNotification(notify);
        }

        _wasOpen = isOpen;
        _group.alpha = 1f;
        float iconAlpha = _hasNotification ? 1f : baseAlpha;
        foreach (Transform child in _canvasObject.transform)
        {
            if (child.TryGetComponent(out Image image) && image != _glow)
            {
                Color color = image.color;
                color.a = child == _bookIcon?.transform ? iconAlpha : baseAlpha;
                image.color = color;
            }
        }

        if (_hasNotification)
        {
            float t = Time.unscaledTime - _notifyTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
            if (_glow != null)
            {
                Color glow = _notifyColor;
                glow.a = 0.35f + 0.3f * pulse;
                _glow.color = glow;
            }

            if (_dot != null)
            {
                float pop = 1f + 0.5f * Mathf.Clamp01(1f - t / 0.15f);
                _dot.rectTransform.localScale = Vector3.one * pop;
            }
        }
    }

    private void SetNotification(bool isOn)
    {
        _hasNotification = isOn;
        _notifyTime = Time.unscaledTime;
        if (_glow != null) _glow.enabled = isOn;
        if (_dot != null) _dot.enabled = isOn;
    }

    /// <summary>지금 새로 장착할 수 있는 룬 워드 (룬 문자가 충분하거나 완성품이 있고, 아직 장착하지 않았거나 겹쳐 넣을 수 있음)</summary>
    private void RefreshAffordable()
    {
        _affordable.Clear();
        PlayerRuneManager manager = PlayerRuneManager.Instance;
        RuneEffectDatabase database = RuneEffectDatabase.Instance;
        if (manager == null || !manager.IsInitialized || database == null || !database.DatabaseReady)
        {
            return;
        }

        RuneEffectLoadout loadout = manager.RuneEffectLoadout;
        foreach (RuneEffectDefinition effect in database.GetAll())
        {
            if (effect == null || string.IsNullOrEmpty(effect.RuneWord))
            {
                continue;
            }

            if (loadout != null && loadout.GetEquippedCount(effect.Id) > 0 && !loadout.CanOverlap(effect.Id))
            {
                continue;
            }

            bool affordable = OwnedRuneWords.GetCount(effect.Id) > 0;
            if (!affordable)
            {
                _cost.Clear();
                foreach (char rune in effect.RuneWord)
                {
                    if (char.IsWhiteSpace(rune)) continue;
                    _cost[rune] = _cost.TryGetValue(rune, out int count) ? count + 1 : 1;
                }

                affordable = _cost.Count > 0 && manager.RuneStoneInventory.HasEnoughRuneStones(_cost);
            }

            if (affordable)
            {
                _affordable.Add(effect.Id);
            }
        }

        // 처음 확인한 것(런 시작 때 이미 가능한 것)은 알림 대상이 아니다.
        if (!_primed)
        {
            _primed = true;
            _seen.UnionWith(_affordable);
        }
    }

    private void OnDestroy()
    {
        if (_canvasObject != null)
        {
            Destroy(_canvasObject);
        }
    }
}
