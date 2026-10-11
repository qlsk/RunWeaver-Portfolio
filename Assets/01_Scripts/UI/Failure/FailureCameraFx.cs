using UnityEngine;

/// <summary>
/// 실패 연출의 카메라 흔들림과 확대. RunFailureSequence가 실행 중에 Main Camera에 붙인다.
/// CameraFollow(실행 순서 10000)가 위치와 크기를 정한 뒤에 덧입힌다.
/// 게임 시간이 멈춰도 흔들리도록 실제 시간을 쓴다.
/// </summary>
[DefaultExecutionOrder(10001)]
[DisallowMultipleComponent]
public class FailureCameraFx : MonoBehaviour
{
    private const float ShakeFrequency = 38f;

    private Camera _camera;
    private Vector3 _appliedOffset;
    private float _baseSize;
    private float _appliedSize = -1f;

    /// <summary>화면 크기 배율. 1이면 그대로, 0.8이면 20% 확대</summary>
    public float Zoom { get; set; } = 1f;

    /// <summary>흔들림 세기 (월드 단위)</summary>
    public float Shake { get; set; }

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (_camera != null && _camera.orthographic)
        {
            // 다른 스크립트가 크기를 바꿨으면 그 값을 새 기준으로 삼는다.
            if (!Mathf.Approximately(_camera.orthographicSize, _appliedSize))
            {
                _baseSize = _camera.orthographicSize;
            }

            _appliedSize = _baseSize * Mathf.Max(0.05f, Zoom);
            _camera.orthographicSize = _appliedSize;
        }

        float time = Time.unscaledTime * ShakeFrequency;
        Vector3 offset = new Vector3(Mathf.PerlinNoise(time, 0.37f) - 0.5f, Mathf.PerlinNoise(0.71f, time) - 0.5f, 0f) * (2f * Shake);
        transform.localPosition += offset - _appliedOffset;
        _appliedOffset = offset;
    }

    /// <summary>흔들림과 확대를 되돌리고 컴포넌트를 없앤다.</summary>
    public void Remove()
    {
        transform.localPosition -= _appliedOffset;
        _appliedOffset = Vector3.zero;

        if (_camera != null && _appliedSize > 0f && Mathf.Approximately(_camera.orthographicSize, _appliedSize))
        {
            _camera.orthographicSize = _baseSize;
        }

        Destroy(this);
    }
}
