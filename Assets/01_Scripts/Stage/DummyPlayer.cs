using UnityEngine;

public class DummyPlayer : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private void Update()
    {
        // 간단한 2D 방향키 이동
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        Vector3 dir = new Vector3(moveX, moveY, 0).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
    }
}
