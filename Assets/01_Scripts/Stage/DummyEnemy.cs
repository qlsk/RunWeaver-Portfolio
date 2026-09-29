using System;
using UnityEngine;

public class DummyEnemy : MonoBehaviour
{
    // DummyEnemy 자신을 넘겨주는 이벤트 선언
    public static event Action<DummyEnemy> OnEnemyDied;

    [Header("몬스터 등급")]
    [SerializeField] private bool isElite = false;
    public bool IsElite => isElite;

    private void OnMouseDown()
    {
        Die();
    }

    public void Die()
    {
        OnEnemyDied?.Invoke(this); // 자기 자신(this)을 이벤트 매개변수로 전달
        Destroy(gameObject);
    }
}
