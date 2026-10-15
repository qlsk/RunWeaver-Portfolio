using UnityEngine;

/// <summary>
/// 체력 위험 연출 프리팹(Resources/LowHealth/LowHealthOverlay)에 붙는 값 보관용 컴포넌트.
/// 화면 가장자리 붉은빛과 혈관 무늬는 프리팹 Animator의 클립이 직접 바꾸고,
/// 체력바 밝기(BarGlow)는 HUD 쪽에 있어서 클립이 이 값만 움직이면 LowHealthOverlay가 체력바에 적용한다.
/// </summary>
[DisallowMultipleComponent]
public class LowHealthOverlayView : MonoBehaviour
{
    [Tooltip("체력바를 밝게 할 정도 (0~1). 애니메이션 클립이 움직인다")]
    [SerializeField] private float _barGlow;

    public float BarGlow => _barGlow;
}
