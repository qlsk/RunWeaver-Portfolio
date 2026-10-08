using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 미션 2: 영혼 수집 (지정된 특정 구역 내에서 사망한 적에게서만 영혼 추출 및 수집)
/// </summary>
public class SoulCollectMission : MissionBase
{
    [Header("수집 목표")]
    [Tooltip("모아야 하는 영혼의 수")]
    [SerializeField, FormerlySerializedAs("targetSoulCount")] private int _targetSoulCount = 5;
    private int _currentSoulCount = 0;

    public int CurrentSoulCount => _currentSoulCount;
    public int TargetSoulCount => _targetSoulCount;

    [Header("영혼 수집 구역 (비워두면 씬의 SoulHarvestZone 자동 탐색)")]
    [SerializeField, FormerlySerializedAs("harvestZone")] private SoulHarvestZone _harvestZone;

    private readonly List<SoulHarvestZone> _harvestZones = new();

    /// <summary>
    /// MissionManager가 직전에 생성한 구역들을 직접 연결합니다.
    /// (Initialize 이전에 호출되어야 하며, 연결되면 자동 탐색을 건너뜁니다.)
    /// </summary>
    public void SetHarvestZones(IReadOnlyList<SoulHarvestZone> zones)
    {
        _harvestZones.Clear();
        if (zones != null)
        {
            foreach (SoulHarvestZone zone in zones)
            {
                if (zone != null)
                {
                    _harvestZones.Add(zone);
                }
            }
        }

        _harvestZone = _harvestZones.Count > 0 ? _harvestZones[0] : null;
    }

    /// <summary>
    /// MissionManager가 직전에 생성한 구역을 직접 연결합니다.
    /// (Initialize 이전에 호출되어야 하며, 연결되면 자동 탐색을 건너뜁니다.)
    /// </summary>
    public void SetHarvestZone(SoulHarvestZone zone)
    {
        _harvestZone = zone;
    }

    public override void Initialize()
    {
        _currentSoulCount = 0;
        IsCompleted = false;
        IsFailed = false;

        // 구역 자동 탐색 (직접 연결되지 않은 경우에만, 씬의 전 구역 사용)
        if (_harvestZones.Count == 0)
        {
            if (_harvestZone == null)
            {
                _harvestZone = FindAnyObjectByType<SoulHarvestZone>();
            }

            _harvestZones.Clear();
            if (_harvestZone != null)
            {
                _harvestZones.Add(_harvestZone);
            }

            SoulHarvestZone[] sceneZones = FindObjectsByType<SoulHarvestZone>(FindObjectsSortMode.None);
            foreach (SoulHarvestZone zone in sceneZones)
            {
                // 컨테이너 표시자(자체 콜라이더 없음)는 구역이 아니라 제외한다.
                if (zone != null && zone.GetComponent<Collider2D>() != null && !_harvestZones.Contains(zone))
                {
                    _harvestZones.Add(zone);
                }
            }
        }

        if (_harvestZones.Count == 0)
        {
            Debug.LogWarning("[SoulCollectMission] 씬에 SoulHarvestZone 오브젝트가 없습니다! 영혼 수집 구역을 생성해 주세요.");
        }
        else
        {
            Debug.Log($"[SoulCollectMission] 영혼 수집 구역 연결 완료: {_harvestZones.Count}개");
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

        // 구역 내 사망 여부 판정 (3개 중 하나라도 안이면 수집)
        if (_harvestZones.Count > 0)
        {
            foreach (SoulHarvestZone zone in _harvestZones)
            {
                if (zone != null && zone.IsInsideZone(deathPos))
                {
                    Debug.Log($"[SoulCollectMission] ★ 구역 내 처치 성공! 위치: {deathPos}");
                    AddSoul(1);
                    return;
                }
            }

            Debug.Log($"[SoulCollectMission] ✖ 적이 구역 밖에서 사망하여 영혼을 수집하지 못했습니다. (적 위치: {deathPos})");
        }
        else if (_harvestZone != null)
        {
            if (_harvestZone.IsInsideZone(deathPos))
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

        _currentSoulCount += amount;
        Debug.Log($"[SoulCollectMission] 영혼 수집 진행: {_currentSoulCount} / {_targetSoulCount}");

        NotifyProgressUpdated();

        if (_currentSoulCount >= _targetSoulCount)
        {
            CompleteMission();
        }
    }

    public override float GetProgressRatio()
    {
        if (_targetSoulCount <= 0) return 1f;
        return Mathf.Clamp01((float)_currentSoulCount / _targetSoulCount);
    }

    public override string GetProgressText()
    {
        return $"영혼 수집 (구역 내): {_currentSoulCount} / {_targetSoulCount}";
    }
}
