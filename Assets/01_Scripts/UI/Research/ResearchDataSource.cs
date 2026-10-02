using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 연구 UI가 사용하는 데이터. 단독 테스트는 FakeResearchDataSource,
/// 실제 게임 연결은 이 클래스를 상속해 GameSession의 연구 트리와 거점 보관 재료를 쓰는 소스로 교체한다.
/// </summary>
public abstract class ResearchDataSource : MonoBehaviour
{
    /// <summary>연구 상태나 보유 재료가 바뀌면 발생한다.</summary>
    public event Action Changed;

    public abstract IReadOnlyList<ResearchTrackData> GetTracks();

    public abstract ResearchNodeData GetNode(string researchId);

    public abstract int GetResourceAmount(string resourceId);

    public abstract bool TryResearch(string researchId);

    protected void RaiseChanged()
    {
        SafeEventNotifier.Notify(Changed);
    }
}
