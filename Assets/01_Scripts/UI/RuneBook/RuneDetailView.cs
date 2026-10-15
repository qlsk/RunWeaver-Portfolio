using TMPro;
using UnityEngine;

/// <summary>
/// 룬북 오른쪽 상세 영역. 공개 단계에 따라 표시할 정보를 가린다.
/// </summary>
[DisallowMultipleComponent]
public class RuneDetailView : MonoBehaviour
{
    [Header("구성 요소")]
    [Tooltip("비워두면 자식에서 자동으로 찾습니다.")]
    [SerializeField] private RuneSymbolSlotView _symbolSlot;
    [SerializeField] private TMP_Text _runeWordText;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descriptionText;

    [Header("가려진 정보 표시")]
    [SerializeField] private string _unknownText = "???";
    [SerializeField] private string _noSelectionName = "Select a rune";

    private bool _isInitialized;

    private void Awake()
    {
        Initialize();
    }

    public void Show(RuneBookEntryData entry)
    {
        Initialize();

        if (entry == null)
        {
            ShowEmpty();
            return;
        }

        string description = entry.AreDetailsRevealed ? entry.Description : _unknownText;
        SetTexts(entry.RuneWord, entry.EnglishName, description);

        if (entry.AreDetailsRevealed)
        {
            ShowSymbol(RuneSymbolSlotState.Detailed, entry.Icon);
        }
        else
        {
            ShowSymbol(RuneSymbolSlotState.Listed, entry.Icon);
        }
    }

    public void ShowEmpty()
    {
        Initialize();
        SetTexts(string.Empty, _noSelectionName, string.Empty);
        ShowSymbol(RuneSymbolSlotState.Empty, null);
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        if (_symbolSlot == null)
        {
            _symbolSlot = GetComponentInChildren<RuneSymbolSlotView>(true);
        }

        if (_symbolSlot != null)
        {
            _symbolSlot.SetClickable(false);
        }
    }

    private void SetTexts(string runeWord, string runeName, string description)
    {
        if (_runeWordText != null)
        {
            _runeWordText.text = runeWord;
        }

        if (_nameText != null)
        {
            _nameText.text = runeName;
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = description;
        }
    }

    private void ShowSymbol(RuneSymbolSlotState state, Sprite icon)
    {
        if (_symbolSlot != null)
        {
            _symbolSlot.Show(state, icon);
        }
    }
}
