using UnityEngine;

/// <summary>
/// 미션 성공 패널의 등장 연출. 패널이 켜질 때(OnEnable)마다 애니메이션 클립(MissionClear_Show)을 처음부터 재생한다.
/// - 제목이 떠오르며 나타나 한 번 번쩍이고, 부제가 뒤따라 떠오르며, 글자 주변에서 불씨(Embers 자식 이미지)가 피어오른다.
///   모습은 모두 클립에 들어 있다.
/// 패널 전체의 페이드 인/아웃은 FadePanelView가 맡는다.
/// 애니메이터는 unscaled 시간으로 돌고, 일시정지 메뉴가 열려 있는 동안에는 멈춘다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class MissionClearEffect : MonoBehaviour
{
    private static readonly int s_showHash = Animator.StringToHash("Show");

    [SerializeField] private Animator _animator;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
    }

    private void OnEnable()
    {
        _animator.speed = 1f;
        _animator.Play(s_showHash, 0, 0f);
    }

    private void Update()
    {
        _animator.speed = PauseController.IsGamePaused ? 0f : 1f;
    }
}
