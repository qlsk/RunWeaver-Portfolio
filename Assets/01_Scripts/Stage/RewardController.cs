using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 임무 클리어 보상 흐름 조율. 상태: Idle → WaitingPortal → UIOpen → Done.
///
/// ```
/// OnMissionCompleted → 클리어 메시지(기존 MissionResultPresenter) → 포탈 생성 + 보상 UI 자동 오픈
///   → 선택 + 확정 → 보상 지급 → UI 닫힘, 게임 재개 → 포탈로 다음 스테이지
/// ```
///
/// - MissionBase는 수정하지 않고 OnMissionCompleted 이벤트를 구독한다.
/// - UI 오픈 시점은 포탈 생성 시점(PortalSpawner.OnPortalsSpawned 구독).
/// - 포탈 생성과 동시에 UI가 열리고, 확정 전까지 Time.timeScale = 0이라 포탈 진입이 불가하다.
/// - 선택 없이 닫을 수 없다. Done 이후 같은 스테이지에서 다시 열리지 않는다.
/// - 사망 처리가 우선: 플레이어가 죽은 상태면 UI를 열지 않는다 (읽기 전용 확인).
/// - 씬 전환/파괴 시 timeScale 복구를 보장한다.
/// </summary>
[DisallowMultipleComponent]
public class RewardController : MonoBehaviour
{
    private enum RewardState
    {
        Idle,
        WaitingPortal,
        UIOpen,
        Done
    }

    [Header("연결 (비워두면 씬에서 자동 탐색)")]
    [SerializeField, FormerlySerializedAs("generator")] private RewardGenerator _generator;
    [SerializeField, FormerlySerializedAs("rewardUI")] private RewardSelectUI _rewardUI;
    [SerializeField, FormerlySerializedAs("dropper")] private RuneStoneDropper _dropper;
    [SerializeField, FormerlySerializedAs("passives")] private PassiveStatManager _passives;

    private MissionManager _missionManager;
    private PortalSpawner _portalSpawner;
    private RewardState _state = RewardState.Idle;
    private bool _pausedByReward;

    private void Start()
    {
        if (_generator == null)
        {
            _generator = FindAnyObjectByType<RewardGenerator>();
        }

        if (_rewardUI == null)
        {
            _rewardUI = FindAnyObjectByType<RewardSelectUI>(FindObjectsInactive.Include);
        }

        if (_dropper == null)
        {
            _dropper = FindAnyObjectByType<RuneStoneDropper>();
        }

        if (_passives == null)
        {
            _passives = PassiveStatManager.Instance;
        }

        _missionManager = MissionManager.Instance;
        if (_missionManager != null)
        {
            _missionManager.OnMissionCompleted -= HandleMissionCompleted;
            _missionManager.OnMissionCompleted += HandleMissionCompleted;
            _missionManager.OnMissionInitialized -= HandleMissionInitialized;
            _missionManager.OnMissionInitialized += HandleMissionInitialized;
        }
        else
        {
            Debug.LogWarning("[RewardController] MissionManager를 찾지 못했습니다.", this);
        }

        _portalSpawner = FindAnyObjectByType<PortalSpawner>();
        if (_portalSpawner != null)
        {
            _portalSpawner.OnPortalsSpawned -= HandlePortalsSpawned;
            _portalSpawner.OnPortalsSpawned += HandlePortalsSpawned;
        }
        else
        {
            Debug.LogWarning("[RewardController] PortalSpawner를 찾지 못했습니다.", this);
        }

        if (_rewardUI != null)
        {
            _rewardUI.OnOptionConfirmed -= HandleOptionConfirmed;
            _rewardUI.OnOptionConfirmed += HandleOptionConfirmed;
        }
        else
        {
            Debug.LogWarning("[RewardController] RewardSelectUI를 찾지 못했습니다.", this);
        }
    }

    private void OnDestroy()
    {
        if (_missionManager != null)
        {
            _missionManager.OnMissionCompleted -= HandleMissionCompleted;
            _missionManager.OnMissionInitialized -= HandleMissionInitialized;
        }

        if (_portalSpawner != null)
        {
            _portalSpawner.OnPortalsSpawned -= HandlePortalsSpawned;
        }

        if (_rewardUI != null)
        {
            _rewardUI.OnOptionConfirmed -= HandleOptionConfirmed;
        }

        RestoreTimeScale();
    }

    private void HandleMissionCompleted(MissionBase mission)
    {
        // Done 이후 중복 방지 + 다음 스테이지 전까지 대기 상태로만 전환한다.
        if (_state != RewardState.Idle)
        {
            return;
        }

        _state = RewardState.WaitingPortal;
    }

    private void HandlePortalsSpawned()
    {
        // 포탈 생성 시점에 UI 오픈. 클리어 메시지는 기존 MissionResultPresenter가 먼저 표시한다.
        if (_state != RewardState.WaitingPortal)
        {
            return;
        }

        if (IsPlayerDead())
        {
            // 사망 처리가 우선이며 보상 UI는 열지 않는다.
            _state = RewardState.Done;
            return;
        }

        if (_generator == null || _rewardUI == null)
        {
            Debug.LogWarning("[RewardController] Generator/UI가 없어 보상 선택을 건너뜁니다.", this);
            _state = RewardState.Done;
            return;
        }

        int depth = StageManager.Instance != null ? StageManager.Instance.CurrentDepth : 1;
        _rewardUI.Open(_generator.GenerateOptions(depth));

        _state = RewardState.UIOpen;
        PauseGame();
    }

    private void HandleOptionConfirmed(RewardOption option)
    {
        // 확정 즉시 Done으로 전환해 중복 오픈을 막는다.
        _state = RewardState.Done;

        ApplyReward(option);

        if (_rewardUI != null)
        {
            _rewardUI.Close();
        }

        RestoreTimeScale();
    }

    private void HandleMissionInitialized(MissionBase mission)
    {
        // 새 층이 시작되면 다음 보상을 받을 수 있게 리셋한다.
        _state = RewardState.Idle;

        if (_rewardUI != null && _rewardUI.IsOpen)
        {
            _rewardUI.Close();
        }

        RestoreTimeScale();
    }

    private void ApplyReward(RewardOption option)
    {
        if (option == null)
        {
            return;
        }

        if (option.type == RewardType.RuneStone)
        {
            if (_dropper != null)
            {
                _dropper.Scatter(Mathf.Max(1, option.runeStoneCount));
            }
            else
            {
                Debug.LogWarning("[RewardController] RuneStoneDropper가 없어 룬석을 지급하지 못했습니다.", this);
            }
        }
        else
        {
            if (_passives != null && option.passive != null)
            {
                // 선택 확정 이후에만 스택이 반영된다.
                _passives.AddStack(option.passive.statType, option.passive.valuePerStack);
            }
            else
            {
                Debug.LogWarning("[RewardController] PassiveStatManager/데이터가 없어 패시브를 지급하지 못했습니다.", this);
            }
        }
    }

    private bool IsPlayerDead()
    {
        PlayerHealth health = FindAnyObjectByType<PlayerHealth>();
        return health != null && health.IsDead;
    }

    private void PauseGame()
    {
        if (_pausedByReward)
        {
            return;
        }

        _pausedByReward = true;
        Time.timeScale = 0f;
    }

    private void RestoreTimeScale()
    {
        if (!_pausedByReward)
        {
            return;
        }

        _pausedByReward = false;
        Time.timeScale = 1f;
    }
}
