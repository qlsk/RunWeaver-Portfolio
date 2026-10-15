/// <summary>
/// 튜토리얼 완료 여부와 본 기록 id를 저장 파일(SaveSystem.Tutorial)에 저장한다.
/// 예전에 PlayerPrefs에 저장한 값은 SaveSystem이 처음 읽을 때 옮겨 온다.
/// </summary>
public static class TutorialProgressStore
{
    public static bool IsCompleted => SaveSystem.Tutorial.Completed;

    public static bool IsRuneBookTutorialCompleted => SaveSystem.Tutorial.IsRuneBookTutorialCompleted;

    public static void MarkCompleted()
    {
        if (SaveSystem.Tutorial.Completed)
            return;

        SaveSystem.Tutorial.Completed = true;
        SaveSystem.Commit();
    }

    public static void MarkRuneBookTutorialCompleted()
    {
        if (SaveSystem.Tutorial.IsRuneBookTutorialCompleted)
            return;

        SaveSystem.Tutorial.IsRuneBookTutorialCompleted = true;
        SaveSystem.Commit();
    }

    public static bool IsRecordSeen(string recordId)
    {
        return SaveSystem.Tutorial.SeenRecords.Contains(recordId);
    }

    /// <summary>본 기록 수. 협회 지침서의 "기록 N/전체"에 쓴다.</summary>
    public static int SeenRecordCount => SaveSystem.Tutorial.SeenRecords.Count;

    public static void MarkRecordSeen(string recordId)
    {
        if (string.IsNullOrEmpty(recordId) || SaveSystem.Tutorial.SeenRecords.Contains(recordId))
            return;

        SaveSystem.Tutorial.SeenRecords.Add(recordId);
        SaveSystem.Commit();
    }

    /// <summary>새 게임을 시작할 때 완료 여부와 본 기록을 모두 지운다.</summary>
    public static void ResetForNewGame()
    {
        SaveSystem.Tutorial.Completed = false;
        SaveSystem.Tutorial.IsRuneBookTutorialCompleted = false;
        SaveSystem.Tutorial.SeenRecords.Clear();
        SaveSystem.Commit();
    }

    public static void ResetForDebug()
    {
        ResetForNewGame();
    }
}
