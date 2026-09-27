using UnityEngine;

public class EnemyMovemont : MonoBehaviour
{
    [SerializeField] private EnemyDataSO _enemyData;
    [SerializeField] private EnemyTargetDetector _targetDetector;

    private void Update()
    {
        Transform target = _targetDetector.Target;

        if (target == null)
        {
            return;
        }

        Vector2 direction = (target.position - transform.position).normalized;
        transform.position += (Vector3)(_enemyData.Stat.MoveSpeed * Time.deltaTime * direction);
    }
}
