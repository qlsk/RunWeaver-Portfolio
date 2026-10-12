using UnityEngine;

/// <summary>
/// 켜질 때 애니메이터의 반복 상태를 무작위 지점부터 재생한다.
/// 같은 이펙트가 여러 개 겹쳐도 똑같이 움직이지 않게 한다. (예전 SpriteFramePlayer의 무작위 시작)
/// </summary>
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class AnimatorRandomStart : MonoBehaviour
{
    [Tooltip("무작위 지점부터 재생할 상태 이름 (기본 레이어)")]
    [SerializeField] private string _stateName = "Loop";

    private void OnEnable()
    {
        GetComponent<Animator>().Play(_stateName, 0, Random.value);
    }
}
