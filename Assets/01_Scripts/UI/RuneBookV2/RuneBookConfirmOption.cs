using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 확인 창의 버튼 하나. (확정 인장, 취소 글씨) 마우스를 올리면 고르고, 누르면 실행한다.
/// </summary>
[DisallowMultipleComponent]
public class RuneBookConfirmOption : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private RuneBookConfirmDialog _dialog;
    [Tooltip("0 : 확정, 1 : 취소")]
    [SerializeField] private int _index;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_dialog != null)
        {
            _dialog.Select(_index);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_dialog != null && eventData.button == PointerEventData.InputButton.Left)
        {
            _dialog.Submit(_index);
        }
    }
}
