/// <summary>
/// 한 게임의 스킬, 연구 진행과 재화 소유 상태를 묶는 객체
/// </summary>
public class PlayerProgression
{
    private readonly SkillTree _skillTree;
    private readonly ResearchTree _researchTree;
    private readonly PlayerHoldings _holdings;

    public SkillTree SkillTree => _skillTree;
    public ResearchTree ResearchTree => _researchTree;
    public PlayerHoldings Holdings => _holdings;

    public PlayerProgression(GameDefinitionSet definitions)
    {
        _skillTree = new SkillTree(definitions.SkillNodes);
        _researchTree = new ResearchTree(definitions.Researches);
        // TODO: PlayerHoldings 생성 시 로드된 연구 재료 세이브 데이터 주입
        _holdings = new PlayerHoldings(definitions.Resources);
    }
}
