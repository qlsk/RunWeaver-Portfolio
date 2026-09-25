public class PlayerHoldings
{
    // 원정에서 획득한 연구 재료를 별도로 관리함: 플레이어 사망 시 원정에서 획득한 연구 재료만을 드롭하기 위함
    private Wallet _expeditionWallet;
    private readonly ResourceInventory _storedResources;
    private ResourceInventory _expeditionResources;

    public Wallet ExpeditionWallet => _expeditionWallet;
    public ResourceInventory StoredResources => _storedResources;
    public ResourceInventory ExpeditionResources => _expeditionResources;
}
