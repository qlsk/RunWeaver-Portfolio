using UnityEngine;

/// <summary>
/// 선택한 노드를 표시하는 원(NodeGuideCircle)을 선택 노드로 부드럽게 이동시킨다.
/// 출발할 때 천천히 빨라지고 도착할 때 천천히 멈추며(SmoothStep), 크기도 대상 노드에 맞춰 함께 바뀐다.
/// 이동 중에 대상이 바뀌면 현재 위치에서 새 대상으로 다시 출발한다.
/// 게임이 일시정지(timeScale 0)여도 움직이도록 unscaled 시간을 사용한다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class SkillSelectionCursor : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float _moveDuration = 0.25f;

    [Tooltip("대상 노드 크기에 곱할 배율. 1이면 노드와 같은 크기")]
    [SerializeField, Min(0.1f)] private float _sizeMultiplier = 1.3f;

    private RectTransform _rectTransform;
    private RectTransform _target;
    private Vector3 _startPosition;
    private Vector2 _startSize;
    private float _elapsed;
    private bool _isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        // 도착한 뒤에도 대상의 위치와 크기(선택 시 확대 등)를 계속 따라간다.
        _elapsed = Mathf.Min(_elapsed + Time.unscaledDeltaTime, _moveDuration);
        float eased = Mathf.SmoothStep(0f, 1f, _elapsed / _moveDuration);

        _rectTransform.position = Vector3.Lerp(_startPosition, _target.position, eased);
        _rectTransform.sizeDelta = Vector2.Lerp(_startSize, GetTargetSize(), eased);
    }

    /// <summary>
    /// 대상 노드로 이동을 시작한다. instant가 true면 이동 없이 바로 옮긴다.
    /// </summary>
    public void MoveTo(RectTransform target, bool instant = false)
    {
        Initialize();
        if (target == null)
        {
            return;
        }

        _target = target;
        _startPosition = _rectTransform.position;
        _startSize = _rectTransform.sizeDelta;
        _elapsed = instant ? _moveDuration : 0f;
        LateUpdate();
    }

    private Vector2 GetTargetSize()
    {
        // 대상과 커서의 배율(Scale)이 달라도 화면에서 같은 크기가 되도록 월드 크기로 환산한다.
        Vector3 targetScale = _target.lossyScale;
        Vector3 cursorScale = _rectTransform.lossyScale;
        Vector2 worldSize = new(_target.rect.width * targetScale.x, _target.rect.height * targetScale.y);

        float width = cursorScale.x != 0f ? worldSize.x / cursorScale.x : worldSize.x;
        float height = cursorScale.y != 0f ? worldSize.y / cursorScale.y : worldSize.y;
        return new Vector2(width, height) * _sizeMultiplier;
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        _rectTransform = (RectTransform)transform;
    }
}
