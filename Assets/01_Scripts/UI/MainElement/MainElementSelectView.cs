using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 원정 출발 전 메인 원소 선택 화면. 확정한 원소의 룬 효과를 기본 원소 슬롯에 장착한다.
///
/// 조작
/// - 원소 선택 중: 좌/우 방향키로 원을 따라 원소를 바꾸고, F를 누르거나 아래 방향키를 누르면 Confirm 버튼으로 내려간다.
/// - 버튼 선택 중: 좌/우 방향키로 Confirm / Cancel을 오가고, F로 누른다. 위 방향키를 누르면 원소 선택으로 돌아간다.
/// - Esc: Cancel
/// - 마우스로 원소 구슬과 버튼을 눌러도 된다.
///
/// 연출 (실제 시간)
/// - 열기 : 어둡게 깔리고, 링·구슬·제목·버튼이 아래에서 살짝 떠오르며 나타난다. 도중에 F/클릭이면 바로 끝낸다.
/// - 확정 : 고른 구슬이 커지며 가운데로 모이고 나머지는 가운데로 빨려 들며 사라진다.
///          가운데에서 원소 색 빛이 터진 뒤 원소를 장착하고 Confirmed를 알린다.
/// - 취소 : 아래로 살짝 가라앉으며 사라진 뒤 Cancelled를 알린다.
/// </summary>
[DisallowMultipleComponent]
public class MainElementSelectView : MonoBehaviour
{
    [Serializable]
    private class ElementText
    {
        public ReactionElement Element;
        public string Effect;
    }

    [Header("원소 구슬 (원을 따라 시계 방향 순서)")]
    [SerializeField] private MainElementOrbView[] _orbs;

