using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    private SpriteRenderer _enemySpriteRenderer;
    private EnemyTankAttack _tankAttack;
    private EnemyTargetDetector _targetDetector;
    private EnemyDashAttack _dashAttack;

    private void Start()
    {
        _enemySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _tankAttack = GetComponent<EnemyTankAttack>();
        _targetDetector = GetComponent<EnemyTargetDetector>();
        _dashAttack = GetComponent<EnemyDashAttack>();
    }

    private void Update()
    {
        if (_tankAttack != null && _tankAttack.IsAttackActive)
            return;

        if (_dashAttack != null && _dashAttack.isActiveAndEnabled &&
            _dashAttack.State != EnemyAttackState.Idle)
            return;

        _enemySpriteRenderer.flipX = transform.position.x - _targetDetector.Target.position.x < 0 ? true : false;
    }
}
