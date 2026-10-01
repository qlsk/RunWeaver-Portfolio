using UnityEngine;

/// <summary>
/// 미션 2: 영혼 수집 (지정된 특정 구역 내에서 사망한 적에게서만 영혼 추출 및 수집)
/// </summary>
public class SoulCollectMission : MissionBase
{
    [Header("수집 목표")]
    [Tooltip("모아야 하는 영혼의 수")]
    [SerializeField] private int targetSoulCount = 5;
    private int currentSoulCount = 0;

    [Header("영혼 수집 구역 (비워두면 씬의 SoulHarvestZone 자동 탐색)")]
    [SerializeField] private SoulHarvestZone harvestZone;

    /// <summary>
    /// MissionManager가 직전에 생성한 구역을 직접 연결합니다.
    /// (Initialize 이전에 호출되어야 하며, 연결되면 자동 탐색을 건너뜁니다.)
    /// </summary>
    public void SetHarvestZone(SoulHarvestZone zone)
    {
        harvestZone = zone;
    }

    public override void Initialize()
    {
        currentSoulCount = 0;
        IsCompleted = false;
        IsFailed = false;

        // 구역 자동 탐색 (직접 연결되지 않은 경우에만)
        if (harvestZone == null)
        {
            harvestZone = FindAnyObjectByType<SoulHarvestZone>();
        }

        if (harvestZone == null)
        {
            Debug.LogWarning("[SoulCollectMission] 씬에 SoulHarvestZone 오브젝트가 없습니다! 영혼 수집 구역을 생성해 주세요.");
        }
        else
        {
            Debug.Log($"[SoulCollectMission] 영혼 수집 구역 연결 완료: {harvestZone.name}");
        }

        // 정적 이벤트 중복 구독 방지
        EnemyHealth.OnDeath -= HandleEnemyDied;
        EnemyHealth.OnDeath += HandleEnemyDied;

        NotifyProgressUpdated();
    }

    private void OnDisable()
    {
        EnemyHealth.OnDeath -= HandleEnemyDied;
    }

    private void OnDestroy()
    {
        EnemyHealth.OnDeath -= HandleEnemyDied;
    }

    public override void UpdateMission(float deltaTime)
    {
    }

    private void HandleEnemyDied(EnemyHealth enemy)
    {
        if (IsCompleted || IsFailed) return;
        if (enemy == null) return;

        Vector2 deathPos = enemy.transform.position;

        // 구역 내 사망 여부 판정
        if (harvestZone != null)
        {
            if (harvestZone.IsInsideZone(deathPos))
            {
                Debug.Log($"[SoulCollectMission] ★ 구역 내 처치 성공! 위치: {deathPos}");
                AddSoul(1);
            }
            else
            {
                Debug.Log($"[SoulCollectMission] ✖ 적이 구역 밖에서 사망하여 영혼을 수집하지 못했습니다. (적 위치: {deathPos})");
            }
        }
        else
        {
            AddSoul(1);
        }
    }

    public void AddSoul(int amount = 1)
    {
        if (IsCompleted || IsFailed) return;

        currentSoulCount += amount;
        Debug.Log($"[SoulCollectMission] 영혼 수집 진행: {currentSoulCount} / {targetSoulCount}");

        NotifyProgressUpdated();

        if (currentSoulCount >= targetSoulCount)
        {
            CompleteMission();
        }
    }

    public override float GetProgressRatio()
    {
        if (targetSoulCount <= 0) return 1f;
        return Mathf.Clamp01((float)currentSoulCount / targetSoulCount);
    }

    public override string GetProgressText()
    {
        return $"영혼 수집 (구역 내): {currentSoulCount} / {targetSoulCount}";
    }
}