using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 지정한 대상(Target)을 일정 오프셋 간격으로 부드럽게 따라가는 카메라 컴포넌트.
/// Main Camera 오브젝트에 부착하여 사용합니다.
/// </summary>
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
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        Vector3 desiredPosition = _target.position + _offset;
        // Lerp로 부드럽게 이동 (smoothSpeed * deltaTime을 사용하여 프레임레이트 독립)
        transform.position = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.deltaTime);
    }
}
