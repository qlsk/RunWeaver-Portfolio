using TMPro;
using UnityEngine;

/// <summary>필수 단계 안내 카드. 단계 번호, 제목, 본문, 행동 행(키·마우스)을 표시한다.</summary>
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
        // 키 행과 마우스 행을 지원한다. 나머지 종류는 해당 단위에서 추가하고, 그 전까지는 행을 숨긴다.
        if (_actionRow == null)
            return;

        bool isKeyRow = step.ActionRow == TutorialActionRowKind.Key;
        bool isMouseRow = step.ActionRow == TutorialActionRowKind.Mouse;
        _actionRow.SetActive(isKeyRow || isMouseRow);

        if (!isKeyRow && !isMouseRow)
            return;

        // 키 아이콘은 키 텍스트의 부모 오브젝트이다.
        if (_keyText != null)
            _keyText.transform.parent.gameObject.SetActive(isKeyRow);

        if (_mouseIcon != null)
            _mouseIcon.SetActive(isMouseRow);

        if (isKeyRow && _keyText != null)
            _keyText.text = step.KeyLabels != null && step.KeyLabels.Count > 0 ? step.KeyLabels[0] : string.Empty;

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