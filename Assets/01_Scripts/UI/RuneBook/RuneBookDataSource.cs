using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 룬북 UI가 표시할 데이터를 제공하고 장착 요청을 처리하는 창구.
/// UI는 이 클래스에만 의존하며, 실제 게임 데이터(RuneBook, RuneLoadout, RuneStoneInventory)를 연결할 때는
/// 이 클래스를 상속한 어댑터를 만들어 RuneBookPresenter에 할당한다.
/// </summary>
public abstract class RuneBookDataSource : MonoBehaviour
{
    /// <summary>
    /// 룬 목록, 장착 상태, 보유 룬석 중 하나라도 바뀌면 발생한다.
    /// </summary>
    public event Action Changed;

    /// <summary>
    /// 연구로 확장할 수 있는 최대 활성 칸 수
    /// </summary>
    public abstract int MaxSlotCount { get; }

    /// <summary>
    /// 현재 사용할 수 있는 활성 칸 수
    /// </summary>
    public abstract int ActiveSlotCount { get; }

    public abstract IReadOnlyList<RuneBookEntryData> GetEntries();

    public abstract IReadOnlyList<RuneStoneStackData> GetOwnedRuneStones();

    /// <summary>
    /// 해당 칸에 장착된 룬 효과 ID를 반환한다. 비어 있으면 null을 반환한다.
    /// </summary>
    public abstract string GetEquippedEntryId(int slotIndex);

    public abstract bool CanEquip(int slotIndex, string entryId);

    public abstract bool TryEquip(int slotIndex, string entryId);

    public abstract bool CanUnequip(int slotIndex);

    public abstract bool TryUnequip(int slotIndex);

    protected void NotifyChanged()
    {
        SafeEventNotifier.Notify(Changed);
    }
}
