using System;

/// <summary>
/// 현재 사용할 룬 효과 로드아웃의 생성·교체·폐기를 관리한다.
/// GameSession이 지정한 순서에 따라 초기화되며,
/// 진행 상태가 교체되면 새 연구 트리에 연결된 로드아웃을 생성하고 교체를 알린다.
/// </summary>
public class RuneEffectLoadoutManager : Singleton<RuneEffectLoadoutManager>
{
    private RuneEffectDatabase _runeEffectDatabase;

    public RuneEffectLoadout RuneEffectLoadout { get; private set; }
    public bool IsInitialized { get; private set; }

    public event Action LoadoutReplaced;

    public void Initialize(RuneEffectDatabase runeEffectDatabase)
    {
        if (IsInitialized)
            throw new InvalidOperationException("RuneEffectLoadoutManager가 이미 초기화 되었습니다.");
        _runeEffectDatabase = runeEffectDatabase;
        RuneEffectLoadout = new RuneEffectLoadout(_runeEffectDatabase);
        IsInitialized = true;
        LoadoutReplaced?.Invoke();
    }

    public void InitializeLoadout()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("RuneEffectLoadoutManager가 초기화되지 않았습니다.");
        RuneEffectLoadout = new RuneEffectLoadout(_runeEffectDatabase);
        LoadoutReplaced?.Invoke();
    }
}