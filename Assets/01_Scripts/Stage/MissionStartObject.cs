using System;
using UnityEngine;

/// <summary>
/// 스테이지 시작 오브젝트. 플레이어가 가까이 가서 F로 상호작용하면 미션(보스 스테이지는 보스전)이 시작된다.
///
/// - 미션 오브젝트가 있는 미션(거점 점령, 제단 방어, 영혼 수집)은 미션 오브젝트 위치에 표시 없이 만들어
///   미션 오브젝트 자체를 시작 오브젝트처럼 쓴다. (안내 문구만 보인다)
/// - 미션 오브젝트가 없는 미션(적 처치, 생존)과 보스 스테이지는 원형 표시와 함께 만든다.
/// - StageManager에 프리팹을 지정하지 않으면 Create로 코드에서 임시 오브젝트를 만든다.
/// 상호작용은 PlayerInteraction이 찾을 수 있도록 Interactable 레이어의 Collider2D가 필요하다.
/// </summary>
[DisallowMultipleComponent]
public class MissionStartObject : MonoBehaviour, IInteractable
{
    private const int MarkerTextureSize = 64;
    private const int MarkerSortingOrder = 1;

    private static Sprite s_markerSprite;

    [SerializeField] private string _interactionName = "시작";
    [Tooltip("InteractionGuideView가 F 안내를 띄울 위치. 비워두면 이 오브젝트를 쓴다")]
    [SerializeField] private Transform _interactionGuidePoint;
    [Tooltip("원형 표시. 미션 오브젝트 위에 놓일 때는 숨긴다")]
    [SerializeField] private GameObject _marker;

    private bool _isUsed;

    /// <summary>F로 상호작용했을 때 한 번 발생한다.</summary>
    public event Action Activated;

    public string InteractionName => _interactionName;
    public Transform InteractionGuidePoint => _interactionGuidePoint != null ? _interactionGuidePoint : transform;

    /// <summary>원형 표시를 보이거나 숨긴다. (미션 오브젝트 위에 놓일 때는 숨긴다)</summary>
    public void SetMarkerVisible(bool isVisible)
    {
        if (_marker != null)
        {
            _marker.SetActive(isVisible);
        }
    }

    public void Interact()
    {
        if (_isUsed)
        {
            return;
        }

        _isUsed = true;
        SafeEventNotifier.Notify(Activated);
    }

    /// <summary>
    /// 프리팹 없이 임시 시작 오브젝트를 만든다. (원형 표시 + 안내 위치)
    /// </summary>
    public static MissionStartObject Create(Vector3 position, int layer, float radius)
    {
        GameObject root = new("MissionStartObject");
        root.layer = layer;
        root.transform.position = position;

        CircleCollider2D trigger = root.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = radius;

        GameObject marker = new("Marker");
        marker.layer = layer;
        marker.transform.SetParent(root.transform, false);
        marker.transform.localScale = Vector3.one * radius * 2f;
        SpriteRenderer markerRenderer = marker.AddComponent<SpriteRenderer>();
        markerRenderer.sprite = GetMarkerSprite();
        markerRenderer.color = new Color(0.55f, 0.85f, 1f, 0.75f);
        markerRenderer.sortingOrder = MarkerSortingOrder;

        GameObject guidePoint = new("InteractionGuidePoint");
        guidePoint.layer = layer;
        guidePoint.transform.SetParent(root.transform, false);
        guidePoint.transform.localPosition = new Vector3(0f, radius + 0.6f, 0f);

        MissionStartObject startObject = root.AddComponent<MissionStartObject>();
        startObject._interactionGuidePoint = guidePoint.transform;
        startObject._marker = marker;
        return startObject;
    }

    /// <summary>가장자리가 진한 원형 스프라이트를 한 번만 만들어 재사용한다.</summary>
    private static Sprite GetMarkerSprite()
    {
        if (s_markerSprite != null)
        {
            return s_markerSprite;
        }

        Texture2D texture = new(MarkerTextureSize, MarkerTextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float center = (MarkerTextureSize - 1) * 0.5f;
        float outer = MarkerTextureSize * 0.5f;
        for (int y = 0; y < MarkerTextureSize; y++)
        {
            for (int x = 0; x < MarkerTextureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / outer;
                float alpha = distance > 1f ? 0f : distance > 0.85f ? 1f : 0.25f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        s_markerSprite = Sprite.Create(texture, new Rect(0f, 0f, MarkerTextureSize, MarkerTextureSize),
            new Vector2(0.5f, 0.5f), MarkerTextureSize);
        return s_markerSprite;
    }
}
