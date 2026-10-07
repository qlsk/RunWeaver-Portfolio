using UnityEngine;

/// <summary>
/// 패시브 특성 1종의 데이터. Project 창에서 우클릭 → Create → Reward → PassiveData로 4종을 만든다.
/// 증가량은 하드코딩하지 않고 여기서 읽는다 (기본 스택당 +5%).
/// </summary>
[CreateAssetMenu(fileName = "PassiveData", menuName = "Reward/PassiveData")]
public class PassiveData : ScriptableObject
{
    public string id;
    public PassiveStatType statType;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public float valuePerStack = 0.05f;
}
