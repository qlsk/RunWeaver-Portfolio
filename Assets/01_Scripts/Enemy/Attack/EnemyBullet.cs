using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    private EnemyTargetDetector _targetDetector;
    private Vector2 _direction;
    [SerializeField] private float _speed = 2f;

    private void Start()
    {
        _targetDetector = GetComponent<EnemyTargetDetector>();
        _direction = (_targetDetector.Target.position - transform.position).normalized;
    }

    private void Update()
    {
        Debug.Log(_direction);
        float moveDistance = _speed * Time.deltaTime;
        transform.position += (Vector3)(_direction * moveDistance);
    }
}