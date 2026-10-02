using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    private Rigidbody2D _enemyRigidbody2D;
    private SpriteRenderer _enemySpriteRenderer;
    private void Start()
    {
        _enemyRigidbody2D = GetComponent<Rigidbody2D>();
        _enemySpriteRenderer = GetComponent<SpriteRenderer>();
    }
    private void Update()
    {
        _enemySpriteRenderer.flipX = _enemyRigidbody2D.linearVelocityX > 0 ? true : false;
    }
}
