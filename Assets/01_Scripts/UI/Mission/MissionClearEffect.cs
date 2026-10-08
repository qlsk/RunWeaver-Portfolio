using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미션 성공 패널의 등장 연출. 패널이 켜질 때(OnEnable)마다 재생한다.
/// - 제목 : 살짝 아래에서 떠오르며 나타나고, 등장 직후 한 번 밝게 번쩍인다.
/// - 부제 : 제목보다 조금 늦게 떠오르며 나타난다.
/// - 불씨 : 글자 주변에서 작은 빛 입자가 위로 피어오르다 사라진다. (UI 이미지로 만들어 VFX 없이 동작)
/// 패널 전체의 페이드 인/아웃은 FadePanelView가 맡는다. 게임이 멈춰 있어도 진행되도록 unscaled 시간을 쓴다.
/// </summary>
[DisallowMultipleComponent]
public class MissionClearEffect : MonoBehaviour
{
    private struct Ember
    {
        public RectTransform Rect;
        public Image Image;
        public Vector2 Start;
        public float StartTime;
        public float Life;
        public float Speed;
        public float Phase;
    }

    [Header("제목")]
    [SerializeField] private TMP_Text _titleText;
    [SerializeField, Min(0f)] private float _titleDelay = 0.1f;
    [SerializeField, Min(0.01f)] private float _titleDuration = 0.35f;
    [Tooltip("등장할 때 아래에서 떠오르는 거리")]
    [SerializeField] private float _titleRise = 20f;
    [Tooltip("번쩍일 때의 색")]
    [SerializeField] private Color _flashColor = Color.white;
    [Tooltip("제목이 나타나기 시작한 뒤 번쩍임이 가장 밝아지는 시각(초)")]
    [SerializeField, Min(0f)] private float _flashPeak = 0.12f;
    [Tooltip("번쩍임 길이. 0이면 번쩍이지 않는다")]
    [SerializeField, Min(0f)] private float _flashWidth = 0.1f;
    [SerializeField, Range(0f, 1f)] private float _flashStrength = 0.6f;

    [Header("부제")]
    [SerializeField] private TMP_Text _subText;
    [SerializeField, Min(0f)] private float _subDelay = 0.45f;
    [SerializeField, Min(0.01f)] private float _subDuration = 0.3f;
    [SerializeField] private float _subRise = 12f;

    [Header("불씨")]
    [Tooltip("불씨 이미지. 비워두면 불씨를 만들지 않는다")]
    [SerializeField] private Sprite _emberSprite;
    [SerializeField] private Color _emberColor = new(1f, 0.75f, 0.43f, 1f);
    [SerializeField, Min(0)] private int _emberCount = 40;
    [Tooltip("불씨가 생기는 타원 영역의 반지름 (패널 중앙 기준)")]
    [SerializeField] private Vector2 _emberArea = new(520f, 110f);
    [Tooltip("타원 영역의 중심 위치 (패널 중앙 기준)")]
    [SerializeField] private Vector2 _emberAreaOffset = new(0f, 20f);
    [SerializeField] private Vector2 _emberSize = new(8f, 18f);
    [Tooltip("불씨 하나가 떠오르는 시간(초) 범위")]
    [SerializeField] private Vector2 _emberLife = new(1.2f, 2.2f);
    [Tooltip("위로 떠오르는 속도(초당 픽셀) 범위")]
    [SerializeField] private Vector2 _emberSpeed = new(40f, 90f);
    [Tooltip("패널이 켜진 뒤 불씨가 생기기 시작하는 시각(초) 범위")]
    [SerializeField] private Vector2 _emberStartTime = new(0.1f, 1.4f);
    [Tooltip("좌우로 흔들리는 폭(픽셀)")]
    [SerializeField, Min(0f)] private float _emberSway = 8f;

    private Vector2 _titleBasePosition;
    private Vector2 _subBasePosition;
    private Color _titleBaseColor = Color.white;
    private Color _subBaseColor = Color.white;
    private RectTransform _emberRoot;
    private Ember[] _embers;
    private Coroutine _routine;
    private bool _isInitialized;

    private void OnEnable()
    {
        Initialize();

        if (_routine != null)
        {
            StopCoroutine(_routine);
        }

        _routine = StartCoroutine(PlayRoutine());
    }

    private void OnDisable()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        ResetVisuals();
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        if (_titleText != null)
        {
            _titleBasePosition = _titleText.rectTransform.anchoredPosition;
            _titleBaseColor = _titleText.color;
        }

        if (_subText != null)
        {
            _subBasePosition = _subText.rectTransform.anchoredPosition;
            _subBaseColor = _subText.color;
        }

