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
            return "임무 실패";
        }

        if (mission.IsCompleted)
        {
            return "임무 완료!";
        }

        switch (mission)
        {
            case EnemyKillMission kill:
                return kill.TargetEliteKillCount > 0
                    ? $"적: {kill.CurrentNormalKillCount}/{kill.TargetNormalKillCount}  정예: {kill.CurrentEliteKillCount}/{kill.TargetEliteKillCount}"
                    : $"처치한 적: {kill.CurrentNormalKillCount}/{kill.TargetNormalKillCount}";

            case SoulCollectMission soul:
                return $"모은 영혼: {soul.CurrentSoulCount}/{soul.TargetSoulCount}";

            case SurvivalMission survival:
                return $"생존: {GetSecondsLeft(survival.SurvivalDuration, survival.CurrentTimer)}초 남음";

            case AltarDefenseMission altar:
                return $"제단 방어: {GetSecondsLeft(altar.DefenseDuration, altar.CurrentTimer)}초 남음";

            case CaptureZoneMission capture:
                int percent = Mathf.FloorToInt(Mathf.Clamp01(capture.GetProgressRatio()) * 100f);
                return capture.IsPlayerInZone
                    ? $"점령 중: {percent}%"
                    : $"거점 점령: {percent}%";

            default:
                return $"진행도: {Mathf.RoundToInt(Mathf.Clamp01(mission.GetProgressRatio()) * 100f)}%";
        }
    }

    private static int GetSecondsLeft(float duration, float elapsed)
    {
        return Mathf.Max(0, Mathf.CeilToInt(duration - elapsed));
    }
}
