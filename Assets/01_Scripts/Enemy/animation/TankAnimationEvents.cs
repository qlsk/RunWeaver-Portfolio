using UnityEngine;

// Animator가 자식 오브젝트에 있으므로 루트의 공격 담당자에게 이벤트를 전달한다.
public class TankAnimationEvents : MonoBehaviour
{
    public EnemyAnimation Owner { get; set; }
    public void TankImpact() { if (Owner != null) Owner.AnimationImpact(); }
    public void TankComplete() { if (Owner != null) Owner.AnimationComplete(); }
}
