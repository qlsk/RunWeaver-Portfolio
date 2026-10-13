using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 보상 카드 1장의 연출. RewardSelectPresentation이 순서를 정해 부르고, 시간 계산은 여기서 한다.
/// - 등장 : 아래에서 살짝 떠오르며 나타나고, 테두리는 모서리부터 그려진 뒤 아이콘 → 글자 순서로 나타난다.
/// - 선택 : 테두리가 밝아지고 빛 테두리(SelectGlow)가 켜지며, 가장자리를 따라 잿불이 떠오른다.
/// - 어둡게 : 다른 카드가 선택되면 그림자(Shade)로 어두워진다.
/// - 타들어감 : 아래부터 타 없어진다 (리롤, 고르지 않은 카드).
/// - 확정 : 룬 문양(Sigil)이 찍히듯 번쩍이고 금빛 파동(Wave)이 퍼진 뒤 조용히 사라진다.
/// 그림은 모두 프리팹에 할당한 이미지를 쓰고, 타들어감·모서리 그리기는 MAT_UIRewardCard 셰이더가 맡는다.
/// 보상 UI는 게임 시간을 멈춘 채 열리므로 Time.unscaledDeltaTime으로 흐른다.
/// </summary>
public class RewardCardPresentation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly int s_cardRectId = Shader.PropertyToID("_CardRect");
    private static readonly int s_cornerRevealId = Shader.PropertyToID("_CornerReveal");
    private static readonly int s_burnId = Shader.PropertyToID("_Burn");

    // 셰이더(UIRewardCard)의 타는 경계 계산과 같은 값. 1이면 카드가 전부 탄다.
    private const float BurnRange = 1.9f;

    [Header("그림")]
    [SerializeField] private Image _frame;
    [SerializeField] private Text _selectionLabel;
    [Tooltip("카드 바탕")]
    [SerializeField] private Image _body;
    [SerializeField] private Graphic _icon;
    [Tooltip("카테고리, 이름, 설명 글자")]
    [SerializeField] private Graphic[] _texts;
    [Tooltip("다른 카드가 선택됐을 때 카드를 덮는 어두운 이미지")]
    [SerializeField] private Image _shade;
    [Tooltip("선택했을 때 켜지는 빛 테두리")]
    [SerializeField] private Image _selectGlow;
    [Tooltip("확정할 때 찍히는 룬 문양")]
    [SerializeField] private Image _sigil;
    [Tooltip("확정할 때 바깥으로 퍼지는 빛 테두리")]
    [SerializeField] private Image _wave;
    [Tooltip("타들어감·모서리 그리기 셰이더 머티리얼 (MAT_UIRewardCard). 카드마다 복사해서 쓴다")]
    [SerializeField] private Material _cardMaterial;

    [Header("테두리 색")]
    [SerializeField] private Color _normalFrameColor = new(0.8f, 0.68f, 0.47f, 1f);
    [SerializeField] private Color _selectedFrameColor = new(1f, 0.83f, 0.48f, 1f);
    [SerializeField] private Color _hoverFrameColor = new(0.92f, 0.82f, 0.62f, 1f);
    [SerializeField, Min(0.01f)] private float _transitionDuration = 0.12f;
    [Tooltip("마우스를 올렸을 때 크기. 1이면 바뀌지 않는다")]
    [SerializeField, Range(1f, 1.1f)] private float _hoverScale = 1f;

    [Header("등장")]
    [SerializeField, Min(0.01f)] private float _appearDuration = 0.5f;
    [Tooltip("아래에서 떠오르는 거리")]
    [SerializeField] private float _appearOffset = 28f;
    [Tooltip("테두리가 모서리부터 다 그려지는 시간")]
    [SerializeField, Min(0.01f)] private float _frameDrawDuration = 0.6f;
    [SerializeField, Min(0f)] private float _iconDelay = 0.25f;
    [SerializeField, Min(0f)] private float _textDelay = 0.35f;
    [SerializeField, Min(0.01f)] private float _contentFadeDuration = 0.35f;

    [Header("선택")]
    [SerializeField, Min(0.01f)] private float _selectFadeDuration = 0.2f;
    [Tooltip("다른 카드가 선택됐을 때 어두워지는 정도 (그림자 투명도)")]
    [SerializeField, Range(0f, 1f)] private float _dimAlpha = 0.42f;
    [Tooltip("선택 중 테두리를 따라 잿불이 생기는 간격 (초)")]
    [SerializeField, Min(0.01f)] private float _selectEmberInterval = 0.09f;
    [SerializeField] private Color _selectEmberColor = new(1f, 0.83f, 0.48f, 0.85f);

    [Header("타들어감")]
    [SerializeField, Min(0.01f)] private float _burnDuration = 0.7f;
    [Tooltip("한 프레임에 타는 경계에서 생기는 불티 수")]
    [SerializeField, Min(0)] private int _burnEmbersPerFrame = 3;
    [SerializeField] private Color _burnEmberColor = new(1f, 0.59f, 0.27f, 1f);

    [Header("확정")]
    [Tooltip("룬 문양 크기 (시작, 끝)")]
    [SerializeField] private Vector2 _sigilScale = new(1.25f, 1.1f);
    [SerializeField, Min(0.01f)] private float _sigilPeak = 0.05f;
    [SerializeField, Min(0.01f)] private float _sigilDuration = 0.55f;
    [Tooltip("파동이 퍼지는 거리 (픽셀)")]
    [SerializeField] private float _waveDistance = 70f;
    [SerializeField, Min(0.01f)] private float _waveDuration = 0.55f;
    [Tooltip("확정 후 카드가 사라지기 시작하는 시간")]
    [SerializeField, Min(0f)] private float _chosenFadeDelay = 0.75f;
    [SerializeField, Min(0.01f)] private float _chosenFadeDuration = 0.4f;

    private RewardSelectPresentation _owner;
    private RectTransform _rect;
    private CanvasGroup _group;
    private Material _contentMaterial;
    private Material _frameMaterial;
    private Vector2 _basePosition;
    private Vector2 _waveBaseSize;
    private Vector3 _baseScale;
    private bool _initialized;

    private bool _hovered;
    private bool _selected;
    private bool _dimmed;
    private float _selectBlend;
    private float _dimBlend;
    private float _emberTimer;

    private bool _appearing;
    private bool _burning;
    private bool _chosen;
    private float _appearTime;
    private float _burnTime;
    private float _chosenTime;

    public event System.Action HoverEntered;

    /// <summary>등장·타들어감·확정 연출 중이면 true</summary>
    public bool IsAnimating =>
        (_appearing && _appearTime < AppearDuration)
        || (_burning && _burnTime < _burnDuration)
        || (_chosen && _chosenTime < ChosenDuration);

    /// <summary>확정 연출이 끝나기까지 걸리는 시간</summary>
    public float ChosenDuration => _chosenFadeDelay + _chosenFadeDuration;

    public float BurnDuration => _burnDuration;

    public float AppearDuration => Mathf.Max(_appearDuration, _frameDrawDuration, _textDelay + _contentFadeDuration);

    public void Initialize(RewardSelectPresentation owner)
    {
        _owner = owner;
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _rect = (RectTransform)transform;
        _basePosition = _rect.anchoredPosition;
        _baseScale = transform.localScale;
        _group = GetComponent<CanvasGroup>();
        if (_wave != null)
        {
            _waveBaseSize = _wave.rectTransform.sizeDelta;
        }

        if (_cardMaterial != null)
        {
            _contentMaterial = new Material(_cardMaterial) { name = _cardMaterial.name + " (Content)" };
            _frameMaterial = new Material(_cardMaterial) { name = _cardMaterial.name + " (Frame)" };
            SetMaterial(_body, _contentMaterial);
            SetMaterial(_icon, _contentMaterial);
            SetMaterial(_shade, _contentMaterial);
            if (_texts != null)
            {
                foreach (Graphic text in _texts)
                {
                    SetMaterial(text, _contentMaterial);
                }
            }

            SetMaterial(_frame, _frameMaterial);
        }
    }

    /// <summary>모든 연출을 지우고 다 보이는 상태로 둔다.</summary>
    public void ResetVisual(bool visible)
    {
        _appearing = false;
        _burning = false;
        _chosen = false;
        _selected = false;
        _dimmed = false;
        _selectBlend = 0f;
        _dimBlend = 0f;
        Apply(visible ? 1f : 0f, 0f, 1f, visible ? 1f : 0f, visible ? 1f : 0f, 0f);
        HideEffects();
    }

    public void PlayAppear(float delay)
    {
        ResetVisual(false);
        _appearing = true;
        _appearTime = -delay;
    }

    public void PlayBurn(float delay)
    {
        _burning = true;
        _burnTime = -delay;
        _selected = false;
        if (_group != null)
        {
            _group.blocksRaycasts = false;
        }
    }

    public void PlayChosen()
    {
        _chosen = true;
        _chosenTime = 0f;
        _dimmed = false;
        if (_group != null)
        {
            _group.blocksRaycasts = false;
        }
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        if (_selectionLabel != null)
        {
            _selectionLabel.enabled = selected;
        }
    }

    public void SetDimmed(bool dimmed)
    {
        _dimmed = dimmed;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_hovered)
        {
            HoverEntered?.Invoke();
        }

        _hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData) => _hovered = false;

    private void Update()
    {
        if (!_initialized)
        {
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;
        float blend = 1f - Mathf.Exp(-deltaTime * 5f / _transitionDuration);

        _selectBlend = Mathf.MoveTowards(_selectBlend, _selected ? 1f : 0f, deltaTime / _selectFadeDuration);
        _dimBlend = Mathf.MoveTowards(_dimBlend, _dimmed ? 1f : 0f, deltaTime / _selectFadeDuration);

        // 등장
        float cardAlpha = 1f;
        float offset = 0f;
        float corner = 1f;
        float iconAlpha = 1f;
        float textAlpha = 1f;
        if (_appearing)
        {
            _appearTime += deltaTime;
            float t = Mathf.Max(0f, _appearTime);
            float rise = EaseOut(t / _appearDuration);
            cardAlpha = _appearTime < 0f ? 0f : rise;
            offset = _appearOffset * (1f - rise);
            corner = _appearTime < 0f ? 0f : EaseOut(t / _frameDrawDuration);
            iconAlpha = _appearTime < 0f ? 0f : SmoothStep((t - _iconDelay) / (_contentFadeDuration * 0.85f));
            textAlpha = _appearTime < 0f ? 0f : SmoothStep((t - _textDelay) / _contentFadeDuration);
        }

        // 타들어감
        float burn = 0f;
        if (_burning)
        {
            _burnTime += deltaTime;
            burn = Mathf.Clamp01(_burnTime / _burnDuration);
            if (_burnTime >= 0f && burn < 1f)
            {
                SpawnBurnEmbers(burn);
            }
        }

        // 확정
        float chosenFade = 1f;
        if (_chosen)
        {
            _chosenTime += deltaTime;
            chosenFade = 1f - SmoothStep((_chosenTime - _chosenFadeDelay) / _chosenFadeDuration);
            UpdateChosenEffects(_chosenTime);
        }

        if (_burning && burn >= 1f)
        {
            cardAlpha = 0f;
        }

        Apply(cardAlpha * chosenFade, offset, corner, iconAlpha, textAlpha, burn);

        // 테두리 색과 선택 빛
        if (_frame != null)
        {
            Color target = _selected || _chosen ? _selectedFrameColor : _hovered ? _hoverFrameColor : _normalFrameColor;
            _frame.color = Color.Lerp(_frame.color, target, blend);
        }

        transform.localScale = Vector3.Lerp(transform.localScale,
            _baseScale * (_hovered && !_burning && !_chosen ? _hoverScale : 1f), blend);

        float glow = Mathf.Max(_selectBlend, _chosen ? 1f : 0f) * cardAlpha * chosenFade * (1f - burn);
        SetAlpha(_selectGlow, glow);
        SetAlpha(_shade, _dimBlend * _dimAlpha * cardAlpha);

        if (_selected && !_burning && !_chosen && cardAlpha > 0.5f)
        {
            _emberTimer += deltaTime;
            while (_emberTimer >= _selectEmberInterval)
            {
                _emberTimer -= _selectEmberInterval;
                SpawnEdgeEmber();
            }
        }
    }

    private void Apply(float cardAlpha, float offset, float corner, float iconAlpha, float textAlpha, float burn)
    {
        if (_rect != null)
        {
            _rect.anchoredPosition = _basePosition + Vector2.down * offset;
        }

        if (_group != null)
        {
            _group.alpha = cardAlpha;
            _group.blocksRaycasts = cardAlpha > 0.5f && !_burning && !_chosen;
        }

        SetRendererAlpha(_icon, iconAlpha);
        if (_texts != null)
        {
            foreach (Graphic text in _texts)
            {
                SetRendererAlpha(text, textAlpha);
            }
        }

        UpdateMaterialRect();
        if (_frameMaterial != null)
        {
            _frameMaterial.SetFloat(s_cornerRevealId, corner);
            _frameMaterial.SetFloat(s_burnId, burn);
        }

        if (_contentMaterial != null)
        {
            _contentMaterial.SetFloat(s_burnId, burn);
        }
    }

    private void UpdateChosenEffects(float t)
    {
        if (_sigil != null)
        {
            float alpha = Bump(t, 0f, _sigilPeak, _sigilDuration);
            _sigil.enabled = alpha > 0f;
            float scale = Mathf.Lerp(_sigilScale.x, _sigilScale.y, EaseOut(t / (_sigilDuration * 0.55f)));
            _sigil.rectTransform.localScale = Vector3.one * scale;
            SetAlpha(_sigil, alpha);
        }

        if (_wave != null)
        {
            float p = Mathf.Clamp01(t / _waveDuration);
            _wave.enabled = p < 1f;
            float grow = _waveDistance * 2f * EaseOut(p);
            _wave.rectTransform.sizeDelta = _waveBaseSize + new Vector2(grow, grow);
            SetAlpha(_wave, 1f - SmoothStep(p));
        }
    }

    private void HideEffects()
    {
        SetAlpha(_selectGlow, 0f);
        SetAlpha(_shade, 0f);
        if (_sigil != null)
        {
            _sigil.enabled = false;
        }

        if (_wave != null)
        {
            _wave.enabled = false;
            _wave.rectTransform.sizeDelta = _waveBaseSize;
        }

        if (_selectionLabel != null)
        {
            _selectionLabel.enabled = false;
        }
    }

    private void SpawnBurnEmbers(float burn)
    {
        if (_owner == null || !TryGetCanvasRect(out Rect rect))
        {
            return;
        }

        // 셰이더의 경계(노이즈 평균값 기준)와 같은 높이에서 불티를 만든다.
        float threshold = burn * BurnRange - 0.3f;
        float height = Mathf.Clamp01((threshold - 0.3f) / 0.9f);
        for (int i = 0; i < _burnEmbersPerFrame; i++)
        {
            Vector2 position = new(
                rect.xMin + Random.value * rect.width,
                rect.yMin + Mathf.Clamp01(height + Random.Range(-0.06f, 0.06f)) * rect.height);
            _owner.SpawnEmber(position, _burnEmberColor, new Vector2(Random.Range(-12f, 12f), Random.Range(30f, 70f)),
                Random.Range(0.35f, 0.7f), Random.Range(5f, 9f));
        }
    }

    private void SpawnEdgeEmber()
    {
        if (_owner == null || !TryGetCanvasRect(out Rect rect))
        {
            return;
        }

        bool left = Random.value < 0.5f;
        Vector2 position = new(left ? rect.xMin + 4f : rect.xMax - 4f, rect.yMin + Random.value * rect.height * 0.6f);
        _owner.SpawnEmber(position, _selectEmberColor, new Vector2(Random.Range(-4f, 4f), Random.Range(30f, 60f)),
            Random.Range(0.8f, 1.4f), Random.Range(4f, 7f));
    }

    private void UpdateMaterialRect()
    {
        if (_contentMaterial == null || !TryGetCanvasRect(out Rect rect))
        {
            return;
        }

        Vector4 value = new(rect.xMin, rect.yMin, rect.width, rect.height);
        _contentMaterial.SetVector(s_cardRectId, value);
        _frameMaterial.SetVector(s_cardRectId, value);
    }

    /// <summary>카드 영역을 루트 캔버스 공간으로 구한다. (셰이더와 불티가 같은 공간을 쓴다)</summary>
    private bool TryGetCanvasRect(out Rect rect)
    {
        rect = default;
        Transform canvasTransform = _owner != null ? _owner.CanvasTransform : null;
        if (_rect == null || canvasTransform == null)
        {
            return false;
        }

        Vector3[] corners = RewardSelectPresentation.CornerBuffer;
        _rect.GetWorldCorners(corners);
        Vector2 min = canvasTransform.InverseTransformPoint(corners[0]);
        Vector2 max = canvasTransform.InverseTransformPoint(corners[2]);
        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    private void OnDisable()
    {
        _hovered = false;
        if (_initialized)
        {
            transform.localScale = _baseScale;
        }
    }

    private void OnDestroy()
    {
        if (_contentMaterial != null)
        {
            Destroy(_contentMaterial);
        }

        if (_frameMaterial != null)
        {
            Destroy(_frameMaterial);
        }
    }

    private static void SetMaterial(Graphic graphic, Material material)
    {
        if (graphic != null)
        {
            graphic.material = material;
        }
    }

    private static void SetRendererAlpha(Graphic graphic, float alpha)
    {
        if (graphic != null)
        {
            graphic.canvasRenderer.SetAlpha(alpha);
        }
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
        {
            return;
        }

        graphic.enabled = alpha > 0.001f;
        graphic.canvasRenderer.SetAlpha(alpha);
    }

    private static float Bump(float t, float start, float peak, float end)
    {
        if (t <= start || t >= end)
        {
            return 0f;
        }

        return t < peak ? (t - start) / Mathf.Max(0.0001f, peak - start) : (end - t) / Mathf.Max(0.0001f, end - peak);
    }

    private static float EaseOut(float value)
    {
        value = Mathf.Clamp01(value);
        float inverse = 1f - value;
        return 1f - inverse * inverse * inverse;
    }

    private static float SmoothStep(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }
}
