using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스위치(트랙 + 손잡이) 이미지를 Toggle 상태에 맞춰 보여준다.
/// 켜짐 : 손잡이가 오른쪽, 트랙이 밝은 색 / 꺼짐 : 손잡이가 왼쪽, 트랙이 어두운 색
/// 상태 글자(켜짐 / 꺼짐)를 넣으면 같이 바꾼다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Toggle))]
public class SwitchToggleView : MonoBehaviour
{
    [SerializeField] private Image _track;
    [SerializeField] private RectTransform _knob;

    [Header("켜짐")]
    [SerializeField] private float _onKnobX = 26f;
    [SerializeField] private Color _onTrackColor = Color.white;

    [Header("꺼짐")]
    [SerializeField] private float _offKnobX = -26f;
    [SerializeField] private Color _offTrackColor = new(0.17f, 0.17f, 0.18f, 1f);

    [Header("상태 글자 (선택)")]
    [SerializeField] private TMP_Text _stateText;
    [SerializeField] private string _onText = "켜짐";
    [SerializeField] private string _offText = "꺼짐";
    [SerializeField] private Color _onTextColor = new(0.925f, 0.902f, 0.863f, 1f);
    [SerializeField] private Color _offTextColor = new(0.5f, 0.53f, 0.6f, 1f);

    private Toggle _toggle;

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
        _toggle.onValueChanged.AddListener(Refresh);
    }

    private void OnEnable()
    {
        Refresh(_toggle != null && _toggle.isOn);
    }

    private void LateUpdate()
    {
        // SetIsOnWithoutNotify로 바뀐 경우에도 모습을 맞춘다.
        if (_toggle != null)
        {
            Refresh(_toggle.isOn);
        }
    }

    private void OnDestroy()
    {
        if (_toggle != null)
        {
            _toggle.onValueChanged.RemoveListener(Refresh);
        }
    }

    private void Refresh(bool isOn)
    {
        if (_knob != null)
        {
            Vector2 position = _knob.anchoredPosition;
            position.x = isOn ? _onKnobX : _offKnobX;
            _knob.anchoredPosition = position;
        }

        if (_track != null)
        {
            _track.color = isOn ? _onTrackColor : _offTrackColor;
        }

        if (_stateText != null)
        {
            _stateText.text = isOn ? _onText : _offText;
            _stateText.color = isOn ? _onTextColor : _offTextColor;
        }
    }
}
