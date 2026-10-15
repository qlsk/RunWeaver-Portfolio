using UnityEngine;

/// <summary>
/// 미션 진행도에 맞춰 Animator의 Stage 값을 바꾼다. 스프라이트는 애니메이션 컨트롤러가 바꾼다.
/// 영혼 수집(영혼불 기둥), 거점 점령(점령 게이지 링)처럼 진행도에 따라 모양이 달라지는 미션 오브젝트에 붙인다.
/// - Stage = floor(진행도 * 최대 단계). 진행도 1이면 최대 단계다.
/// - 평소에는 미션이 보내는 진행도 갱신 이벤트를 받는다.
/// - 이미 진행 중이거나 끝난 미션에 연결될 때(늦은 연결, 다시 활성화)만 현재 진행도를 읽어 맞춘다.
///   미션 시작 전에는 읽지 않는다. (목표가 아직 0이라 진행도가 1로 읽힐 수 있다) 그때는 Animator의 기본 상태(0단계)를 유지한다.
/// </summary>
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class MissionStageAnimator : MonoBehaviour
{
    private static readonly int StageHash = Animator.StringToHash("Stage");

    [Tooltip("이 오브젝트가 진행도를 보여줄 미션 종류")]
    [SerializeField] private MissionType _missionType = MissionType.SoulCollect;
    [Tooltip("마지막 단계 번호. 진행도 0~1을 0~이 값으로 나눈다. (영혼불 5, 점령 게이지 8)")]
    [SerializeField, Min(1)] private int _maxStage = 5;

    private Animator _animator;
    private MissionManager _manager;
    private MissionBase _mission;
    private int _shownStage = -1;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        // 다시 활성화되면 Animator가 기본 상태로 돌아가므로 표시 단계를 다시 맞춘다.
        _shownStage = -1;

        // Instance는 없으면 새로 만들기 때문에 씬에 있는 것만 찾는다.
        _manager = FindAnyObjectByType<MissionManager>();
        if (_manager == null)
        {
            return;
        }

        _manager.OnMissionInitialized += HandleMissionInitialized;
        Bind(_manager.GetCurrentMission());
    }

    private void OnDisable()
    {
        if (_manager != null)
        {
            _manager.OnMissionInitialized -= HandleMissionInitialized;
        }

        Unbind();
    }

    private void HandleMissionInitialized(MissionBase mission)
    {
        Bind(mission);
    }

    private void Bind(MissionBase mission)
    {
        if (mission == null || mission.Type != _missionType)
        {
            return;
        }

        if (mission != _mission)
        {
            Unbind();
            _mission = mission;
            _mission.OnMissionProgressUpdated += HandleProgressUpdated;
        }

        SyncProgress();
    }

    private void Unbind()
    {
        if (_mission != null)
        {
            _mission.OnMissionProgressUpdated -= HandleProgressUpdated;
            _mission = null;
        }
    }

    /// <summary>진행 중이거나 끝난 미션이면 현재 진행도로 단계를 맞춘다.</summary>
    private void SyncProgress()
    {
        if (_mission == null || _manager == null)
        {
            return;
        }

        if (_manager.IsMissionRunning || _mission.IsCompleted)
        {
            ApplyProgress(_mission.GetProgressRatio());
        }
    }

    private void HandleProgressUpdated(float ratio, string text)
    {
        ApplyProgress(ratio);
    }

    private void ApplyProgress(float ratio)
    {
        if (float.IsNaN(ratio))
        {
            ratio = 0f;
        }

        int stage = Mathf.Clamp(Mathf.FloorToInt(ratio * _maxStage + 0.0001f), 0, _maxStage);
        if (stage == _shownStage)
        {
            return;
        }

        _shownStage = stage;
        _animator.SetInteger(StageHash, stage);
    }
}
