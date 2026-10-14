using System;
using System.Collections.Generic;

/// <summary>
/// 상점에서 산 룬 워드 완성품 보유 수.
/// - 완성품 1개로 룬 문자 없이 1번 활성화할 수 있다. (Confirm할 때 1개 소모)
/// - 씬이 바뀌어도 유지된다. 게임을 껐다 켜면 이어하기로 돌아올 때만 되살아난다. (RunSaveController가 층 시작 때 저장)
/// </summary>
public static class OwnedRuneWords
{
    private static readonly Dictionary<string, int> s_counts = new();

    /// <summary>보유 수가 바뀌었을 때</summary>
    public static event Action Changed;

    public static int GetCount(string effectId)
    {
        if (string.IsNullOrEmpty(effectId))
        {
            return 0;
        }

        return s_counts.TryGetValue(effectId, out int count) ? count : 0;
    }

    /// <summary>보유한 완성품과 개수 (저장용 복사본)</summary>
    public static IReadOnlyDictionary<string, int> GetAll() => new Dictionary<string, int>(s_counts);

    public static void Add(string effectId, int amount = 1)
    {
        if (string.IsNullOrEmpty(effectId) || amount <= 0)
        {
            return;
        }

        s_counts[effectId] = GetCount(effectId) + amount;
        SafeEventNotifier.Notify(Changed);
    }

    /// <summary>1개 소모한다. 없으면 false</summary>
    public static bool Consume(string effectId)
    {
        int count = GetCount(effectId);
        if (count <= 0)
        {
            return false;
        }

        if (count == 1)
        {
            s_counts.Remove(effectId);
        }
        else
        {
            s_counts[effectId] = count - 1;
        }

        SafeEventNotifier.Notify(Changed);
        return true;
    }

    public static void Clear()
    {
        if (s_counts.Count == 0)
        {
            return;
        }

        s_counts.Clear();
        SafeEventNotifier.Notify(Changed);
    }

    // 도메인 리로드를 끈 플레이 모드에서도 플레이할 때마다 비운다.
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        s_counts.Clear();
        Changed = null;
    }
}
