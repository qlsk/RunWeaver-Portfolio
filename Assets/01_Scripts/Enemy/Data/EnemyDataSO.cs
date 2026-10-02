using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDataSO", menuName = "Scriptable Objects/EnemyDataSO")]
public class EnemyDataSO : ScriptableObject
{
    [SerializeField] private EnemyStatData _stat;
    [SerializeField] private EnemyInfoData _info;

    public EnemyStatData Stat => _stat;
    public EnemyInfoData Info => _info;
}