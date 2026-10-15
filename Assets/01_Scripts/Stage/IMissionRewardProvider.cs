using System;

/// <summary>
/// 미션 클리어 보상 확장 지점.
/// 미션을 클리어하면 StageManager가 포탈을 만들기 전에 Grant를 부른다.
/// 보상 선택(예: 업그레이드 3택1)이 끝나면 onFinished를 불러야 포탈이 나온다.
/// 등록된 보상이 없으면 바로 포탈이 나온다. (StageManager.SetMissionRewardProvider)
/// </summary>
public interface IMissionRewardProvider
{
    /// <param name="depth">클리어한 층</param>
    /// <param name="onFinished">보상 처리가 끝나면 부른다</param>
    void Grant(int depth, Action onFinished);
}
