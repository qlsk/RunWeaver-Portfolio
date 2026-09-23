using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;
    public float MaxHealth => _maxHealth;
    [SerializeField] private float _currentHealth;
    public float CurrentHealth => _currentHealth;

    private PlayerMove _playerMove;
    private PlayerFire _playerFire;
    private Dash _dash;

    private void Start()
    {
        _currentHealth = _maxHealth;
        _playerMove = GetComponent<PlayerMove>();
        _playerFire = GetComponent<PlayerFire>();
        _dash = GetComponent<Dash>();
    }

    public void TakeDamage(float damage)
    {
        if (_currentHealth - damage <= 0f)
        {
            _currentHealth = 0f;
            Death();
        }
        else
        {
            _currentHealth -= damage;
        }
    }

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

    public void Death()
    {
        _playerMove.enabled = false;
        _playerFire.enabled = false;
        _dash.enabled = false;
    }
}
