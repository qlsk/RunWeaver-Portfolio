using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyHealth _enemyHealth;
    [SerializeField] private Burn _burn;

    public Burn Burn => _burn;

    public void Start()
    {
        _burn.OnBurnTriggered += _enemyHealth.TakeDamage;
    }
}