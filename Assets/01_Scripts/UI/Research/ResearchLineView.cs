using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 연구 노드 사이 연결선. 앞 단계를 연구했으면 켜진 선으로 바꾼다.
/// 이미지를 지정하면 이미지를 바꾸고, 색은 항상 켜짐/꺼짐 색으로 바꾼다.
/// </summary>
[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class ResearchLineView : MonoBehaviour
{
    [SerializeField] private Sprite _onSprite;
    [SerializeField] private Sprite _offSprite;
    [SerializeField] private Color _onColor = Color.white;
    [SerializeField] private Color _offColor = Color.white;

    public void SetOn(bool isOn)
    {
        Image image = GetComponent<Image>();
        Sprite sprite = isOn ? _onSprite : _offSprite;
        if (sprite != null)
        {
            image.sprite = sprite;
        }

        image.color = isOn ? _onColor : _offColor;
    }
}
