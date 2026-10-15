using UnityEngine;

/// <summary>
/// 원정 중 지갑(GameSession → PlayerProgression → PlayerHoldings → ExpeditionWallet)의 골드를 HUD에 표시한다.
/// 진행 데이터 교체(ProgressReplaced), 원정 시작/종료(ExpeditionChanged), 잔액 변경(BalanceChanged)을 따라간다.
/// 원정 중이 아니면(마을) 원정 시작 골드(RunFlow.StartingGold)를, GameSession이 없으면 0을 표시한다.
/// </summary>
[DisallowMultipleComponent]
public class GoldHudBinder : MonoBehaviour
{
    [SerializeField] private GoldHudView _view;

    private GameSession _session;
    private PlayerHoldings _holdings;
    private Wallet _wallet;

    private void Start()
    {
        _session = GameSession.Instance;
        if (_session == null)
        {
            Debug.LogWarning("[GoldHudBinder] GameSession이 없어 골드를 표시하지 않습니다. (BootStrap 씬을 거치지 않은 경우)", this);
            ShowAmount(0);
            return;
        }

        _session.ProgressReplaced += HandleProgressReplaced;
        BindHoldings(_session.IsReady ? _session.Current?.Holdings : null);
    }

    private void OnDestroy()
    {
        if (_session != null)
        {
            _session.ProgressReplaced -= HandleProgressReplaced;
        }

        BindHoldings(null);
    }

    private void HandleProgressReplaced(PlayerProgression progression)
    {
        BindHoldings(progression?.Holdings);
    }

    private void BindHoldings(PlayerHoldings holdings)
    {
        if (_holdings != null)
        {
            _holdings.ExpeditionChanged -= HandleExpeditionChanged;
        }

        _holdings = holdings;

        if (_holdings != null)
        {
            _holdings.ExpeditionChanged += HandleExpeditionChanged;
        }

        HandleExpeditionChanged();
    }

    private void HandleExpeditionChanged()
    {
        if (_wallet != null)
        {
            _wallet.BalanceChanged -= ShowAmount;
        }

        _wallet = _holdings?.ExpeditionWallet;

        if (_wallet != null)
        {
            _wallet.BalanceChanged += ShowAmount;
            ShowAmount(_wallet.Balance);
        }
        else
        {
            ShowAmount(_holdings != null ? RunFlow.StartingGold : 0);
        }
    }

    private void ShowAmount(int amount)
    {
        if (_view != null)
        {
            _view.SetAmount(amount);
        }
    }
}
