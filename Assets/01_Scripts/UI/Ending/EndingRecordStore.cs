using UnityEngine;

/// <summary>
/// 판이 끝나도 남아야 하는 엔딩 기록. (기기에 저장, PlayerPrefs)
/// - 호문쿨루스 번호 : 판이 끝날 때마다 1씩 늘어난다. 보고서의 "제 N호"
/// - 최단 정화 기록 : 클리어했을 때의 조사 시간 중 가장 짧은 값
/// </summary>
public static class EndingRecordStore
{
    private const string HomunculusNumberKey = "RuneWeaver.HomunculusNumber";
    private const string BestClearSecondsKey = "RuneWeaver.BestClearSeconds";

    /// <summary>클리어 기록이 있는지</summary>
    public static bool HasBestClear => PlayerPrefs.HasKey(BestClearSecondsKey);

    /// <summary>최단 정화 기록(초). 기록이 없으면 -1</summary>
    public static float BestClearSeconds => PlayerPrefs.GetFloat(BestClearSecondsKey, -1f);

    /// <summary>이번 판의 호문쿨루스 번호를 받는다. (부를 때마다 1씩 늘어난다)</summary>
    public static int TakeHomunculusNumber()
    {
        int number = PlayerPrefs.GetInt(HomunculusNumberKey, 0) + 1;
        PlayerPrefs.SetInt(HomunculusNumberKey, number);
        PlayerPrefs.Save();
        return number;
    }

    /// <summary>클리어 시간을 기록한다. 최단 기록을 갱신했으면 true</summary>
    public static bool TryRecordClear(float seconds)
    {
        if (seconds <= 0f)
        {
            return false;
        }

        float best = BestClearSeconds;
        if (best >= 0f && seconds >= best)
        {
            return false;
        }

        PlayerPrefs.SetFloat(BestClearSecondsKey, seconds);
        PlayerPrefs.Save();
        return true;
    }
}
