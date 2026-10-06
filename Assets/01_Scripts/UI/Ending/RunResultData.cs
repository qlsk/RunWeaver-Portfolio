/// <summary>
/// 한 판의 결과. 게임 쪽(GameRunResultSource 등)이 판이 끝날 때 채워서 결과창에 넘긴다.
/// 장착 룬과 남은 룬석은 룬북 데이터 소스에서 직접 읽는다.
/// (연구 자원은 연구 기획 폐기로 제거)
/// </summary>
public sealed class RunResultData
{
    public float SurvivalSeconds { get; }
    public int MaxDepth { get; }
    public long TotalDamage { get; }
    public int KillCount { get; }
    public RunEndReason EndReason { get; }

    public RunResultData(float survivalSeconds, int maxDepth, long totalDamage, int killCount, RunEndReason endReason)
    {
        SurvivalSeconds = survivalSeconds;
        MaxDepth = maxDepth;
        TotalDamage = totalDamage;
        KillCount = killCount;
        EndReason = endReason;
    }
}
