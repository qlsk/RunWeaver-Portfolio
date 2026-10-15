using UnityEngine;

[System.Serializable]
public class EnemyStatData
{
    [SerializeField] private float _maxHealth;
    [SerializeField] private float _moveSpeed;
    [SerializeField, Min(0f)] private float _defense;

    public float MaxHealth => _maxHealth;
    public float MoveSpeed => _moveSpeed;
    public float Defense => _defense;
}
