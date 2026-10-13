using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>튜토리얼 완료 여부와 본 기록 id를 PlayerPrefs에 저장한다.</summary>
public static class TutorialProgressStore
{
    private const string CompletedKey = "Tutorial.Completed";
    private const string SeenRecordsKey = "Tutorial.SeenRecords";
    private const char RecordSeparator = '|';

    public static bool IsCompleted => PlayerPrefs.GetInt(CompletedKey) == 1;

    public static void MarkCompleted()
    {
        PlayerPrefs.SetInt(CompletedKey, 1);
        PlayerPrefs.Save();
    }

    public static bool IsRecordSeen(string recordId)
    {
        return ReadSeenRecords().Contains(recordId);
    }

    /// <summary>본 기록 수. 협회 지침서의 "기록 N/전체"에 쓴다.</summary>
    public static int SeenRecordCount => ReadSeenRecords().Count;

    public static void MarkRecordSeen(string recordId)
    {
        HashSet<string> seen = ReadSeenRecords();
        if (!seen.Add(recordId))
            return;

        PlayerPrefs.SetString(SeenRecordsKey, string.Join(RecordSeparator, seen));
        PlayerPrefs.Save();
    }

    public static void ResetForDebug()
    {
        PlayerPrefs.DeleteKey(CompletedKey);
        PlayerPrefs.DeleteKey(SeenRecordsKey);
        PlayerPrefs.Save();
    }

    private static HashSet<string> ReadSeenRecords()
    {
        string stored = PlayerPrefs.GetString(SeenRecordsKey, string.Empty);
        return new HashSet<string>(stored.Split(new[] { RecordSeparator }, StringSplitOptions.RemoveEmptyEntries));
    }
}
