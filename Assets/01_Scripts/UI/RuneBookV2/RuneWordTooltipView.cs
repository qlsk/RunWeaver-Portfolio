using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 새 룬북(RuneBookV2)에서 룬 워드에 마우스를 올리면 뜨는 팝업.
/// 아이콘, 이름, "카테고리 · 겹치기 가능 여부", 설명, 룬 워드(룬 문자 이미지)와 모자란 룬 문자를 보여준다.
/// - Hidden: 이름과 설명을 ??? 로, 룬 워드는 숨긴다.
/// - Listed: 설명만 ??? 로 숨긴다.
/// 크기와 위치는 RuneBookTooltipLayout이 맞춘다.
/// </summary>
[DisallowMultipleComponent]
public class RuneWordTooltipView : MonoBehaviour
{
    [Serializable]
    private class StoneSlot
    {
        public Image icon;
        [Tooltip("모자랄 때 켜는 붉은 고리")]
        public GameObject missingRing;
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

    [Header("문구")]
    [SerializeField] private string _unknownText = "???";
    [SerializeField] private string _hiddenDescription = "Not discovered yet.";
    [SerializeField] private string _stackableText = "Stackable";
    [SerializeField] private string _notStackableText = "Not stackable";

    [Header("색")]
    [SerializeField] private Color _iconBoxColor = new(0.894f, 0.804f, 0.678f, 1f);
    [SerializeField] private Color _hiddenIconColor = new(0.275f, 0.216f, 0.188f, 1f);

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
    public void Show(RuneBookEntryData entry, bool canOverlap, Color? elementColor,
        IReadOnlyList<RuneStoneView> stones, IReadOnlyList<Sprite> missingStones, RectTransform target)
    {
        if (entry == null)
        {
            Hide();
            return;
        }

        bool isRevealed = entry.IsRevealed;
        bool isDetailed = entry.AreDetailsRevealed;

        if (_iconBox != null)
        {
            _iconBox.color = elementColor.HasValue && isDetailed ? elementColor.Value : _iconBoxColor;
        }

        if (_icon != null)
        {
            _icon.sprite = entry.Icon;
            _icon.enabled = entry.Icon != null;
            _icon.color = isRevealed ? Color.white : _hiddenIconColor;
        }

        SetText(_nameText, isRevealed ? entry.EnglishName : _unknownText);
        string category = entry.Category.ToString().ToUpperInvariant();
        SetText(_categoryText, isRevealed
            ? $"{category}  ·  {(canOverlap ? _stackableText : _notStackableText)}"
            : category);
        SetText(_descriptionText, isDetailed ? entry.Description : isRevealed ? _unknownText : _hiddenDescription);

        if (_runeWordGroup != null)
        {
            _runeWordGroup.SetActive(isRevealed);
        }

        int stoneCount = stones?.Count ?? 0;
        if (_runeWordStones != null)
        {
            for (int i = 0; i < _runeWordStones.Length; i++)
            {
                StoneSlot slot = _runeWordStones[i];
                bool visible = i < stoneCount;
                if (slot.icon != null)
                {
                    slot.icon.gameObject.SetActive(visible);
                    if (visible)
                    {
                        slot.icon.sprite = stones[i].Icon;
                    }
                }

                if (slot.missingRing != null)
                {
                    slot.missingRing.SetActive(visible && stones[i].IsMissing);
                }
            }
        }

        int missingCount = missingStones?.Count ?? 0;
        if (_missingGroup != null)
        {
            _missingGroup.SetActive(isRevealed && missingCount > 0);
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
