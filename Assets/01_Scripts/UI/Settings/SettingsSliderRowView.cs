using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 설정 창의 볼륨 한 줄. (이름 - 슬라이더 - 값)
/// - 오른쪽에 현재 값을 % 로 보여준다.
/// - 마우스를 올리거나 키보드로 슬라이더를 고르면 이름과 손잡이를 밝게 한다.
/// 실제 시간으로 움직인다. (일시정지 중에도 동작)
/// </summary>
[DisallowMultipleComponent]
public class SettingsSliderRowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Slider _slider;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private TMP_Text _valueText;
    [SerializeField] private Graphic _handle;
    [SerializeField] private Graphic _fill;
    [SerializeField] private string _valueFormat = "{0}%";

    [Header("색")]
    [SerializeField] private Color _labelColor = new(0.659f, 0.69f, 0.761f, 1f);
    [SerializeField] private Color _activeLabelColor = new(0.925f, 0.902f, 0.863f, 1f);
    [SerializeField] private Color _handleColor = new(0.72f, 0.8f, 0.95f, 1f);
    [SerializeField] private Color _activeHandleColor = Color.white;
    [SerializeField] private Color _fillColor = new(0.45f, 0.62f, 0.95f, 0.85f);
    [SerializeField] private Color _activeFillColor = new(0.6f, 0.78f, 1f, 1f);
    [SerializeField, Min(0f)] private float _sharpness = 14f;

    private bool _isHovered;
    private float _active;
    private int _shownValue = -1;

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        // 키보드로 고른 줄과 둘 다 밝아지지 않게, 마우스를 올린 줄의 슬라이더를 선택 항목으로 만든다.
        if (_slider != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(_slider.gameObject);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
    }

    private void OnEnable()
    {
        _shownValue = -1;
        _isHovered = false;
        _active = IsActive() ? 1f : 0f;
        Refresh();
    }

    private void Update()
    {
        _active = Mathf.Lerp(_active, IsActive() ? 1f : 0f, 1f - Mathf.Exp(-_sharpness * Time.unscaledDeltaTime));
        Refresh();
    }

    private bool IsActive()
    {
        if (_isHovered)
        {
            return true;
        }

        EventSystem system = EventSystem.current;
        return system != null && _slider != null && system.currentSelectedGameObject == _slider.gameObject;
    }

    private void Refresh()
    {
        if (_slider != null && _valueText != null)
        {
            int value = Mathf.RoundToInt(_slider.normalizedValue * 100f);
            if (value != _shownValue)
            {
                _shownValue = value;
                _valueText.text = string.Format(_valueFormat, value);
            }
        }

        if (_label != null) _label.color = Color.Lerp(_labelColor, _activeLabelColor, _active);
        if (_valueText != null) _valueText.color = Color.Lerp(_labelColor, _activeLabelColor, _active);
        if (_handle != null) _handle.color = Color.Lerp(_handleColor, _activeHandleColor, _active);
        if (_fill != null) _fill.color = Color.Lerp(_fillColor, _activeFillColor, _active);
    }
}
