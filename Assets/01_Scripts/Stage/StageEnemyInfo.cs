using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 스테이지 시스템이 몬스터에 부착하는 메타 정보.
/// Enemy 폴더(타 팀원 코드)를 수정하지 않고 엘리트 여부를 전달하기 위한 어댑터.
/// </summary>
public class StageEnemyInfo : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("isElite")] private bool _isElite = false;

    public bool IsElite => _isElite;

    public void Setup(bool elite)
    {
        _isElite = elite;
    }
}
