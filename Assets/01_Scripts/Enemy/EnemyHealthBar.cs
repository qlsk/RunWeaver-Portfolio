using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Vector2 _size = new(0.8f, 0.09f);
    [SerializeField] private float _headGap = 0.18f;
    [SerializeField] private Color _fillColor = new(0.85f, 0.15f, 0.18f, 1f);

    private EnemyHealth _health;
    private RectTransform _root;
    private Slider _slider;
    private float _height;

    public void Initialize(EnemyHealth health)
    {
        if (_health != null) _health.OnDamaged -= Refresh;
        _health = health;
        if (_root == null) CreateBar();
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
        if (_root != null) _root.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_health != null) _health.OnDamaged -= Refresh;
        if (_root != null) Destroy(_root.gameObject);
    }

    private void CreateBar()
    {
        var canvasObject = new GameObject("HealthBarCanvas", typeof(RectTransform), typeof(Canvas));
        _root = canvasObject.GetComponent<RectTransform>();
        _root.SetParent(transform, false);
        _root.sizeDelta = new Vector2(100f, 12f);
        _root.localScale = new Vector3(_size.x / 100f, _size.y / 12f, 1f);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        _height = 1f;
        var body = GetComponentInChildren<SpriteRenderer>();
        if (body != null)
        {
            _height = body.bounds.max.y - transform.position.y;
            canvas.sortingLayerID = body.sortingLayerID;
            canvas.sortingOrder = body.sortingOrder + 10;
        }

        var sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        var sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.SetParent(_root, false);
        Stretch(sliderRect, 0f);
        _slider = sliderObject.GetComponent<Slider>();
        _slider.interactable = false;
        _slider.transition = Selectable.Transition.None;
        _slider.navigation = new Navigation { mode = Navigation.Mode.None };
        _slider.minValue = 0f;
        _slider.maxValue = 1f;
        _slider.wholeNumbers = false;
        _slider.direction = Slider.Direction.LeftToRight;

        CreateImage("Background", sliderRect, new Color(0.08f, 0.08f, 0.1f, 0.9f));
        var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(sliderRect, false);
        Stretch(fillArea, 2f);
        _slider.fillRect = CreateImage("Fill", fillArea, _fillColor).rectTransform;
        UpdatePosition();
    }

    private static Image CreateImage(string name, RectTransform parent, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        image.rectTransform.SetParent(parent, false);
        Stretch(image.rectTransform, 0f);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = Vector2.one * -inset;
    }

    private void Refresh()
    {
        if (_root == null || _health == null) return;
        _slider.SetValueWithoutNotify(_health.MaxHealth > 0f
            ? Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth) : 0f);
        _root.gameObject.SetActive(isActiveAndEnabled && !_health.IsDead);
    }

    private void LateUpdate()
    {
        if (_root != null && _root.gameObject.activeSelf) UpdatePosition();
    }

    private void UpdatePosition()
    {
        _root.position = transform.position + Vector3.up * (_height + _headGap);
        _root.rotation = Quaternion.identity;
    }
}
