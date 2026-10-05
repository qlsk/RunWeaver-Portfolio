using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 메뉴 버튼 선택 표시. 선택되면(방향키 / 마우스를 올림) 빛 이미지를 켜고 글자 색을 바꾼다.
/// 마우스를 올리면 그 버튼을 선택 상태로 만들어 키보드 선택과 같은 모습이 되게 한다.
/// (OnDisable 중 SetActive를 피하기 위해 빛은 Graphic.enabled로 켜고 끈다)
/// </summary>
[DisallowMultipleComponent]
public class MenuButtonHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    [SerializeField] private Graphic _highlight;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _selectedColor = Color.white;

    private void OnEnable()
    {
        bool isSelected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        Apply(isSelected);
    }

    private void OnDisable()
    {
        Apply(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        Apply(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        Apply(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    private void Apply(bool isSelected)
    {
        if (_highlight != null)
        {
            _highlight.enabled = isSelected;
        }

        if (_label != null)
        {
            _label.color = isSelected ? _selectedColor : _normalColor;
        }
    }
}
