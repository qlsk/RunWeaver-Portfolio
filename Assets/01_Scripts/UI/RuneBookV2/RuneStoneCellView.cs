using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 새 룬북(RuneBookV2)의 보유 룬 문자 칸 하나. 룬 문자 이미지 + 개수.
/// - 평소: "x3"
/// - 확정 전 장착으로 쓸 예정인 수가 있으면: "3→2" (남는 수가 0 미만이면 붉게)
/// - 룬 워드에 마우스를 올리면 그 룬 워드가 쓰는 수를 붉은 " -1"로 덧붙인다.
/// - 모자라면 붉은 고리, 마우스를 올린 룬 워드가 쓰면 금색 고리, 쓸 예정이면 주황 고리를 켠다.
/// - 남는 수가 0 이하이고 고리가 없으면 흐리게 표시한다.
/// </summary>
[DisallowMultipleComponent]
public class RuneStoneCellView : MonoBehaviour
{
    [Header("구성 요소")]
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _countText;
    [Tooltip("룬 문자를 두르는 고리 (흰색 스프라이트에 색을 입힌다)")]
    [SerializeField] private Image _ring;
    [Tooltip("고리 뒤의 번진 빛 (흰색 스프라이트에 색을 입힌다)")]
    [SerializeField] private Image _glow;

    [Header("글자 색")]
    [SerializeField] private Color _countColor = new(0.376f, 0.290f, 0.243f, 1f);
    [SerializeField] private Color _emptyCountColor = new(0.647f, 0.569f, 0.502f, 1f);

    [Header("강조 색")]
    [SerializeField] private Color _pendingColor = new(0.886f, 0.627f, 0.235f, 1f);
    [SerializeField] private Color _hoverColor = new(0.941f, 0.784f, 0.471f, 1f);
    [SerializeField] private Color _shortColor = new(0.784f, 0.188f, 0.157f, 1f);
    [SerializeField, Range(0f, 1f)] private float _glowAlpha = 0.75f;

    [Header("흐리게 (남는 수가 0 이하)")]
    [SerializeField] private Color _dimIconColor = new(0.45f, 0.45f, 0.45f, 0.7f);

    /// <summary>연출용 빛 이미지 (고리 뒤 빛 또는 고리 스프라이트)</summary>
    public Sprite FlashSprite => _glow != null && _glow.sprite != null ? _glow.sprite
        : _ring != null ? _ring.sprite : null;

    /// <param name="stone">보유 룬 문자. null이면 칸을 숨긴다.</param>
    /// <param name="pendingUse">확정 전 장착으로 쓸 예정인 수</param>
    /// <param name="hoverUse">마우스를 올린 룬 워드가 쓸 수</param>
    public void Show(RuneStoneStackData stone, int pendingUse, int hoverUse)
    {
        if (gameObject.activeSelf != (stone != null))
        {
            gameObject.SetActive(stone != null);
        }

        if (stone == null)
        {
            return;
        }

        int owned = stone.Quantity;
        int left = owned - pendingUse;
        int afterHover = left - hoverUse;

        Color? ringColor = null;
        if (left < 0 || afterHover < 0)
        {
            ringColor = _shortColor;
        }
        else if (hoverUse > 0)
        {
            ringColor = _hoverColor;
        }
        else if (pendingUse > 0)
        {
            ringColor = _pendingColor;
        }

        SetRing(ringColor);

        if (_icon != null)
        {
            _icon.sprite = stone.Icon;
            _icon.enabled = stone.Icon != null;
            _icon.color = left <= 0 && !ringColor.HasValue ? _dimIconColor : Color.white;
        }

        if (_countText == null)
        {
            return;
        }

        string text;
        if (pendingUse > 0)
        {
            Color leftColor = left < 0 ? _shortColor : _pendingColor;
            text = $"{owned}{Colored("→", _pendingColor)}{Colored(left.ToString(), leftColor)}";
        }
        else
        {
            text = $"x{owned}";
        }

        if (hoverUse > 0)
        {
            text += Colored($" -{hoverUse}", _shortColor);
        }

        _countText.color = owned > 0 ? _countColor : _emptyCountColor;
        _countText.text = text;
    }

    private void SetRing(Color? color)
    {
        if (_ring != null)
        {
            _ring.gameObject.SetActive(color.HasValue);
            if (color.HasValue)
            {
                _ring.color = color.Value;
            }
        }

        if (_glow != null)
        {
            _glow.gameObject.SetActive(color.HasValue);
            if (color.HasValue)
            {
                Color glow = color.Value;
                glow.a = _glowAlpha;
                _glow.color = glow;
            }
        }
    }

    private static string Colored(string value, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{value}</color>";
    }
}
