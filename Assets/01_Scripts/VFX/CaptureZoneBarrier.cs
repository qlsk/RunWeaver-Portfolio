using UnityEngine;

/// <summary>
/// 거점 점령 링의 안/밖 표시를 Animator 값으로 넘긴다. 결계, 빛, 링 어둡게 하기는 컨트롤러(CaptureZone.controller)의 클립이 한다.
/// - "Presence" (0~1) : 플레이어가 링 안에 있는 정도. 안에 들어가면 천천히 오르고(결계가 솟는다), 나가면 더 빨리 내려간다.
///   임무가 진행 중일 때만 오른다. (시작 전과 끝난 뒤에는 결계가 없다)
/// - "Dim" (0~1) : 임무 중 링 밖에 있을 때 링을 어둡고 차갑게 하는 정도. 결계가 솟는 만큼 줄어든다.
/// 안/밖은 같은 오브젝트의 CaptureZone.IsPlayerInside로 읽는다. 게임 시간으로 움직인다.
/// </summary>
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class CaptureZoneBarrier : MonoBehaviour
{
    private static readonly int PresenceHash = Animator.StringToHash("Presence");
    private static readonly int DimHash = Animator.StringToHash("Dim");

    [Tooltip("결계가 바닥에서 다 솟아오르는 데 걸리는 시간")]
    [SerializeField, Min(0.01f)] private float _riseDuration = 1.2f;
    [Tooltip("결계가 가라앉는 데 걸리는 시간")]
    [SerializeField, Min(0.01f)] private float _fallDuration = 0.6f;
    [Tooltip("임무가 시작되거나 끝날 때 링이 어두워지거나 밝아지는 데 걸리는 시간")]
    [SerializeField, Min(0.01f)] private float _dimDuration = 0.5f;

    private Animator _animator;
    private CaptureZone _zone;
    private MissionManager _manager;
    private float _weight;
    private float _activeTint;

    /// <summary>안에 있는 정도 (0 : 밖, 1 : 안)</summary>
    public float Weight => _weight;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _zone = GetComponent<CaptureZone>();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        bool active = IsMissionActive();
        bool inside = active && _zone != null && _zone.IsPlayerInside;
        float target = inside ? 1f : 0f;
        _weight = Mathf.MoveTowards(_weight, target, dt / (target > _weight ? _riseDuration : _fallDuration));
        _activeTint = Mathf.MoveTowards(_activeTint, active ? 1f : 0f, dt / _dimDuration);

        float reveal = _weight * _weight * (3f - 2f * _weight);
        _animator.SetFloat(PresenceHash, _weight);
        _animator.SetFloat(DimHash, _activeTint * (1f - reveal));
    }

    /// <summary>거점 점령 임무가 시작됐고 아직 끝나지 않았는지</summary>
    private bool IsMissionActive()
    {
        if (_manager == null)
        {
            // Instance는 없으면 새로 만들기 때문에 씬에 있는 것만 찾는다.
            _manager = FindAnyObjectByType<MissionManager>();
            if (_manager == null)
            {
                return false;
            }
        }

        MissionBase mission = _manager.GetCurrentMission();
        return _manager.IsMissionRunning && mission != null && mission.Type == MissionType.CaptureZone &&
               !mission.IsCompleted && !mission.IsFailed;
    }
}
