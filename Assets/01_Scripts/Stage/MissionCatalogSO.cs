using System;
using UnityEngine;

/// <summary>미션 종류별 HUD 이름과 설명을 보관한다.</summary>
[CreateAssetMenu(fileName = "MissionCatalog", menuName = "Stage/Mission Catalog")]
public class MissionCatalogSO : ScriptableObject
{
    [Serializable]
    public class MissionText
    {
        public string DisplayName;
        [TextArea] public string Description;
    }

    [Serializable]
    private class MissionEntry
    {
        public MissionType Type;
        public MissionText Text;
    }

    [SerializeField] private MissionEntry[] _missions = Array.Empty<MissionEntry>();
    [SerializeField] private MissionText _bossMission;

    public MissionText BossMission => _bossMission;

    public MissionText Get(MissionType type)
    {
        foreach (MissionEntry entry in _missions)
        {
            if (entry.Type == type)
            {
                return entry.Text;
            }
        }

        return null;
    }
}
