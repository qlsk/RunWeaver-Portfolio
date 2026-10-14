using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 상점의 보유 골드 표시 연출.
/// - 골드가 바뀌면 숫자가 굴러가듯 바뀌고, 바뀐 만큼 "-120" / "+120" 글자가 떠올랐다 사라진다.
/// - 골드가 부족할 때는 좌우로 흔들리며 붉게 깜빡인다.
/// 숫자 표시는 GoldHudView에 맡긴다. 실제 시간으로 움직인다.
/// </summary>
[DisallowMultipleComponent]
public class ShopGoldCounter : MonoBehaviour
{
    [SerializeField] private GoldHudView _goldView;
    [Tooltip("숫자 글자. 바뀐 양을 띄울 때 이 글자를 복제해서 쓴다")]
    [SerializeField] private TMP_Text _amountText;

    [Header("숫자")]
    [SerializeField, Min(0.01f)] private float _countDuration = 0.45f;
    [Tooltip("골드가 줄어드는 동안 똑딱이는 소리 간격")]
    [SerializeField, Min(0.02f)] private float _tickInterval = 0.09f;
    [SerializeField] private float _changePunch = 0.12f;

    [Header("바뀐 양")]
    [SerializeField] private Color _spendColor = new(1f, 0.5f, 0.42f, 1f);
    [SerializeField] private Color _gainColor = new(1f, 0.86f, 0.42f, 1f);
    [SerializeField] private Vector2 _deltaOffset = new(0f, -40f);
    [Tooltip("바뀐 양 글자가 움직이는 거리 (음수면 아래로)")]
    [SerializeField] private float _deltaMove = -30f;
    [SerializeField, Min(0.01f)] private float _deltaDuration = 0.9f;

    [Header("부족")]
    [SerializeField] private Color _deniedColor = new(0.9f, 0.2f, 0.15f, 1f);
    [SerializeField, Min(0.01f)] private float _deniedDuration = 0.45f;
    [SerializeField] private float _deniedShake = 9f;

    private class DeltaPopup
    {
        public TMP_Text Text;
        public Vector2 Start;
        public Color Color;
        public float Time;
    }

    private readonly List<DeltaPopup> _popups = new();
    private RectTransform _rect;
    private Vector2 _basePosition;
    private Vector3 _baseScale = Vector3.one;
    private Color _normalTextColor = Color.white;
    private bool _isInitialized;
    private bool _hasValue;
    private int _target;
    private float _from;
    private float _displayed;
    private float _countTime = float.NaN;
    private float _deniedTime = float.NaN;
    private float _tickTimer;
    private ShopSfx _sfx;
    private bool _hasSearchedSfx;

    private void Awake()
    {
        Initialize();
    }

    private void OnDisable()
    {
        _countTime = float.NaN;
        _deniedTime = float.NaN;
        _displayed = _target;
        ClearPopups();
        ApplyVisual();
    }

    /// <summary>연출 없이 바로 표시한다. (상점을 열 때)</summary>
    public void SetImmediate(int amount)
    {
        Initialize();
        _hasValue = true;
        _target = amount;
        _displayed = amount;
        _countTime = float.NaN;
        ApplyVisual();
    }

    /// <summary>골드가 바뀌었을 때. 창이 켜져 있으면 굴러가듯 바뀐다.</summary>
    public void SetAmount(int amount)
    {
        Initialize();
        if (!_hasValue || !isActiveAndEnabled)
        {
            SetImmediate(amount);
            return;
        }

        if (amount == _target)
        {
            return;
        }

        SpawnDelta(amount - _target);
        _from = _displayed;
        _target = amount;
        _countTime = 0f;
        // 구매 소리와 겹치지 않게 한 박자 뒤부터 똑딱인다.
        _tickTimer = _tickInterval;
    }

