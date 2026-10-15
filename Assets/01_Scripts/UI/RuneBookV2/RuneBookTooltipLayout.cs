using TMPro;
using UnityEngine;

/// <summary>
/// 새 룬북(RuneBookV2)의 팝업(툴팁) 크기와 위치를 맞춘다.
/// - 설명 글 길이에 맞춰 설명 칸 높이를 바꾸고, 그 아래 요소들을 같이 내리고, 팝업 높이를 맞춘다.
///   (자식들은 팝업 왼쪽 위 기준으로 배치되어 있어야 한다: 앵커 (0,1), 피벗 위쪽)
/// - 대상 칸의 오른쪽에 붙이고, 공간이 모자라면 왼쪽에 붙인다. 영역 밖으로 나가지 않게 위아래를 맞춘다.
/// 팝업의 피벗은 왼쪽 위 (0,1)이어야 한다.
/// 보이기/숨기기는 CanvasGroup 투명도로 한다. (칸이 꺼지는 도중에도 안전하게 숨길 수 있게 오브젝트는 켜 둔다)
/// </summary>
[DisallowMultipleComponent]
public class RuneBookTooltipLayout : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("높이 맞춤")]
    [SerializeField] private TMP_Text _flowText;
    [Tooltip("설명 아래에 있어 설명 높이만큼 같이 내려갈 요소")]
    [SerializeField] private RectTransform[] _followers;
    [SerializeField] private float _bottomPadding = 18f;

    [Header("위치")]
    [Tooltip("팝업이 벗어나지 않을 영역. 비우면 부모 영역")]
    [SerializeField] private RectTransform _bounds;
    [SerializeField] private float _gap = 20f;
    [Tooltip("대상 칸 위쪽에서 팝업 위쪽까지 올릴 거리")]
    [SerializeField] private float _raise = 36f;

    private readonly Vector3[] _corners = new Vector3[4];
    private float _baseTextHeight;
    private float[] _baseFollowerY;
    private bool _isInitialized;

    private RectTransform Rect => (RectTransform)transform;

    private void Awake()
    {
        Hide();
    }

    public void Show(RectTransform target)
    {
        Relayout();
        PlaceNextTo(target);
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool isVisible)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = isVisible ? 1f : 0f;
        }
        else if (gameObject.activeSelf != isVisible)
        {
            gameObject.SetActive(isVisible);
        }
    }

    public void Relayout()
    {
        Initialize();
        if (_flowText == null)
        {
            return;
        }

        RectTransform textRect = _flowText.rectTransform;
        float width = textRect.rect.width;
        float height = Mathf.Ceil(_flowText.GetPreferredValues(_flowText.text, width, 0f).y);
        textRect.sizeDelta = new Vector2(textRect.sizeDelta.x, height);
        float delta = height - _baseTextHeight;

        float bottom = -textRect.anchoredPosition.y + height;
        if (_followers != null)
        {
            for (int i = 0; i < _followers.Length; i++)
            {
                RectTransform follower = _followers[i];
                if (follower == null)
                {
                    continue;
                }

                follower.anchoredPosition = new Vector2(follower.anchoredPosition.x, _baseFollowerY[i] - delta);
                if (follower.gameObject.activeSelf)
                {
                    float followerBottom = -follower.anchoredPosition.y + follower.rect.height * follower.pivot.y;
                    bottom = Mathf.Max(bottom, followerBottom);
                }
            }
        }

        Rect.sizeDelta = new Vector2(Rect.sizeDelta.x, bottom + _bottomPadding);
    }

    public void PlaceNextTo(RectTransform target)
    {
        RectTransform parent = Rect.parent as RectTransform;
        if (target == null || parent == null)
        {
            return;
        }

        target.GetWorldCorners(_corners);
        Vector3 topLeft = parent.InverseTransformPoint(_corners[1]);
        Vector3 topRight = parent.InverseTransformPoint(_corners[2]);

        RectTransform boundsRect = _bounds != null ? _bounds : parent;
        boundsRect.GetWorldCorners(_corners);
        Vector3 boundsMin = parent.InverseTransformPoint(_corners[0]);
        Vector3 boundsMax = parent.InverseTransformPoint(_corners[2]);

        float width = Rect.rect.width;
        float height = Rect.rect.height;

        float x = topRight.x + _gap;
        if (x + width > boundsMax.x)
        {
            x = topLeft.x - _gap - width;
        }

        float y = Mathf.Min(topLeft.y + _raise, boundsMax.y);
        if (y - height < boundsMin.y)
        {
            y = boundsMin.y + height;
        }

        Rect.localPosition = new Vector3(x, y, 0f);
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        if (_flowText != null)
        {
            _baseTextHeight = _flowText.rectTransform.sizeDelta.y;
        }

        int count = _followers?.Length ?? 0;
        _baseFollowerY = new float[count];
        for (int i = 0; i < count; i++)
        {
            _baseFollowerY[i] = _followers[i] != null ? _followers[i].anchoredPosition.y : 0f;
        }
    }
}
