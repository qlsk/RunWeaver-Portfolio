using System;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private EnemyDataSO _enemyData;
    [SerializeField] private EnemyTargetDetector _targetDetector;
    [SerializeField] private bool _enemyMove = true;
    private Rigidbody2D _enemyRigidbody2D;
    private EnemyStatusEffectController _statusEffectController;

    private void Awake()
    {
        _enemyRigidbody2D = GetComponent<Rigidbody2D>();
        _statusEffectController = GetComponent<EnemyStatusEffectController>();
    }

    private void FixedUpdate()
    {
        Vector2 moveVelocity = Vector2.zero;
        Transform target = _targetDetector.Target;
        if (_enemyMove && !_statusEffectController.IsStaggered && target != null)
        {
            Vector2 direction = (target.position - transform.position).normalized;
            moveVelocity = direction * (_enemyData.Stat.MoveSpeed * _statusEffectController.MoveSpeedMultiplier);
        }

        _enemyRigidbody2D.linearVelocity = moveVelocity + _statusEffectController.KnockbackVelocity;
    }
}