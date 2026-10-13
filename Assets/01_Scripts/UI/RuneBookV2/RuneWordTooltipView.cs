using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 새 룬북(RuneBookV2)에서 룬 워드에 마우스를 올리면 뜨는 팝업.
/// 아이콘, 이름, "카테고리 / 겹치기 가능 여부", 설명, 룬 워드(룬 문자 이미지)와 모자란 룬 문자를 보여준다.
/// - Listed: 설명만 ??? 로 숨긴다.
/// - 완성품을 가지고 있으면 모자란 룬 문자 줄 대신 "Owned x1 - No rune stones needed"를 보여준다.
/// 크기와 위치는 RuneBookTooltipLayout이 맞춘다.
/// </summary>
[DisallowMultipleComponent]
public class RuneWordTooltipView : MonoBehaviour
{
    [Serializable]
    private class StoneSlot
    {
        // 프리팹에는 예전 이름(icon, missingRing)으로 저장되어 있어 연결이 끊기지 않도록 이전 이름을 지정한다.
        [FormerlySerializedAs("icon")]
        public Image Icon;
        [Tooltip("모자랄 때 켜는 붉은 고리")]
        [FormerlySerializedAs("missingRing")]
        public GameObject MissingRing;
    }

    [SerializeField] private RuneBookTooltipLayout _layout;

    [Header("머리")]
    [SerializeField] private Image _iconBox;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _categoryText;
    [SerializeField] private TMP_Text _descriptionText;

    [Header("룬 워드")]
    [SerializeField] private GameObject _runeWordGroup;
    [SerializeField] private StoneSlot[] _runeWordStones;
    [Tooltip("모자란 룬 문자 줄 (Missing: + 룬 문자 이미지)")]
    [SerializeField] private GameObject _missingGroup;
    [SerializeField] private Image[] _missingStones;
    [Tooltip("완성품 보유 줄 (모자란 룬 문자 줄과 같은 자리)")]
    [SerializeField] private TMP_Text _ownedText;

    [Header("문구")]
    [SerializeField] private string _unknownText = "???";
    [SerializeField] private string _stackableText = "중첩 가능";
    [SerializeField] private string _notStackableText = "중첩 불가";
    [SerializeField] private string _ownedFormat = "보유 x{0} - 룬석 필요 없음";

    [Header("색")]
    [SerializeField] private Color _iconBoxColor = new(0.894f, 0.804f, 0.678f, 1f);
    [SerializeField] private Color _iconColor = new(0.227f, 0.141f, 0.094f, 1f);
    [SerializeField] private Color _onElementIconColor = new(0.98f, 0.94f, 0.88f, 1f);
    [Tooltip("{0} 카테고리, {1} 중첩 가능 여부")]
    [SerializeField] private string _categoryFormat = "{0}  /  {1}";

    public readonly struct RuneStoneView
    {
        public RuneStoneView(Sprite icon, bool isMissing)
        {
            Icon = icon;
            IsMissing = isMissing;
        }

        public Sprite Icon { get; }
        public bool IsMissing { get; }
    }

