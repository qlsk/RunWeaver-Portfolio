using UnityEngine;

/// <summary>게임 안의 첫 사건을 감시해 알맞은 상황 안내 카드를 한 번씩 띄운다. 본 기록은 저장해 다시 보여주지 않는다.</summary>
public class TutorialRecordTracker : MonoBehaviour
{
    [SerializeField] private RecordCardSO[] _cards;
    [SerializeField] private TutorialRecordCardView _view;

    [Header("긴박 카드 기준")]
    [SerializeField, Min(0f)] private float _remainingTimeWarningSeconds = 10f;
    [Tooltip("체력이 최대 체력 대비 이 비율 이하이면 체력 위험 카드를 띄운다. 값은 아직 정해지지 않은 임시값")]
    [SerializeField, Range(0f, 1f)] private float _healthDangerRatio = 0.3f;

    private PlayerHealth _playerHealth;
    private MissionManager _missionManager;
    private StageManager _stageManager;
    private RuneStoneInventory _runeStoneInventory;
    private int _lastRuneStoneTotal;
    private bool _isTimeLowNotified;
    private bool _isHealthLowNotified;

    private void OnEnable()
    {
        Bullet.ReactionExecuted += HandleReactionExecuted;
        EnemyHealth.OnDamageDealt += HandleEnemyDamaged;
    }

    private void Start()
    {
        // Instance 프로퍼티는 없으면 새로 만들거나 예외를 던지므로, 생성 없이 기존 인스턴스만 찾는다.
        _playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (_playerHealth != null)
            _playerHealth.Damaged += HandlePlayerDamaged;

        _missionManager = FindAnyObjectByType<MissionManager>();
        if (_missionManager != null)
            _missionManager.OnMissionStarted += HandleMissionStarted;

        _stageManager = FindAnyObjectByType<StageManager>();
    }

    private void Update()
    {
        BindRuneStoneInventory();
        PollRemainingTime();
        PollHealth();
    }

    private void OnDisable()
    {
        Bullet.ReactionExecuted -= HandleReactionExecuted;
        EnemyHealth.OnDamageDealt -= HandleEnemyDamaged;

        if (_playerHealth != null)
            _playerHealth.Damaged -= HandlePlayerDamaged;

        if (_missionManager != null)
            _missionManager.OnMissionStarted -= HandleMissionStarted;

        if (_runeStoneInventory != null)
            _runeStoneInventory.OnInventoryChanged -= HandleRuneStonesChanged;

        _playerHealth = null;
        _missionManager = null;
        _runeStoneInventory = null;
    }

    /// <summary>사건이 일어났음을 알린다. 조건이 맞고 아직 보지 않은 카드를 모두 띄운다.</summary>
    public void Notify(RecordTriggerKind trigger, string key = "")
    {
        if (_cards == null || _view == null)
            return;

        foreach (RecordCardSO card in _cards)
        {
            if (card == null || card.Trigger != trigger)
                continue;

            if (!string.IsNullOrEmpty(card.TriggerKey) && card.TriggerKey != key)
                continue;

            if (TutorialProgressStore.IsRecordSeen(card.Id))
                continue;

            TutorialProgressStore.MarkRecordSeen(card.Id);
            _view.Enqueue(card);
        }
    }

    private void HandleReactionExecuted(ReactionElement primary, ReactionElement secondary)
    {
        // 서로 다른 원소의 순서는 구분하지 않으므로 이름 순으로 정렬한 쌍을 키로 쓴다.
        string first = primary.ToString();
        string second = secondary.ToString();
        string key = string.CompareOrdinal(first, second) <= 0 ? $"{first}+{second}" : $"{second}+{first}";
        Notify(RecordTriggerKind.FirstReaction, key);
    }

    private void HandleEnemyDamaged(EnemyHealth enemy, float damage)
    {
        Notify(RecordTriggerKind.FirstEnemyHit);
    }

    private void HandlePlayerDamaged(float damage)
    {
        Notify(RecordTriggerKind.PlayerDamaged);
    }

    private void HandleMissionStarted(MissionBase mission)
    {
        Notify(RecordTriggerKind.MissionTypeStarted, GetMissionTypeName(mission));
    }

    private void HandleRuneStonesChanged()
    {
        int total = CountRuneStones();
        if (total > _lastRuneStoneTotal)
            Notify(RecordTriggerKind.RuneStoneAcquired);

        _lastRuneStoneTotal = total;
    }

    /// <summary>룬 시스템은 로딩 뒤에 준비되므로 준비되는 때에 한 번 연결한다. 연결 시점의 보유량은 기준으로만 삼는다.</summary>
    private void BindRuneStoneInventory()
    {
        if (_runeStoneInventory != null)
            return;

        PlayerRuneManager runeManager = PlayerRuneManager.Instance;
        if (runeManager == null || !runeManager.IsInitialized)
            return;

        _runeStoneInventory = runeManager.RuneStoneInventory;
        _lastRuneStoneTotal = CountRuneStones();
        _runeStoneInventory.OnInventoryChanged += HandleRuneStonesChanged;
    }

    private int CountRuneStones()
    {
        int total = 0;
        foreach (RuneStoneItem item in _runeStoneInventory.GetAll())
            total += item.Quantity;

        return total;
    }

    private void PollRemainingTime()
    {
        if (_missionManager == null || _stageManager == null || !_missionManager.IsMissionRunning)
        {
            _isTimeLowNotified = false;
            return;
        }

        float remaining = _stageManager.RemainingTime;
        bool isLow = remaining > 0f && remaining <= _remainingTimeWarningSeconds;
        if (isLow && !_isTimeLowNotified)
            Notify(RecordTriggerKind.RemainingTimeLow);

        _isTimeLowNotified = isLow;
    }

    private void PollHealth()
    {
        if (_playerHealth == null || _playerHealth.MaxHealth <= 0f)
            return;

        bool isLow = !_playerHealth.IsDead && _playerHealth.CurrentHealth / _playerHealth.MaxHealth <= _healthDangerRatio;
        if (isLow && !_isHealthLowNotified)
            Notify(RecordTriggerKind.HealthLow);

        _isHealthLowNotified = isLow;
    }

    private static string GetMissionTypeName(MissionBase mission)
    {
        switch (mission)
        {
            case EnemyKillMission: return nameof(MissionType.EnemyKill);
            case SoulCollectMission: return nameof(MissionType.SoulCollect);
            case AltarDefenseMission: return nameof(MissionType.AltarDefense);
            case SurvivalMission: return nameof(MissionType.Survival);
            case CaptureZoneMission: return nameof(MissionType.CaptureZone);
            default: return string.Empty;
        }
    }
}
