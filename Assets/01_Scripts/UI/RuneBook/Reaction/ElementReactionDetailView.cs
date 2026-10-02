using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 원소 반응 도감의 오른쪽 페이지.
/// - 아이콘 칸: 반응이면 두 원소 아이콘을 겹쳐서, 기본 원소면 아이콘 하나를 표시한다.
/// - 이름, 조합 줄([원소 배지] Fire + [원소 배지] Water), 설명
/// - 반응이면 두 원소의 기본 효과 줄을 표시한다.
/// - 피해량 / 지속시간 (비어 있으면 미정 문구)
/// </summary>
[DisallowMultipleComponent]
public class ElementReactionDetailView : MonoBehaviour
{
    [Serializable]
    private class ElementBadge
    {
        [Tooltip("배지 바탕 (원소 색을 입힌다)")]
        public Image background;
        [Tooltip("배지 안의 흰색 원소 아이콘")]
        public Image icon;
        public TMP_Text text;
    }

    [Header("아이콘 칸")]
    [SerializeField] private Image _iconA;
    [SerializeField] private Image _iconB;
    [SerializeField] private Image _iconSingle;

    [Header("이름 / 조합 / 설명")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private ElementBadge _comboA;
    [SerializeField] private GameObject _comboPlus;
    [SerializeField] private ElementBadge _comboB;
    [SerializeField] private TMP_Text _comboBaseText;
    [SerializeField] private TMP_Text _descriptionText;

    [Header("기본 원소 효과 줄 (반응일 때만 표시)")]
    [SerializeField] private GameObject _baseEffectGroup;
    [SerializeField] private ElementBadge[] _baseEffects;

    [Header("피해량 / 지속시간")]
    [SerializeField] private TMP_Text _statsText;
    [SerializeField] private string _emptyStatsText = "Damage / Duration: shown once decided";

    public void Show(ElementReactionData data, Func<ReactionElement, Sprite> getIcon,
        Func<ReactionElement, ElementReactionData> getBaseEffect)
    {
        if (data == null)
        {
            return;
        }

        bool isBase = data.IsBaseElement;
        ReactionElement a = data.ElementA;
        ReactionElement b = data.ElementB ?? data.ElementA;

        SetIcon(_iconSingle, getIcon(a), isBase);
        SetIcon(_iconA, getIcon(a), !isBase);
        SetIcon(_iconB, getIcon(b), !isBase);

        SetText(_nameText, data.DisplayName);
        SetDescription(data.Description);

        ShowBadge(_comboA, a, ReactionElementStyle.GetName(a), getIcon, true);
        ShowBadge(_comboB, b, ReactionElementStyle.GetName(b), getIcon, !isBase);
        if (_comboPlus != null)
        {
            _comboPlus.SetActive(!isBase);
        }

        if (_comboBaseText != null)
        {
            _comboBaseText.gameObject.SetActive(isBase);
        }

        if (_baseEffectGroup != null)
        {
            _baseEffectGroup.SetActive(!isBase);
        }

        if (!isBase && _baseEffects != null)
        {
            ReactionElement[] elements = a == b ? new[] { a } : new[] { a, b };
            for (int i = 0; i < _baseEffects.Length; i++)
            {
                bool visible = i < elements.Length;
                string effect = visible ? getBaseEffect(elements[i])?.ShortEffect ?? string.Empty : string.Empty;
                ShowBadge(_baseEffects[i], visible ? elements[i] : a, effect, getIcon, visible);
            }
        }

        if (_statsText != null)
        {
            string stats = string.Empty;
            if (!string.IsNullOrEmpty(data.Damage))
            {
                stats += $"Damage  {data.Damage}";
            }

            if (!string.IsNullOrEmpty(data.Duration))
            {
                stats += (stats.Length > 0 ? "     " : string.Empty) + $"Duration  {data.Duration}";
            }

            _statsText.text = stats.Length > 0 ? stats : _emptyStatsText;
            _statsText.gameObject.SetActive(_statsText.text.Length > 0);
        }
    }

    private void SetDescription(string description)
    {
        if (_descriptionText == null)
        {
            return;
        }

        _descriptionText.text = description;
    }

    private static void ShowBadge(ElementBadge badge, ReactionElement element, string text,
        Func<ReactionElement, Sprite> getIcon, bool visible)
    {
        if (badge == null)
        {
            return;
        }

        if (badge.background != null)
        {
            badge.background.gameObject.SetActive(visible);
            badge.background.color = ReactionElementStyle.GetColor(element);
        }

        if (badge.icon != null)
        {
            Sprite sprite = getIcon(element);
            badge.icon.sprite = sprite;
            badge.icon.enabled = sprite != null;
        }

        if (badge.text != null)
        {
            badge.text.gameObject.SetActive(visible);
            badge.text.text = text;
        }
    }

    private static void SetIcon(Image image, Sprite sprite, bool visible)
    {
        if (image == null)
        {
            return;
        }

        image.gameObject.SetActive(visible && sprite != null);
        image.sprite = sprite;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
