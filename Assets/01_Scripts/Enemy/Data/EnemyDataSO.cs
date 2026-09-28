using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDataSO", menuName = "Scriptable Objects/EnemyDataSO")]
public class EnemyDataSO : ScriptableObject
{
    [SerializeField] private EnemyStatData _stat;

    public EnemyStatData Stat => _stat;
}
