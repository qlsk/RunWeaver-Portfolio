using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>보상 카드의 선택·호버 상태에 맞춰 테두리와 글로우를 갱신한다.</summary>
public class RewardCardPresentation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _frame;
    [SerializeField] private Text _selectionLabel;
    [SerializeField] private Color _normalFrameColor = new(0.8f, 0.68f, 0.47f, 1f);
    [SerializeField] private Color _selectedFrameColor = new(1f, 0.83f, 0.48f, 1f);
    [SerializeField] private Color _hoverFrameColor = new(1f, 1f, 1f, 1f);
    [SerializeField, Min(0.01f)] private float _transitionDuration = 0.12f;
    [SerializeField, Range(1f, 1.1f)] private float _hoverScale = 1.02f;

    private Outline _glow;
    private Vector3 _baseScale;
    private bool _selected;
    private bool _hovered;
    private float _selectionFlash;

    public event System.Action HoverEntered;

    private void Awake()
    {
        _baseScale = transform.localScale;
        if (_frame != null)
        {
            _glow = _frame.gameObject.AddComponent<Outline>();
            _glow.effectDistance = new Vector2(1.5f, 1.5f);
            _glow.useGraphicAlpha = true;
            _glow.effectColor = Color.clear;
        }
    }

    private void Update()
    {
        float blend = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 5f / _transitionDuration);
        Color frameColor = _selected ? _selectedFrameColor : _hovered ? _hoverFrameColor : _normalFrameColor;
        if (_frame != null)
            _frame.color = Color.Lerp(_frame.color, frameColor, blend);
        transform.localScale = Vector3.Lerp(transform.localScale, _baseScale * (_hovered ? _hoverScale : 1f), blend);

        _selectionFlash = Mathf.MoveTowards(_selectionFlash, 0f, Time.unscaledDeltaTime / 0.2f);
        if (_glow != null)
        {
            Color glowColor = _selected ? new Color(1f, 0.8f, 0.25f) : Color.white;
            glowColor.a = _selected ? 0.45f + _selectionFlash * 0.2f : _hovered ? 0.25f : 0f;
            _glow.effectColor = Color.Lerp(_glow.effectColor, glowColor, blend);
        }
    }

    public void SetSelected(bool selected)
    {
        if (selected && !_selected)
            _selectionFlash = 1f;
        _selected = selected;
        if (_selectionLabel != null)
            _selectionLabel.enabled = selected;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_hovered)
            HoverEntered?.Invoke();
        _hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData) => _hovered = false;

    private void OnDisable()
    {
        _hovered = false;
        _selected = false;
        _selectionFlash = 0f;
        transform.localScale = _baseScale;
        if (_frame != null)
            _frame.color = _normalFrameColor;
        if (_glow != null)
            _glow.effectColor = Color.clear;
        if (_selectionLabel != null)
            _selectionLabel.enabled = false;
    }
}