    /// <summary>골드가 부족할 때</summary>
    public void PlayDenied()
    {
        if (isActiveAndEnabled)
        {
            _deniedTime = 0f;
        }
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        if (!float.IsNaN(_countTime))
        {
            _countTime += deltaTime;
            float t = _countTime / _countDuration;
            _displayed = Mathf.Lerp(_from, _target, ShopEase.OutCubic(t));
            UpdateTick(deltaTime);
            if (t >= 1f)
            {
                _countTime = float.NaN;
                _displayed = _target;
            }
        }

        if (!float.IsNaN(_deniedTime))
        {
            _deniedTime += deltaTime;
            if (_deniedTime >= _deniedDuration)
            {
                _deniedTime = float.NaN;
            }
        }

        UpdatePopups(deltaTime);
        ApplyVisual();
    }

    /// <summary>골드가 줄어드는 동안 일정한 간격으로 똑딱인다.</summary>
    private void UpdateTick(float deltaTime)
    {
        if (_target >= _from)
        {
            return;
        }

        _tickTimer -= deltaTime;
        if (_tickTimer > 0f)
        {
            return;
        }

        _tickTimer += _tickInterval;
        if (!_hasSearchedSfx)
        {
            _hasSearchedSfx = true;
            _sfx = GetComponentInParent<ShopSfx>(true);
        }

        if (_sfx != null)
        {
            _sfx.PlayGoldTick();
        }
    }

    private void ApplyVisual()
    {
        if (_goldView != null)
        {
            _goldView.SetAmount(Mathf.RoundToInt(_displayed));
        }

        if (_rect == null)
        {
            return;
        }

        float punch = float.IsNaN(_countTime) ? 0f : _changePunch * ShopEase.Bump(_countTime / _countDuration * 1.5f);
        Vector2 shake = Vector2.zero;
        Color textColor = _normalTextColor;
        if (!float.IsNaN(_deniedTime))
        {
            float t = _deniedTime / _deniedDuration;
            shake.x = Mathf.Sin(t * Mathf.PI * 7f) * _deniedShake * (1f - t);
            textColor = Color.Lerp(_deniedColor, _normalTextColor, ShopEase.InCubic(t));
        }

        _rect.anchoredPosition = _basePosition + shake;
        _rect.localScale = _baseScale * (1f + punch);
        if (_amountText != null)
        {
            _amountText.color = textColor;
        }
    }

    private void SpawnDelta(int delta)
    {
        if (_amountText == null || delta == 0)
        {
            return;
        }

        TMP_Text text = Instantiate(_amountText, _amountText.transform.parent);
        text.name = "GoldDelta";
        text.raycastTarget = false;
        text.text = delta > 0 ? "+" + delta : delta.ToString();

        var popup = new DeltaPopup
        {
            Text = text,
            Start = _amountText.rectTransform.anchoredPosition + _deltaOffset,
            Color = delta > 0 ? _gainColor : _spendColor,
            Time = 0f,
        };

        _popups.Add(popup);
        UpdatePopup(popup);
    }

    private void UpdatePopups(float deltaTime)
    {
        for (int i = _popups.Count - 1; i >= 0; i--)
        {
            DeltaPopup popup = _popups[i];
            popup.Time += deltaTime;
            if (popup.Time >= _deltaDuration || popup.Text == null)
            {
                if (popup.Text != null)
                {
                    Destroy(popup.Text.gameObject);
                }

                _popups.RemoveAt(i);
                continue;
            }

            UpdatePopup(popup);
        }
    }

    private void UpdatePopup(DeltaPopup popup)
    {
        float t = popup.Time / _deltaDuration;
        popup.Text.rectTransform.anchoredPosition = popup.Start + new Vector2(0f, _deltaMove * ShopEase.OutCubic(t));
        popup.Text.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, ShopEase.OutCubic(t * 3f));

        Color color = popup.Color;
        color.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
        popup.Text.color = color;
    }

    private void ClearPopups()
    {
        foreach (DeltaPopup popup in _popups)
        {
            if (popup.Text != null)
            {
                Destroy(popup.Text.gameObject);
            }
        }

        _popups.Clear();
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        if (_goldView == null)
        {
            _goldView = GetComponent<GoldHudView>();
        }

        _rect = transform as RectTransform;
        if (_rect != null)
        {
            _basePosition = _rect.anchoredPosition;
            _baseScale = _rect.localScale;
        }

        if (_amountText != null)
        {
            _normalTextColor = _amountText.color;
        }
    }
}
