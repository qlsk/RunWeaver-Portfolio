using TMPro;
using UnityEngine;

/// <summary>
/// HUD 미션 두루마리. 미션 제목과 진행 내용을 표시하고, 토글 키로 말고 편다.
/// 두루마리가 말려 있을 때 진행 내용이 바뀌면 자동으로 펼쳐서 알려준다.
/// (기존 ScrollAnimationTester의 M키 토글 기능을 옮겨 왔다.)
/// </summary>
[DisallowMultipleComponent]
public class MissionScrollView : MonoBehaviour
{
    private static readonly int s_rollUpTrigger = Animator.StringToHash("RollUp");
    private static readonly int s_rollDownTrigger = Animator.StringToHash("RollDown");

    [Header("구성 요소")]
    [SerializeField] private Animator _scrollAnimator;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _progressText;

    [Header("동작")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.M;
    [SerializeField] private bool _isAutoExpandOnProgress = true;

    // Animator의 기본 상태(Idle)가 펼쳐진 상태이므로 true로 시작한다.
    private bool _isRolledDown = true;
    private string _shownTitle;
    private string _shownProgress;

    public bool IsRolledDown => _isRolledDown;

    private void Awake()
    {
        if (_scrollAnimator == null)
        {
            Debug.LogError("[MissionScrollView] Animator가 할당되지 않았습니다.", this);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            Toggle();
        }
    }

    /// <param name="allowAutoExpand">false면 내용이 바뀌어도 말린 두루마리를 펼치지 않는다. (남은 시간처럼 자주 바뀌는 값)</param>
    public void SetMission(string title, string progress, bool allowAutoExpand = true)
    {
        bool hasChanged = title != _shownTitle || progress != _shownProgress;
        _shownTitle = title;
        _shownProgress = progress;

        if (_titleText != null)
        {
            _titleText.text = title;
        }

        if (_progressText != null)
        {
            _progressText.text = progress;
        }

        if (hasChanged && allowAutoExpand && _isAutoExpandOnProgress && !_isRolledDown)
        {
            RollDown();
        }
    }

    public void Toggle()
    {
        if (_isRolledDown)
        {
            RollUp();
        }
        else
        {
            RollDown();
        }
    }

    public void RollUp()
    {
        if (!_isRolledDown || _scrollAnimator == null)
        {
            return;
        }

        _scrollAnimator.ResetTrigger(s_rollDownTrigger);
        _scrollAnimator.SetTrigger(s_rollUpTrigger);
        _isRolledDown = false;
    }

    public void RollDown()
    {
        if (_isRolledDown || _scrollAnimator == null)
        {
            return;
        }

        _scrollAnimator.ResetTrigger(s_rollUpTrigger);
        _scrollAnimator.SetTrigger(s_rollDownTrigger);
        _isRolledDown = true;
    }
}
