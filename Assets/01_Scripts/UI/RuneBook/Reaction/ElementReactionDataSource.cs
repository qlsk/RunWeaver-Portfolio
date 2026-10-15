using UnityEngine;

/// <summary>
/// 원소 반응 도감 데이터. 지금은 StaticElementReactionDataSource(코드에 담은 데이터)를 쓰고,
/// ElementalEffect DB가 생기면 이 클래스를 상속한 DB 소스로 교체한다.
/// </summary>
public abstract class ElementReactionDataSource : MonoBehaviour
{
    /// <summary>원소 하나만 맞혔을 때의 기본 효과</summary>
    public abstract ElementReactionData GetBaseEffect(ReactionElement element);

    /// <summary>두 원소의 반응. 순서는 상관없다.</summary>
    public abstract ElementReactionData GetReaction(ReactionElement a, ReactionElement b);
}
