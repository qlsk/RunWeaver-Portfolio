using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 연구 비용 한 줄 (재료 아이콘, 이름, "250 (Own 350)"). 재료가 부족하면 수량을 붉게 표시한다.
/// </summary>
[DisallowMultipleComponent]
public class ResearchCostRowView : MonoBehaviour
{
    [Serializable]
    public class ResourceIcon
    {
        public string resourceId;
        public Sprite icon;
    }

    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _amountText;
    [SerializeField] private string _amountFormat = "Need {0} (Own {1})";
    [SerializeField] private Color _insufficientColor = new(0.69f, 0.16f, 0.11f, 1f);
    [SerializeField] private ResourceIcon[] _icons;

    private Color _normalColor = Color.black;
    private bool _isInitialized;

    public void Show(ResearchCostData cost)
    {
        Initialize();
        if (cost == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        if (_nameText != null)
        {
            _nameText.text = cost.DisplayName;
        }

        if (_amountText != null)
        {
            _amountText.text = string.Format(_amountFormat,
                cost.Required.ToString("#,0", CultureInfo.InvariantCulture),
                cost.Owned.ToString("#,0", CultureInfo.InvariantCulture));
            _amountText.color = cost.IsEnough ? _normalColor : _insufficientColor;
        }

        if (_iconImage != null && _icons != null)
        {
            foreach (ResourceIcon icon in _icons)
            {
                if (icon != null && icon.resourceId == cost.ResourceId && icon.icon != null)
                {
                    _iconImage.sprite = icon.icon;
                    break;
                }
            }
        }
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        if (_amountText != null)
        {
            _normalColor = _amountText.color;
        }
    }
}