    [Header("가운데 정보")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _effectText;
    [SerializeField] private ElementText[] _effects =
    {
        new() { Element = ReactionElement.Fire, Effect = "화상 - 지속 피해" },
        new() { Element = ReactionElement.Water, Effect = "감속 - 이동 속도 감소" },
        new() { Element = ReactionElement.Earth, Effect = "경직 - 행동 중단" },
        new() { Element = ReactionElement.Wind, Effect = "넉백 - 적을 밀쳐냄" }
    };

    [Header("버튼")]
    [SerializeField] private MainElementButtonView _confirmButton;
    [SerializeField] private MainElementButtonView _cancelButton;

    [Header("키")]
    [SerializeField] private KeyCode _submitKey = KeyCode.F;
    [SerializeField] private KeyCode _cancelKey = KeyCode.Escape;

    [Header("연출")]
    [SerializeField, Min(0.01f)] private float _openDuration = 0.45f;
    [SerializeField] private float _openRise = 36f;
    [SerializeField, Min(0.01f)] private float _cancelDuration = 0.25f;
    [Tooltip("고른 구슬이 가운데로 모이는 시간")]
    [SerializeField, Min(0.01f)] private float _gatherDuration = 0.45f;
    [Tooltip("가운데에서 빛이 터지는 시간")]
    [SerializeField, Min(0.01f)] private float _burstDuration = 0.5f;

    private int _selectedIndex;
    private bool _isButtonFocused;
    private bool _isConfirmFocused = true;

    private enum Phase { Idle, Opening, Closing }
    private Phase _phase;
    private bool _skipOpening;
    private UiRiseParts _parts;
    private Vector2[] _orbBasePositions;
    private CanvasGroup[] _orbGroups;
    private Graphic _ringGraphic;
    private float _ringAlpha = 1f;
    private CanvasGroup _infoGroup;
    private readonly List<GameObject> _bursts = new();

    /// <summary>Confirm을 눌렀을 때 (고른 원소)</summary>
    public event Action<ReactionElement> Confirmed;

    /// <summary>Cancel을 눌렀을 때</summary>
    public event Action Cancelled;

    public ReactionElement SelectedElement =>
        _orbs != null && _orbs.Length > 0 ? _orbs[_selectedIndex].Element : ReactionElement.Fire;

    private void Awake()
    {
        if (_orbs != null)
        {
            foreach (MainElementOrbView orb in _orbs)
            {
                if (orb != null)
                {
                    orb.Clicked += HandleOrbClicked;
                }
            }
        }

        if (_confirmButton != null && _confirmButton.Button != null)
        {
            _confirmButton.Button.onClick.AddListener(Confirm);
        }

        if (_cancelButton != null && _cancelButton.Button != null)
        {
            _cancelButton.Button.onClick.AddListener(Cancel);
        }
    }

    private void OnEnable()
    {
        _isButtonFocused = false;
        _isConfirmFocused = true;
        Refresh();
        PrepareAnimation();
        StartCoroutine(OpenRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ResetAnimation();
        _phase = Phase.Idle;
    }

    private void OnDestroy()
    {
        if (_orbs != null)
        {
            foreach (MainElementOrbView orb in _orbs)
            {
                if (orb != null)
                {
                    orb.Clicked -= HandleOrbClicked;
                }
            }
        }

        if (_confirmButton != null && _confirmButton.Button != null)
        {
            _confirmButton.Button.onClick.RemoveListener(Confirm);
        }

        if (_cancelButton != null && _cancelButton.Button != null)
        {
            _cancelButton.Button.onClick.RemoveListener(Cancel);
        }
    }

    private void Update()
    {
        if (_phase == Phase.Opening)
        {
            if (Input.GetKeyDown(_submitKey) || Input.GetMouseButtonDown(0))
            {
                _skipOpening = true;
            }

            return;
        }

        if (_phase == Phase.Closing)
        {
            return;
        }

        if (Input.GetKeyDown(_cancelKey))
        {
            Cancel();
            return;
        }

        if (!_isButtonFocused)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                Select(_selectedIndex + 1);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                Select(_selectedIndex - 1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(_submitKey))
            {
                FocusButtons(true);
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            _isConfirmFocused = !_isConfirmFocused;
            Refresh();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            _isButtonFocused = false;
            Refresh();
        }
        else if (Input.GetKeyDown(_submitKey))
        {
            if (_isConfirmFocused)
            {
                Confirm();
            }
            else
            {
                Cancel();
            }
        }
    }

    public void Select(int index)
    {
        if (_orbs == null || _orbs.Length == 0)
        {
            return;
        }

        _selectedIndex = (index % _orbs.Length + _orbs.Length) % _orbs.Length;
        Refresh();
    }

    private void FocusButtons(bool confirm)
    {
        _isButtonFocused = true;
        _isConfirmFocused = confirm;
        Refresh();
    }

    private void HandleOrbClicked(MainElementOrbView orb)
    {
        if (_phase != Phase.Idle)
        {
            return;
        }

        int index = Array.IndexOf(_orbs, orb);
        if (index >= 0)
        {
            _isButtonFocused = false;
            Select(index);
        }
    }

    private void Confirm()
    {
        if (_phase != Phase.Idle)
        {
            return;
        }

        StartCoroutine(ConfirmRoutine());
    }

    private void EquipAndNotify()
    {
        PlayerRuneManager manager = PlayerRuneManager.Instance;
        if (manager == null || !manager.IsInitialized)
        {
            throw new InvalidOperationException("PlayerRuneManager가 초기화되지 않았습니다.");
        }

        string effectId = SelectedElement switch
        {
            ReactionElement.Fire => "rune_fire",
            ReactionElement.Water => "rune_water",
            ReactionElement.Earth => "rune_earth",
            ReactionElement.Wind => "rune_wind",
            _ => throw new ArgumentOutOfRangeException(nameof(SelectedElement))
        };
        manager.RuneEffectLoadout.EquipPrimaryElement(effectId);
        Debug.Log($"[MainElementSelectView] Confirm: {SelectedElement}", this);
        Confirmed?.Invoke(SelectedElement);
    }

    private void Cancel()
    {
        if (_phase != Phase.Idle)
        {
            return;
        }

        StartCoroutine(CancelRoutine());
    }

    // ───────────────────────── 연출 ─────────────────────────

    private void PrepareAnimation()
    {
        _parts ??= new UiRiseParts(transform, _openRise);

        if (_orbs != null && _orbBasePositions == null)
        {
            _orbBasePositions = new Vector2[_orbs.Length];
            _orbGroups = new CanvasGroup[_orbs.Length];
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i] == null)
                {
                    continue;
                }

                var rect = (RectTransform)_orbs[i].transform;
                _orbBasePositions[i] = rect.anchoredPosition;
                _orbGroups[i] = _orbs[i].GetComponent<CanvasGroup>();
                if (_orbGroups[i] == null)
                {
                    _orbGroups[i] = _orbs[i].gameObject.AddComponent<CanvasGroup>();
                }
            }

            // 구슬들이 놓인 링 (구슬의 부모 이미지)
            Transform container = _orbs.Length > 0 && _orbs[0] != null ? _orbs[0].transform.parent : null;
            _ringGraphic = container != null ? container.GetComponent<Graphic>() : null;
            if (_ringGraphic != null)
            {
                _ringAlpha = _ringGraphic.color.a;
            }

            // 가운데 정보(이름, 효과)는 링 안에 있으므로 따로 사라지게 한다.
            Transform info = _nameText != null ? _nameText.transform.parent : null;
            if (info == null || info == container)
            {
                info = _nameText != null ? _nameText.transform : null;
            }

            if (info != null)
            {
                _infoGroup = info.GetComponent<CanvasGroup>();
                if (_infoGroup == null)
                {
                    _infoGroup = info.gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        ResetAnimation();
    }

    /// <summary>구슬 위치와 크기, 투명도, 링, 터지는 빛을 원래대로 돌린다.</summary>
    private void ResetAnimation()
    {
        if (_orbs != null && _orbBasePositions != null)
        {
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i] == null)
                {
                    continue;
                }

                ((RectTransform)_orbs[i].transform).anchoredPosition = _orbBasePositions[i];
                _orbGroups[i].alpha = 1f;
            }
        }

