using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 새 룬북(RuneBookV2)에서 룬 워드에 마우스를 올리면 뜨는 팝업.
/// 아이콘, 이름, "카테고리 · 겹치기 가능 여부", 설명, 룬 워드(룬 문자 이미지)와 모자란 룬 문자를 보여준다.
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
    [SerializeField] private string _stackableText = "Stackable";
    [SerializeField] private string _notStackableText = "Not stackable";
    [SerializeField] private string _ownedFormat = "Owned x{0} - No rune stones needed";

    [Header("색")]
    [SerializeField] private Color _iconBoxColor = new(0.894f, 0.804f, 0.678f, 1f);

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
            _icon.color = Color.white;
        }

        SetText(_nameText, entry.EnglishName);
        string category = entry.Category.ToString().ToUpperInvariant();
        SetText(_categoryText, $"{category}  ·  {(canOverlap ? _stackableText : _notStackableText)}");
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
}
