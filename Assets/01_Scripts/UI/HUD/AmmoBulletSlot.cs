using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bullet 프리팹 루트에 붙어 탄환 칸의 채움 이미지를 노출한다.
/// 바탕(Background)과 구분해 AmmoView가 Fill만 제어하도록 한다.
/// </summary>
[DisallowMultipleComponent]
public class AmmoBulletSlot : MonoBehaviour
{
    [SerializeField] private Image _fill;

    public Image Fill => _fill;
}
