using UnityEngine;

public class TutorialRecordTracker : MonoBehaviour
{
    [SerializeField] private RecordCardSO[] _cards;
    [SerializeField] private TutorialRecordCardView _view;

    [Header("긴박 카드 기준")]
    [SerializeField][Min(0f)] private float _remainingTimeWarningSeconds = 10f;
    [SerializeField][Range(0f, 1f)] private float _healthDangerRatio = 0.3f;

    private PlayerHealth _playerHealth;
    private MissionManager _missionManager;
    private StageManager _stageManager;
    private RuneStoneInventory _runeStoneInventory;
    private int _lastRuneStoneTotal;
    private bool _isTimeLowNotified;
    private bool _isHealthLowNotified;
    private bool _isStarted;
    private float _nextBindTime;

    private void OnEnable()
    {
        Bullet.ReactionExecuted += HandleReactionExecuted;
        EnemyHealth.OnDamageDealt += HandleEnemyDamaged;

        if (_isStarted)
            BindSceneObjects();
    }

    private void Start()
    {
        _isStarted = true;
        BindSceneObjects();
    }

    private void BindSceneObjects()
    {
        if (_playerHealth == null)
        {
            _playerHealth = FindAnyObjectByType<PlayerHealth>();
            _playerHealth.Damaged += HandlePlayerDamaged;
        }

        if (_missionManager == null)
        {
            _missionManager = FindAnyObjectByType<MissionManager>();
            _missionManager.OnMissionStarted += HandleMissionStarted;
        }

        if (_stageManager == null)
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
        _stageManager = null;
        _runeStoneInventory = null;
        _isTimeLowNotified = false;
        _isHealthLowNotified = false;
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