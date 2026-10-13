using TMPro;
using UnityEngine;

/// <summary>월드의 대상(시작 석상, 포탈) 위에 안내 표식을 띄운다. 화면 밖이면 가장자리에 붙이고 화살표로 방향을 알린다.</summary>
public class TutorialWorldMarkerView : MonoBehaviour
{
    private const string LabelName = "Label";
    private const string EdgeArrowName = "EdgeArrow";

    [Tooltip("표식을 놓는 전체 화면 RectTransform")]
    [SerializeField] private RectTransform _root;
    [SerializeField] private Canvas _canvas;
    [Tooltip("대상 위치에서 위로 띄우는 월드 거리")]
    [SerializeField] private Vector3 _worldOffset = new(0f, 3f, 0f);
    [Tooltip("화면 가장자리에서 띄우는 거리(픽셀)")]
    [SerializeField, Min(0f)] private float _edgePadding = 80f;

    private GameObject _current;
    private RectTransform _currentRect;
    private GameObject _edgeArrow;
    private TutorialMarkerTarget _targetKind;
    private Transform _target;

    public void Show(TutorialStepSO step)
    {
        Hide();

        if (step.OverlayPrefab == null)
        {
            Debug.LogError("[TutorialWorldMarkerView] 월드 표식 프리팹이 지정되지 않았습니다.");
            return;
        }

        _targetKind = step.MarkerTarget;
        _current = Instantiate(step.OverlayPrefab, _root);
        _currentRect = _current.transform as RectTransform;

        Transform label = _current.transform.Find(LabelName);
        if (label != null && label.TryGetComponent(out TMP_Text labelText))
            labelText.text = step.OverlayText;

        Transform edgeArrow = _current.transform.Find(EdgeArrowName);
        _edgeArrow = edgeArrow != null ? edgeArrow.gameObject : null;

        // 대상을 찾을 때까지 숨긴다. 포탈처럼 나중에 생기는 대상이 있다.
        _current.SetActive(false);
    }

    public void Hide()
    {
        _target = null;

        if (_current == null)
            return;

        // Destroy는 프레임 끝에 실행되므로, 같은 프레임에 새 표식이 생겨도 겹치지 않게 먼저 끈다.
        _current.SetActive(false);
        Destroy(_current);
        _current = null;
        _currentRect = null;
        _edgeArrow = null;
    }

    private void LateUpdate()
    {
        if (_current == null || _currentRect == null)
            return;

        if (_target == null)
            _target = ResolveTarget();

        Camera worldCamera = Camera.main;
        if (_target == null || worldCamera == null)
        {
            _current.SetActive(false);
            return;
        }

        _current.SetActive(true);

        Vector3 screen = worldCamera.WorldToScreenPoint(_target.position + _worldOffset);
        bool isBehind = screen.z < 0f;
        if (isBehind)
        {
            // 카메라 뒤쪽 대상은 투영 좌표가 뒤집히므로 반대로 돌려 같은 방향 가장자리에 붙인다.
            screen.x = Screen.width - screen.x;
            screen.y = Screen.height - screen.y;
        }

        float minX = _edgePadding;
        float maxX = Screen.width - _edgePadding;
        float minY = _edgePadding;
        float maxY = Screen.height - _edgePadding;
        bool isOnScreen = !isBehind && screen.x >= minX && screen.x <= maxX && screen.y >= minY && screen.y <= maxY;

        Vector2 clamped = new(Mathf.Clamp(screen.x, minX, maxX), Mathf.Clamp(screen.y, minY, maxY));
        Camera uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, clamped, uiCamera, out Vector2 local);
        _currentRect.anchoredPosition = local;

        if (_edgeArrow == null)
            return;

        _edgeArrow.SetActive(!isOnScreen);
        if (!isOnScreen)
        {
            Vector2 direction = new Vector2(screen.x, screen.y) - new Vector2(Screen.width, Screen.height) * 0.5f;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _edgeArrow.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private Transform ResolveTarget()
    {
        switch (_targetKind)
        {
            case TutorialMarkerTarget.StartStatue:
                ExpeditionStatue statue = FindAnyObjectByType<ExpeditionStatue>();
                return statue != null ? statue.transform : null;
            case TutorialMarkerTarget.Portal:
                Portal portal = FindAnyObjectByType<Portal>();
                return portal != null ? portal.transform : null;
            default:
                return null;
        }
    }
}