        CreateEmbers();
    }

    private IEnumerator PlayRoutine()
    {
        SetupEmbers();
        float elapsed = 0f;

        while (true)
        {
            UpdateTitle(elapsed);
            UpdateSub(elapsed);
            UpdateEmbers(elapsed);

            // 일시정지 메뉴가 열려 있는 동안에는 연출을 멈춘다.
            if (!PauseController.IsGamePaused)
            {
                elapsed += Time.unscaledDeltaTime;
            }

            yield return null;
        }
    }

    // ───────────────────────── 글자 ─────────────────────────

    private void UpdateTitle(float time)
    {
        if (_titleText == null)
        {
            return;
        }

        float t = EaseOut((time - _titleDelay) / _titleDuration);
        _titleText.rectTransform.anchoredPosition = _titleBasePosition + Vector2.down * (_titleRise * (1f - t));

        float flash = 0f;
        if (_flashWidth > 0f)
        {
            float x = (time - _titleDelay - _flashPeak) / _flashWidth;
            flash = Mathf.Exp(-x * x) * _flashStrength;
        }

        Color color = Color.Lerp(_titleBaseColor, _flashColor, flash);
        color.a = _titleBaseColor.a * t;
        _titleText.color = color;
    }

    private void UpdateSub(float time)
    {
        if (_subText == null)
        {
            return;
        }

        float t = EaseOut((time - _subDelay) / _subDuration);
        _subText.rectTransform.anchoredPosition = _subBasePosition + Vector2.down * (_subRise * (1f - t));

        Color color = _subBaseColor;
        color.a = _subBaseColor.a * t;
        _subText.color = color;
    }

    private void ResetVisuals()
    {
        if (_titleText != null)
        {
            _titleText.rectTransform.anchoredPosition = _titleBasePosition;
            _titleText.color = _titleBaseColor;
        }

        if (_subText != null)
        {
            _subText.rectTransform.anchoredPosition = _subBasePosition;
            _subText.color = _subBaseColor;
        }

        if (_embers == null)
        {
            return;
        }

        foreach (Ember ember in _embers)
        {
            ember.Image.enabled = false;
        }
    }

    // ───────────────────────── 불씨 ─────────────────────────

    private void CreateEmbers()
    {
        if (_emberSprite == null || _emberCount <= 0)
        {
            return;
        }

        // 글자 바로 뒤(비네트 앞)에 그려지도록 글자보다 앞 순서에 둔다.
        GameObject root = new GameObject("Embers", typeof(RectTransform));
        _emberRoot = root.GetComponent<RectTransform>();
        _emberRoot.SetParent(transform, false);
        _emberRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _emberRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _emberRoot.sizeDelta = Vector2.zero;
        _emberRoot.anchoredPosition = Vector2.zero;
        _emberRoot.SetSiblingIndex(GetEmberSiblingIndex());

        _embers = new Ember[_emberCount];
        for (int i = 0; i < _emberCount; i++)
        {
            GameObject go = new GameObject("Ember", typeof(RectTransform), typeof(Image));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(_emberRoot, false);

            Image image = go.GetComponent<Image>();
            image.sprite = _emberSprite;
            image.raycastTarget = false;
            image.enabled = false;

            _embers[i] = new Ember { Rect = rect, Image = image };
        }
    }

    /// <summary>글자보다 앞선 순서 중 가장 뒤 (글자 바로 아래)</summary>
    private int GetEmberSiblingIndex()
    {
        int index = transform.childCount - 1;
        if (_titleText != null && _titleText.transform.parent == transform)
        {
            index = Mathf.Min(index, _titleText.transform.GetSiblingIndex());
        }

        if (_subText != null && _subText.transform.parent == transform)
        {
            index = Mathf.Min(index, _subText.transform.GetSiblingIndex());
        }

        return Mathf.Max(0, index);
    }

    private void SetupEmbers()
    {
        if (_embers == null)
        {
            return;
        }

        for (int i = 0; i < _embers.Length; i++)
        {
            Ember ember = _embers[i];
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Mathf.Sqrt(Random.value);
            ember.Start = _emberAreaOffset + new Vector2(Mathf.Cos(angle) * _emberArea.x, Mathf.Sin(angle) * _emberArea.y) * radius;
            ember.StartTime = Random.Range(_emberStartTime.x, _emberStartTime.y);
            ember.Life = Mathf.Max(0.1f, Random.Range(_emberLife.x, _emberLife.y));
            ember.Speed = Random.Range(_emberSpeed.x, _emberSpeed.y);
            ember.Phase = Random.Range(0f, Mathf.PI * 2f);

            float size = Random.Range(_emberSize.x, _emberSize.y);
            ember.Rect.sizeDelta = new Vector2(size, size);
            ember.Image.enabled = false;
            _embers[i] = ember;
        }
    }

    private void UpdateEmbers(float time)
    {
        if (_embers == null)
        {
            return;
        }

        foreach (Ember ember in _embers)
        {
            float life = (time - ember.StartTime) / ember.Life;
            bool isAlive = life >= 0f && life <= 1f;
            ember.Image.enabled = isAlive;
            if (!isAlive)
            {
                continue;
            }

            float x = Mathf.Sin(life * 6f + ember.Phase) * _emberSway;
            float y = ember.Speed * ember.Life * life;
            ember.Rect.anchoredPosition = ember.Start + new Vector2(x, y);

            Color color = _emberColor;
            color.a = _emberColor.a * Mathf.Sin(life * Mathf.PI);
            ember.Image.color = color;
        }
    }

    private static float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }
}
