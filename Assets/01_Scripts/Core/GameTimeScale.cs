using MoreMountains.Feedbacks;
using UnityEngine;

/// <summary>
/// 일시정지(룬북, Esc 메뉴, 보상 선택)와 Feel 시간 연출(피격 정지 프레임 등, MMTimeManager)이 함께 Time.timeScale을 쓰므로 둘을 맞춘다.
/// - 정지 프레임 도중에 일시정지하면, 정지 프레임이 끝날 때 MMTimeManager가 timeScale을 1로 되돌려 일시정지가 풀리고,
///   일시정지를 닫을 때는 정지 프레임 값(0)으로 되돌려 게임이 멈춘 채 남는다. 일시정지 직전에 BeginPause로 연출을 끝내 이를 막는다.
/// - 정지 프레임 때문에 잠깐 timeScale이 0인 것은 "다른 화면이 멈춰 둔 것"과 구별한다. (IsTimeEffectActive)
/// </summary>
public static class GameTimeScale
{
    /// <summary>
    /// 일시정지 직전에 부른다. 진행 중인 Feel 시간 연출을 끝내고(기본 배속으로 되돌리고),
    /// 일시정지를 닫을 때 되돌릴 timeScale을 돌려준다.
    /// </summary>
    public static float BeginPause()
    {
        MMTimeManager manager = Object.FindAnyObjectByType<MMTimeManager>();
        if (manager != null && IsEffectActive(manager))
        {
            // 쌓인 연출을 지우고 기본 배속을 적용한다. 이후 MMTimeManager는 timeScale을 다시 덮어쓰지 않는다.
            manager.ResetTimeScale();
            return manager.NormalTimeScale;
        }

        return Time.timeScale;
    }

    /// <summary>지금 timeScale이 Feel 시간 연출(정지 프레임, 슬로 모션) 때문에 바뀌어 있는지</summary>
    public static bool IsTimeEffectActive
    {
        get
        {
            MMTimeManager manager = Object.FindAnyObjectByType<MMTimeManager>();
            return manager != null && IsEffectActive(manager);
        }
    }

    private static bool IsEffectActive(MMTimeManager manager)
    {
        // 연출 중에는 목표 배속이 기본과 다르고, 지금 timeScale도 MMTimeManager가 마지막으로 정한 값이다.
        // (일시정지가 0으로 바꿨다면 CurrentTimeScale과 달라진다)
        return !Mathf.Approximately(manager.TargetTimeScale, manager.NormalTimeScale)
               && Mathf.Approximately(Time.timeScale, manager.CurrentTimeScale);
    }
}
