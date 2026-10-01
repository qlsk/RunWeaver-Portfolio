using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 업그레이드 비용 한 줄 (재료 아이콘, 이름, "Need 25 (Own 50)").
/// 비용 줄 오브젝트(예: ItemImage)에 붙인다. 재료가 부족하면 수량 글자를 붉게 표시한다.
/// </summary>
[DisallowMultipleComponent]
public class SkillCostRowView : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _costText;
    [SerializeField] private string _costFormat = "Need {0} (Own {1})";
    [SerializeField] private Color _insufficientColor = new(0.95f, 0.45f, 0.4f, 1f);

    private Sprite _defaultIcon;
    private Color _normalCostColor = Color.white;
    private bool _isInitialized;

    private void Awake()
    {
        Initialize();
    }

    public void Show(SkillCostData cost)
    {
        Initialize();
        if (cost == null)
        {
            Hide();
            return;
        }

        gameObject.SetActive(true);

        if (_iconImage != null)
        {
            _iconImage.sprite = cost.Icon != null ? cost.Icon : _defaultIcon;
        }

        if (_nameText != null)
        {
            _nameText.text = cost.DisplayName;
        }

        if (_costText != null)
        {
            _costText.text = string.Format(_costFormat, cost.Required, cost.Owned);
            _costText.color = cost.IsEnough ? _normalCostColor : _insufficientColor;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        if (_iconImage != null)
        {
            _defaultIcon = _iconImage.sprite;
        }

        if (_costText != null)
        {
            _normalCostColor = _costText.color;
        }
    }
}
