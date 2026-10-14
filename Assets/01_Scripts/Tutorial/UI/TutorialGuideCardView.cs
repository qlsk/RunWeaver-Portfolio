using TMPro;
using UnityEngine;

public class TutorialGuideCardView : MonoBehaviour
{
    [SerializeField] private GameObject _cardRoot;
    [SerializeField] private TMP_Text _headerLabel;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _bodyText;
    [SerializeField] private GameObject _actionRow;
    [SerializeField] private TMP_Text _keyText;
    [SerializeField] private TMP_Text _actionLabel;
    [SerializeField] private GameObject _mouseIcon;

    private void Awake()
    {
        _cardRoot.SetActive(false);
    }

    public void Show(TutorialStepSO step, int number, int total)
    {
        _cardRoot.SetActive(true);
        _headerLabel.text = $"{number:D2} / {total:D2}";
        _titleText.text = step.Title;
        _bodyText.text = step.Body;

        bool isKeyRow = step.ActionRow == TutorialActionRowKind.Key;
        bool isMouseRow = step.ActionRow == TutorialActionRowKind.Mouse;
        _actionRow.SetActive(isKeyRow || isMouseRow);

        if (_keyText != null)
            _keyText.transform.parent.gameObject.SetActive(isKeyRow);

        if (_mouseIcon != null)
            _mouseIcon.SetActive(isMouseRow);

        if (isKeyRow && _keyText != null)
            _keyText.text = step.KeyLabels is { Count: > 0 } ? step.KeyLabels[0] : string.Empty;

        if (_actionLabel != null)
            _actionLabel.text = step.ActionLabel;
    }

    public void Hide()
    {
        if (_cardRoot == null)
            return;

        _cardRoot.SetActive(false);
    }
}