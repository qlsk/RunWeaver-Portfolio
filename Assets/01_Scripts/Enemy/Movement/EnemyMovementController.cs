using UnityEngine;

/// 활성 이동 행동의 목적지를 직선 이동 속도로 바꾸어 Motor에 전달한다.
[DefaultExecutionOrder(50)]
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyMotor))]
public class EnemyMovementController : MonoBehaviour
{
    private EnemyMotor _motor;
    private MonoBehaviour[] _behaviors;
    private bool _warnedMultipleBehaviors;

    private void Awake()
    {
        _motor = GetComponent<EnemyMotor>();
    }

    private void Start()
    {
        // 모든 컴포넌트의 Awake가 끝난 뒤 행동 후보를 확보한다.
        _behaviors = GetComponents<MonoBehaviour>();
    }

    private void FixedUpdate()
    {
        IEnemyMovementBehavior behavior = FindActiveBehavior();
        Vector2 velocity = Vector2.zero;
        if (behavior != null)
        {
            MovementGoal goal = behavior.CalculateGoal();
            if (!goal.ShouldStop)
            {
                Vector2 offset = goal.Destination - (Vector2)transform.position;
                if (offset.magnitude > Mathf.Max(0f, goal.StoppingDistance))
                {
                    velocity = offset.normalized * Mathf.Max(0f, behavior.MoveSpeed);
                }
            }
        }

        _motor.ApplyMovement(velocity);
    }

    private IEnemyMovementBehavior FindActiveBehavior()
    {
        IEnemyMovementBehavior selected = null;
        if (_behaviors == null) return null;

        foreach (MonoBehaviour component in _behaviors)
        {
            if (component == null || !(component is IEnemyMovementBehavior behavior))
                continue;

            if (selected != null)
            {
                if (!_warnedMultipleBehaviors)
                {
                    Debug.LogWarning("EnemyMovementController: 이동 행동은 하나만 활성화하세요.", this);
                    _warnedMultipleBehaviors = true;
                }

                return null;
            }

            selected = behavior;
        }

        _warnedMultipleBehaviors = false;
        return selected;
    }

    private void OnDisable()
    {
        if (_motor != null) _motor.ApplyMovement(Vector2.zero);
    }
}