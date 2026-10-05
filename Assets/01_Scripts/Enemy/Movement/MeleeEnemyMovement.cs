using System;
using UnityEngine;

public class MeleeEnemyMovement : MonoBehaviour
{
    [SerializeField] private EnemyDataSO _enemyData;
    private EnemyTargetDetector _targetDetector;
    private Rigidbody2D _enemyRigidbody2D;
    private EnemyDashAttack _dashAttack;
    [SerializeField] private bool _enemyMove = true;
    private EnemyStatusEffectController _statusEffectController;

    private void Awake()
    {
        _targetDetector = GetComponent<EnemyTargetDetector>();
        _enemyRigidbody2D = GetComponent<Rigidbody2D>();
        _dashAttack = GetComponent<EnemyDashAttack>();
        _statusEffectController = GetComponent<EnemyStatusEffectController>();
    }

    private void FixedUpdate()
    {
        if (_dashAttack != null && _dashAttack.IsDashActive)
            return;

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