using UnityEngine;

public class PlayerArm : MonoBehaviour
{
    [Header("참조")]
    private PlayerInputHandler _playerInputHandler;
    [SerializeField] private SpriteRenderer _playerRifle;
    [SerializeField] private SpriteRenderer _playerSupportArm;

    [Header("설정")]
    [Tooltip("기본 소총과 보조 팔 스프라이트가 왼쪽을 바라보면 활성화합니다.")][SerializeField] private bool _reverse = true;
    [SerializeField] private float _offsetY;
    [SerializeField] private float _offsetX;

    private Vector3 _rifleOrigin;
    private Vector3 _supportArmOrigin;

    private void Start()
    {
        _playerInputHandler = GetComponent<PlayerInputHandler>();
        _rifleOrigin = transform.InverseTransformPoint(_playerRifle.transform.position);
        _supportArmOrigin = _playerRifle.transform.InverseTransformPoint(_playerSupportArm.transform.position);
    }

    private void LateUpdate()
    {
        Vector2 direction = _playerInputHandler.MousePosition - (Vector2)transform.position;
        if (direction == Vector2.zero)
            return;

        // 이동 여부와 관계없이 몸통과 같은 마우스 방향을 바라본다.
        bool isFacingRight = direction.x > 0f;
        bool flipX = _reverse ? isFacingRight : !isFacingRight;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (!isFacingRight)
            angle -= 180f;

        Vector3 riflePosition = _rifleOrigin + new Vector3(_offsetX, _offsetY, 0f);
        if (isFacingRight)
            riflePosition.x = -riflePosition.x;

        _playerRifle.transform.position = transform.TransformPoint(riflePosition);
        _playerRifle.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        _playerRifle.flipX = flipX;
        _playerRifle.flipY = false;

        // SpriteRenderer의 반전은 자식 위치에 적용되지 않으므로 보조 팔 위치도 대칭 이동한다.
        Vector3 supportArmPosition = _supportArmOrigin;
        if (isFacingRight)
            supportArmPosition.x = -supportArmPosition.x;

        _playerSupportArm.transform.position = _playerRifle.transform.TransformPoint(supportArmPosition);
        _playerSupportArm.transform.rotation = _playerRifle.transform.rotation;
        _playerSupportArm.flipX = flipX;
        _playerSupportArm.flipY = false;
    }
}