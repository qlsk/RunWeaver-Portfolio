using UnityEngine;

public class PlayerArm : MonoBehaviour
{
    private PlayerInputHandler _playerInputHandler;
    private SpriteRenderer _armSpriteRenderer;
    [SerializeField] private GameObject _arm;
    private Vector3 _origin;
    [SerializeField] private float _offsetY;
    [SerializeField] private float _offsetX;

    private void Start()
    {
        _playerInputHandler = GetComponent<PlayerInputHandler>();
        _armSpriteRenderer = _arm.GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        _origin = gameObject.transform.position;

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(_playerInputHandler.MousePosition);

        Vector2 direction = mousePosition - (Vector2)_origin;

        _arm.SetActive(_playerInputHandler.MoveDirection != Vector2.zero);

        if (_playerInputHandler.MoveDirection.x > 0f)
        {
            _arm.transform.position = _origin + new Vector3(_offsetX, _offsetY, 0f);
            _armSpriteRenderer.flipX = true;
            _arm.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            if (mousePosition.x > transform.position.x)
            {
                _armSpriteRenderer.flipY = false;
                _arm.transform.position = _origin + new Vector3(_offsetX, _offsetY, 0f);
            }
            else
            {
                _armSpriteRenderer.flipY = true;
                _arm.transform.position = _origin + new Vector3(-_offsetX, _offsetY, 0f);
            }
        }
        else if (_playerInputHandler.MoveDirection.x < 0f)
        {
            _armSpriteRenderer.flipX = false;
            _arm.transform.rotation =
                Quaternion.Euler(0f, 0f, 180 + Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            if (mousePosition.x > transform.position.x)
            {
                _armSpriteRenderer.flipY = true;
                _arm.transform.position = _origin + new Vector3(_offsetX, _offsetY, 0f);
            }
            else
            {
                _armSpriteRenderer.flipY = false;
                _arm.transform.position = _origin + new Vector3(-_offsetX, _offsetY, 0f);
            }
        }
    }
}