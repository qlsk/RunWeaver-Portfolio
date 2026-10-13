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

    private void Awake()
    {
        if (_actionRow == null || _keyText == null || _actionLabel == null)
            Debug.LogError("[TutorialGuideCardView] 행동 행 참조가 연결되지 않았습니다.", this);

        _cardRoot.SetActive(false);
    }

    public void Show(TutorialStepSO step, int number, int total)
    {
        _cardRoot.SetActive(true);
        _headerLabel.text = $"{number:D2} / {total:D2}";
        _titleText.text = step.Title;
        _bodyText.text = step.Body;
        ShowActionRow(step);
    }

    private void ShowActionRow(TutorialStepSO step)
    {
        // 키 한 개 행만 지원한다. 나머지 종류는 해당 단위에서 추가하고, 그 전까지는 행을 숨긴다.
        if (_actionRow == null)
            return;

        bool isKeyRow = step.ActionRow == TutorialActionRowKind.Key;
        _actionRow.SetActive(isKeyRow);

        if (!isKeyRow || _keyText == null || _actionLabel == null)
            return;

        _keyText.text = step.KeyLabels != null && step.KeyLabels.Count > 0 ? step.KeyLabels[0] : string.Empty;
        _actionLabel.text = step.ActionLabel;
    }

    public void Hide()
    {
        if (_cardRoot == null)
            return;

        _cardRoot.SetActive(false);
    }
}