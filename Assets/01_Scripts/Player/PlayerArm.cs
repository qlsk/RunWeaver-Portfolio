using UnityEngine;

public class PlayerArm : MonoBehaviour
{
    private PlayerInputHandler _playerInputHandler;
    private SpriteRenderer _armSpriteRenderer;
    [SerializeField] private GameObject _arm;
    private Vector3 _origin;
    [SerializeField] private float _offsetY;

    private void Start()
    {
        _playerInputHandler = GetComponent<PlayerInputHandler>();
        _armSpriteRenderer = _arm.GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        _origin = gameObject.transform.position;

        _arm.SetActive(_playerInputHandler.MoveDirection != Vector2.zero);

        if (_playerInputHandler.MoveDirection.x > 0f)
        {
            _armSpriteRenderer.flipX = true;
            _arm.transform.position = _origin + new Vector3(0f, _offsetY, 0f);
        }
        else if (_playerInputHandler.MoveDirection.x < 0f)
        {
            _armSpriteRenderer.flipX = false;
            _arm.transform.position = _origin + new Vector3(0f, _offsetY, 0f);
            ;
        }
    }
}