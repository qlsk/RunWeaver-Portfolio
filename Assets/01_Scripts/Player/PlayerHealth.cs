using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("최대 체력")]
    [SerializeField] private float _maxHealth = 100f;
    public float MaxHealth => _maxHealth;
    [Header("현재 체력")]
    [SerializeField] private float _currentHealth;
    public float CurrentHealth => _currentHealth;

    [Header("피격 무적 지속 시간")]
    [SerializeField] private float _invincibilityDuration = 1f;

    private Rigidbody2D _playerRigidbody2D;

    private PlayerMove _playerMove;
    private PlayerAttack _playerAttack;
    private Dash _dash;

    private bool _isDead = false;

    private bool _dashInvincibility = false;

    public void SetDashInvincibility(bool invincibility)
    {
        _dashInvincibility = invincibility;
    }

    private bool _hitInvincibility = false;

    // private bool _canTakeDamage = true;
    //
    // public void SetCanTakeDamage(bool canTakeDamage)
    // {
    //     _canTakeDamage = canTakeDamage;
    // }

    private void Start()
    {
        _currentHealth = _maxHealth;

        // 사망 시 움직임을 멈추기 위해 가져옴
        _playerRigidbody2D = GetComponent<Rigidbody2D>();
        // 사망 시 비활성화 할 Component
        _playerMove = GetComponent<PlayerMove>();
        _playerAttack = GetComponent<PlayerAttack>();
        _dash = GetComponent<Dash>();
    }

    // 피격
    public void TakeDamage(float damage)
    {
        if (_hitInvincibility || _dashInvincibility) return;
        StartCoroutine(TakeDamageCoroutine(damage));
    }

    private IEnumerator TakeDamageCoroutine(float damage)
    {
        _hitInvincibility = true;

        if (_currentHealth - damage <= 0f)
        {
            _currentHealth = 0f;
            Death();
        }
        else
        {
            _currentHealth -= damage;
        }

        yield return new WaitForSeconds(_invincibilityDuration);
        _hitInvincibility = false;
    }

    // 체력 회복
    public void Heal(float healAmount)
    {
        if (_currentHealth + healAmount > _maxHealth)
        {
            _currentHealth = _maxHealth;
        }
        else
        {
            _currentHealth += healAmount;
        }
    }

    // 사망
    public void Death()
    {
        if (_isDead) return;

        _playerRigidbody2D.linearVelocity = Vector2.zero;
        _playerMove.enabled = false;
        _playerAttack.enabled = false;
        _dash.enabled = false;
        _isDead = true;
    }
}
