using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보상 선택 UI 전체 연출. 보상 UI 루트(RewardSelectUI_Root)에 붙인다. RewardSelectUI가 부른다.
/// - 열기   : 화면이 어두워지고 제목 아래 금빛 선이 그어진 뒤, 카드가 차례로 떠오르고 버튼이 나타난다.
/// - 리롤   : 카드 3장이 타들어가고, 새 카드로 바꾼 뒤 다시 떠오른다.
/// - 확정   : 고른 카드에 룬 문양이 찍히고(짧게 멈춤), 나머지는 타들어간다. 제목이 보상 이름으로 바뀐 뒤 UI가 닫힌다.
///            닫힌 뒤 캐릭터 발밑에 룬 고리(PlayerRewardAcquireFx)를 띄운다.
/// - 잿불   : 카드가 요청하면 꺼 둔 견본(_emberTemplate)을 복제해 띄운다.
/// 보상 UI는 게임 시간을 멈춘 채 열리므로 Time.unscaledDeltaTime으로 흐른다.
/// </summary>
[DisallowMultipleComponent]
public class RewardSelectPresentation : MonoBehaviour
{
    /// <summary>카드 영역 계산용 (GetWorldCorners)</summary>
    public static readonly Vector3[] CornerBuffer = new Vector3[4];

    [Header("연결")]
    [SerializeField] private Canvas _canvas;
    [Tooltip("화면을 어둡게 덮는 이미지")]
    [SerializeField] private Image _dim;
    [SerializeField] private Text _title;
    [Tooltip("확정했을 때 보상 이름을 보여줄 글자")]
    [SerializeField] private Text _acquiredTitle;
    [Tooltip("제목 아래 금빛 선. 가운데에서 양옆으로 그어진다")]
    [SerializeField] private Image _titleLine;
    [SerializeField] private RewardCardPresentation[] _cards;
    [Tooltip("확정 버튼, 다시 뽑기 버튼, 조작 안내")]
    [SerializeField] private CanvasGroup[] _footer;

    [Header("다시 뽑기")]
    [SerializeField] private Button _rerollButton;
    [SerializeField] private Text _rerollLabel;
    [Tooltip("{0} = 남은 횟수")]
    [SerializeField] private string _rerollFormat = "다시 뽑기 ({0})";
    [Tooltip("다시 뽑기를 다 썼을 때 버튼 투명도")]
    [SerializeField, Range(0f, 1f)] private float _rerollDisabledAlpha = 0.4f;

    [Header("잿불")]
    [SerializeField] private RectTransform _emberRoot;
    [Tooltip("복제해서 쓸 잿불 한 알 (꺼 둔 자식)")]
    [SerializeField] private Image _emberTemplate;

    [Header("열기")]
    [SerializeField, Min(0.01f)] private float _dimDuration = 0.35f;
    [SerializeField, Min(0f)] private float _lineDelay = 0.15f;
    [SerializeField, Min(0.01f)] private float _lineDuration = 0.45f;
    [SerializeField, Min(0f)] private float _titleDelay = 0.3f;
    [SerializeField, Min(0.01f)] private float _titleDuration = 0.4f;
    [Tooltip("첫 카드가 떠오르기 시작하는 시간")]
    [SerializeField, Min(0f)] private float _cardDelay = 0.45f;
    [Tooltip("카드끼리 떠오르는 간격")]
    [SerializeField, Min(0f)] private float _cardStagger = 0.12f;
    [SerializeField, Min(0f)] private float _footerDelay = 1.25f;
    [SerializeField, Min(0.01f)] private float _footerDuration = 0.35f;

    [Header("리롤")]
    [Tooltip("카드끼리 타들어가기 시작하는 간격")]
    [SerializeField, Min(0f)] private float _rerollBurnStagger = 0.05f;
    [Tooltip("다 탄 뒤 새 카드가 떠오르기 전까지 쉬는 시간")]
    [SerializeField, Min(0f)] private float _rerollGap = 0.05f;

