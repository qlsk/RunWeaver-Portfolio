using TMPro;
using UnityEngine;

public class TutorialGuideCardView : MonoBehaviour
{
    [SerializeField] private GameObject _cardRoot;
    [SerializeField] private TMP_Text _headerLabel;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _bodyText;

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
    }

    public void Hide()
    {
        if (_cardRoot == null)
            return;

        _cardRoot.SetActive(false);
    }
}