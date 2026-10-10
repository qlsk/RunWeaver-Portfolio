using TMPro;
using UnityEngine;

/// <summary>
/// 보고서의 기록 한 줄. "조사 시간 ........ 12:47"
/// 이름은 왼쪽, 값은 오른쪽에 붙고, 사이의 점선(Tiled 이미지)은 두 글자 길이에 맞춰 늘어난다.
/// </summary>
[DisallowMultipleComponent]
public class EndingStatRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text _labelText;
    [SerializeField] private TMP_Text _valueText;
    [Tooltip("이름과 값 사이의 점선. 가로로 늘어나는(stretch) RectTransform이어야 한다")]
    [SerializeField] private RectTransform _leader;
    [SerializeField, Min(0f)] private float _leaderGap = 14f;

    private Color _normalValueColor = Color.black;
    private bool _isInitialized;

    /// <summary>프리팹에 적힌 처음 이름</summary>
    public string DefaultLabel { get; private set; } = string.Empty;

    /// <summary>이름을 바꾼다. (임무 실패 때 "오염도"처럼 줄의 뜻이 바뀔 때)</summary>
    public void SetLabel(string label)
    {
        Initialize();
        if (_labelText != null)
        {
            _labelText.text = label;
        }

        LayoutLeader();
    }

    public void SetValue(string value)
    {
        Initialize();
        SetValue(value, _normalValueColor);
    }

    public void SetValue(string value, Color color)
    {
        Initialize();
        if (_valueText != null)
        {
            _valueText.text = value;
            _valueText.color = color;
        }

        LayoutLeader();
    }

    private void LayoutLeader()
    {
        if (_leader == null)
        {
            return;
        }

        float labelWidth = _labelText != null ? _labelText.GetPreferredValues(_labelText.text).x : 0f;
        float valueWidth = _valueText != null ? _valueText.GetPreferredValues(_valueText.text).x : 0f;
        _leader.offsetMin = new Vector2(labelWidth + _leaderGap, _leader.offsetMin.y);
        _leader.offsetMax = new Vector2(-(valueWidth + _leaderGap), _leader.offsetMax.y);
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        if (_valueText != null)
        {
            _normalValueColor = _valueText.color;
        }

        if (_labelText != null)
        {
            DefaultLabel = _labelText.text;
        }
    }
}
