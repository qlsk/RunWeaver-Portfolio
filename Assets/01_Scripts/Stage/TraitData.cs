using UnityEngine;

/// <summary>
/// 특성 1종의 데이터. 로직은 없고 조건값만 담는다.
/// Project 창에서 우클릭 → Create → Reward → TraitData로 만든다.
/// </summary>
[CreateAssetMenu(fileName = "TraitData", menuName = "Reward/TraitData")]
public class TraitData : ScriptableObject
{
    public string id;
    public TraitCategory category;
    public TraitEffectKind effectKind;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Tooltip("주 효과 수치 (비율/배율/개수. 예: 피해 +20% → 0.2)")]
    public float magnitude;
    [Tooltip("지속 시간 (초). 윈도우형 효과용")]
    public float duration;
    [Tooltip("횟수 조건 (예: 메아리 5회마다)")]
    public int count = 1;
    [Tooltip("범위 반경 (노바형 효과용)")]
    public float radius;
}
