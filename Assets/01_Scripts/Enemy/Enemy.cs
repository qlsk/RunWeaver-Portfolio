using UnityEngine;

public enum EnemyElementType
{
    None,
    Fire,
    Water,
    Earth,
    Wind
}

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyElementType _elementType;
    [SerializeField] private EnemyHealth _enemyHealth;
    [SerializeField] private Burn _burn;

    public Burn Burn => _burn;
    public EnemyElementType ElementType => _elementType;
    public ReactionElement? AttackElement => _elementType switch
    {
        EnemyElementType.Fire => ReactionElement.Fire,
        EnemyElementType.Water => ReactionElement.Water,
        EnemyElementType.Earth => ReactionElement.Earth,
        EnemyElementType.Wind => ReactionElement.Wind,
        _ => null
    };

    public void Start()
    {
        if (_enemyHealth == null) _enemyHealth = GetComponent<EnemyHealth>();
        if (_burn != null && _enemyHealth != null)
            _burn.OnBurnTriggered += _enemyHealth.TakeDamage;
    }

    private void OnDestroy()
    {
        if (_burn != null && _enemyHealth != null)
            _burn.OnBurnTriggered -= _enemyHealth.TakeDamage;
    }
}
