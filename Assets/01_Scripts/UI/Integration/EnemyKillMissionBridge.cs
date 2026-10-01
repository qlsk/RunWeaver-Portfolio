using UnityEngine;

/// <summary>
/// EnemyHealth 적이 죽으면 현재 처치 미션(EnemyKillMission)에 처치 수를 전달한다.
/// 처치 미션은 DummyEnemy의 사망만 세고 있어, 실제 전투 적(EnemyHealth)도 미션에 반영되도록 임시로 연결한다.
/// 적 시스템이 하나로 합쳐지면 제거한다.
/// </summary>
[DisallowMultipleComponent]
public class EnemyKillMissionBridge : MonoBehaviour
{
    [SerializeField] private EnemyDeathTracker _tracker;

    private void OnEnable()
    {
        if (_tracker == null)
        {
            _tracker = FindAnyObjectByType<EnemyDeathTracker>();
        }

        if (_tracker != null)
        {
            _tracker.EnemyKilled += HandleEnemyKilled;
        }
        else
        {
            Debug.LogWarning("[EnemyKillMissionBridge] EnemyDeathTracker가 없어 처치 미션에 반영하지 않습니다.", this);
        }
    }

    private void OnDisable()
    {
        if (_tracker != null)
        {
            _tracker.EnemyKilled -= HandleEnemyKilled;
        }
    }

    private void HandleEnemyKilled(EnemyHealth enemy)
    {
        // DummyEnemy가 함께 붙은 적은 미션이 이미 직접 세므로 중복으로 보내지 않는다.
        if (enemy != null && enemy.GetComponent<DummyEnemy>() != null)
        {
            return;
        }

        MissionManager missionManager = MissionManager.Instance;
        if (missionManager != null && missionManager.GetCurrentMission() is EnemyKillMission killMission)
        {
            killMission.AddKillCount(false);
        }
    }
}
