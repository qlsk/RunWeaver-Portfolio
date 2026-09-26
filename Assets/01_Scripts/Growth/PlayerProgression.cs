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
        Wallet expeditionWallet = new(definitions.Gold);
        ResourceInventory storedResources = new(definitions.Resources);
        ResourceInventory expeditionResources = new(definitions.Resources);

        _skillTree = new SkillTree(definitions.SkillNodes);
        _researchTree = new ResearchTree(definitions.Researches);
        _holdings = new PlayerHoldings(expeditionWallet, storedResources, expeditionResources);
    }
}
