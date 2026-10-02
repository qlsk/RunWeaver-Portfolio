using System;
using System.Collections.Generic;
using UnityEngine;

public class RuneEffectPipeline : MonoBehaviour
{
    private RuneLoadout _loadout;
    private RuneEffectDatabase _database;
    private PlayerCombatStats _baseStats;
    private IReadOnlyList<CombatStatModifier> _otherModifiers;
    private RuneEffectSnapshot _current;
    private bool _isSynchronized;

    public bool IsSynchronized => _isSynchronized;

    public void Bind(RuneLoadout loadout,
        RuneEffectDatabase database,
        PlayerCombatStats baseStats,
        IReadOnlyList<CombatStatModifier> otherModifier)
    {
        if (loadout == null)
            throw new ArgumentNullException(nameof(loadout));
        if (database == null)
            throw new ArgumentNullException(nameof(database));
        if (!database.DatabaseReady)
            throw new InvalidOperationException("[RuneEffectPipeline.Bind] Rune effect database is not ready.");
        if (baseStats == null)
            throw new ArgumentNullException(nameof(baseStats));
        if (otherModifier == null)
            throw new ArgumentNullException(nameof(otherModifier));

        List<CombatStatModifier> modifiers = new(otherModifier);
        _current = BuildSnapshot(loadout, database, baseStats, modifiers);

        if (_loadout != null)
            _loadout.LoadoutChanged -= Refresh;

        _loadout = loadout;
        _database = database;
        _baseStats = baseStats;
        _otherModifiers = modifiers;

        _loadout.LoadoutChanged += Refresh;
        _isSynchronized = true;
    }

    public void Refresh()
    {
        if (_loadout == null)
            throw new InvalidOperationException("[RuneEffectPipeline.Refresh] Pipeline is not bound.");

        try
        {
            _current = BuildSnapshot(_loadout, _database, _baseStats, _otherModifiers);
            _isSynchronized = true;
        }
        catch
        {
            _isSynchronized = false;
            throw;
        }
    }

    private static RuneEffectSnapshot BuildSnapshot(RuneLoadout loadout,
        RuneEffectDatabase database,
        PlayerCombatStats baseStats,
        IReadOnlyList<CombatStatModifier> otherModifiers)
    {
        if (!database.DatabaseReady)
            throw new InvalidOperationException("[RuneEffectPipeline.BuildSnapshot] Rune effect database isn't ready.");

        IReadOnlyList<string> equippedEffectIds = loadout.GetEquippedEffectIds();
        List<RuneEffectSlot> slots = new();
        HashSet<string> equippedIds = new(StringComparer.Ordinal);
        List<CombatStatModifier> modifiers = new(otherModifiers);
        for (int slotIndex = 0; slotIndex < equippedEffectIds.Count; slotIndex++)
        {
            string effectId = equippedEffectIds[slotIndex];
            if (string.IsNullOrWhiteSpace(effectId))
                continue;
            if (!database.TryGetById(effectId, out RuneEffectDefinition effect))
                throw new InvalidOperationException("[RuneEffectPipeline.BuildSnapshot] Effect not found.");
            if (!equippedIds.Add(effectId) && !effect.CanOverlap)
                throw new InvalidOperationException(
                    $"[RuneEffectPipeline.BuildSnapshot] Effect cannot overlap: {effectId}.");

            RuneEffectExecutionSettings executionSettings = effect.ExecutionSettings;
            if (executionSettings == null)
                throw new InvalidOperationException("[RuneEffectPipeline.BuildSnapshot] Execution settings not found.");
            if (effectId != executionSettings.EffectId)
                throw new InvalidOperationException("[RuneEffectPipeline.BuildSnapshot] Effect id not match.");

            RuneEffectSlot slot = new(slotIndex, effectId, $"Rune:{slotIndex}:{effectId}", executionSettings);

            // TODO: 룬 효과 구현 후 마저 구현
            // CombatStatModifier modifier = new($"Rune:{slotIndex}:{effectId}", );

            slots.Add(slot);
        }
        // TODO: 룬 효과 구현 후 실행 설정을 기준으로 Return/Fix/Orbit 상호 배타 검증 추가.

        PlayerCombatStats combatStats = PlayerCombatStatCalculator.Calculate(baseStats, modifiers);
        ValidateCombatStats(combatStats);

        return new RuneEffectSnapshot(combatStats, slots);
    }

    public void SetOtherModifiers(IReadOnlyList<CombatStatModifier> modifiers)
    {
        if (_loadout == null)
            throw new InvalidOperationException("[RuneEffectPipeline.SetOtherModifier] Pipeline is not bound.");
        List<CombatStatModifier> otherModifiers = new(modifiers);
        _current = BuildSnapshot(_loadout, _database, _baseStats, otherModifiers);
        _otherModifiers = otherModifiers;
        _isSynchronized = true;
    }

    public RuneEffectSnapshot GetCurrent()
    {
        if (_loadout == null)
            throw new InvalidOperationException("[RuneEffectPipeline.GetCurrent] Pipeline is not bound.");
        if (!_isSynchronized)
            throw new InvalidOperationException("[RuneEffectPipeline.GetCurrent] Current is not synchronized.");
        return _current;
    }

    public void Unbind()
    {
        if (_loadout != null)
            _loadout.LoadoutChanged -= Refresh;

        _loadout = null;
        _database = null;
        _baseStats = null;
        _otherModifiers = null;
        _current = null;
        _isSynchronized = false;
    }

    public void OnDisable()
    {
        Unbind();
    }

    public void OnDestroy()
    {
        Unbind();
    }

    private static void ValidateCombatStats(PlayerCombatStats combatStats)
    {
        if (combatStats.MaxHealth <= 0)
        {
            throw new InvalidOperationException(
                "[RuneEffectPipeline.ValidateCombatStats] Max health must be positive.");
        }
        if (combatStats.MoveSpeed < 0)
        {
            throw new InvalidOperationException(
                "[RuneEffectPipeline.ValidateCombatStats] Move speed cannot be negative.");
        }
        if (combatStats.FireInterval <= 0)
        {
            throw new InvalidOperationException(
                "[RuneEffectPipeline.ValidateCombatStats] Fire interval must be positive.");
        }
        if (combatStats.BulletDamage < 0)
        {
            throw new InvalidOperationException(
                "[RuneEffectPipeline.ValidateCombatStats] Bullet damage cannot be negative.");
        }
    }
}