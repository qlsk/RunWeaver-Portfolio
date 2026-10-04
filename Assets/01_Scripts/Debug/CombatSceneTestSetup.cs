using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CombatSceneTestSetup : MonoBehaviour
{
    [SerializeField] private GameBootstrap _bootstrap;
    [SerializeField] private ReactionElement _testElement = ReactionElement.Fire;

    private void Start()
    {
        InitializeAsync().Forget();
    }

    private async UniTask InitializeAsync()
    {
        await UniTask.WaitUntil(() => _bootstrap.State == BootstrapState.Ready,
            cancellationToken: this.GetCancellationTokenOnDestroy());

        string effectId = "rune_" + _testElement.ToString().ToLowerInvariant();
        if (!RuneEffectDatabase.Instance.TryGetById(effectId, out RuneEffectDefinition effect))
            throw new InvalidOperationException($"Unknown rune effect ID: {effectId}");

        RuneEffectLoadoutManager.Instance.RuneEffectLoadout.ConfirmEquipment(new[] { effect.Id });
    }
}
