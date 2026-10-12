using UnityEngine;

/// <summary>
/// 정해진 시간이 지나면 스스로 사라진다. (한 번 재생하고 끝나는 이펙트 프리팹용. 예: 제단 파편)
/// 게임 시간으로 센다.
/// </summary>
[DisallowMultipleComponent]
public class FxAutoDestroy : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _lifetime = 1.5f;

    private void Start()
    {
        Destroy(gameObject, _lifetime);
    }
}
