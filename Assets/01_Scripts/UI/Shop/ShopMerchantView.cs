using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 상점 주인 연출.
/// - 말풍선 : 상점에서 일이 생길 때마다 한마디씩 던진다. 글자가 한 자씩 찍히고, 잠시 뒤 사라진다.
///   같은 상황에서 바로 전에 한 말은 다시 하지 않는다.
///   중요한 말(구매, 거절 등)이 찍히는 중이면 덜 중요한 말(선택, 혼잣말)은 끼어들지 않는다.
/// - 눈 : 배경에 그려진 눈 위에 눈꺼풀(어두운 이미지)을 덮어 가끔 깜빡이고, 눈빛이 은은하게 일렁인다.
///   말하는 동안에는 눈빛이 조금 밝아진다.
/// - 클릭 : 상점 주인을 누르면 반응한다. 연달아 누를수록 짜증을 내며 눈을 가늘게 뜨고,
///   계속 누르면 비밀스러운 말을 한 번 하고 처음으로 돌아간다.
///   마우스를 올리면 눈빛이 살짝 밝아진다. (클릭 영역은 자식의 투명 이미지가 받는다)
/// - 몸 : 배경에서 떼어낸 몸통 이미지(_body)가 숨 쉬듯 들썩이고, 말할 때 끄덕이며,
///   상황마다 움찔하거나(클릭) 깡총 뛰거나(구매) 고개를 젓거나(거절) 인사한다(작별).
///   몸통의 pivot은 아래 가운데여야 한다. (아래쪽은 배경과 자연스럽게 섞이도록 흐려져 있다)
/// 상점이 열린 동안 게임이 멈춰 있으므로 실제 시간으로 움직인다.
/// </summary>
[DisallowMultipleComponent]
public class ShopMerchantView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private enum BodyMotion
    {
        None,
        Squash,
        Hop,
        Shake,
        Tremble,
        Lean,
        Bow
    }

    private enum BubbleState
    {
        Hidden,
        Typing,
        Holding,
        Hiding
    }

    private const int PriorityLow = 0;
    private const int PriorityNormal = 1;
    private const int PriorityHigh = 2;

    [Header("말풍선")]
    [Tooltip("말풍선 루트. 꼬리 쪽 모서리를 pivot으로 두면 그쪽에서 튀어나온다")]
    [SerializeField] private RectTransform _bubble;
    [SerializeField] private TMP_Text _bubbleText;
    [Tooltip("말풍선 테두리와 글자 사이 여백 (좌우, 위아래)")]
    [SerializeField] private Vector2 _bubblePadding = new(30f, 22f);
    [SerializeField, Min(10f)] private float _minTextWidth = 90f;
    [SerializeField, Min(10f)] private float _maxTextWidth = 360f;
    [Tooltip("1초에 찍히는 글자 수")]
    [SerializeField, Min(1f)] private float _charsPerSecond = 26f;
    [Tooltip("다 찍힌 뒤 유지 시간 = 기본 + 글자 수 x 글자당 시간")]
    [SerializeField, Min(0f)] private float _holdBase = 1.6f;
    [SerializeField, Min(0f)] private float _holdPerChar = 0.06f;
    [SerializeField, Min(0.01f)] private float _popDuration = 0.26f;
    [SerializeField, Min(0.01f)] private float _hideDuration = 0.2f;

    [Header("타이밍")]
    [Tooltip("상점이 열리고 인사하기까지")]
    [SerializeField, Min(0f)] private float _greetingDelay = 0.5f;
    [Tooltip("품목을 골랐을 때 한마디 할 확률")]
    [SerializeField, Range(0f, 1f)] private float _selectLineChance = 0.55f;
    [Tooltip("아무것도 안 할 때 혼잣말 간격 (최소, 최대)")]
    [SerializeField] private Vector2 _idleInterval = new(13f, 22f);

    [Header("눈")]
    [Tooltip("눈 위를 덮는 어두운 이미지. pivot을 위쪽에 두고 세로 크기로 눈을 감는다")]
    [SerializeField] private RectTransform[] _eyeLids;
    [SerializeField] private Graphic[] _eyeGlows;
    [Tooltip("깜빡이는 간격 (최소, 최대)")]
    [SerializeField] private Vector2 _blinkInterval = new(2.4f, 5.2f);
    [SerializeField, Min(0.01f)] private float _blinkDuration = 0.16f;
    [SerializeField, Range(0f, 1f)] private float _doubleBlinkChance = 0.25f;
    [SerializeField, Range(0f, 1f)] private float _glowAlpha = 0.4f;
    [SerializeField, Range(0f, 1f)] private float _glowPulse = 0.12f;
    [SerializeField, Min(0f)] private float _glowPulseSpeed = 2.1f;
    [Tooltip("말하는 동안 더해지는 눈빛 밝기")]
    [SerializeField, Range(0f, 1f)] private float _glowTalkBoost = 0.25f;

    [Header("몸")]
    [Tooltip("배경에서 떼어낸 상점 주인 몸통. pivot은 아래 가운데")]
    [SerializeField] private RectTransform _body;
    [Tooltip("숨 쉴 때 세로로 늘어나는 비율")]
    [SerializeField, Range(0f, 0.1f)] private float _breathAmount = 0.012f;
    [SerializeField, Min(0.1f)] private float _breathPeriod = 3.2f;
    [Tooltip("말할 때 끄덕이는 비율")]
    [SerializeField, Range(0f, 0.1f)] private float _talkBobAmount = 0.012f;
    [Tooltip("움찔하거나 깡총 뛸 때 찌그러지는 비율")]
    [SerializeField, Range(0f, 0.2f)] private float _bounceAmount = 0.06f;
    [Tooltip("고개를 젓거나 부들거릴 때 좌우로 움직이는 거리")]
    [SerializeField, Min(0f)] private float _shakeDistance = 3f;
    [Tooltip("비밀 이야기를 할 때 다가오는 비율")]
    [SerializeField, Range(0f, 0.2f)] private float _leanAmount = 0.04f;

    [Header("클릭")]
    [Tooltip("이 시간 동안 다시 누르지 않으면 짜증이 풀린다")]
    [SerializeField, Min(0f)] private float _pokeResetTime = 4f;
    [Tooltip("이만큼 연달아 누르면 짜증을 낸다")]
    [SerializeField, Min(1)] private int _annoyedPokeCount = 3;
    [Tooltip("이만큼 연달아 누르면 비밀스러운 말을 한다")]
    [SerializeField, Min(1)] private int _secretPokeCount = 7;
    [SerializeField, Range(0f, 1f)] private float _hoverGlowBoost = 0.18f;
    [Tooltip("눌렀을 때 눈빛이 번쩍이는 세기")]
    [SerializeField, Range(0f, 1f)] private float _pokeGlowFlash = 0.6f;
    [Tooltip("짜증 낼 때 눈을 감는 정도 (0 = 뜬 눈, 1 = 감은 눈)")]
    [SerializeField, Range(0f, 1f)] private float _squintAmount = 0.45f;
    [SerializeField, Min(0.1f)] private float _squintDuration = 2.2f;

    [Header("대사")]
    [SerializeField] private string[] _greetingLines =
    {
        "어서 오게, 정령계의 손님.",
        "살아서 또 왔구먼. 반갑네.",
        "흐흐, 오늘은 물건이 좋다네.",
        "심연에서 건져 올린 물건들일세. 천천히 보게.",
        "또 보는군. 주머니는 두둑한가?",
    };

    [SerializeField] private string[] _selectRuneWordLines =
    {
        "그 룬 워드는 제법 쓸 만하지.",
        "주문에 새 말을 새기는 거라네.",
        "룬 워드라... 안목이 있구먼.",
    };

    [SerializeField] private string[] _selectRuneStoneLines =
    {
        "룬석은 많을수록 좋지.",
        "룬을 새기려면 그게 있어야 할 걸세.",
    };

    [SerializeField] private string[] _selectPotionLines =
    {
        "한 모금이면 상처가 아문다네.",
        "몸이 성해야 정화도 하는 법이지.",
    };

    [SerializeField] private string[] _selectOtherLines =
    {
        "눈이 높구먼.",
        "흐흐, 그게 마음에 드나?",
    };

    [SerializeField] private string[] _potionFullLines =
    {
        "멀쩡해 보이는데? 아껴 두게.",
        "다친 데도 없으면서 뭘 마시려고.",
    };

    [SerializeField] private string[] _purchaseLines =
    {
        "좋은 선택일세!",
        "거래 성립! 흐흐.",
        "잘 쓰게나.",
        "후회는 없을 걸세.",
    };

    [SerializeField] private string[] _purchasePotionLines =
    {
        "쭉 들이켜게!",
        "맛은 장담 못 하네. 흐흐.",
    };

    [SerializeField] private string[] _notEnoughGoldLines =
    {
        "골드가 모자라는구먼.",
        "외상은 안 받네.",
        "그 돈으론 어림없지.",
        "적을 좀 더 잡고 오게.",
    };

    [SerializeField] private string[] _rerollLines =
    {
        "다른 물건도 꺼내 보지.",
        "창고 깊숙한 데 이런 게 있었군.",
        "까다롭구먼. 자, 이건 어떤가?",
    };

    [SerializeField] private string[] _rerollDeniedLines =
    {
        "새 물건을 보려면 골드가 필요하다네.",
        "공짜로 보여 줄 순 없지.",
    };

    [SerializeField] private string[] _soldOutLines =
    {
        "싹 쓸어 가는구먼! 기분 좋군.",
        "오늘 장사는 여기까지일세. 흐흐.",
    };

    [SerializeField] private string[] _farewellLines =
    {
        "살아서 또 보세.",
        "심연은 깊다네. 조심하게.",
        "다음 층에서 기다리지.",
        "다음엔 더 좋은 물건을 구해 두지.",
    };

    [SerializeField] private string[] _pokeLines =
    {
        "응? 뭔가 필요한가?",
        "흐흐, 간지럽구먼.",
        "물건은 저쪽일세.",
        "이 등짐엔 심연 열 층 분량의 물건이 들어 있다네.",
    };

    [SerializeField] private string[] _annoyedLines =
    {
        "그만 찌르게.",
        "손버릇이 고약하구먼.",
        "자꾸 그러면 값을 올리겠네!",
        "어허, 후드 구겨지네.",
    };

    [SerializeField] private string[] _secretLines =
    {
        "...후드 속이 궁금한가? 그건 비밀일세.",
        "정령계에선 호기심이 사람을 잡아먹지. 흐흐.",
        "...자네, 끈기 하나는 알아줘야겠군.",
    };

    [SerializeField] private string[] _idleLines =
    {
        "구경만 할 텐가?",
        "흠... 서두를 것 없네.",
        "이 등짐이 보기보다 무겁다네.",
        "고르는 데 오래 걸리는구먼.",
    };

    private CanvasGroup _bubbleGroup;
    private RectTransform _textRect;
    private BubbleState _state = BubbleState.Hidden;
    private float _stateTime;
    private float _holdTime;
    private int _charCount;
    private int _currentPriority;

    private string _queuedLine;
    private int _queuedPriority;
    private float _queuedDelay;

    private float _idleTimer;
    private float _blinkTimer;
    private float _blinkTime = float.NaN;
    private bool _blinkAgain;
    private float _talkGlow;
    private bool _isInitialized;

    private BodyMotion _motion;
    private float _motionTime;
    private BodyMotion _queuedMotion;
    private Vector2 _bodyBasePosition;
    private Vector3 _bodyBaseScale = Vector3.one;

    private int _pokeCount;
    private float _lastPokeTime = float.NegativeInfinity;
    private bool _isHovered;
    private float _hoverGlow;
    private float _flash;
    private float _squintTime;

    // 상황별로 바로 전에 한 대사 (같은 말을 연달아 하지 않도록)
    private readonly System.Collections.Generic.Dictionary<string[], int> _lastLineIndex = new();

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        HideImmediate();
        ResetIdleTimer();
        _blinkTimer = Random.Range(_blinkInterval.x, _blinkInterval.y);
        _blinkTime = float.NaN;
        _talkGlow = 0f;
        _pokeCount = 0;
        _isHovered = false;
        _hoverGlow = 0f;
        _flash = 0f;
        _squintTime = 0f;
        _motion = BodyMotion.None;
        _queuedMotion = BodyMotion.None;
    }

    private void OnDisable()
    {
        if (_body != null && _isInitialized)
        {
            _body.localScale = _bodyBaseScale;
            _body.anchoredPosition = _bodyBasePosition;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        // 상점이 열리거나 닫히는 중(조작을 막아 둔 동안)에는 반응하지 않는다.
        foreach (CanvasGroup group in GetComponentsInParent<CanvasGroup>())
        {
            if (!group.interactable)
            {
                return;
            }
        }

        float now = Time.unscaledTime;
        if (now - _lastPokeTime > _pokeResetTime)
        {
            _pokeCount = 0;
        }

        _lastPokeTime = now;
        _pokeCount++;
        _flash = 1f;

        if (_pokeCount >= _secretPokeCount)
        {
            // 끝까지 누른 손님에게 한마디 하고 처음으로 돌아간다.
            _pokeCount = 0;
            _squintTime = 0f;
            PlayMotion(BodyMotion.Lean);
            Say(_secretLines, PriorityHigh);
            return;
        }

        if (_pokeCount >= _annoyedPokeCount)
        {
            _squintTime = _squintDuration;
            PlayMotion(BodyMotion.Tremble);
            Say(_annoyedLines, PriorityNormal);
            return;
        }

        // 깜짝 놀라 한 번 깜빡인다.
        _blinkTime = 0f;
        _blinkAgain = false;
        PlayMotion(BodyMotion.Squash);
        Say(_pokeLines, PriorityNormal);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
    }

    /// <summary>상점이 열렸을 때. 잠시 뒤 인사한다.</summary>
    public void PlayGreeting()
    {
        Queue(Pick(_greetingLines), PriorityHigh, _greetingDelay, BodyMotion.Bow);
    }

    /// <summary>품목을 골랐을 때. 살 수 없는 포션이면 꼭 한마디 하고, 아니면 가끔 한마디 한다.</summary>
    public void ReactToSelect(ShopOfferData offer, bool isAvailable)
    {
        if (offer == null)
        {
            return;
        }

        if (offer.Type == ShopItemType.Potion && !isAvailable)
        {
            Say(_potionFullLines, PriorityNormal, BodyMotion.Shake);
            return;
        }

        if (Random.value > _selectLineChance)
        {
            ResetIdleTimer();
            return;
        }

        string[] pool = offer.Type switch
        {
            ShopItemType.RuneWord => _selectRuneWordLines,
            ShopItemType.RuneStone => _selectRuneStoneLines,
            ShopItemType.Potion => _selectPotionLines,
            _ => _selectOtherLines,
        };

        Say(pool, PriorityLow);
    }

    public void ReactToPurchase(ShopOfferData offer, bool isSoldOut)
    {
        if (isSoldOut)
        {
            Say(_soldOutLines, PriorityNormal, BodyMotion.Hop);
            return;
        }

        bool isPotion = offer != null && offer.Type == ShopItemType.Potion;
        Say(isPotion ? _purchasePotionLines : _purchaseLines, PriorityNormal, BodyMotion.Hop);
    }

    public void ReactToNotEnoughGold()
    {
        Say(_notEnoughGoldLines, PriorityNormal, BodyMotion.Shake);
    }

    public void ReactToPotionFull()
    {
        Say(_potionFullLines, PriorityNormal, BodyMotion.Shake);
    }

    public void ReactToReroll(bool success)
    {
        Say(success ? _rerollLines : _rerollDeniedLines, PriorityNormal, success ? BodyMotion.Hop : BodyMotion.Shake);
    }

    public void SayFarewell()
    {
        Say(_farewellLines, PriorityHigh, BodyMotion.Bow);
    }

    private void Say(string[] pool, int priority, BodyMotion motion = BodyMotion.None)
    {
        Queue(Pick(pool), priority, 0f, motion);
    }

    private void Queue(string line, int priority, float delay, BodyMotion motion = BodyMotion.None)
    {
        ResetIdleTimer();
        if (string.IsNullOrEmpty(line) || !isActiveAndEnabled)
        {
            return;
        }

        // 더 중요한 말이 찍히는 중이거나 기다리는 중이면 끼어들지 않는다.
        bool isBusy = _state == BubbleState.Typing || (_state == BubbleState.Holding && _stateTime < 0.6f);
        if (isBusy && priority < _currentPriority)
        {
            return;
        }

        if (_queuedLine != null && priority < _queuedPriority)
        {
            return;
        }

        _queuedLine = line;
        _queuedPriority = priority;
        _queuedDelay = delay;
        _queuedMotion = motion;
    }

    private string Pick(string[] pool)
    {
        if (pool == null || pool.Length == 0)
        {
            return null;
        }

        int index = Random.Range(0, pool.Length);
        if (pool.Length > 1 && _lastLineIndex.TryGetValue(pool, out int last) && index == last)
        {
            index = (index + 1 + Random.Range(0, pool.Length - 1)) % pool.Length;
        }

        _lastLineIndex[pool] = index;
        return pool[index];
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        UpdateQueue(deltaTime);
        UpdateBubble(deltaTime);
        UpdateIdle(deltaTime);
        UpdateEyes(deltaTime);
        UpdateBody(deltaTime);
    }

    private void PlayMotion(BodyMotion motion)
    {
        _motion = motion;
        _motionTime = 0f;
    }

    private static float GetMotionDuration(BodyMotion motion)
    {
        return motion switch
        {
            BodyMotion.Squash => 0.7f,
            BodyMotion.Hop => 0.7f,
            BodyMotion.Shake => 0.45f,
            BodyMotion.Tremble => 0.45f,
            BodyMotion.Lean => 1.6f,
            BodyMotion.Bow => 0.8f,
            _ => 0f,
        };
    }

    /// <summary>몸통의 크기와 위치를 정한다. 숨쉬기 + 말할 때 끄덕임 + 상황별 움직임을 더한다.</summary>
    private void UpdateBody(float deltaTime)
    {
        if (_body == null)
        {
            return;
        }

        float time = Time.unscaledTime;
        // stretch : 세로로 늘어나는 비율 (+면 늘어나고 가로는 그만큼 줄어든다)
        float stretch = Mathf.Sin(time * Mathf.PI * 2f / _breathPeriod) * _breathAmount;
        float grow = 0f;
        Vector2 offset = Vector2.zero;

        if (_state == BubbleState.Typing)
        {
            stretch += Mathf.Abs(Mathf.Sin(time * 9f)) * _talkBobAmount;
        }

        if (_motion != BodyMotion.None)
        {
            _motionTime += deltaTime;
            float duration = GetMotionDuration(_motion);
            float t = _motionTime;
            float k = Mathf.Clamp01(t / duration);
            float wave = Mathf.Sin(t * Mathf.PI * 2f * 2.4f) * Mathf.Exp(-6f * t);

            switch (_motion)
            {
                case BodyMotion.Squash:
                    stretch -= _bounceAmount * wave;
                    break;
                case BodyMotion.Hop:
                    stretch += _bounceAmount * 0.8f * wave;
                    break;
                case BodyMotion.Shake:
                    offset.x = Mathf.Sin(t * Mathf.PI * 2f * 6f) * _shakeDistance * (1f - k);
                    break;
                case BodyMotion.Tremble:
                    offset.x = Mathf.Sin(t * Mathf.PI * 2f * 18f) * _shakeDistance * 0.7f * (1f - k);
                    stretch -= 0.02f * (1f - k);
                    break;
                case BodyMotion.Lean:
                    grow = _leanAmount * Mathf.SmoothStep(0f, 1f, ShopEase.Bump(k));
                    break;
                case BodyMotion.Bow:
                    stretch -= 0.05f * ShopEase.Bump(k);
                    break;
            }

            if (_motionTime >= duration)
            {
                _motion = BodyMotion.None;
            }
        }

        float scaleX = 1f - stretch * 0.6f + grow;
        float scaleY = 1f + stretch + grow;
        _body.localScale = new Vector3(_bodyBaseScale.x * scaleX, _bodyBaseScale.y * scaleY, _bodyBaseScale.z);
        _body.anchoredPosition = _bodyBasePosition + offset;
    }

    private void UpdateQueue(float deltaTime)
    {
        if (_queuedLine == null)
        {
            return;
        }

        _queuedDelay -= deltaTime;
        if (_queuedDelay > 0f)
        {
            return;
        }

        string line = _queuedLine;
        int priority = _queuedPriority;
        _queuedLine = null;
        if (_queuedMotion != BodyMotion.None)
        {
            PlayMotion(_queuedMotion);
            _queuedMotion = BodyMotion.None;
        }

        StartLine(line, priority);
    }

    private void StartLine(string line, int priority)
    {
        if (_bubble == null || _bubbleText == null)
        {
            return;
        }

        _currentPriority = priority;
        _bubbleText.text = line;
        _bubbleText.maxVisibleCharacters = 0;

        // 다 찍혔을 때의 크기로 말풍선을 미리 맞춰 둔다. (찍히는 동안 크기가 변하지 않게)
        Vector2 preferred = _bubbleText.GetPreferredValues(line, _maxTextWidth, 0f);
        float width = Mathf.Clamp(Mathf.Ceil(preferred.x) + 2f, _minTextWidth, _maxTextWidth);
        float height = Mathf.Ceil(_bubbleText.GetPreferredValues(line, width, 0f).y);
        _bubble.sizeDelta = new Vector2(width + _bubblePadding.x * 2f, height + _bubblePadding.y * 2f);

        if (_textRect != null)
        {
            _textRect.offsetMin = _bubblePadding;
            _textRect.offsetMax = -_bubblePadding;
        }

        _bubbleText.ForceMeshUpdate();
        _charCount = _bubbleText.textInfo.characterCount;

        // 이미 떠 있던 말풍선이면 처음부터 튀어나오지 않고 살짝만 튄다.
        _stateTime = _state == BubbleState.Hidden || _state == BubbleState.Hiding ? 0f : _popDuration * 0.55f;
        _state = BubbleState.Typing;
        _holdTime = _holdBase + _charCount * _holdPerChar;
    }

    private void UpdateBubble(float deltaTime)
    {
        if (_bubble == null || _state == BubbleState.Hidden)
        {
            return;
        }

        _stateTime += deltaTime;
        float scale = 1f;
        float alpha = 1f;

        switch (_state)
        {
            case BubbleState.Typing:
            {
                float pop = _stateTime / _popDuration;
                scale = Mathf.LerpUnclamped(0.6f, 1f, ShopEase.OutBack(pop, 2.2f));
                alpha = ShopEase.OutCubic(pop * 1.5f);

                int visible = Mathf.FloorToInt(Mathf.Max(0f, _stateTime - _popDuration * 0.4f) * _charsPerSecond);
                _bubbleText.maxVisibleCharacters = Mathf.Min(visible, _charCount);
                if (visible >= _charCount && pop >= 1f)
                {
                    _bubbleText.maxVisibleCharacters = _charCount;
                    _state = BubbleState.Holding;
                    _stateTime = 0f;
                }

                break;
            }
            case BubbleState.Holding:
                if (_stateTime >= _holdTime)
                {
                    _state = BubbleState.Hiding;
                    _stateTime = 0f;
                }

                break;
            case BubbleState.Hiding:
            {
                float t = _stateTime / _hideDuration;
                scale = Mathf.Lerp(1f, 0.85f, ShopEase.InCubic(t));
                alpha = 1f - ShopEase.OutCubic(t);
                if (t >= 1f)
                {
                    HideImmediate();
                    return;
                }

                break;
            }
        }

        _bubble.localScale = new Vector3(scale, scale, 1f);
        _bubbleGroup.alpha = alpha;
    }

    private void HideImmediate()
    {
        _state = BubbleState.Hidden;
        _stateTime = 0f;
        _currentPriority = PriorityLow;
        _queuedLine = null;
        if (_bubble != null)
        {
            _bubble.localScale = Vector3.one;
        }

        if (_bubbleGroup != null)
        {
            _bubbleGroup.alpha = 0f;
        }
    }

    private void UpdateIdle(float deltaTime)
    {
        if (_state != BubbleState.Hidden || _queuedLine != null)
        {
            return;
        }

        _idleTimer -= deltaTime;
        if (_idleTimer <= 0f)
        {
            Say(_idleLines, PriorityLow);
        }
    }

    private void ResetIdleTimer()
    {
        _idleTimer = Random.Range(_idleInterval.x, Mathf.Max(_idleInterval.x, _idleInterval.y));
    }

    private void UpdateEyes(float deltaTime)
    {
        if (float.IsNaN(_blinkTime))
        {
            _blinkTimer -= deltaTime;
            if (_blinkTimer <= 0f)
            {
                _blinkTime = 0f;
                _blinkAgain = Random.value < _doubleBlinkChance;
            }
        }
        else
        {
            _blinkTime += deltaTime;
            if (_blinkTime >= _blinkDuration)
            {
                if (_blinkAgain)
                {
                    _blinkAgain = false;
                    _blinkTime = -0.08f; // 잠깐 떴다가 한 번 더 감는다.
                }
                else
                {
                    _blinkTime = float.NaN;
                    _blinkTimer = Random.Range(_blinkInterval.x, Mathf.Max(_blinkInterval.x, _blinkInterval.y));
                }
            }
        }

        // 0 = 뜬 눈, 1 = 감은 눈
        float closed = float.IsNaN(_blinkTime) || _blinkTime < 0f ? 0f : ShopEase.Bump(_blinkTime / _blinkDuration);

        // 짜증 낼 때는 눈을 가늘게 뜬다. (시작과 끝은 부드럽게)
        if (_squintTime > 0f)
        {
            _squintTime -= deltaTime;
            float elapsed = _squintDuration - _squintTime;
            float fade = Mathf.Min(1f, elapsed / 0.15f, Mathf.Max(0f, _squintTime) / 0.3f);
            closed = Mathf.Max(closed, _squintAmount * fade);
        }

        _hoverGlow = Mathf.Lerp(_hoverGlow, _isHovered ? 1f : 0f, ShopEase.Damp(10f, deltaTime));
        _flash = Mathf.Max(0f, _flash - deltaTime * 3f);
        if (_eyeLids != null)
        {
            foreach (RectTransform lid in _eyeLids)
            {
                if (lid != null)
                {
                    lid.localScale = new Vector3(1f, closed, 1f);
                }
            }
        }

        bool isTalking = _state == BubbleState.Typing || _state == BubbleState.Holding;
        _talkGlow = Mathf.Lerp(_talkGlow, isTalking ? 1f : 0f, ShopEase.Damp(6f, deltaTime));

        if (_eyeGlows != null)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * _glowPulseSpeed) * 0.5f + 0.5f;
            float boost = _glowTalkBoost * _talkGlow + _hoverGlowBoost * _hoverGlow + _pokeGlowFlash * _flash;
            float alpha = (_glowAlpha + _glowPulse * pulse + boost) * (1f - closed * 0.85f);
            foreach (Graphic glow in _eyeGlows)
            {
                if (glow != null)
                {
                    Color color = glow.color;
                    color.a = Mathf.Clamp01(alpha);
                    glow.color = color;
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
        if (_body != null)
        {
            _bodyBasePosition = _body.anchoredPosition;
            _bodyBaseScale = _body.localScale;
        }

        if (_bubble != null)
        {
            _bubbleGroup = _bubble.GetComponent<CanvasGroup>();
            if (_bubbleGroup == null)
            {
                _bubbleGroup = _bubble.gameObject.AddComponent<CanvasGroup>();
            }

            _bubbleGroup.blocksRaycasts = false;
            _bubbleGroup.interactable = false;
        }

        if (_bubbleText != null)
        {
            _textRect = _bubbleText.rectTransform;
        }
    }
}
