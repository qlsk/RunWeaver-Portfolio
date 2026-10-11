using UnityEngine;

/// <summary>
/// 제단의 남은 체력 비율에 맞춰 Animator의 Stage 값을 바꾼다. 스프라이트는 애니메이션 컨트롤러가 바꾼다.
/// Stage 1 : 온전 / 2 : 금이 감 / 3 : 크게 부서짐 / 4 : 파괴
/// 제단은 체력이 0이 되면 바로 사라지므로(DefenseAltar.Die), 파괴 단계는 사라지기 직전의 한 프레임에만 설정된다.
/// </summary>
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class DefenseAltarStage : MonoBehaviour
{
    private static readonly int StageHash = Animator.StringToHash("Stage");

    [SerializeField] private DefenseAltar _altar;
    [Tooltip("남은 체력 비율이 이 값 미만이면 2단계(금이 감)")]
    [SerializeField, Range(0f, 1f)] private float _crackedBelow = 0.66f;
    [Tooltip("남은 체력 비율이 이 값 미만이면 3단계(크게 부서짐)")]
    [SerializeField, Range(0f, 1f)] private float _brokenBelow = 0.33f;

    private Animator _animator;
    private int _shownStage;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_altar == null)
        {
            _altar = GetComponent<DefenseAltar>();
        }
    }

    private void Update()
    {
        if (_altar == null)
        {
            return;
        }

        int stage = GetStage();
        if (stage == _shownStage)
        {
            return;
        }

        _shownStage = stage;
        _animator.SetInteger(StageHash, stage);
    }

    private int GetStage()
    {
        if (_altar.IsDestroyed)
        {
            return 4;
        }

        float ratio = _altar.MaxHp > 0f ? _altar.CurrentHp / _altar.MaxHp : 1f;
        if (ratio < _brokenBelow)
        {
            return 3;
        }

        return ratio < _crackedBelow ? 2 : 1;
    }
}
