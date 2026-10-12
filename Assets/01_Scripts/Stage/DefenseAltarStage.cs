using UnityEngine;

/// <summary>
/// 제단의 남은 체력 비율에 맞춰 Animator의 Stage 값을 바꾼다. 스프라이트와 연출은 애니메이션 컨트롤러가 바꾼다.
/// Stage 1 : 온전 / 2 : 금이 감 / 3 : 크게 부서짐 / 4 : 파괴
/// 제단은 체력이 0이 되면 바로 사라지므로(DefenseAltar.Die), 파괴 단계는 사라지기 직전의 한 프레임에만 설정된다.
/// 연출용 트리거도 건다. (연출 자체는 컨트롤러의 클립이 한다)
/// - "Hit" : 체력이 줄었을 때 (하얗게 번쩍임, 흔들림, 작은 파편)
/// - "Crack" : 금이 가는 단계(2, 3)로 넘어갈 때 (큰 파편, 빛 일렁임)
/// 체력이 다해 사라질 때는 그 자리에 파편 프리팹(_destroyedFxPrefab)을 남긴다.
/// </summary>
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class DefenseAltarStage : MonoBehaviour
{
    private static readonly int s_stageHash = Animator.StringToHash("Stage");
    private static readonly int s_hitHash = Animator.StringToHash("Hit");
    private static readonly int s_crackHash = Animator.StringToHash("Crack");

    [SerializeField] private DefenseAltar _altar;
    [Tooltip("남은 체력 비율이 이 값 미만이면 2단계(금이 감)")]
    [SerializeField, Range(0f, 1f)] private float _crackedBelow = 0.66f;
    [Tooltip("남은 체력 비율이 이 값 미만이면 3단계(크게 부서짐)")]
    [SerializeField, Range(0f, 1f)] private float _brokenBelow = 0.33f;
    [Tooltip("제단이 파괴될 때 그 자리에 남기는 파편 프리팹 (AltarDebris)")]
    [SerializeField] private GameObject _destroyedFxPrefab;

    private Animator _animator;
    private int _shownStage;
    private float _lastHp = -1f;

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

        float hp = _altar.CurrentHp;
        if (_lastHp >= 0f && hp < _lastHp - 0.001f)
        {
            _animator.SetTrigger(s_hitHash);
        }

        _lastHp = hp;

        int stage = GetStage();
        if (stage == _shownStage)
        {
            return;
        }

        if (_shownStage > 0 && stage > _shownStage && stage <= 3)
        {
            _animator.SetTrigger(s_crackHash);
        }

        _shownStage = stage;
        _animator.SetInteger(s_stageHash, stage);
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

    private void OnDestroy()
    {
        // 체력이 다해 사라질 때만 파편을 남긴다. (씬을 닫을 때는 남기지 않는다)
        if (_destroyedFxPrefab != null && _altar != null && _altar.IsDestroyed && gameObject.scene.isLoaded)
        {
            Instantiate(_destroyedFxPrefab, transform.position, Quaternion.identity);
        }
    }
}
