using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 메인 원소 선택 화면의 버튼(Confirm, Cancel) 표시. 키보드 선택(포커스) 중이면
/// 하늘색 테두리 스프라이트와 빛, 양옆 화살표를 켠다.
/// </summary>
[DisallowMultipleComponent]
public class MainElementButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _background;
    [SerializeField] private Sprite _normalSprite;
    [SerializeField] private Sprite _focusedSprite;
    [SerializeField] private GameObject _glow;
    [SerializeField] private GameObject _arrows;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Color _normalLabelColor = new(0.667f, 0.667f, 0.725f, 1f);
    [SerializeField] private Color _focusedLabelColor = new(0.922f, 0.961f, 1f, 1f);

    public Button Button => _button;

    public event Action PointerEntered;
    public event Action PointerExited;

    public void SetFocused(bool isFocused)
    {
        if (_background != null)
        {
            Sprite sprite = isFocused ? _focusedSprite : _normalSprite;
            if (sprite != null)
            {
                _background.sprite = sprite;
            }
        }

        if (_glow != null)
        {
            _glow.SetActive(isFocused);
        }

        if (_arrows != null)
        {
            _arrows.SetActive(isFocused);
        }

        if (_label != null)
        {
            _label.color = isFocused ? _focusedLabelColor : _normalLabelColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEntered?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExited?.Invoke();
    }
}