    [Header("확정")]
    [Tooltip("확정 순간 잠깐 멈추는 시간 (이 동안 나머지 카드는 타지 않는다)")]
    [SerializeField, Min(0f)] private float _hitStop = 0.1f;
    [SerializeField, Min(0.01f)] private float _titleSwapDuration = 0.25f;
    [Tooltip("확정 후 UI 전체가 사라지기 시작하는 시간")]
    [SerializeField, Min(0f)] private float _closeDelay = 1.1f;
    [SerializeField, Min(0.01f)] private float _closeDuration = 0.4f;

    private sealed class Ember
    {
        public Image Image;
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Age;
        public float Life;
        public float Seed;
    }

    private enum Phase
    {
        Hidden,
        Opening,
        Idle,
        Rerolling,
        Confirming
    }

    private readonly List<Ember> _embers = new();
    private readonly Stack<Image> _emberPool = new();

    private Phase _phase = Phase.Hidden;
    private float _time;
    private float _dimAlpha = 1f;
    private Color _titleColor;
    private Color _acquiredColor;
    private Color _lineColor;
    private bool _initialized;
    private Action _pendingSwap;
    private bool _swapped;
    private float _rerollSwapTime;
    private Action _pendingFinish;
    private bool _finished;
    private bool _rerollDimmed;

    public Transform CanvasTransform => _canvas != null ? _canvas.transform : transform;

    /// <summary>열기·리롤·확정 연출 중에는 입력을 받지 않는다.</summary>
    public bool IsBusy => _phase is Phase.Rerolling or Phase.Confirming;

