using UnityEngine;

/// 활성 이동 행동의 목적지를 Navigation에 전달하고 계산된 속도를 Motor에 요청한다.
[DefaultExecutionOrder(50)]
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyMotor), typeof(EnemyNavigation))]
public class EnemyMovementController : MonoBehaviour
{
    private EnemyMotor _motor;
    private EnemyNavigation _navigation;
    private MonoBehaviour[] _behaviors;
    private bool _warnedMultipleBehaviors;

    private void Awake()
    {
        _motor = GetComponent<EnemyMotor>();
        if (!TryGetComponent(out _navigation))
        {
            _navigation = gameObject.AddComponent<EnemyNavigation>();
        }
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
            velocity = _navigation.CalculateVelocity(goal, behavior.MoveSpeed);
        }

        if (behavior == null)
        {
            _navigation.ClearPath();
        }

        _motor.ApplyMovement(velocity);
    }

    private IEnemyMovementBehavior FindActiveBehavior()
    {
        IEnemyMovementBehavior selected = null;
        if (_behaviors == null)
        {
            return null;
        }

        foreach (MonoBehaviour component in _behaviors)
        {
            if (component == null || !component.isActiveAndEnabled || !(component is IEnemyMovementBehavior behavior))
            {
                continue;
            }

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
        if (_motor != null)
        {
            _motor.ApplyMovement(Vector2.zero);
        }
    }
}