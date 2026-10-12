using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 체력 위험 연출. 체력이 낮으면 화면 가장자리가 옅게 붉어졌다 가라앉는다(약 3초). 체력바도 같은 호흡으로 밝아진다.
/// - 연출은 Resources/LowHealth/LowHealthOverlay 프리팹(전용 캔버스, HUD보다 아래 순서)의 Animator 클립이 한다.
///   이 컴포넌트는 체력으로 "Danger" 값(0~1)만 정해 넘기고, 클립이 정한 체력바 밝기(BarGlow)를 체력바에 적용한다.
/// - _startRatio(35%) 아래에서 서서히 나타나 _fullRatio(15%)에서 가장 진하다. 회복하면 천천히 사라진다.
/// - 혈관 무늬는 과하다는 피드백으로 꺼 두었다. (프리팹의 Veins 오브젝트를 켜면 보인다)
/// - PlayerHealthHudBinder가 붙이고 Initialize로 체력과 체력바를 넘겨준다.
/// </summary>
[DisallowMultipleComponent]
public class LowHealthOverlay : MonoBehaviour
{
    private const string PrefabPath = "LowHealth/LowHealthOverlay";
    private static readonly int DangerHash = Animator.StringToHash("Danger");

    [SerializeField, Range(0f, 1f)] private float _startRatio = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _fullRatio = 0.15f;
    [SerializeField] private Color _barBrightColor = new(1f, 0.67f, 0.63f, 1f);
    [Tooltip("체력이 낮아질 때 나타나는 데 걸리는 시간 (초)")]
    [SerializeField, Min(0.01f)] private float _fadeInDuration = 0.6f;
    [Tooltip("체력이 회복됐을 때 사라지는 데 걸리는 시간 (초). 뚝 끊기지 않고 천천히 걷힌다")]
    [SerializeField, Min(0.01f)] private float _fadeOutDuration = 1.6f;

    private PlayerHealth _health;
    private Image _barFill;
    private Color _barBaseColor;
    private GameObject _overlay;
    private Animator _animator;
    private LowHealthOverlayView _view;
    private float _weight;

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
        if (_overlay != null)
        {
            return;
        }

        // HUD 캔버스 안에 중첩 캔버스를 두면 HUD 그리기에 간섭할 수 있어서, 씬 최상위에 따로 만든다.
        GameObject prefab = Resources.Load<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[LowHealthOverlay] Resources/{PrefabPath} 프리팹이 없습니다.", this);
            return;
        }

        _overlay = Instantiate(prefab);
        _overlay.name = prefab.name;
        _animator = _overlay.GetComponent<Animator>();
        _view = _overlay.GetComponent<LowHealthOverlayView>();
    }

    private void LateUpdate()
    {
        if (_health == null || _animator == null)
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
        _animator.SetFloat(DangerHash, Mathf.SmoothStep(0f, 1f, _weight));

        if (_barFill != null && _view != null)
        {
            _barFill.color = Color.Lerp(_barBaseColor, _barBrightColor, _view.BarGlow);
        }
    }

    private void OnDisable()
    {
        _weight = 0f;
        if (_animator != null)
        {
            _animator.SetFloat(DangerHash, 0f);
        }

        if (_barFill != null)
        {
            _barFill.color = _barBaseColor;
        }
    }

    private void OnDestroy()
    {
        if (_overlay != null)
        {
            Destroy(_overlay);
        }
    }
}
