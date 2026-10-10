using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 체력 위험 연출. 체력이 낮으면 화면 가장자리가 천천히 붉어졌다 가라앉고(약 2초),
/// 가장자리에서 안쪽으로 연한 붉은 혈관 같은 금이 함께 짙어졌다 옅어진다. 체력바도 같은 호흡으로 밝아진다.
/// - _startRatio(35%) 아래에서 서서히 나타나 _fullRatio(15%)에서 가장 진하다. 회복하면 사라진다.
/// - 이미지는 Resources/LowHealth에서 불러와 씬 최상위의 전용 캔버스(HUD보다 아래 순서)에 깐다. 씬이나 프리팹을 고치지 않아도 된다.
/// - PlayerHealthHudBinder가 붙이고 Initialize로 체력과 체력바를 넘겨준다.
/// </summary>
[DisallowMultipleComponent]
public class LowHealthOverlay : MonoBehaviour
{
    private const string VignettePath = "LowHealth/LowHealth_Vignette";
    private const string VeinsPath = "LowHealth/LowHealth_Veins";

    [SerializeField, Range(0f, 1f)] private float _startRatio = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _fullRatio = 0.15f;
    [SerializeField, Min(0.1f)] private float _breathPeriod = 2f;
    [SerializeField] private Color _vignetteColor = new(0.6f, 0.04f, 0.06f, 1f);
    [SerializeField, Range(0f, 1f)] private float _vignetteMin = 0.22f;
    [SerializeField, Range(0f, 1f)] private float _vignetteMax = 0.6f;
    [SerializeField, Range(0f, 1f)] private float _veinsMin = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _veinsMax = 0.8f;
    [SerializeField] private Color _barBrightColor = new(1f, 0.67f, 0.63f, 1f);
    [Tooltip("HUD 캔버스보다 아래(뒤)에 그린다")]
    [SerializeField] private int _canvasSortingOrder = -10;
    [Tooltip("체력이 낮아질 때 나타나는 데 걸리는 시간 (초)")]
    [SerializeField, Min(0.01f)] private float _fadeInDuration = 0.6f;
    [Tooltip("체력이 회복됐을 때 사라지는 데 걸리는 시간 (초). 뚝 끊기지 않고 천천히 걷힌다")]
    [SerializeField, Min(0.01f)] private float _fadeOutDuration = 1.6f;

    private PlayerHealth _health;
    private Image _barFill;
    private Color _barBaseColor;
    private Image _vignette;
    private Image _veins;
    private float _weight;
    private GameObject _canvasObject;

    public void Initialize(PlayerHealth health, HealthBarView barView)
    {
        _health = health;
        _barFill = barView != null ? barView.FillImage : null;
        if (_barFill != null)
        {
            _barBaseColor = _barFill.color;
        }

        Build();
    }

    private void Build()
    {
        if (_vignette != null)
        {
            return;
        }

        // HUD 캔버스 안에 중첩 캔버스를 만들면 HUD 그리기에 간섭할 수 있어서, 씬 최상위에 따로 만든다.
        _canvasObject = new GameObject("LowHealthOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        GameObject canvasObject = _canvasObject;
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = _canvasSortingOrder;

        _vignette = CreateLayer(canvasObject.transform, "Vignette", Resources.Load<Sprite>(VignettePath));
        _veins = CreateLayer(canvasObject.transform, "Veins", Resources.Load<Sprite>(VeinsPath));
        Apply(0f, 0f);
    }

    private static Image CreateLayer(Transform parent, string name, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.enabled = sprite != null;
        return image;
    }

    private void LateUpdate()
    {
        if (_health == null || _vignette == null)
        {
            return;
        }

        float ratio = _health.MaxHealth > 0f ? _health.CurrentHealth / _health.MaxHealth : 1f;
        float target = _health.CurrentHealth > 0f
            ? Mathf.Clamp01((_startRatio - ratio) / Mathf.Max(0.01f, _startRatio - _fullRatio))
            : 0f;
        // 켜질 때보다 꺼질 때 더 천천히. 끝부분도 부드럽게(SmoothStep) 걷힌다.
        float duration = target > _weight ? _fadeInDuration : _fadeOutDuration;
        _weight = Mathf.MoveTowards(_weight, target, Time.unscaledDeltaTime / duration);

        float breath = 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime * 2f * Mathf.PI / _breathPeriod);
        Apply(Mathf.SmoothStep(0f, 1f, _weight), breath);
    }

    private void Apply(float weight, float breath)
    {
        if (_vignette != null)
        {
            Color color = _vignetteColor;
            color.a = Mathf.Lerp(_vignetteMin, _vignetteMax, breath) * weight;
            _vignette.color = color;
        }

        if (_veins != null)
        {
            _veins.color = new Color(1f, 1f, 1f, Mathf.Lerp(_veinsMin, _veinsMax, breath) * weight);
        }

        if (_barFill != null)
        {
            _barFill.color = Color.Lerp(_barBaseColor, _barBrightColor, breath * weight * 0.8f);
        }
    }

    private void OnDisable()
    {
        Apply(0f, 0f);
        _weight = 0f;
    }

    private void OnDestroy()
    {
        if (_canvasObject != null)
        {
            Destroy(_canvasObject);
        }
    }
}
