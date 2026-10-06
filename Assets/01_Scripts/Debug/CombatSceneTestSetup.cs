using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

public class CombatSceneTestSetup : MonoBehaviour
{
    [SerializeField] private GameBootstrap _bootstrap;
    [SerializeField, Min(0), OnValueChanged(nameof(ResizeEquippedEffects))]
    private int _equippedRuneCount = 1;

    [SerializeField, ListDrawerSettings(ShowIndexLabels = true, IsReadOnly = true)]
    private string[] _equippedEffectIds = { RuneEffectIds.Fire };

    [SerializeField, HideInInspector] private int _selectedSlot;

    private bool CanSelectRune => _equippedRuneCount > 0;

    [Button("Fire"), BoxGroup("Element"), ButtonGroup("Element/Selection"), EnableIf(nameof(CanSelectRune))]
    private void SelectFire() => SelectRune(RuneEffectIds.Fire);

    [Button("Water"), BoxGroup("Element"), ButtonGroup("Element/Selection"), EnableIf(nameof(CanSelectRune))]
    private void SelectWater() => SelectRune(RuneEffectIds.Water);

    [Button("Earth"), BoxGroup("Element"), ButtonGroup("Element/Selection"), EnableIf(nameof(CanSelectRune))]
    private void SelectEarth() => SelectRune(RuneEffectIds.Earth);

    [Button("Wind"), BoxGroup("Element"), ButtonGroup("Element/Selection"), EnableIf(nameof(CanSelectRune))]
    private void SelectWind() => SelectRune(RuneEffectIds.Wind);

    [Button("Spread"), BoxGroup("Behaviour"), ButtonGroup("Behaviour/Selection"), EnableIf(nameof(CanSelectRune))]
    private void SelectSpread() => SelectRune(RuneEffectIds.Spread);

    [Button("Multiple"), BoxGroup("Behaviour"), ButtonGroup("Behaviour/Selection"), EnableIf(nameof(CanSelectRune))]
    private void SelectMultiple() => SelectRune(RuneEffectIds.Multiple);

    [Button("Pierce"), BoxGroup("Behaviour"), ButtonGroup("Behaviour/Selection"), EnableIf(nameof(CanSelectRune))]
    private void SelectPierce() => SelectRune(RuneEffectIds.Pierce);

    [Button("Reverse"), BoxGroup("Behaviour"), ButtonGroup("Behaviour/Selection"), EnableIf(nameof(CanSelectRune))]
    private void SelectReverse() => SelectRune(RuneEffectIds.Reverse);

    [Button("Return"), BoxGroup("Behaviour"), ButtonGroup("Behaviour/Selection"), EnableIf(nameof(CanSelectRune))]
    private void SelectReturn() => SelectRune(RuneEffectIds.Return);

    [Button("Seek"), BoxGroup("Behaviour"), ButtonGroup("Behaviour/Selection"), EnableIf(nameof(CanSelectRune))]
    private void SelectSeek() => SelectRune(RuneEffectIds.Seek);

    private void SelectRune(string effectId)
    {
        ResizeEquippedEffects();
        _equippedEffectIds[_selectedSlot] = effectId;
    }

    private void ResizeEquippedEffects()
    {
        Array.Resize(ref _equippedEffectIds, _equippedRuneCount);
        _selectedSlot = Mathf.Clamp(_selectedSlot, 0, Mathf.Max(0, _equippedRuneCount - 1));
    }

#if UNITY_EDITOR
    [OnInspectorGUI, PropertyOrder(-1)]
    private void DrawSlotSelection()
    {
        UnityEditor.EditorGUILayout.LabelField("Selected Slot");
        UnityEditor.EditorGUILayout.BeginHorizontal();

        Color originalColor = GUI.backgroundColor;
        for (int i = 0; i < _equippedRuneCount; i++)
        {
            GUI.backgroundColor = i == _selectedSlot ? Color.cyan : originalColor;
            string label = i == 0 ? "Main" : $"Slot {i + 1}";

            if (GUILayout.Button(label))
            {
                UnityEditor.Undo.RecordObject(this, "Select Rune Slot");
                _selectedSlot = i;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        GUI.backgroundColor = originalColor;
        UnityEditor.EditorGUILayout.EndHorizontal();
    }
#endif

    private void OnValidate()
    {
        _equippedRuneCount = Mathf.Max(0, _equippedRuneCount);
        ResizeEquippedEffects();
    }

    private void Start()
    {
        InitializeAsync().Forget();
    }

    private async UniTask InitializeAsync()
    {
        await UniTask.WaitUntil(() => _bootstrap.State == BootstrapState.Ready,
            cancellationToken: this.GetCancellationTokenOnDestroy());

        // 마을에서 넘어와 이미 장착한 룬이 있으면 그대로 쓴다. (전투 씬을 바로 실행했을 때만 테스트 장착)
        RuneEffectLoadout loadout = PlayerRuneManager.Instance.RuneEffectLoadout;
        if (loadout.EquippedEffectIds.Count > 0)
            return;

        List<string> effectIds = new(_equippedRuneCount);
        for (int i = 0; i < _equippedRuneCount; i++)
        {
            string effectId = _equippedEffectIds[i];
            if (string.IsNullOrEmpty(effectId))
                continue;

            if (!RuneEffectDatabase.Instance.TryGetById(effectId, out RuneEffectDefinition effect))
                throw new InvalidOperationException($"Unknown rune effect ID: {effectId}");

            effectIds.Add(effect.Id);
        }

        loadout.ConfirmEquipment(effectIds);
    }
}