        SetRingAlpha(1f);
        if (_infoGroup != null)
        {
            _infoGroup.alpha = 1f;
        }

        foreach (GameObject burst in _bursts)
        {
            if (burst != null)
            {
                Destroy(burst);
            }
        }

        _bursts.Clear();
        _parts?.Restore();
        Refresh();
    }

    private void SetRingAlpha(float alpha)
    {
        if (_ringGraphic == null)
        {
            return;
        }

        Color color = _ringGraphic.color;
        color.a = _ringAlpha * alpha;
        _ringGraphic.color = color;
    }

    private IEnumerator OpenRoutine()
    {
        _phase = Phase.Opening;
        _skipOpening = false;
        float elapsed = 0f;
        while (elapsed < _openDuration && !_skipOpening)
        {
            elapsed += Time.unscaledDeltaTime;
            _parts.Apply(UiRiseParts.EaseOut(elapsed / _openDuration));
            yield return null;
        }

        _parts.Apply(1f);
        _phase = Phase.Idle;
    }

    private IEnumerator CancelRoutine()
    {
        _phase = Phase.Closing;
        float elapsed = 0f;
        while (elapsed < _cancelDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _parts.Apply(1f - UiRiseParts.EaseOut(elapsed / _cancelDuration));
            yield return null;
        }

        Debug.Log("[MainElementSelectView] Cancel", this);
        Cancelled?.Invoke();
        FinishIfStillOpen();
    }

    /// <summary>알림 뒤에도 창이 켜져 있으면(받는 쪽이 닫지 않으면) 원래 모습으로 되돌린다.</summary>
    private void FinishIfStillOpen()
    {
        if (isActiveAndEnabled)
        {
            ResetAnimation();
            _phase = Phase.Idle;
        }
    }

    private IEnumerator ConfirmRoutine()
    {
        _phase = Phase.Closing;
        MainElementOrbView selected = _orbs[_selectedIndex];
        Transform container = selected.transform.parent;
        Color elementColor = ReactionElementStyle.GetColor(selected.Element);
        float selectedScale = selected.transform.localScale.x;

        // 1. 고른 구슬은 커지며 가운데로, 나머지는 가운데로 빨려 들며 사라진다. 글자와 버튼은 먼저 사라진다.
        float elapsed = 0f;
        while (elapsed < _gatherDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = UiRiseParts.EaseOut(elapsed / _gatherDuration);
            float contentAlpha = 1f - UiRiseParts.EaseOut(elapsed / (_gatherDuration * 0.55f));
            _parts.FadeContent(contentAlpha, container);
            if (_infoGroup != null)
            {
                _infoGroup.alpha = contentAlpha;
            }

            SetRingAlpha(1f - t);
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i] == null)
                {
                    continue;
                }

                var rect = (RectTransform)_orbs[i].transform;
                rect.anchoredPosition = Vector2.Lerp(_orbBasePositions[i], Vector2.zero, t);
                if (_orbs[i] == selected)
                {
                    rect.localScale = Vector3.one * Mathf.Lerp(selectedScale, selectedScale * 1.45f, t);
                }
                else
                {
                    rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.4f, t);
                    _orbGroups[i].alpha = 1f - t;
                }
            }

            yield return null;
        }

        // 2. 가운데에서 원소 색 빛이 터지고 구슬은 빛 속으로 사라진다.
        Image burstColor = SpawnBurst(selected, container, elementColor);
        Image burstWhite = SpawnBurst(selected, container, new Color(1f, 1f, 1f, 0.9f));
        elapsed = 0f;
        while (elapsed < _burstDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _burstDuration);
            float eased = UiRiseParts.EaseOut(t);
            SetBurst(burstColor, Mathf.Lerp(1f, 7f, eased), (1f - t) * 0.95f, elementColor);
            SetBurst(burstWhite, Mathf.Lerp(0.8f, 3f, eased), (1f - t) * (1f - t) * 0.9f, Color.white);
            int index = _selectedIndex;
            _orbGroups[index].alpha = 1f - UiRiseParts.EaseOut(t / 0.5f);
            selected.transform.localScale = Vector3.one * selectedScale * Mathf.Lerp(1.45f, 1.9f, eased);
            yield return null;
        }

        EquipAndNotify();
        FinishIfStillOpen();
    }

    private Image SpawnBurst(MainElementOrbView orb, Transform container, Color color)
    {
        if (orb.Glow == null)
        {
            return null;
        }

        GameObject burst = Instantiate(orb.Glow.gameObject, container, false);
        burst.name = "Fx_Burst";
        burst.SetActive(true);
        _bursts.Add(burst);
        var rect = (RectTransform)burst.transform;
        rect.anchoredPosition = Vector2.zero;
        rect.SetAsLastSibling();
        var image = burst.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = color;
        return image;
    }

    private static void SetBurst(Image burst, float scale, float alpha, Color color)
    {
        if (burst == null)
        {
            return;
        }

        burst.rectTransform.localScale = Vector3.one * scale;
        color.a = alpha;
        burst.color = color;
    }

    private void Refresh()
    {
        if (_orbs != null)
        {
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i] != null)
                {
                    _orbs[i].SetSelected(i == _selectedIndex);
                }
            }
        }

        ReactionElement element = SelectedElement;
        if (_nameText != null)
        {
            _nameText.text = ReactionElementStyle.GetName(element);
        }

        if (_effectText != null)
        {
            _effectText.text = GetEffect(element);
        }

        if (_confirmButton != null)
        {
            _confirmButton.SetFocused(_isButtonFocused && _isConfirmFocused);
        }

        if (_cancelButton != null)
        {
            _cancelButton.SetFocused(_isButtonFocused && !_isConfirmFocused);
        }
    }

    private string GetEffect(ReactionElement element)
    {
        if (_effects != null)
        {
            foreach (ElementText text in _effects)
            {
                if (text != null && text.Element == element)
                {
                    return text.Effect;
                }
            }
        }

        return string.Empty;
    }
}
