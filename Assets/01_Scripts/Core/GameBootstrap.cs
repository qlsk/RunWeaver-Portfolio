using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

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
        ValidateReferences();
        if (_session.IsReady)
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

    private void ValidateReferences()
    {
        Check(_gold, nameof(_gold));
        Check(_resources, nameof(_resources));
        Check(_session, nameof(_session));
        Check(_runeStoneDatabase, nameof(_runeStoneDatabase));
        Check(_runeEffectDatabase, nameof(_runeEffectDatabase));
        Check(_skillDatabase, nameof(_skillDatabase));
        Check(_researchDatabase, nameof(_researchDatabase));
    }

    private static void Check(Object target, string name)
    {
        if (target == null)
        {
            throw new MissingReferenceException(name);
        }
    }

    private static void Check<T>(IReadOnlyList<T> targets, string name) where T : Object
    {
        if (targets == null)
        {
            throw new MissingReferenceException(name);
        }

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null)
            {
                throw new MissingReferenceException($"{name}[{i}]");
            }
        }
    }
}
