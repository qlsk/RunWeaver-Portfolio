/// <summary>
/// 룬 워드 공개 단계. 모든 룬 워드는 처음부터 Listed(이름, 룬 워드 공개)이며,
/// 실제로 장착하면 Detailed(효과 설명 공개)가 된다. (Hidden 단계는 연구 기획 폐기로 제거)
/// </summary>
public enum RuneWordRevealLevel
{
    Listed,
    Detailed
}
