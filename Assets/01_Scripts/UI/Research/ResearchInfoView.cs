using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오른쪽 정보 판넬. 선택한 연구의 이름, 설명, 이번 단계 효과, 누적 효과, 선행 연구, 비용과 연구 버튼을 표시한다.
/// </summary>
[DisallowMultipleComponent]
public class ResearchInfoView : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _tagText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _stepEffectText;
    [SerializeField] private TMP_Text _totalEffectText;
    [SerializeField] private TMP_Text _requiresText;
    [SerializeField] private ResearchCostRowView[] _costRows;

    [Header("연구 버튼")]
    [SerializeField] private Button _researchButton;
    [SerializeField] private Image _buttonImage;
    [SerializeField] private TMP_Text _buttonText;
    [SerializeField] private Sprite _buttonSprite;
    [SerializeField] private Sprite _buttonDisabledSprite;
    [SerializeField] private Color _buttonTextColor = Color.white;
    [SerializeField] private Color _buttonDisabledTextColor = Color.gray;

    [Header("문구")]
    [SerializeField] private string _researchLabel = "RESEARCH";
    [SerializeField] private string _researchedLabel = "RESEARCHED";
    [SerializeField] private string _lockedLabel = "LOCKED";
    [SerializeField] private Color _metColor = new(0.18f, 0.42f, 0.38f, 1f);
    [SerializeField] private Color _unmetColor = new(0.69f, 0.16f, 0.11f, 1f);

    public event Action ResearchClicked;

    private void Awake()
    {
        if (_researchButton != null)
        {
            _researchButton.onClick.AddListener(HandleResearchClicked);
        }
    }

    private void OnDestroy()
    {
        if (_researchButton != null)
        {
            _researchButton.onClick.RemoveListener(HandleResearchClicked);
        }
    }

    public void Show(ResearchNodeData node)
    {
        if (node == null)
        {
            SetText(_nameText, string.Empty);
            SetText(_tagText, string.Empty);
            SetText(_descriptionText, string.Empty);
            SetText(_stepEffectText, string.Empty);
            SetText(_totalEffectText, string.Empty);
            SetText(_requiresText, string.Empty);
            ShowCosts(null);
            SetButton(false, _researchLabel);
            return;
        }

        SetText(_nameText, node.DisplayName);
        SetText(_tagText, $"{node.CategoryName.ToUpperInvariant()} - STEP {node.Step} / {node.StepCount}");
        SetText(_descriptionText, node.Description);
        SetText(_stepEffectText, node.StepEffect);
        SetText(_totalEffectText, node.TotalEffect);

        if (_requiresText != null)
        {
            // 스킬 업그레이드 화면처럼 "Requires: 이름" 한 줄로 표시하고, 충족 여부를 색으로 구분한다.
            if (string.IsNullOrEmpty(node.PrerequisiteName))
            {
                _requiresText.text = "No requirement";
                _requiresText.color = _metColor;
            }
            else
            {
                _requiresText.text = $"Requires: {node.PrerequisiteName}";
                _requiresText.color = node.IsPrerequisiteMet ? _metColor : _unmetColor;
            }
        }

        // 연구를 마친 노드는 비용 대신 비워 둔다.
        ShowCosts(node.State == ResearchNodeState.Researched ? null : node);

        switch (node.State)
        {
            case ResearchNodeState.Researched:
                SetButton(false, _researchedLabel);
                break;
            case ResearchNodeState.Locked:
                SetButton(false, _lockedLabel);
                break;
            case ResearchNodeState.NotEnough:
                SetButton(false, _researchLabel);
                break;
            default:
                SetButton(true, _researchLabel);
                break;
        }
    }

    private void ShowCosts(ResearchNodeData node)
    {
        if (_costRows == null)
        {
            return;
        }

        for (int i = 0; i < _costRows.Length; i++)
        {
            if (_costRows[i] == null)
            {
                continue;
            }

            ResearchCostData cost = node != null && node.Costs != null && i < node.Costs.Count ? node.Costs[i] : null;
            _costRows[i].Show(cost);
        }
    }

    private void SetButton(bool interactable, string label)
    {
        if (_researchButton != null)
        {
            _researchButton.interactable = interactable;
        }

        if (_buttonImage != null)
        {
            Sprite sprite = interactable ? _buttonSprite : _buttonDisabledSprite;
            if (sprite != null)
            {
                _buttonImage.sprite = sprite;
            }
        }

        SetText(_buttonText, label);
        if (_buttonText != null)
        {
            _buttonText.color = interactable ? _buttonTextColor : _buttonDisabledTextColor;
        }
    }

    private void HandleResearchClicked()
    {
        ResearchClicked?.Invoke();
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
