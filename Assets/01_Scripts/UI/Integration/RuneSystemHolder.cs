using System;
using UnityEngine;

/// <summary>
/// 한 판(원정) 동안 사용할 RuneBook과 RuneLoadout을 생성해서 보관한다.
/// 현재 이 둘을 생성하는 코드가 없어 UI 연동을 위해 임시로 만든 관리 컴포넌트이며,
/// 팀원 쪽에 정식 관리 코드가 생기면 그쪽으로 교체한다. (RuneBook, RuneLoadout 클래스 자체는 수정하지 않는다)
///
/// DB(RuneEffectDatabase, ResearchDatabase)와 GameSession이 준비되면 한 번 생성하고 Ready 이벤트를 보낸다.
/// 기획서 14-2에 따라 Element, Enhancement 카테고리는 처음부터 도감에 공개한다.
/// </summary>
[DisallowMultipleComponent]
public class RuneSystemHolder : MonoBehaviour
{
    [Tooltip("플레이어가 룬석을 줍는 인벤토리 (씬의 RuneStoneInventory)")]
    [SerializeField] private RuneStoneInventory _stoneInventory;

    [Tooltip("시작할 때 도감에 공개할 카테고리")]
    [SerializeField] private RuneEffectCategory[] _startRevealedCategories =
    {
        RuneEffectCategory.Element,
        RuneEffectCategory.Enhancement
    };

    private RuneBook _runeBook;
    private RuneLoadout _loadout;
    private bool _hasLoggedWaiting;

    /// <summary>RuneBook과 RuneLoadout 생성이 끝났을 때 발생한다.</summary>
    public event Action Ready;

    public bool IsReady => _loadout != null;
    public RuneBook RuneBook => _runeBook;
    public RuneLoadout Loadout => _loadout;
    public RuneStoneInventory StoneInventory => _stoneInventory;

    private void Awake()
    {
        if (_stoneInventory == null)
        {
            _stoneInventory = FindAnyObjectByType<RuneStoneInventory>();
        }
    }

    private void Update()
    {
        if (IsReady)
        {
            enabled = false;
            return;
        }

        if (!CanCreate())
        {
            if (!_hasLoggedWaiting)
            {
                _hasLoggedWaiting = true;
                Debug.Log("[RuneSystemHolder] DB와 GameSession 준비를 기다리는 중입니다.", this);
            }

            return;
        }

        Create();
    }

    private void OnDestroy()
    {
        _loadout?.Dispose();
    }

    private bool CanCreate()
    {
        RuneEffectDatabase effectDatabase = RuneEffectDatabase.Instance;
        ResearchDatabase researchDatabase = ResearchDatabase.Instance;
        GameSession session = GameSession.Instance;

        return _stoneInventory != null
               && effectDatabase != null && effectDatabase.DatabaseReady
               && researchDatabase != null && researchDatabase.DatabaseReady
               && session != null && session.IsReady && session.Current != null;
    }

    private void Create()
    {
        try
        {
            RuneEffectDatabase effectDatabase = RuneEffectDatabase.Instance;
            _runeBook = new RuneBook(effectDatabase);
            _runeBook.Initialize();

            foreach (RuneEffectCategory category in _startRevealedCategories)
            {
                if (effectDatabase.TryGetByCategory(category, out _))
                {
                    _runeBook.RevealCategory(category);
                }
            }

            _loadout = new RuneLoadout(effectDatabase, _stoneInventory, _runeBook,
                GameSession.Instance.Current.ResearchTree, ResearchDatabase.Instance.ResearchNodes);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            _runeBook = null;
            _loadout = null;
            enabled = false;
            return;
        }

        Debug.Log("[RuneSystemHolder] RuneBook, RuneLoadout 생성 완료", this);
        SafeEventNotifier.Notify(Ready);
    }
}
