using System;
using System.Collections.Generic;
using UnityEngine;

public class ProgressSceneBinder : MonoBehaviour
{
    private GameSession _session;
    private PlayerProgression _boundProgress;
    private RuneLoadout _boundLoadout;
    private RuneEffectPipeline _pipeline;
    private PlayerCombatBaseStatsSO _baseStatsAsset;
    private bool _isBound;
    [SerializeField] private RuneEffectDatabase _effectDatabase;
    [SerializeField] private SkillDatabase _skillDatabase;
    [SerializeField] private ResearchDatabase _researchDatabase;
    // private readonly ProgressCombatModifierCalculator _modifierCalculator = new();

    public bool IsCombatReady =>
        isActiveAndEnabled &&
        _isBound &&
        _pipeline != null &&
        _session.IsReady &&
        _boundProgress == _session.Current &&
        _boundLoadout == _session.CurrentRuneLoadout &&
        _boundProgress.Holdings.IsOnExpedition &&
        _pipeline.IsSynchronized;

    /// <summary>필수 참조를 확인하고 세션 이벤트를 구독한 뒤 준비된 진행 상태를 연결한다.</summary>
    private void Start()
    {
        _session = GameSession.Instance;
        if (_session == null)
            throw new InvalidOperationException("Bootstrap did not execute.");
        if (_effectDatabase == null || _skillDatabase == null || _researchDatabase == null)
            throw new MissingReferenceException("ProgressSceneBinder requires rune, skill and research databases.");
        _session.ProgressReplaced += Bind;
        if (_session.IsReady)
            Bind(_session.Current);
    }

    /// <summary>초기화 이후 재활성화되면 세션 이벤트를 다시 구독하고 현재 진행 상태를 연결한다.</summary>
    private void OnEnable()
    {
        // Start 이전에는 세션 연결이 아직 구성되지 않았다.
        if (_session == null)
            return;
        _session.ProgressReplaced += Bind;
        if (_session.IsReady)
            Bind(_session.Current);
    }

    /// <summary>세션 이벤트 구독을 해제하고 진행 상태와 Pipeline의 연결을 정리한다.</summary>
    private void OnDisable()
    {
        if (_session != null)
            _session.ProgressReplaced -= Bind;
        Unbind();
    }

    /// <summary>현재 진행 상태와 장착 정보를 Pipeline에 연결하고 성장 이벤트를 구독한다.</summary>
    public void Bind(PlayerProgression progress)
    {
        if (progress == null)
            throw new ArgumentNullException(nameof(progress));

        if (_session == null)
            throw new InvalidOperationException("[ProgressSceneBinder.Bind] Binder has not started.");

        if (!isActiveAndEnabled || !_session.IsReady)
            throw new InvalidOperationException("[ProgressSceneBinder.Bind] Binder or session is not ready.");

        if (progress != _session.Current)
            throw new InvalidOperationException("[ProgressSceneBinder.Bind] Progress is not current.");

        if (_pipeline == null)
        {
            Unbind();
            return;
        }

        if (_isBound && _boundProgress == progress && _boundLoadout == _session.CurrentRuneLoadout &&
            _pipeline.IsSynchronized)
            return;

        Unbind();

        try
        {
            if (!_effectDatabase.DatabaseReady || !_skillDatabase.DatabaseReady || !_researchDatabase.DatabaseReady)
                throw new InvalidOperationException("[ProgressSceneBinder] Definition databases are not ready.");

            PlayerCombatStats baseStats = _baseStatsAsset.CreateStats();

            // IReadOnlyList<CombatStatModifier> modifiers = _modifierCalculator.Calculate(progress,
            // _skillDatabase.SkillNodeParameters, _researchDatabase.ResearchNodes);

            // _pipeline.Bind(_session.CurrentRuneLoadout, _effectDatabase, baseStats, modifiers);

            progress.SkillTree.NodeLevelChanged += OnSkillNodeLevelChanged;
            progress.ResearchTree.NodeActivated += OnResearchNodeActivated;

            _boundProgress = progress;
            _boundLoadout = _session.CurrentRuneLoadout;

            _isBound = true;
        }
        catch
        {
            progress.SkillTree.NodeLevelChanged -= OnSkillNodeLevelChanged;
            progress.ResearchTree.NodeActivated -= OnResearchNodeActivated;
            Unbind();
            throw;
        }
    }

    /// <summary>Pipeline과 기본값 대상을 지정하고 세션이 준비되면 현재 진행 상태를 연결한다.</summary>
    public void SetTarget(RuneEffectPipeline pipeline, PlayerCombatBaseStatsSO baseStats)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));

        if (baseStats == null)
            throw new ArgumentNullException(nameof(baseStats));

        if (_pipeline != pipeline || _baseStatsAsset != baseStats)
        {
            Unbind();
            _pipeline = pipeline;
            _baseStatsAsset = baseStats;
        }

        if (_session != null && isActiveAndEnabled && _session.IsReady)
            Bind(_session.Current);
    }

    /// <summary>재연결할 대상 참조는 유지하면서 성장 이벤트 구독과 Pipeline 연결을 해제한다.</summary>
    public void Unbind()
    {
        _isBound = false;
        if (_boundProgress != null)
        {
            _boundProgress.SkillTree.NodeLevelChanged -= OnSkillNodeLevelChanged;
            _boundProgress.ResearchTree.NodeActivated -= OnResearchNodeActivated;
        }

        _boundProgress = null;
        _boundLoadout = null;

        if (_pipeline != null)
            _pipeline.Unbind();
    }

    /// <summary>스킬 노드 레벨이 변경되면 다른 출처의 전투 보정을 갱신한다.</summary>
    private void OnSkillNodeLevelChanged(string nodeId, int level)
    {
        RefreshOtherModifiers();
    }

    /// <summary>연구 노드가 활성화되면 다른 출처의 전투 보정을 갱신한다.</summary>
    private void OnResearchNodeActivated(string nodeId)
    {
        RefreshOtherModifiers();
    }

    /// <summary>현재 성장 상태의 보정을 재계산하여 Pipeline에 전달하고 실패 시 연결을 해제한다.</summary>
    private void RefreshOtherModifiers()
    {
        if (!_isBound)
            return;

        if (_pipeline == null)
        {
            Unbind();
            return;
        }

        try
        {
            if (!_effectDatabase.DatabaseReady || !_skillDatabase.DatabaseReady || !_researchDatabase.DatabaseReady)
                throw new InvalidOperationException("[ProgressSceneBinder] Definition databases are not ready.");

            // IReadOnlyList<CombatStatModifier> modifiers = _modifierCalculator.Calculate(_boundProgress,
            // _skillDatabase.SkillNodeParameters, _researchDatabase.ResearchNodes);

            // _pipeline.SetOtherModifiers(modifiers);
        }
        catch
        {
            Unbind();
            throw;
        }
    }
}