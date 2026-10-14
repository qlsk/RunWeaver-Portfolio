using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RuneSlotUnlocker : MonoBehaviour
{
    [SerializeField, Min(1)] private int _floorsPerUnlock = 5; // 이 층 수를 클리어할 때마다 슬롯 1칸 개방
    [SerializeField] private RuneBookToastView _toast;
    [SerializeField] private string _unlockedMessageFormat = "룬 슬롯이 열렸습니다 ({0}/{1})"; // {0} = 열린 칸 수, {1} = 최대 칸 수

    private StageManager _stageManager;

    private void Start()
    {
        _stageManager = StageManager.Instance;
        _stageManager.MissionCleared += HandleMissionCleared;
    }

    private void OnDestroy()
    {
        if (_stageManager != null)
            _stageManager.MissionCleared -= HandleMissionCleared;
    }

    private void HandleMissionCleared(int clearedDepth)
    {
        int slotNumberToUnlock = clearedDepth / _floorsPerUnlock;
        int targetSlotCount = RuneEffectLoadout.InitialSlotCount + slotNumberToUnlock;
        RuneEffectLoadout loadout = PlayerRuneManager.Instance.RuneEffectLoadout;
        if (loadout.EnsureSlotCount(targetSlotCount))
            _toast.Show(string.Format(_unlockedMessageFormat, loadout.ActiveSlotCount, RuneEffectLoadout.MaxSlotCount));
    }
}
