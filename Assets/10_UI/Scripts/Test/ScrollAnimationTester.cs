using UnityEngine;

public class ScrollAnimationTester : MonoBehaviour
{
    [SerializeField] private Animator _scrollAnimator;

    // 유니티 Animator의 기본 상태가 펼쳐진 상태(RollUp_Down)이므로 true로 시작
    private bool _isRolledDown = true;

    private readonly string _rollUpTrigger = "RollUp";
    private readonly string _rollDownTrigger = "RollDown";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleScrollAnimation();
        }
    }

    private void ToggleScrollAnimation()
    {
        if (_scrollAnimator == null)
        {
            Debug.LogWarning("Scroll Animator 컴포넌트가 할당되지 않았습니다.");
            return;
        }

        if (_isRolledDown)
        {
            // 현재 펼쳐져 있으므로 말아올리는(RollUp) 트리거 실행
            _scrollAnimator.SetTrigger(_rollUpTrigger);
            Debug.Log("말아 올리는 애니메이션 재생");
        }
        else
        {
            // 현재 말려 있으므로 펼쳐 내리는(RollDown) 트리거 실행
            _scrollAnimator.SetTrigger(_rollDownTrigger);
            Debug.Log("펼쳐 내리는 애니메이션 재생");
        }

        // 상태 반전
        _isRolledDown = !_isRolledDown;
    }
}
