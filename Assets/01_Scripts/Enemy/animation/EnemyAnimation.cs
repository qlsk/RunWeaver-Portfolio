using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    private SpriteRenderer _enemySpriteRenderer;
    private EnemyTargetDetector _targetDetector;
    private EnemyDashAttack _dashAttack;

    private void Start()
    {
        _enemySpriteRenderer = GetComponent<SpriteRenderer>();
        _targetDetector = GetComponent<EnemyTargetDetector>();
        _dashAttack = GetComponent<EnemyDashAttack>();
    }

    private void Update()
    {
        if (_dashAttack != null && _dashAttack.isActiveAndEnabled &&
            _dashAttack.State != DashAttackState.Idle)
            return;

        _enemySpriteRenderer.flipX = transform.position.x - _targetDetector.Target.position.x < 0 ? true : false;
    }
}