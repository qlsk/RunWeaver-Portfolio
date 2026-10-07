using UnityEngine;

[DefaultExecutionOrder(100)]
public class PlayerArm : MonoBehaviour
{
    private const float BaseAngle = 4f;

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
        _supportArmOrigin = transform.InverseTransformPoint(_playerSupportArm.transform.position);

        Quaternion baseRotation = transform.rotation * Quaternion.Euler(0f, 0f, BaseAngle);
        _playerRifle.transform.SetPositionAndRotation(transform.TransformPoint(_rifleOrigin), baseRotation);
        _playerSupportArm.transform.SetPositionAndRotation(transform.TransformPoint(_supportArmOrigin), baseRotation);
    }

    private void LateUpdate()
    {
        Vector2 direction = transform.InverseTransformVector(
            _playerInputHandler.MousePosition - (Vector2)transform.position);
        if (direction == Vector2.zero)
            return;

        Vector3 movementOffset = Vector3.zero;
        if (_playerInputHandler.MoveDirection != Vector2.zero)
        {
            movementOffset = new Vector3(_offsetX, _offsetY, 0f);
        }
        // 이동 여부와 관계없이 몸통과 같은 마우스 방향을 바라본다.
        bool isFacingRight = direction.x > 0f;
        bool flipX = _reverse ? isFacingRight : !isFacingRight;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (!isFacingRight)
            angle -= 180f;

        // 좌우 대칭에서는 기본 기울기도 반전해야 같은 자세를 유지한다.
        float baseAngle = flipX ? -BaseAngle : BaseAngle;
        Quaternion armRotation = transform.rotation * Quaternion.Euler(0f, 0f, angle + baseAngle);

        ApplyArmPose(_playerRifle, _rifleOrigin + movementOffset, flipX, armRotation);
        ApplyArmPose(_playerSupportArm, _supportArmOrigin + movementOffset, flipX, armRotation);
    }

    private void ApplyArmPose(SpriteRenderer arm, Vector3 origin, bool flipX, Quaternion armRotation)
    {
        // 조준 회전은 각 팔의 피벗에만 적용하고, 위치는 좌우 반전할 때만 대칭 이동한다.
        if (flipX)
            origin.x = -origin.x;

        Vector3 position = transform.TransformPoint(origin);
        arm.transform.SetPositionAndRotation(position, armRotation);
        arm.flipX = flipX;
        arm.flipY = false;
    }
}