    /// <param name="stones">룬 워드의 룬 문자 순서대로 (모자라면 IsMissing)</param>
    /// <param name="missingStones">모자란 룬 문자 이미지 (중복 없이)</param>
    /// <param name="ownedCount">남은 완성품 수. 1 이상이면 모자란 룬 문자 줄 대신 보유 줄을 보여준다.</param>
    public void Show(RuneBookEntryData entry, bool canOverlap, Color? elementColor,
        IReadOnlyList<RuneStoneView> stones, IReadOnlyList<Sprite> missingStones, RectTransform target,
        int ownedCount = 0)
    {
        if (entry == null)
        {
            Hide();
            return;
        }

        bool isDetailed = entry.AreDetailsRevealed;

        if (_iconBox != null)
        {
            _iconBox.color = elementColor.HasValue && isDetailed ? elementColor.Value : _iconBoxColor;
        }

        if (_icon != null)
        {
            _icon.sprite = entry.Icon;
            _icon.enabled = entry.Icon != null;
            _icon.color = elementColor.HasValue && isDetailed ? _onElementIconColor : _iconColor;
        }

        SetText(_nameText, entry.DisplayName);
        string category = GetCategoryName(entry.Category);
        SetText(_categoryText, string.Format(_categoryFormat, category, canOverlap ? _stackableText : _notStackableText));
        SetText(_descriptionText, isDetailed ? entry.Description : _unknownText);

        if (_runeWordGroup != null)
        {
            _runeWordGroup.SetActive(true);
        }

        int stoneCount = stones?.Count ?? 0;
        if (_runeWordStones != null)
        {
            for (int i = 0; i < _runeWordStones.Length; i++)
            {
                StoneSlot slot = _runeWordStones[i];
                bool visible = i < stoneCount;
                if (slot.Icon != null)
                {
                    slot.Icon.gameObject.SetActive(visible);
                    if (visible)
                    {
                        slot.Icon.sprite = stones[i].Icon;
                    }
                }

                if (slot.MissingRing != null)
                {
                    slot.MissingRing.SetActive(visible && stones[i].IsMissing);
                }
            }
        }

        bool showOwned = ownedCount > 0;
        if (_ownedText != null)
        {
            _ownedText.gameObject.SetActive(showOwned);
            if (showOwned)
            {
                _ownedText.text = string.Format(_ownedFormat, ownedCount);
            }
        }

        int missingCount = showOwned ? 0 : missingStones?.Count ?? 0;
        EnsureMissingSlots(missingCount);
        if (_missingGroup != null)
        {
            _missingGroup.SetActive(missingCount > 0);
        }

        if (_missingStones != null)
        {
            for (int i = 0; i < _missingStones.Length; i++)
            {
                bool visible = i < missingCount;
                _missingStones[i].gameObject.SetActive(visible);
                if (visible)
                {
                    _missingStones[i].sprite = missingStones[i];
                }
            }
        }

        if (_layout != null)
        {
            _layout.Show(target);
        }
    }

    /// <summary>
    /// 모자란 룬 문자 칸이 부족하면 마지막 칸을 복제해 늘린다. (룬 워드는 최대 8글자)
    /// </summary>
    private void EnsureMissingSlots(int count)
    {
        if (_missingStones == null || _missingStones.Length == 0 || count <= _missingStones.Length)
        {
            return;
        }

        Image template = _missingStones[_missingStones.Length - 1];
        if (template == null)
        {
            return;
        }

        // 칸들은 레이아웃 그룹 없이 일정한 간격으로 놓여 있으므로, 마지막 두 칸의 간격만큼 옆으로 이어 놓는다.
        int oldLength = _missingStones.Length;
        RectTransform templateRect = template.rectTransform;
        Vector2 step = oldLength >= 2 && _missingStones[oldLength - 2] != null
            ? templateRect.anchoredPosition - _missingStones[oldLength - 2].rectTransform.anchoredPosition
            : new Vector2(templateRect.rect.width, 0f);

        Array.Resize(ref _missingStones, count);
        for (int i = oldLength; i < count; i++)
        {
            Image slot = Instantiate(template, template.transform.parent);
            slot.name = $"{template.name} ({i + 1})";
            slot.rectTransform.anchoredPosition = templateRect.anchoredPosition + step * (i - oldLength + 1);
            _missingStones[i] = slot;
        }
    }

    public void Hide()
    {
        if (_layout != null)
        {
            _layout.Hide();
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private static string GetCategoryName(RuneEffectCategory category)
    {
        return category switch
        {
            RuneEffectCategory.Element => "원소",
            RuneEffectCategory.Enhancement => "강화",
            RuneEffectCategory.Behaviour => "동작",
            // 제어 카테고리는 룬 북에서 없앴다. 남은 제어 룬 워드(흡수)는 특수로 보여 준다.
            RuneEffectCategory.Control => "특수",
            RuneEffectCategory.Special => "특수",
            _ => category.ToString()
        };
    }
}
