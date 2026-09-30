using System;
using System.Collections.Generic;

/// <summary>
/// 한 판의 결과. 게임 쪽(GameManager 등)이 판이 끝날 때 채워서 결과창에 넘긴다.
/// 장착 룬과 남은 룬석은 룬북 데이터 소스에서 직접 읽는다.
/// </summary>
public sealed class RunResultData
{
    public float SurvivalSeconds { get; }
    public int MaxDepth { get; }
    public long TotalDamage { get; }
    public int KillCount { get; }
    public RunEndReason EndReason { get; }
    public IReadOnlyList<ResourceAmountData> ResearchResources { get; }

    public RunResultData(float survivalSeconds, int maxDepth, long totalDamage, int killCount,
        RunEndReason endReason, IReadOnlyList<ResourceAmountData> researchResources)
    {
        SurvivalSeconds = survivalSeconds;
        MaxDepth = maxDepth;
        TotalDamage = totalDamage;
        KillCount = killCount;
        EndReason = endReason;
        ResearchResources = researchResources ?? Array.Empty<ResourceAmountData>();
    }
}
