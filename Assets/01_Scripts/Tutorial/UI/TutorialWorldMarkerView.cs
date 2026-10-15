using TMPro;
using UnityEngine;

public class TutorialWorldMarkerView : MonoBehaviour
{
    private const string LabelName = "Label";
    private const string EdgeArrowName = "EdgeArrow";

    [SerializeField] private RectTransform _root;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Vector3 _worldOffset = new(0f, 3f, 0f);
    [SerializeField][Min(0f)] private float _edgePadding = 80f;

    private GameObject _current;
    private RectTransform _currentRect;
    private GameObject _edgeArrow;
    private TutorialMarkerTarget _targetKind;
    private Transform _target;

    public void Show(TutorialStepSO step)
    {
        Hide();

        _targetKind = step.MarkerTarget;
        _current = Instantiate(step.OverlayPrefab, _root);
        _currentRect = _current.transform as RectTransform;

        Transform label = _current.transform.Find(LabelName);
        if (label != null && label.TryGetComponent(out TMP_Text labelText))
            labelText.text = step.OverlayText;

        Transform edgeArrow = _current.transform.Find(EdgeArrowName);
        _edgeArrow = edgeArrow != null ? edgeArrow.gameObject : null;

        _current.SetActive(false);
    }

    public void Hide()
    {
        _target = null;

        if (_current == null)
            return;

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

        Vector3 screen = ProjectToScreen(worldCamera, _target.position + _worldOffset, out bool isBehind);
        bool isOnScreen = PlaceMarker(screen, isBehind);
        UpdateEdgeArrow(screen, isOnScreen);
    }

    private static Vector3 ProjectToScreen(Camera worldCamera, Vector3 worldPosition, out bool isBehind)
    {
        Vector3 screen = worldCamera.WorldToScreenPoint(worldPosition);
        isBehind = screen.z < 0f;
        if (isBehind)
        {
            screen.x = Screen.width - screen.x;
            screen.y = Screen.height - screen.y;
        }

        return screen;
    }

    private bool PlaceMarker(Vector3 screen, bool isBehind)
    {
        float minX = _edgePadding;
        float maxX = Screen.width - _edgePadding;
        float minY = _edgePadding;
        float maxY = Screen.height - _edgePadding;
        bool isOnScreen = !isBehind && screen.x >= minX && screen.x <= maxX && screen.y >= minY && screen.y <= maxY;

        Vector2 clamped = new(Mathf.Clamp(screen.x, minX, maxX), Mathf.Clamp(screen.y, minY, maxY));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, clamped, _canvas.GetUiCamera(), out Vector2 local);
        _currentRect.anchoredPosition = local;
        return isOnScreen;
    }

    private void UpdateEdgeArrow(Vector3 screen, bool isOnScreen)
    {
        if (_edgeArrow == null)
            return;

        _edgeArrow.SetActive(!isOnScreen);
        if (isOnScreen)
            return;

        Vector2 direction = new Vector2(screen.x, screen.y) - new Vector2(Screen.width, Screen.height) * 0.5f;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _edgeArrow.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private Transform ResolveTarget()
    {
        switch (_targetKind)
        {
            case TutorialMarkerTarget.StartStatue:
                var statue = FindAnyObjectByType<ExpeditionStatue>();
                return statue != null ? statue.transform : null;
            case TutorialMarkerTarget.Portal:
                var portal = FindAnyObjectByType<Portal>();
                return portal != null ? portal.transform : null;
            default:
                return null;
        }
    }
}