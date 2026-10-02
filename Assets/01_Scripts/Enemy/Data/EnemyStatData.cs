using UnityEngine;

[System.Serializable]
public class EnemyStatData
{
    [SerializeField] private float _maxHealth;
    [SerializeField] private float _moveSpeed;

    public float MaxHealth => _maxHealth;
    public float MoveSpeed => _moveSpeed;
}
