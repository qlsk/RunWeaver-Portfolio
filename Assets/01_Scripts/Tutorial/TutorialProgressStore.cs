using UnityEngine;

public static class TutorialProgressStore
{
    private const string CompletedKey = "Tutorial.Completed";

    public static bool IsCompleted => PlayerPrefs.GetInt(CompletedKey) == 1;

    public static void MarkCompleted()
    {
        PlayerPrefs.SetInt(CompletedKey, 1);
        PlayerPrefs.Save();
    }

    public static void ResetForDebug()
    {
        PlayerPrefs.DeleteKey(CompletedKey);
        PlayerPrefs.Save();
    }
}