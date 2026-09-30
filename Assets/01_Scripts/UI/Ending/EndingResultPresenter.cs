using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 결과창이 켜질 때 결과 데이터와 룬북 데이터를 읽어 EndingResultView에 표시한다.
/// </summary>
[DisallowMultipleComponent]
public class EndingResultPresenter : MonoBehaviour
{
    [SerializeField] private EndingResultView _view;
    [SerializeField] private RunResultSource _resultSource;
    [SerializeField] private RuneBookDataSource _runeBookDataSource;

    private void OnEnable()
    {
        if (_view == null)
        {
            Debug.LogError("[EndingResultPresenter] EndingResultView가 할당되지 않았습니다.", this);
            return;
        }

        if (_resultSource != null)
        {
            _view.ShowResult(_resultSource.GetResult());
        }
        else
        {
            Debug.LogWarning("[EndingResultPresenter] 결과 데이터가 할당되지 않았습니다.", this);
        }

        if (_runeBookDataSource != null)
        {
            _view.ShowActivatedRunes(GetEquippedEntries());
            _view.ShowGatheredRunes(_runeBookDataSource.GetOwnedRuneStones());
        }
        else
        {
            Debug.LogWarning("[EndingResultPresenter] 룬 데이터가 할당되지 않았습니다.", this);
        }
    }

    private List<RuneBookEntryData> GetEquippedEntries()
    {
        IReadOnlyList<RuneBookEntryData> entries = _runeBookDataSource.GetEntries();
        List<RuneBookEntryData> equippedEntries = new();

        for (int i = 0; i < _runeBookDataSource.MaxSlotCount; i++)
        {
            string entryId = i < _runeBookDataSource.ActiveSlotCount
                ? _runeBookDataSource.GetEquippedEntryId(i)
                : null;
            equippedEntries.Add(FindEntry(entries, entryId));
        }

        return equippedEntries;
    }

    private static RuneBookEntryData FindEntry(IReadOnlyList<RuneBookEntryData> entries, string entryId)
    {
        if (string.IsNullOrEmpty(entryId))
        {
            return null;
        }

        foreach (RuneBookEntryData entry in entries)
        {
            if (entry.Id == entryId)
            {
                return entry;
            }
        }

        return null;
    }
}
