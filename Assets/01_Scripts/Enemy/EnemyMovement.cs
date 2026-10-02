using System;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private EnemyDataSO _enemyData;
    [SerializeField] private EnemyTargetDetector _targetDetector;
    [SerializeField] private bool _enemyMove;
    private Rigidbody2D _enemyRigidbody2D;

    private void Awake()
    {
        _enemyRigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (!_enemyMove)
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