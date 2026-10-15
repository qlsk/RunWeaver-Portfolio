using UnityEngine;

/// <summary>
/// 튜토리얼 안내(하단 조작 안내, 월드 표식)를 잠시 숨겨야 하는지 알려 준다.
/// 일시정지 메뉴나 룬북이 열려 있으면 그 화면 위에 안내가 겹쳐 보이지 않게 숨긴다.
/// </summary>
public static class TutorialOverlayBlocker
{
    private static RuneBookPauseController[] s_runeBookPauses;
    private static int s_cachedFrame = -1;
    private static bool s_isBlocked;

    public static bool IsBlocked
    {
        get
        {
            // 한 프레임에 한 번만 계산한다.
            if (s_cachedFrame == Time.frameCount)
            {
                return s_isBlocked;
            }

            s_cachedFrame = Time.frameCount;
            s_isBlocked = PauseController.IsGamePaused || IsRuneBookOpen();
            return s_isBlocked;
        }
    }

    private static bool IsRuneBookOpen()
    {
        if (s_runeBookPauses == null || System.Array.Exists(s_runeBookPauses, pause => pause == null))
        {
            s_runeBookPauses = Object.FindObjectsByType<RuneBookPauseController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        foreach (RuneBookPauseController pause in s_runeBookPauses)
        {
            if (pause != null && pause.IsPaused)
            {
                return true;
            }
        }

        return false;
    }
}
