using UnityEngine;

/// 애니메이션 클립의 종료 이벤트를 받아 일회성 이펙트를 제거한다.
[RequireComponent(typeof(Animator))]
public class AnimationEffectLifetime : MonoBehaviour
{
    public void OnAnimationFinished()
    {
        Destroy(gameObject);
    }
}
