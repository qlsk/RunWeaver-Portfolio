using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 연구 노드 하나. 상태에 따라 노드 이미지(지정한 경우)와 색을 바꾸고, 단계 숫자와 단계 이름을 표시한다.
/// 스킬 업그레이드 노드처럼 은색 메달 이미지 하나를 색으로 물들여 상태를 구분할 수 있다.
/// 어떤 연구를 표시할지는 ResearchTrackView가 정해 준다.
/// </summary>
[DisallowMultipleComponent]
public class ResearchNodeView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _image;
    [SerializeField] private TMP_Text _numeralText;
    [SerializeField] private TMP_Text _stepNameText;

    [Header("상태별 이미지")]
    [SerializeField] private Sprite _researchedSprite;
    [SerializeField] private Sprite _availableSprite;
    [SerializeField] private Sprite _notEnoughSprite;
    [SerializeField] private Sprite _lockedSprite;

    [Header("상태별 노드 색 (이미지에 곱해진다)")]
    [SerializeField] private Color _researchedTint = Color.white;
    [SerializeField] private Color _availableTint = Color.white;
    [SerializeField] private Color _notEnoughTint = Color.white;
    [SerializeField] private Color _lockedTint = Color.white;

    [Header("상태별 숫자 색")]
    [SerializeField] private Color _researchedColor = new(0.29f, 0.16f, 0.03f, 1f);
    [SerializeField] private Color _availableColor = new(0.12f, 0.29f, 0.27f, 1f);
    [SerializeField] private Color _notEnoughColor = new(0.69f, 0.16f, 0.11f, 1f);
    [SerializeField] private Color _lockedColor = new(0.42f, 0.38f, 0.32f, 1f);

    public event Action<ResearchNodeView> Clicked;

    public string ResearchId { get; private set; }

    public RectTransform RectTransform => (RectTransform)transform;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && !string.IsNullOrEmpty(ResearchId))
        {
            Clicked?.Invoke(this);
        }
    }

    public void Show(ResearchNodeData node)
    {
        if (node == null)
        {
            ResearchId = null;
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        ResearchId = node.Id;

        if (_numeralText != null)
        {
            _numeralText.text = ResearchText.GetRoman(node.Step);
        }

        if (_stepNameText != null)
        {
            _stepNameText.text = node.StepName;
        }

        Sprite sprite;
        Color color;
        Color tint;
        switch (node.State)
        {
            case ResearchNodeState.Researched:
                sprite = _researchedSprite;
                color = _researchedColor;
                tint = _researchedTint;
                break;
            case ResearchNodeState.Available:
                sprite = _availableSprite;
                color = _availableColor;
                tint = _availableTint;
                break;
            case ResearchNodeState.NotEnough:
                sprite = _notEnoughSprite;
                color = _notEnoughColor;
                tint = _notEnoughTint;
                break;
            default:
                sprite = _lockedSprite;
                color = _lockedColor;
                tint = _lockedTint;
                break;
        }

        if (_image != null)
        {
            if (sprite != null)
            {
                _image.sprite = sprite;
            }

            _image.color = tint;
        }

        if (_numeralText != null)
        {
            _numeralText.color = color;
        }
    }
}
