using UnityEngine;

public class PlayerLifecycle : MonoBehaviour
{
    [Header("사망 시 비활성화할 행동 컴포넌트")]
    [SerializeField] private Behaviour[] _disableOnDeath = new Behaviour[1];

    private PlayerHealth _health;
    private Rigidbody2D _rigidbody;

    private void Awake()
    {
        _health = GetComponent<PlayerHealth>();
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        _health.Died += HandleDeath;
        if (_health.IsDead)
        {
            HandleDeath();
        }
    }

    private void OnDisable()
    {
        _health.Died -= HandleDeath;
    }

    private void HandleDeath()
    {
        foreach (Behaviour behaviour in _disableOnDeath)
        {
            if (behaviour == null)
            {
                continue;
            }

            if (behaviour == this || behaviour == _health)
            {
                continue;
            }

            behaviour.enabled = false;
        }

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = 0f;
    }
}
