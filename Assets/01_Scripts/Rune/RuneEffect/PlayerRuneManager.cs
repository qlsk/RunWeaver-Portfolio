using System;

/// <summary>
/// 플레이어의 룬 상태를 관리한다.
/// - 룬 효과 로드아웃(RuneEffectLoadout) : 장착한 룬 효과
/// - 룬 문자 인벤토리(RuneStoneInventory) : 보유한 룬 문자
/// GameSession이 지정한 순서에 따라 초기화되며, 씬이 바뀌어도 유지된다. (Singleton)
/// 새 게임을 시작하면 로드아웃을 새로 만들고 인벤토리를 비운 뒤 교체를 알린다.
/// 다른 스크립트는 PlayerRuneManager.Instance를 통해 로드아웃과 인벤토리에 접근한다.
/// </summary>
public class PlayerRuneManager : Singleton<PlayerRuneManager>
{
    private RuneEffectDatabase _runeEffectDatabase;

    public RuneEffectLoadout RuneEffectLoadout { get; private set; }
    public RuneStoneInventory RuneStoneInventory { get; private set; }
    public bool IsInitialized { get; private set; }

    /// <summary>로드아웃이 새로 만들어졌을 때 (초기화, 새 게임). 인벤토리는 같은 인스턴스를 비우기만 한다.</summary>
    public event Action LoadoutReplaced;

    public void Initialize(RuneEffectDatabase runeEffectDatabase)
    {
        if (IsInitialized)
            throw new InvalidOperationException("PlayerRuneManager가 이미 초기화 되었습니다.");
        _runeEffectDatabase = runeEffectDatabase;
        RuneEffectLoadout = new RuneEffectLoadout(_runeEffectDatabase);
        RuneStoneInventory = new RuneStoneInventory();
        IsInitialized = true;
        LoadoutReplaced?.Invoke();
    }

    /// <summary>새 게임 : 로드아웃을 새로 만들고 보유 룬 문자를 비운다.</summary>
    public void ResetForNewGame()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("PlayerRuneManager가 초기화되지 않았습니다.");
        RuneEffectLoadout = new RuneEffectLoadout(_runeEffectDatabase);
        RuneStoneInventory.Clear();
        LoadoutReplaced?.Invoke();
    }
}
