using UnityEngine;

/// <summary>
/// 미션 진행 문구를 영문으로 만든다.
/// 미션의 GetProgressText()는 한글이라 TMP 폰트로 표시할 수 없어, 미션이 공개한 숫자로 영문 문구를 만든다.
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

        switch (mission)
        {
            case EnemyKillMission kill:
                return kill.TargetEliteKillCount > 0
                    ? $"Enemies: {kill.CurrentNormalKillCount}/{kill.TargetNormalKillCount}  Elites: {kill.CurrentEliteKillCount}/{kill.TargetEliteKillCount}"
                    : $"Enemies defeated: {kill.CurrentNormalKillCount}/{kill.TargetNormalKillCount}";

            case SoulCollectMission soul:
                return $"Souls collected: {soul.CurrentSoulCount}/{soul.TargetSoulCount}";

            case SurvivalMission survival:
                return $"Survive: {GetSecondsLeft(survival.SurvivalDuration, survival.CurrentTimer)}s left";

            case AltarDefenseMission altar:
                return $"Defend the altar: {GetSecondsLeft(altar.DefenseDuration, altar.CurrentTimer)}s left";

            case CaptureZoneMission capture:
                int percent = Mathf.FloorToInt(Mathf.Clamp01(capture.GetProgressRatio()) * 100f);
                return capture.IsPlayerInZone
                    ? $"Capturing: {percent}%"
                    : $"Capture the zone: {percent}%";

            default:
                return $"Progress: {Mathf.RoundToInt(Mathf.Clamp01(mission.GetProgressRatio()) * 100f)}%";
        }
    }

    private static int GetSecondsLeft(float duration, float elapsed)
    {
        return Mathf.Max(0, Mathf.CeilToInt(duration - elapsed));
    }
}
