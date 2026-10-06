using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 실제 게임 데이터로 결과창(EndingUI) 값을 만든다.
/// - 생존 시간: 이 컴포넌트가 시작된 뒤 플레이어가 죽을 때까지 (결과창을 연 시점까지)
/// - 최고 심도: GameStageStatusSource가 기록한 값
/// - 처치 수, 총 피해량: EnemyDeathTracker
/// - 종료 사유: 플레이어가 죽었으면 Died, 시간 초과를 받았으면 TimeOver
/// - 연구 자원: 원정 자원(ExpeditionResources)
/// </summary>
public class GameRunResultSource : RunResultSource
{
    private static readonly string[] s_researchResourceIds = { "prima_materia", "azoth", "mercury", "aether_crystal" };

    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private GameStageStatusSource _stageSource;
    [SerializeField] private EnemyDeathTracker _deathTracker;
    [Tooltip("연구 자원 아이콘을 찾을 자원 정의 (원초 물질, 아조트, 수은, 에테르 결정)")]
    [SerializeField] private ResourceDefinitionSO[] _resourceDefinitions;

    private float _startTime;
    private float _endTime = -1f;
    private bool _isTimeOver;

    private void Start()
    {
        _startTime = Time.time;

        if (_playerHealth != null)
        {
            _playerHealth.Died += HandlePlayerDied;
        }

        if (_stageSource != null)
        {
            _stageSource.TimeExpired += HandleTimeExpired;
        }
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.Died -= HandlePlayerDied;
        }

        if (_stageSource != null)
        {
            _stageSource.TimeExpired -= HandleTimeExpired;
        }
    }

    public override RunResultData GetResult()
    {
        float endTime = _endTime >= 0f ? _endTime : Time.time;
        int maxDepth = _stageSource != null ? Mathf.Max(1, _stageSource.MaxDepth) : 1;
        int killCount = _deathTracker != null ? _deathTracker.KillCount : 0;
        int totalDamage = _deathTracker != null ? Mathf.RoundToInt(_deathTracker.TotalDamage) : 0;

        bool isDead = _playerHealth != null && _playerHealth.IsDead;
        RunEndReason endReason = _isTimeOver && !isDead ? RunEndReason.TimeOver : RunEndReason.Died;

        return new RunResultData(endTime - _startTime, maxDepth, totalDamage, killCount, endReason, GetResearchResources());
    }

    private void HandlePlayerDied()
    {
        StopClock();
    }

    private void HandleTimeExpired()
    {
        _isTimeOver = true;
        StopClock();
    }

    private void StopClock()
    {
        if (_endTime < 0f)
        {
            _endTime = Time.time;
        }
    }

    private IReadOnlyList<ResourceAmountData> GetResearchResources()
    {
        List<ResourceAmountData> resources = new(s_researchResourceIds.Length);
        ResourceInventory inventory = GameSession.Instance != null && GameSession.Instance.IsReady
            ? GameSession.Instance.Current?.Holdings?.ExpeditionResources
            : null;

        IReadOnlyDictionary<string, int> snapshot = inventory?.GetSnapshot();
        foreach (string resourceId in s_researchResourceIds)
        {
            int amount = 0;
            snapshot?.TryGetValue(resourceId, out amount);
            resources.Add(new ResourceAmountData(resourceId, FindIcon(resourceId), amount));
        }

        return resources;
    }

    private Sprite FindIcon(string resourceId)
    {
        if (_resourceDefinitions == null)
        {
            return null;
        }

        foreach (ResourceDefinitionSO definition in _resourceDefinitions)
        {
            if (definition != null && definition.Id == resourceId)
            {
                return definition.Icon;
            }
        }

        return null;
    }
}
