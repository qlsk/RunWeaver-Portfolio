using UnityEngine;

/// <summary>
/// 패시브 특성 1종의 데이터. Project 창에서 우클릭 → Create → Reward → PassiveData로 4종을 만든다.
/// 증가량은 여기서 읽는다. 비율 스탯은 기본 스택당 +5%, 탄창 크기는 정수 탄수다.
/// </summary>
[CreateAssetMenu(fileName = "PassiveData", menuName = "Reward/PassiveData")]
public class PassiveData : ScriptableObject
{
    public string Id;
    public PassiveStatType StatType;
    public string DisplayName;
    [TextArea] public string Description;
    public Sprite Icon;
    [Tooltip("스택당 증가량. 비율 스탯은 0.05 = 5%, 탄창 크기는 1 = 1발.")]
    public float ValuePerStack = 0.05f;

    [Tooltip("최대 레벨(스택). 이 횟수만큼 선택되면 추첨에서 제외된다.")]
    [Min(1)] public int MaxLevel = 5;
}
