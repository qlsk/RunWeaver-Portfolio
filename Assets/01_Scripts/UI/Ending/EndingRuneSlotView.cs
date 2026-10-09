using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>엔딩 보고서에 표시할 장착 룬 하나</summary>
public sealed class EndingRuneData
{
    public EndingRuneData(string name, Sprite icon, bool isMain, Color mainColor)
    {
        Name = name;
        Icon = icon;
        IsMain = isMain;
        MainColor = mainColor;
    }

    public string Name { get; }
    public Sprite Icon { get; }

    /// <summary>주력 원소 칸인지. 주력 원소는 원소 색으로 칠한다</summary>
    public bool IsMain { get; }

    public Color MainColor { get; }
}

/// <summary>
/// 엔딩 보고서의 "사용한 룬 구성" 칸 하나.
/// - 주력 원소 : 원소 색으로 칠한 칸에 밝은 아이콘
/// - 나머지 룬 : 종이색 칸에 잉크색 아이콘
/// - 빈 칸 : 점선 테두리와 "빈 칸" 글자
/// </summary>
[DisallowMultipleComponent]
public class EndingRuneSlotView : MonoBehaviour
{
    [SerializeField] private Image _fill;
    [SerializeField] private Image _border;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _emptyBorder;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _emptyText;

    [Header("색")]
    [SerializeField] private Color _normalFillColor = new(0.91f, 0.84f, 0.71f, 1f);
    [SerializeField] private Color _normalBorderColor = new(0.44f, 0.32f, 0.23f, 1f);
    [SerializeField] private Color _mainBorderColor = new(0.23f, 0.14f, 0.09f, 1f);
    [SerializeField] private Color _normalIconColor = new(0.23f, 0.14f, 0.09f, 1f);
    [SerializeField] private Color _mainIconColor = new(0.98f, 0.94f, 0.88f, 1f);
    [SerializeField] private string _mainSuffix = " (주력)";

    public void Show(EndingRuneData data)
    {
        if (data == null)
        {
            ShowEmpty();
            return;
        }

        gameObject.SetActive(true);
        SetActive(_fill, true);
        SetActive(_border, true);
        SetActive(_emptyBorder, false);
        SetActive(_emptyText, false);

        if (_fill != null)
        {
            _fill.color = data.IsMain ? data.MainColor : _normalFillColor;
        }

        if (_border != null)
        {
            _border.color = data.IsMain ? _mainBorderColor : _normalBorderColor;
        }

        if (_icon != null)
        {
            _icon.enabled = data.Icon != null;
            _icon.sprite = data.Icon;
            _icon.color = data.IsMain ? _mainIconColor : _normalIconColor;
        }

        if (_nameText != null)
        {
            _nameText.gameObject.SetActive(true);
            _nameText.text = data.IsMain ? data.Name + _mainSuffix : data.Name;
        }
    }

    /// <summary>쓸 수 있었지만 비워 둔 칸</summary>
    public void ShowEmpty()
    {
        gameObject.SetActive(true);
        SetActive(_fill, false);
        SetActive(_border, false);
        SetActive(_emptyBorder, true);
        SetActive(_emptyText, true);

        if (_icon != null)
        {
            _icon.enabled = false;
        }

        if (_nameText != null)
        {
            _nameText.gameObject.SetActive(false);
        }
    }

    /// <summary>열리지 않은 칸</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private static void SetActive(Component target, bool active)
    {
        if (target != null)
        {
            target.gameObject.SetActive(active);
        }
    }
}
