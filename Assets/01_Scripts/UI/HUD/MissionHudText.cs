using UnityEngine;

/// <summary>
/// 미션 진행 문구를 영문으로 만든다.
/// 미션의 GetProgressText()는 한글이라 TMP 폰트로 표시할 수 없고, 처치 수 등 세부 숫자는 공개되어 있지 않아
/// 공개된 값(진행률, 생존 미션의 남은 시간)으로 문구를 만든다. 미션 쪽에 영문 문구나 숫자가 공개되면 교체한다.
/// </summary>
public static class MissionHudText
{
    public static string GetProgressText(MissionBase mission)
    {
        if (mission == null)
        {
            return string.Empty;
        }

        if (mission.IsFailed)
        {
            return "Mission failed";
        }

        if (mission.IsCompleted)
        {
            return "Mission complete!";
        }

        if (mission is SurvivalMission survival)
        {
            return $"Time left: {Mathf.CeilToInt(survival.Remaining)}s";
        }

        int percent = Mathf.RoundToInt(Mathf.Clamp01(mission.GetProgressRatio()) * 100f);
        return mission switch
        {
            EnemyKillMission => $"Enemies defeated: {percent}%",
            SoulCollectMission => $"Souls collected: {percent}%",
            AltarDefenseMission => $"Defense progress: {percent}%",
            CaptureZoneMission => $"Capture progress: {percent}%",
            _ => $"Progress: {percent}%"
        };
    }
}
