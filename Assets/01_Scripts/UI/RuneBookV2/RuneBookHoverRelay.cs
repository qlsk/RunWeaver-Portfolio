using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 마우스가 올라가고 나갈 때를 알리고, 올라가 있는 동안 지정한 오브젝트(빛 등)를 켠다.
/// 꺼질 때는 알리지 않고 상태만 지운다. (켜질 때 표시를 되돌린다)
/// 이 칸들을 쓰는 프레젠터는 칸과 같이 꺼지므로 자기 OnDisable에서 정리한다.
/// </summary>
[DisallowMultipleComponent]
public class RuneBookHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("마우스가 올라가 있는 동안 켤 오브젝트")]
    [SerializeField] private GameObject[] _hoverObjects;

    private bool _isHovered;

    public event Action<RuneBookHoverRelay> PointerEntered;
    public event Action<RuneBookHoverRelay> PointerExited;

    public RectTransform RectTransform => (RectTransform)transform;

    private void OnEnable()
    {
        SetHovered(false);
    }

    private void OnDisable()
    {
        _isHovered = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHovered(true);
        PointerEntered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHovered(false);
        PointerExited?.Invoke(this);
    }

    private void SetHovered(bool isHovered)
    {
        _isHovered = isHovered;
        if (_hoverObjects == null)
        {
            return;
        }

        foreach (GameObject hoverObject in _hoverObjects)
        {
            if (hoverObject != null)
            {
                hoverObject.SetActive(isHovered);
            }
        }
    }
}
