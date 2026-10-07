using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 임무 클리어 보상 흐름 조율. StageManager의 공식 확장점(IMissionRewardProvider) 구현.
///
/// ```
/// 미션 클리어 → HandleMissionCleared → Grant(depth, onFinished) → 보상 UI 오픈
///   → 선택 + 확정 → 보상 지급 → UI 닫힘, 게임 재개 → onFinished() → 포탈 생성
/// ```
///
/// - MissionBase는 수정하지 않는다. StageManager가 포탈 전에 Grant를 부른다.
/// - 확정 전까지 Time.timeScale = 0. 선택 없이 닫을 수 없다.
/// - Done 이후 같은 스테이지에서 다시 열리지 않는다. 새 층(OnMissionInitialized)에서 리셋.
/// - 사망 시에는 UI를 열지 않고 즉시 onFinished를 불러 포탈 흐름을 막지 않는다.
/// - 런 종료(RunEnded) 중 UI가 열려 있으면 닫고 timeScale을 복구한다.
/// - 씬 전환/파괴 시 timeScale 복구를 보장한다.
/// </summary>
[DisallowMultipleComponent]
public class RewardController : MonoBehaviour, IMissionRewardProvider
{
    private enum RewardState
    {
        Idle,
        UIOpen,
        Done
    }

    [Header("연결 (비워두면 씬에서 자동 탐색)")]
    [SerializeField, FormerlySerializedAs("generator")] private RewardGenerator _generator;
    [SerializeField, FormerlySerializedAs("rewardUI")] private RewardSelectUI _rewardUI;
    [SerializeField, FormerlySerializedAs("dropper")] private RuneStoneDropper _dropper;
    [SerializeField, FormerlySerializedAs("passives")] private PassiveStatManager _passives;
    private TraitManager _traits;

    private MissionManager _missionManager;
    private StageManager _stageManager;
    private RewardState _state = RewardState.Idle;
    private Action _pendingFinish;
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

        _traits = TraitManager.Instance;

        // 특성 핸들러·합성기는 씬에 없어도 동작해야 하므로 여기서 보장한다.
        EnsureHandler<TraitStatComposer>();
        EnsureHandler<AttackTraitHandler>();
        EnsureHandler<DashTraitHandler>();
        EnsureHandler<ShieldTraitHandler>();
        EnsureHandler<CleanseTraitHandler>();

        _missionManager = MissionManager.Instance;
        if (_missionManager != null)
        {
            _missionManager.OnMissionInitialized -= HandleMissionInitialized;
            _missionManager.OnMissionInitialized += HandleMissionInitialized;
        }
        else
        {
            Debug.LogWarning("[RewardController] MissionManager를 찾지 못했습니다.", this);
        }

        _stageManager = StageManager.Instance;
        if (_stageManager != null)
        {
            _stageManager.RunEnded -= HandleRunEnded;
            _stageManager.RunEnded += HandleRunEnded;
            _stageManager.SetMissionRewardProvider(this);
        }
        else
        {
            Debug.LogWarning("[RewardController] StageManager를 찾지 못했습니다.", this);
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
            _missionManager.OnMissionInitialized -= HandleMissionInitialized;
        }

        if (_stageManager != null)
        {
            _stageManager.RunEnded -= HandleRunEnded;
            // 단일 컨트롤러 전제라 등록 해제한다.
            _stageManager.SetMissionRewardProvider(null);
        }

        if (_rewardUI != null)
        {
            _rewardUI.OnOptionConfirmed -= HandleOptionConfirmed;
        }

        RestoreTimeScale();
    }

    /// <summary>
    /// StageManager가 미션 클리어 후 포탈 전에 부른다.
    /// 보상 처리가 끝나면 onFinished를 불러야 포탈이 나온다.
    /// </summary>
    public void Grant(int depth, Action onFinished)
    {
        if (_state != RewardState.Idle)
        {
            onFinished?.Invoke();
            return;
        }

        if (IsPlayerDead())
        {
            // 사망 처리가 우선이며 보상 UI는 열지 않는다.
            _state = RewardState.Done;
            onFinished?.Invoke();
            return;
        }

        if (_generator == null || _rewardUI == null)
        {
            Debug.LogWarning("[RewardController] Generator/UI가 없어 보상 선택을 건너뜁니다.", this);
            _state = RewardState.Done;
            onFinished?.Invoke();
            return;
        }

        _pendingFinish = onFinished;
        _rewardUI.Open(_generator.GenerateOptions(Mathf.Max(1, depth)));

        _state = RewardState.UIOpen;
        PauseGame();
    }

    private void HandleOptionConfirmed(RewardOption option)
    {
        if (_state != RewardState.UIOpen)
        {
            return;
        }

        // 확정 즉시 Done으로 전환해 중복 오픈을 막는다.
        _state = RewardState.Done;

        ApplyReward(option);

        if (_rewardUI != null)
        {
            _rewardUI.Close();
        }

        RestoreTimeScale();
        FinishGrant();
    }

    private void HandleMissionInitialized(MissionBase mission)
    {
        // 새 층이 시작되면 다음 보상을 받을 수 있게 리셋한다.
        _state = RewardState.Idle;
        _pendingFinish = null;

        if (_rewardUI != null && _rewardUI.IsOpen)
        {
            _rewardUI.Close();
        }

        RestoreTimeScale();
    }

    private void HandleRunEnded(RunEndReason reason)
    {
        // 런이 끝나면 보상 대기/UI를 정리한다. 포탈은 Ended 단계라 나오지 않는다.
        _state = RewardState.Done;

        if (_rewardUI != null && _rewardUI.IsOpen)
        {
            _rewardUI.Close();
        }

        RestoreTimeScale();
        FinishGrant();
    }

    private void FinishGrant()
    {
        Action finish = _pendingFinish;
        _pendingFinish = null;
        finish?.Invoke();
    }

    private void EnsureHandler<T>() where T : Component
    {
        if (FindAnyObjectByType<T>() == null)
        {
            gameObject.AddComponent<T>();
            Debug.Log($"[RewardController] {typeof(T).Name} 자동 생성", this);
        }
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
        else if (option.type == RewardType.Trait)
        {
            if (_traits != null && option.trait != null)
            {
                // 선택 확정 이후에만 보유에 반영된다. shield_01은 1회성 최대치 증가라 즉시 적용.
                if (_traits.AcquireTrait(option.trait) && option.trait.id == "shield_01")
                {
                    Shield shield = FindAnyObjectByType<Shield>();
                    if (shield != null)
                    {
                        shield.ShieldMaxStackUp();
                    }
                }
            }
            else
            {
                Debug.LogWarning("[RewardController] TraitManager/데이터가 없어 특성을 지급하지 못했습니다.", this);
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
