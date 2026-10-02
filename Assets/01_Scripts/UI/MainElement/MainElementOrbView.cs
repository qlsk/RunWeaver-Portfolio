using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 메인 원소 선택 화면의 원소 구슬 하나. 원소 색 바탕 + 흰 아이콘 + 이름표.
/// 선택되면 크게 키우고 밝은 테두리와 원소 색 빛을 켠다. (이름표는 가운데 정보로 대신하므로 숨긴다)
/// </summary>
[DisallowMultipleComponent]
public class MainElementOrbView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private ReactionElement _element;

    [Header("구성 요소")]
    [SerializeField] private Image _fill;
    [SerializeField] private Image _glow;
    [SerializeField] private GameObject _normalRing;
    [SerializeField] private GameObject _selectedRing;
    [SerializeField] private TMP_Text _label;

    [Header("선택 표시")]
    [SerializeField] private float _selectedScale = 1.25f;

    public event Action<MainElementOrbView> Clicked;

    public ReactionElement Element => _element;

    private void Awake()
    {
        Color color = ReactionElementStyle.GetColor(_element);
        if (_fill != null)
        {
            _fill.color = color;
        }

        if (_glow != null)
        {
            _glow.color = new Color(color.r, color.g, color.b, 0.9f);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Clicked?.Invoke(this);
        }
    }

    public void SetSelected(bool isSelected)
    {
        transform.localScale = Vector3.one * (isSelected ? _selectedScale : 1f);
        SetActive(_glow != null ? _glow.gameObject : null, isSelected);
        SetActive(_selectedRing, isSelected);
        SetActive(_normalRing, !isSelected);
        SetActive(_label != null ? _label.gameObject : null, !isSelected);
    }

    private static void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }
}