    private void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
        }

        _dimAlpha = _dim != null ? _dim.color.a : 1f;
        _titleColor = _title != null ? _title.color : Color.white;
        _acquiredColor = _acquiredTitle != null ? _acquiredTitle.color : Color.white;
        _lineColor = _titleLine != null ? _titleLine.color : Color.white;
        if (_emberTemplate != null)
        {
            _emberTemplate.gameObject.SetActive(false);
        }

        if (_cards != null)
        {
            foreach (RewardCardPresentation card in _cards)
            {
                if (card != null)
                {
                    card.Initialize(this);
                }
            }
        }
    }

    /// <summary>UI를 연 직후 부른다. 처음부터 열기 연출을 한다.</summary>
    public void PlayOpen()
    {
        Initialize();
        ClearEmbers();
        _phase = Phase.Opening;
        _time = 0f;
        _pendingSwap = null;
        _pendingFinish = null;
        SetAcquiredTitle(string.Empty, 0f);
        for (int i = 0; i < CardCount; i++)
        {
            _cards[i].PlayAppear(_cardDelay + _cardStagger * i);
        }

        Refresh();
    }

    /// <summary>카드 3장을 태운 뒤 swapContent로 내용을 바꾸고 다시 떠오르게 한다.</summary>
    public void PlayReroll(Action swapContent)
    {
        Initialize();
        if (_phase == Phase.Hidden)
        {
            swapContent?.Invoke();
            PlayOpen();
            return;
        }

        _phase = Phase.Rerolling;
        _time = 0f;
        _pendingSwap = swapContent;
        _swapped = false;
        float longest = 0f;
        for (int i = 0; i < CardCount; i++)
        {
            float delay = _rerollBurnStagger * i;
            _cards[i].SetSelected(false);
            _cards[i].SetDimmed(false);
            _cards[i].PlayBurn(delay);
            longest = Mathf.Max(longest, delay + _cards[i].BurnDuration);
        }

        _rerollSwapTime = longest + _rerollGap;
    }

    /// <summary>
    /// 확정 연출. 끝나면 onFinished를 부르고(여기서 UI가 닫힌다) 캐릭터 발밑 연출을 띄운다.
    /// </summary>
    public void PlayConfirm(int chosenIndex, string rewardName, Action onFinished)
    {
        Initialize();
        _phase = Phase.Confirming;
        _time = 0f;
        _pendingFinish = onFinished;
        _finished = false;
        if (_acquiredTitle != null)
        {
            _acquiredTitle.text = rewardName;
        }

        for (int i = 0; i < CardCount; i++)
        {
            if (i == chosenIndex)
            {
                _cards[i].SetDimmed(false);
                _cards[i].PlayChosen();
            }
            else
            {
                _cards[i].SetSelected(false);
                _cards[i].PlayBurn(_hitStop);
            }
        }
    }

    /// <summary>선택이 바뀔 때 부른다. 고른 카드는 밝게, 나머지는 어둡게.</summary>
    public void SetSelection(int index)
    {
        for (int i = 0; i < CardCount; i++)
        {
            _cards[i].SetSelected(i == index);
            _cards[i].SetDimmed(index >= 0 && i != index);
        }
    }

    public void SetRerollRemaining(int remaining)
    {
        if (_rerollLabel != null)
        {
            _rerollLabel.text = string.Format(_rerollFormat, Mathf.Max(0, remaining));
        }

        if (_rerollButton != null)
        {
            _rerollButton.interactable = remaining > 0;
        }

        // 다 쓴 버튼은 흐리게 한다. (열기 연출의 투명도와 곱해지도록 Refresh에서 반영)
        _rerollDimmed = remaining <= 0;
    }

    /// <summary>UI를 닫을 때 부른다. 남은 연출을 지운다.</summary>
    public void ResetAll()
    {
        _phase = Phase.Hidden;
        _pendingSwap = null;
        _pendingFinish = null;
        ClearEmbers();
        if (_cards == null)
        {
            return;
        }

        foreach (RewardCardPresentation card in _cards)
        {
            if (card != null)
            {
                card.ResetVisual(true);
            }
        }
    }

    /// <summary>잿불 한 알을 띄운다. position은 루트 캔버스 공간.</summary>
    public void SpawnEmber(Vector2 position, Color color, Vector2 velocity, float life, float size)
    {
        if (_emberTemplate == null || _emberRoot == null)
        {
            return;
        }

        Image image = _emberPool.Count > 0 ? _emberPool.Pop() : Instantiate(_emberTemplate, _emberRoot);
        image.gameObject.SetActive(true);
        image.rectTransform.sizeDelta = new Vector2(size, size);
        _embers.Add(new Ember
        {
            Image = image,
            Position = position,
            Velocity = velocity,
            Color = color,
            Life = Mathf.Max(0.05f, life),
            Seed = UnityEngine.Random.value * Mathf.PI * 2f
        });
    }

    private void Update()
    {
        if (_phase == Phase.Hidden)
        {
            return;
        }

        _time += Time.unscaledDeltaTime;

        if (_phase == Phase.Opening && _time >= _footerDelay + _footerDuration)
        {
            _phase = Phase.Idle;
        }

        if (_phase == Phase.Rerolling && !_swapped && _time >= _rerollSwapTime)
        {
            _swapped = true;
            Action swap = _pendingSwap;
            _pendingSwap = null;
            swap?.Invoke();
            for (int i = 0; i < CardCount; i++)
            {
                _cards[i].PlayAppear(_cardStagger * i);
            }
        }

        if (_phase == Phase.Rerolling && _swapped && _time >= _rerollSwapTime + CardsAppearTime())
        {
            _phase = Phase.Idle;
        }

        Refresh();
        UpdateEmbers(Time.unscaledDeltaTime);

        if (_phase == Phase.Confirming && !_finished && _time >= _closeDelay + _closeDuration)
        {
            _finished = true;
            Action finish = _pendingFinish;
            _pendingFinish = null;
            finish?.Invoke();
            PlayAcquireFx();
        }
    }

    private void Refresh()
    {
        float uiAlpha = 1f;
        float dim = 1f;
        float lineProgress = 1f;
        float titleAlpha = 1f;
        float footerAlpha = 1f;

        if (_phase == Phase.Opening)
        {
            dim = SmoothStep(_time / _dimDuration);
            lineProgress = EaseOut((_time - _lineDelay) / _lineDuration);
            titleAlpha = SmoothStep((_time - _titleDelay) / _titleDuration);
            footerAlpha = SmoothStep((_time - _footerDelay) / _footerDuration);
        }

        float acquired = 0f;
        if (_phase == Phase.Confirming)
        {
            uiAlpha = 1f - SmoothStep((_time - _closeDelay) / _closeDuration);
            dim = uiAlpha;
            float swap = SmoothStep((_time - _hitStop) / _titleSwapDuration);
            titleAlpha = 1f - swap;
            acquired = swap;
        }

        if (_dim != null)
        {
            Color color = _dim.color;
            color.a = _dimAlpha * dim;
            _dim.color = color;
        }

        if (_titleLine != null)
        {
            _titleLine.rectTransform.localScale = new Vector3(Mathf.Max(0.001f, lineProgress), 1f, 1f);
            _titleLine.color = WithAlpha(_lineColor, _lineColor.a * Mathf.Clamp01(lineProgress * 3f) * uiAlpha);
        }

        if (_title != null)
        {
            _title.color = WithAlpha(_titleColor, _titleColor.a * titleAlpha * uiAlpha);
        }

        SetAcquiredTitle(null, acquired * uiAlpha);

        if (_footer != null)
        {
            foreach (CanvasGroup group in _footer)
            {
                if (group == null)
                {
                    continue;
                }

                bool isReroll = _rerollButton != null && group.gameObject == _rerollButton.gameObject;
                group.alpha = footerAlpha * uiAlpha * (isReroll && _rerollDimmed ? _rerollDisabledAlpha : 1f);
            }
        }
    }

    private void SetAcquiredTitle(string text, float alpha)
    {
        if (_acquiredTitle == null)
        {
            return;
        }

        if (text != null)
        {
            _acquiredTitle.text = text;
        }

        _acquiredTitle.color = WithAlpha(_acquiredColor, _acquiredColor.a * alpha);
        _acquiredTitle.enabled = alpha > 0.001f;
    }

    private void UpdateEmbers(float deltaTime)
    {
        Transform canvasTransform = CanvasTransform;
        for (int i = _embers.Count - 1; i >= 0; i--)
        {
            Ember ember = _embers[i];
            ember.Age += deltaTime;
            if (ember.Age >= ember.Life || ember.Image == null)
            {
                Release(i);
                continue;
            }

            float t = ember.Age / ember.Life;
            ember.Position += ember.Velocity * deltaTime;
            Vector2 sway = new(Mathf.Sin(ember.Seed + ember.Age * 6f) * 3f, 0f);
            ember.Image.rectTransform.position = canvasTransform.TransformPoint(ember.Position + sway);
            float flicker = 0.75f + 0.25f * Mathf.Sin(ember.Seed + ember.Age * 18f);
            ember.Image.color = WithAlpha(ember.Color, ember.Color.a * (1f - t) * flicker);
        }
    }

    private void Release(int index)
    {
        Ember ember = _embers[index];
        _embers.RemoveAt(index);
        if (ember.Image != null)
        {
            ember.Image.gameObject.SetActive(false);
            _emberPool.Push(ember.Image);
        }
    }

    private void ClearEmbers()
    {
        for (int i = _embers.Count - 1; i >= 0; i--)
        {
            Release(i);
        }
    }

    private void PlayAcquireFx()
    {
        PlayerRewardAcquireFx fx = FindAnyObjectByType<PlayerRewardAcquireFx>();
        if (fx != null)
        {
            fx.Play();
        }
    }

    private float CardsAppearTime()
    {
        float longest = 0f;
        for (int i = 0; i < CardCount; i++)
        {
            longest = Mathf.Max(longest, _cardStagger * i + _cards[i].AppearDuration);
        }

        return longest;
    }

    private int CardCount => _cards != null ? _cards.Length : 0;

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static float EaseOut(float value)
    {
        value = Mathf.Clamp01(value);
        float inverse = 1f - value;
        return 1f - inverse * inverse * inverse;
    }

    private static float SmoothStep(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }
}
