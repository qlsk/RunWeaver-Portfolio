using UnityEngine;

public class PlayerBulletPosition : MonoBehaviour
{
    [SerializeField] private GameObject _playerBulletPosition;
    private PlayerInputHandler _playerInputHandler;
    private Vector3 _origin;

    private void Start()
    {
        _playerInputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        _origin = transform.position;
        Vector2 mousePosition = _playerInputHandler.MousePosition;
        _playerBulletPosition.transform.position =
            (Vector2)_origin + (mousePosition - (Vector2)transform.position).normalized;
    }
}