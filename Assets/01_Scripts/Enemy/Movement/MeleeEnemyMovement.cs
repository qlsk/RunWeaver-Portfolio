using System;
using UnityEngine;

public class MeleeEnemyMovement : MonoBehaviour
{
    [SerializeField] private EnemyDataSO _enemyData;
    private EnemyTargetDetector _targetDetector;
    private Rigidbody2D _enemyRigidbody2D;
    private EnemyDashAttack _dashAttack;

    private void Awake()
    {
        _targetDetector = GetComponent<EnemyTargetDetector>();
        _enemyRigidbody2D = GetComponent<Rigidbody2D>();
        _dashAttack = GetComponent<EnemyDashAttack>();
    }

    private void FixedUpdate()
    {
        if (_dashAttack != null && _dashAttack.IsDashActive)
            return;

        Transform target = _targetDetector.Target;

        if (target == null)
        {
            _enemyRigidbody2D.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = (target.position - transform.position).normalized;
        _enemyRigidbody2D.linearVelocity = direction * _enemyData.Stat.MoveSpeed;
    }
}