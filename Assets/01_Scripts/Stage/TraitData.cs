using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 특성 1종의 데이터. 로직은 없고 조건값만 담는다.
/// Project 창에서 우클릭 → Create → Reward → TraitData로 만든다.
/// </summary>
[CreateAssetMenu(fileName = "TraitData", menuName = "Reward/TraitData")]
public class TraitData : ScriptableObject
{
    [FormerlySerializedAs("id")] public string Id;
    [FormerlySerializedAs("category")] public TraitCategory Category;
    [FormerlySerializedAs("effectKind")] public TraitEffectKind EffectKind;
    [FormerlySerializedAs("displayName")] public string DisplayName;
    [TextArea, FormerlySerializedAs("description")] public string Description;
    [FormerlySerializedAs("icon")] public Sprite Icon;

    [Tooltip("주 효과 수치 (비율/배율/개수. 예: 피해 +20% → 0.2)")]
    [FormerlySerializedAs("magnitude")] public float Magnitude;
    [Tooltip("지속 시간 (초). 윈도우형 효과용")]
    [FormerlySerializedAs("duration")] public float Duration;
    [Tooltip("횟수 조건 (예: 메아리 5회마다)")]
    [FormerlySerializedAs("count")] public int Count = 1;
    [Tooltip("범위 반경 (노바형 효과용)")]
    [FormerlySerializedAs("radius")] public float Radius;
}
