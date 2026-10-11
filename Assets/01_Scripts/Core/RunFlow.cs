using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 한 판(런)의 시작과 끝을 정리한다.
/// - 런이 끝난 사유를 한 곳에 기록한다. (처음 기록된 사유만 남는다)
/// - 결과창의 다시 시작(전투 씬을 다시 연다)과 타이틀로 돌아가기를 담당한다. (마을 씬은 쓰지 않는다)
///   씬을 옮기기 전에 원정 데이터(골드)와 장착 룬을 비운다. 보유 룬 문자는 그대로 남는다.
/// - 원정 시작(지갑, 시작 골드)은 전투 씬의 시작 석상(ExpeditionStatue)이 부른다.
/// </summary>
public static class RunFlow
{
    /// <summary>원정을 시작할 때 지갑에 넣어 주는 골드. 마을에서는 이 값을 보여준다.</summary>
    public const int StartingGold = 1000;

    /// <summary>타이틀 씬 이름 (결과창의 타이틀로 돌아가기)</summary>
    private const string TitleSceneName = "StartScene";

    /// <summary>씬 전환 : 버튼이 반짝이는 동안 기다리는 시간, 검게 덮이는 시간, 새 전투 씬에서 걷히는 시간 (초)</summary>
    private const float TransitionDelay = 0.15f;
    private const float TransitionCoverDuration = 0.65f;
    private const float TransitionRevealDuration = 0.5f;

    private static RunEndReason? s_endReason;
    private static bool s_isRestartRequested;

    /// <summary>이번 런이 끝난 사유. 아직 끝나지 않았으면 null</summary>
    public static RunEndReason? EndReason => s_endReason;

    public static bool HasEnded => s_endReason.HasValue;

    /// <summary>새 런을 시작한다. (전투 씬 시작 시)</summary>
    public static void BeginRun()
    {
        s_endReason = null;
    }

    /// <summary>런이 끝난 사유를 기록한다. 이미 기록되어 있으면 무시하고 false</summary>
    public static bool TrySetEndReason(RunEndReason reason)
    {
        if (s_endReason.HasValue)
        {
            return false;
        }

        s_endReason = reason;
        return true;
    }

    /// <summary>
    /// 원정을 시작한다. (마을에서 룬 확정 후 출발할 때)
    /// 원정 지갑을 만들고 시작 골드를 넣는다. 이미 원정 중이면 아무것도 하지 않고 false
    /// </summary>
    public static bool StartExpedition()
    {
        GameSession session = GameSession.Instance;
        if (session == null || !session.IsReady || session.Current == null)
        {
            return false;
        }

        PlayerHoldings holdings = session.Current.Holdings;
        if (holdings.IsOnExpedition)
        {
            return false;
        }

        session.StartExpedition();
        if (StartingGold > 0)
        {
            holdings.ExpeditionWallet?.AddGold(StartingGold);
        }

        return true;
    }

    /// <summary>
    /// (마을 씬을 쓰지 않으므로) 런을 정리하고 타이틀로 간다. GoToTitle과 같다.
    /// </summary>
    public static void GoToLobby()
    {
        GoToTitle();
    }

    /// <summary>
    /// 결과창 타이틀로 돌아가기 : 화면을 검게 덮은 뒤 런을 정리하고 타이틀(StartScene)로 간다.
    /// 타이틀은 스스로 검은 화면에서 밝아지므로 넘어간 뒤 바로 가림막을 지운다.
    /// </summary>
    public static void GoToTitle()
    {
        SceneTransition.FadeAndLoad(() =>
        {
            FinishRun();
            UnityEngine.SceneManagement.SceneManager.LoadScene(TitleSceneName,
                UnityEngine.SceneManagement.LoadSceneMode.Single);
        }, TransitionDelay, TransitionCoverDuration, 0f);
    }

    /// <summary>
    /// 결과창 다시 시작 : 런을 정리하고 전투 씬을 처음부터 다시 연다.
    /// 전투 씬의 시작 석상에서 다시 원소와 룬을 고른다. (마을 씬을 쓰지 않는다)
    /// </summary>
    public static void Restart()
    {
        SceneTransition.FadeAndLoad(() =>
        {
            FinishRun();
            SceneLoader.LoadCombatSceneScene();
        }, TransitionDelay, TransitionCoverDuration, TransitionRevealDuration);
    }

    /// <summary>ReStart로 마을에 왔는지 확인하고 요청을 지운다.</summary>
    public static bool ConsumeRestartRequest()
    {
        bool isRequested = s_isRestartRequested;
        s_isRestartRequested = false;
        return isRequested;
    }

    /// <summary>원정 데이터(골드)와 장착 룬을 비우고 게임 시간을 되돌린다. 보유 룬 문자는 유지한다.</summary>
    private static void FinishRun()
    {
        Time.timeScale = 1f;
        s_endReason = null;

        GameSession session = GameSession.Instance;
        if (session != null && session.IsReady && session.Current != null && session.Current.Holdings.IsOnExpedition)
        {
            // 연구 자원 정산이 없어졌으므로 회수할 자원 없이 원정만 끝낸다.
            session.CompleteFailure(new Dictionary<string, int>());
        }

        PlayerRuneManager runeManager = PlayerRuneManager.Instance;
        if (runeManager != null && runeManager.IsInitialized)
        {
            runeManager.RuneEffectLoadout.ClearAll();
        }
    }

    // 도메인 리로드를 끈 플레이 모드에서도 플레이할 때마다 비운다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        s_endReason = null;
        s_isRestartRequested = false;
    }
}
