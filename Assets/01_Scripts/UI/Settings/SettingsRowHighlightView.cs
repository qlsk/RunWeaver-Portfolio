using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 설정 창의 한 줄(예: 전체 화면)을 마우스를 올리거나 키보드로 골랐을 때 이름을 밝게 한다.
/// 볼륨 줄은 SettingsSliderRowView가 같은 일을 한다.
/// 실제 시간으로 움직인다.
/// </summary>
[DisallowMultipleComponent]
public class SettingsRowHighlightView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Selectable _target;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Color _labelColor = new(0.659f, 0.69f, 0.761f, 1f);
    [SerializeField] private Color _activeLabelColor = new(0.925f, 0.902f, 0.863f, 1f);
    [SerializeField, Min(0f)] private float _sharpness = 14f;

    private bool _isHovered;
    private float _active;

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        if (_target != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(_target.gameObject);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
    }

    private void OnEnable()
    {
        _isHovered = false;
        _active = IsActive() ? 1f : 0f;
        Apply();
    }

    private void Update()
    {
        _active = Mathf.Lerp(_active, IsActive() ? 1f : 0f, 1f - Mathf.Exp(-_sharpness * Time.unscaledDeltaTime));
        Apply();
    }

    private bool IsActive()
    {
        if (_isHovered)
        {
            return true;
        }

        EventSystem system = EventSystem.current;
        return system != null && _target != null && system.currentSelectedGameObject == _target.gameObject;
    }

    private void Apply()
    {
        if (_label != null)
        {
            _label.color = Color.Lerp(_labelColor, _activeLabelColor, _active);
        }
    }
}
