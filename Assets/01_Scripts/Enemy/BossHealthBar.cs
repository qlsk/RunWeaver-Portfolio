using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private Vector2 _size = new(900f, 116f);
    [SerializeField] private float _topMargin = 32f;
    [SerializeField, Min(0.1f)] private float _animationFramesPerSecond = 8f;

    private EnemyHealth _health;
    private GameObject _canvasObject;
    private Slider _slider;
    private Sprite _frameSprite;
    private Image _fillImage;
    private Sprite[] _fillFrames;
    private float _animationTime;
    private int _frameIndex;

    public void Initialize(EnemyHealth health)
    {
        if (_health != null) _health.OnDamaged -= Refresh;
        _health = health;
        foreach (var worldBar in health.GetComponentsInChildren<EnemyHealthBar>(true))
        {
            worldBar.enabled = false;
            Destroy(worldBar);
        }
        if (_canvasObject == null) CreateBar();
        if (isActiveAndEnabled) _health.OnDamaged += Refresh;
        Refresh();
    }

    private void OnEnable()
    {
        if (_health == null) return;
        _health.OnDamaged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (_health != null) _health.OnDamaged -= Refresh;
        if (_canvasObject != null) _canvasObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_canvasObject != null) Destroy(_canvasObject);
        if (_frameSprite != null) Destroy(_frameSprite);
        if (_fillFrames != null)
            foreach (var frame in _fillFrames)
                if (frame != null) Destroy(frame);
    }

    private void CreateBar()
    {
        _canvasObject = new GameObject("BossHealthBarCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(_canvasObject, gameObject.scene);
        var canvas = _canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1;
        var runeBook = FindAnyObjectByType<RuneBookV2Presenter>(FindObjectsInactive.Include);
        var runeBookCanvas = runeBook != null ? runeBook.GetComponentInParent<Canvas>(true) : null;
        if (runeBookCanvas == null)
        {
            var legacyBook = FindAnyObjectByType<RuneBookPresenter>(FindObjectsInactive.Include);
            runeBookCanvas = legacyBook != null ? legacyBook.GetComponentInParent<Canvas>(true) : null;
        }
        if (runeBookCanvas != null)
        {
            var rootCanvas = runeBookCanvas.rootCanvas;
            canvas.renderMode = rootCanvas.renderMode;
            canvas.worldCamera = rootCanvas.worldCamera;
            canvas.planeDistance = rootCanvas.planeDistance;
            canvas.targetDisplay = rootCanvas.targetDisplay;
            canvas.sortingLayerID = rootCanvas.sortingLayerID;
            canvas.sortingOrder = rootCanvas.sortingOrder - 1;
        }
        var scaler = _canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var panel = new GameObject("BossHealthBar", typeof(RectTransform)).GetComponent<RectTransform>();
        panel.SetParent(_canvasObject.transform, false);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.sizeDelta = _size;
        panel.anchoredPosition = new Vector2(0f, -_topMargin);

        _frameSprite = LoadSprite("BossHealthBar/Frame", new Rect(0f, 220f, 2172f, 280f));
        CreateFillFrames();

        var track = new GameObject("Slider", typeof(RectTransform), typeof(Slider)).GetComponent<RectTransform>();
        track.SetParent(panel, false);
        track.anchorMin = new Vector2(0.135f, 0.37f);
        track.anchorMax = new Vector2(0.865f, 0.55f);
        track.offsetMin = track.offsetMax = Vector2.zero;
        _slider = track.GetComponent<Slider>();
        _slider.interactable = false;
        _slider.transition = Selectable.Transition.None;
        _slider.navigation = new Navigation { mode = Navigation.Mode.None };
        _slider.minValue = 0f;
        _slider.maxValue = 1f;
        _slider.wholeNumbers = false;
        _slider.direction = Slider.Direction.LeftToRight;

        AddImage("Background", track, null, new Color(0.015f, 0.04f, 0.04f, 1f));
        var fill = AddImage("Fill", track, _fillFrames != null ? _fillFrames[0] : null, Color.white);
        _fillImage = fill;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _slider.fillRect = fill.rectTransform;
        AddImage("Frame", panel, _frameSprite, Color.white);
    }

    private void CreateFillFrames()
    {
        var texture = Resources.Load<Texture2D>("BossHealthBar/Fill");
        if (texture == null)
        {
            Debug.LogError("[BossHealthBar] Missing texture: BossHealthBar/Fill", this);
            return;
        }

        // 원본 시트는 간격이 일정하지 않으므로 게이지 내부를 동일한 크기로 지정한다.
        // 위에서 아래로, 각 행의 왼쪽 → 오른쪽 순서로 재생한다.
        int[] rowTop = { 130, 424, 714 };
        int[] columnLeft = { 8, 807 };
        _fillFrames = new Sprite[6];
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 2; column++)
            {
                var rect = new Rect(columnLeft[column], texture.height - rowTop[row] - 46, 650, 46);
                _fillFrames[row * 2 + column] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
            }
        }
    }

    private void Update()
    {
        if (_fillFrames == null || _fillImage == null || !_canvasObject.activeSelf) return;
        _animationTime += Time.deltaTime * Mathf.Max(0.1f, _animationFramesPerSecond);
        if (_animationTime < 1f) return;
        int steps = Mathf.FloorToInt(_animationTime);
        _animationTime -= steps;
        _frameIndex = (_frameIndex + steps) % _fillFrames.Length;
        _fillImage.sprite = _fillFrames[_frameIndex];
    }

    private static Sprite LoadSprite(string path, Rect rect)
    {
        var texture = Resources.Load<Texture2D>(path);
        if (texture != null) return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
        Debug.LogError($"[BossHealthBar] Missing texture: {path}");
        return null;
    }

    private static Image AddImage(string name, RectTransform parent, Sprite sprite, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        image.rectTransform.SetParent(parent, false);
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void Refresh()
    {
        if (_health == null || _canvasObject == null) return;
        _slider.SetValueWithoutNotify(_health.MaxHealth > 0f
            ? Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth) : 0f);
        _canvasObject.SetActive(isActiveAndEnabled && !_health.IsDead);
    }
}
