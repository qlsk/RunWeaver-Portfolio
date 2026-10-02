using UnityEngine;

[DefaultExecutionOrder(200)]
public class PlayerBulletPosition : MonoBehaviour
{
    [SerializeField] private GameObject _playerBulletPosition;
    [SerializeField] private SpriteRenderer _armSpriteRenderer;
    private Vector3 _muzzleLocalPosition;
    private PlayerInputHandler _playerInputHandler;
    private Vector3 _origin;
    [SerializeField] private float _offsetScale;
    [SerializeField] private float _originOffset;

    private void Awake()
    {
        _playerInputHandler = GetComponent<PlayerInputHandler>();

        if (_playerBulletPosition == null)
        {
            enabled = false;
            return;
        }

        if (_armSpriteRenderer == null && _playerBulletPosition.transform.parent != null)
        {
            _armSpriteRenderer = _playerBulletPosition.transform.parent.GetComponent<SpriteRenderer>();
        }

        if (_armSpriteRenderer != null && _armSpriteRenderer.gameObject != gameObject)
        {
            _muzzleLocalPosition = _armSpriteRenderer.transform.InverseTransformPoint(
                _playerBulletPosition.transform.position);

            if (_armSpriteRenderer.flipX) _muzzleLocalPosition.x *= -1f;
            if (_armSpriteRenderer.flipY) _muzzleLocalPosition.y *= -1f;
        }
        else
        {
            _armSpriteRenderer = null;
        }
    }

    private void Update()
    {
        Vector2 direction = _playerInputHandler.MousePosition - (Vector2)transform.position;
        _playerBulletPosition.transform.rotation =
            Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        if (_armSpriteRenderer != null)
        {
            Vector3 muzzlePosition = _muzzleLocalPosition;
            if (_armSpriteRenderer.flipX) muzzlePosition.x *= -1f;
            if (_armSpriteRenderer.flipY) muzzlePosition.y *= -1f;
            _playerBulletPosition.transform.position =
                _armSpriteRenderer.transform.TransformPoint(muzzlePosition);
            return;
        }

        _origin = transform.position;
        Vector2 mousePosition = _playerInputHandler.MousePosition;
        _playerBulletPosition.transform.position =
            (Vector2)_origin + new Vector2(0, _originOffset) +
            (mousePosition - (Vector2)transform.position).normalized * _offsetScale;
    }
}
