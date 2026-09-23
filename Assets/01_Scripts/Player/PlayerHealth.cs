using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("최대 체력")] [SerializeField] private float _maxHealth = 100f;
    public float MaxHealth => _maxHealth;
    [Header("현재 체력")] [SerializeField] private float _currentHealth;
    public float CurrentHealth => _currentHealth;

    [Header("피격 무적 지속 시간")] [SerializeField]
    private float _invincibilityDuration = 1f;

    private PlayerMove _playerMove;
    private PlayerFire _playerFire;
    private Dash _dash;

    private bool _canTakeDamage = true;

    public void SetCanTakeDamage(bool canTakeDamage)
    {
        _canTakeDamage = canTakeDamage;
    }

    private void Start()
    {
        _currentHealth = _maxHealth;

        // 사망 시 비활성화 할 Component
        _playerMove = GetComponent<PlayerMove>();
        _playerFire = GetComponent<PlayerFire>();
        _dash = GetComponent<Dash>();
    }

    // 피격
    public void TakeDamage(float damage)
    {
        if (!_canTakeDamage) return;
        StartCoroutine("TakeDamageCoroutine", damage);
    }

    private IEnumerator TakeDamageCoroutine(float damage)
    {
        // 피격 무적 시간 동안 데미지 처리 금지
        SetCanTakeDamage(false);

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
        SetCanTakeDamage(true);
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
        _playerMove.enabled = false;
        _playerFire.enabled = false;
        _dash.enabled = false;
    }
}
