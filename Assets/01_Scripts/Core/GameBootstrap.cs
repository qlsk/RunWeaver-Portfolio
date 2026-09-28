using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("Definitions")]
    [SerializeField] private GoldDefinitionSO _gold;
    [SerializeField] private List<ResourceDefinitionSO> _resources;

    [Header("References")]
    [SerializeField] private GameSession _session;
    [SerializeField] private RuneStoneDatabase _runeStoneDatabase;
    [SerializeField] private RuneEffectDatabase _runeEffectDatabase;
    [SerializeField] private SkillDatabase _skillDatabase;
    [SerializeField] private ResearchDatabase _researchDatabase;

    private void Start()
    {
        BootStrap().Forget();
    }

    private async UniTask BootStrap()
    {
        if (GameSession.Instance.IsReady)
        {
            return;
        }
        await _runeStoneDatabase.Initialize();
        await _runeEffectDatabase.Initialize();
        await _skillDatabase.Initialize();
        await _researchDatabase.Initialize();
        GameDefinitionSet definitionSet =
            new(_skillDatabase.SkillNodes, _researchDatabase.ResearchNodes, _gold, _resources);
        _session.Initialize(definitionSet);
    }
}
