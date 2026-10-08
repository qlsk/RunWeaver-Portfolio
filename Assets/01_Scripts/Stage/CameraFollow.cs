using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 지정한 대상(Target)을 일정 오프셋 간격으로 부드럽게 따라가는 카메라 컴포넌트.
/// Main Camera 오브젝트에 부착하여 사용합니다.
/// </summary>
[DefaultExecutionOrder(10000)]
public class CameraFollow : MonoBehaviour
{
    [Header("추적 대상")]
    [Tooltip("따라갈 대상. 비어 있으면 Player 태그로 자동 탐색합니다.")]
    [SerializeField, FormerlySerializedAs("target")] private Transform _target;

    [Header("이동 설정")]
    [Tooltip("카메라 추적의 부드러움 정도 (값이 클수록 딱딱, 작을수록 부드럽게 따라감)")]
    [SerializeField, FormerlySerializedAs("smoothSpeed")] private float _smoothSpeed = 8f;

    [Header("오프셋")]
    [Tooltip("카메라 위치 오프셋 (z값은 2D에서 카메라 거리이므로 건드리지 않는 것을 권장)")]
    [SerializeField, FormerlySerializedAs("offset")] private Vector3 _offset = new Vector3(0f, 0f, -10f);

    [Header("마우스 방향 이동")]
    [SerializeField] private bool _mouseLookAhead;
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    [SerializeField, Range(0f, 0.5f)] private float _mouseLookAheadRatio = 0.15f;
    [SerializeField, Min(0f)] private float _mouseLookAheadDistance = 1.5f;

    [Header("배경 경계")]
    [SerializeField] private Renderer _backgroundRenderer;

    [SerializeField] private Transform _backgroundRoot;

    private Camera _camera;
    private float _defaultOrthographicSize;
    private Renderer[] _backgroundRenderers;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
            _camera = GetComponentInChildren<Camera>();

        if (_camera != null)
            _defaultOrthographicSize = _camera.orthographicSize;

        if (_backgroundRoot != null)
            _backgroundRenderers = _backgroundRoot.GetComponentsInChildren<Renderer>();

        if (_camera == null)
            Debug.LogWarning("[CameraFollow] 이 오브젝트 또는 자식에서 Camera를 찾지 못했습니다.", this);
    }

    private void Start()
    {
        // target이 인스펙터에서 할당되지 않았다면 Player 태그로 자동 탐색
        if (_target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _target = player.transform;
                Debug.Log("[CameraFollow] Player 태그 오브젝트를 자동으로 추적 대상으로 설정했습니다.");
            }
            else
            {
                Debug.LogWarning("[CameraFollow] Player 태그를 가진 오브젝트를 찾을 수 없습니다. 인스펙터에서 Target을 직접 지정해 주세요.");
            }
        }

        if (_playerInputHandler == null && _target != null)
        {
            _playerInputHandler = _target.GetComponentInParent<PlayerInputHandler>();
            if (_playerInputHandler == null)
                _playerInputHandler = _target.GetComponentInChildren<PlayerInputHandler>();
        }

        transform.position = ClampToBackground(transform.position);
    }

    private void LateUpdate()
    {
        Vector3 desiredPosition = _target != null
            ? ClampToBackground(_target.position + _offset + GetMouseLookAheadOffset())
            : transform.position;
        // Lerp로 부드럽게 이동 (smoothSpeed * deltaTime을 사용하여 프레임레이트 독립)
        Vector3 nextPosition = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.deltaTime);
        // 시작 위치나 화면 비율이 바뀌어도 보간 중 배경 밖이 보이지 않도록 최종 위치도 제한한다.
        transform.position = ClampToBackground(nextPosition);
    }

    private Vector3 GetMouseLookAheadOffset()
    {
        if (!_mouseLookAhead || _playerInputHandler == null || _target == null || !Application.isFocused)
            return Vector3.zero;

        // PlayerInputHandler가 Update에서 갱신한 월드 좌표를 LateUpdate에서 사용한다.
        Vector2 direction = _playerInputHandler.MousePosition - (Vector2)_target.position;
        Vector2 offset = Vector2.ClampMagnitude(direction * _mouseLookAheadRatio, _mouseLookAheadDistance);
        return new Vector3(offset.x, offset.y, 0f);
    }

    private Vector3 ClampToBackground(Vector3 position)
    {
        if (_camera == null || !_camera.orthographic || !TryGetBackgroundBounds(out Bounds bounds))
            return position;

        float aspect = _camera.aspect;
        if (bounds.size.x <= 0f || bounds.size.y <= 0f || aspect <= 0f)
            return position;

        // 초기 크기를 기준으로 계산하여 화면 비율이 바뀌면 줄어든 크기도 복원한다.
        float maxSize = Mathf.Min(bounds.extents.y, bounds.extents.x / aspect);
        _camera.orthographicSize = Mathf.Min(_defaultOrthographicSize, maxSize);

        float halfHeight = _camera.orthographicSize;
        float halfWidth = halfHeight * aspect;
        float minX = bounds.min.x + halfWidth;
        float maxX = bounds.max.x - halfWidth;
        float minY = bounds.min.y + halfHeight;
        float maxY = bounds.max.y - halfHeight;

        // 부모 Rig를 움직일 때도 자식 카메라의 실제 화면 중심을 제한한다.
        Vector3 cameraOffset = _camera.transform.position - transform.position;
        position.x = (minX < maxX ? Mathf.Clamp(position.x + cameraOffset.x, minX, maxX) : bounds.center.x) - cameraOffset.x;
        position.y = (minY < maxY ? Mathf.Clamp(position.y + cameraOffset.y, minY, maxY) : bounds.center.y) - cameraOffset.y;
        return position;
    }

    public bool TryGetBackgroundBounds(out Bounds bounds)
    {
        bounds = default;
        if (_backgroundRoot == null)
        {
            if (_backgroundRenderer == null) return false;
            bounds = _backgroundRenderer.bounds;
            return true;
        }

        if (_backgroundRenderers == null) return false;
        bool found = false;
        foreach (Renderer background in _backgroundRenderers)
        {
            if (background == null || !background.enabled || !background.gameObject.activeInHierarchy)
                continue;

            if (found) bounds.Encapsulate(background.bounds);
            else bounds = background.bounds;
            found = true;
        }
        return found;
    }
}
