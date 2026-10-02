using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 새 룬북 화면(RuneBookV2)이 쓰는 데이터.
/// - 룬 워드 목록, 보유 룬 문자(0개인 것도 포함), 활성 칸 수, 확정된 장착 상태를 제공한다.
/// - Confirm으로 장착을 확정하면 새로 넣은 룬 워드의 룬 문자를 그때 소모한다.
/// 실제 게임 연결은 이 클래스를 상속한 소스로 교체한다. (지금은 FakeRuneBookV2DataSource)
/// </summary>
public abstract class RuneBookV2DataSource : MonoBehaviour
{
    public const int SlotLimit = 5;

    /// <summary>데이터가 바뀌었을 때 (확정, 룬 문자 획득 등)</summary>
    public event Action Changed;

    public abstract int ActiveSlotCount { get; }

    public abstract IReadOnlyList<RuneBookEntryData> GetEntries();

    /// <summary>같은 룬 워드를 여러 칸에 넣을 수 있는지</summary>
    public abstract bool CanOverlap(string entryId);

    /// <summary>보유 룬 문자 전체 (보유 수가 0인 것도 포함, 표시 순서대로)</summary>
    public abstract IReadOnlyList<RuneStoneStackData> GetRuneStones();

    /// <summary>확정된 장착 상태 (빈 칸은 null)</summary>
    public abstract string GetConfirmedId(int slotIndex);

    /// <summary>
    /// 장착을 확정한다. slotIds: 칸별 룬 워드 id (빈 칸은 null),
    /// newlyAdded: 이번에 새로 넣은 칸 (이 칸의 룬 워드만 룬 문자를 소모한다)
    /// </summary>
    public abstract bool Confirm(IReadOnlyList<string> slotIds, IReadOnlyList<bool> newlyAdded);

    protected void NotifyChanged()
    {
        SafeEventNotifier.Notify(Changed);
    }
}
