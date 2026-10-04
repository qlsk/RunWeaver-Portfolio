using System;
using UnityEngine;

public class EnemyStatusEffectController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyHealth _enemyHealth;

    [Header("Burn")]
    [SerializeField] private float _burnDuration = 3f;
    [SerializeField] private float _burnInterval = 1f;
    [SerializeField] private float _burnDamageMultiplier = 0.2f;

    [Header("Slow")]
    [SerializeField] private float _slowDuration = 3f;
    [SerializeField] private float _slowMoveSpeedMultiplier = 0.5f;

    [Header("Stagger")]
    [SerializeField] private float _staggerDuration = 0.5f;

    [Header("Knockback")]
    [SerializeField] private float _knockbackDuration = 0.2f;
    [SerializeField] private float _knockbackSpeed = 5f;

    private float _burnRemaining;
    private float _burnTickTimer;
    private float _burnDamage;
    private float _slowRemaining;
    private float _staggerRemaining;
    private float _knockbackRemaining;
    private Vector2 _knockbackVelocity;

    public float MoveSpeedMultiplier => _slowRemaining > 0f ? _slowMoveSpeedMultiplier : 1f;
    public bool IsStaggered => _staggerRemaining > 0f;
    public Vector2 KnockbackVelocity => _knockbackRemaining > 0f ? _knockbackVelocity : Vector2.zero;

    public void ApplyElementEffect(ReactionElement element, float bulletDamage, Vector2 bulletDirection)
    {
        switch (element)
        {
            case ReactionElement.Fire:
                float damage = bulletDamage * _burnDamageMultiplier;
                if (_burnRemaining > 0f)
                    _burnDamage = Mathf.Max(_burnDamage, damage);
                else
                {
                    _burnDamage = damage;
                    _burnTickTimer = 0f;
                }
                _burnRemaining = _burnDuration;
                break;
            case ReactionElement.Water:
                _slowRemaining = _slowDuration;
                break;
            case ReactionElement.Earth:
                _staggerRemaining = _staggerDuration;
                break;
            case ReactionElement.Wind:
                _knockbackVelocity = bulletDirection.normalized * _knockbackSpeed;
                _knockbackRemaining = _knockbackDuration;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(element), element, null);
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        if (!(_burnRemaining <= 0f))
        {
            _burnTickTimer += Mathf.Min(deltaTime, _burnRemaining);
            _burnRemaining = Mathf.Max(_burnRemaining - deltaTime, 0f);
            while (_burnTickTimer >= _burnInterval)
            {
                _burnTickTimer -= _burnInterval;
                _enemyHealth.TakeDamage(_burnDamage);
            }
        }

        _slowRemaining = Mathf.Max(_slowRemaining - deltaTime, 0f);
        _staggerRemaining = Mathf.Max(_staggerRemaining - deltaTime, 0f);
        _knockbackRemaining = Mathf.Max(_knockbackRemaining - deltaTime, 0f);
    }
}