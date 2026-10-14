using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 보상 선택지 3개 생성. 3슬롯 완전 랜덤 (룬석 + 패시브 + 특성 중 서로 다른 3종).
///
/// - 룬석 개수 = _baseRuneStoneCount + (depth - 1) × _runeStonePerDepth
/// - 패시브: 풀에서 추첨. 보유 스택이 있어도 제외하지 않는다.
/// - 특성: 보유済 제외 (한 번 선택되면 다시 안 뜬다).
/// - 한 UI에서 같은 종류 중복 금지. 후보가 3종 미만이면 중복 허용으로 채운다.
/// - 풀이 비어 있으면 내장 기본값으로 동작한다 (에셋 교체 전 플레이스홀더).
/// - 심층은 StageManager에서 읽기만 한다 (Stage 외부 수정 없음).
/// </summary>
[DisallowMultipleComponent]
public class RewardGenerator : MonoBehaviour
{
    [Header("룬석 개수 공식: base + (depth - 1) × perDepth")]
    [SerializeField, FormerlySerializedAs("baseRuneStoneCount"), Min(1)] private int _baseRuneStoneCount = 5;
    [SerializeField, FormerlySerializedAs("runeStonePerDepth"), Min(0)] private int _runeStonePerDepth = 1;

    [Header("패시브 풀 (4종 권장, 비워두면 내장 기본값 사용)")]
    [SerializeField, FormerlySerializedAs("passivePool")] private PassiveData[] _passivePool;
    [Tooltip("룬석 카드 아이콘. 비워두면 UI에서 색상 플레이스홀더로 표시된다.")]
    [SerializeField, FormerlySerializedAs("runeStoneIcon")] private Sprite _runeStoneIcon;
    [Tooltip("룬석 카드 제목 형식 ({0} = 개수)")]
    [SerializeField] private string _runeStoneTitleFormat = "룬석 {0}개";
    [Tooltip("룬석 카드 설명 형식 ({0} = 개수)")]
    [SerializeField] private string _runeStoneDescriptionFormat = "플레이어 주변에 룬석 {0}개가 생성됩니다.";

    [Header("특성 풀 (20종, 비워두면 내장 기본값 사용)")]
    [Tooltip("보유한 특성은 추첨에서 제외된다.")]
    [SerializeField] private TraitData[] _traitPool;

    [Header("테스트용")]
    [Tooltip("켜면 3개 중 1개가 항상 룬석. 기본값off, 출시는 끄고 나갈 것.")]
    [SerializeField] private bool _alwaysIncludeRuneStone;

    /// <summary>
    /// UI를 열기 직전에 한 번 호출한다. 항상 3개를 반환한다.
    /// </summary>
    public List<RewardOption> GenerateOptions(int depth)
    {
        return GenerateOptions(depth, null);
    }

    /// <summary>
    /// exclude에 있는 선택지(다시 뽑기 전에 보여준 카드)는 빼고 뽑는다.
    /// 빼고 나서 3종이 안 되면 뺀 선택지도 다시 후보에 넣어 채운다.
    /// </summary>
    public List<RewardOption> GenerateOptions(int depth, IReadOnlyList<RewardOption> exclude)
    {
        int safeDepth = Mathf.Max(1, depth);
        int runeCount = _baseRuneStoneCount + (safeDepth - 1) * _runeStonePerDepth;

        List<PassiveData> passives = GetEffectivePassivePool();
        List<TraitData> traits = GetEffectiveTraitPool();

        TraitManager manager = TraitManager.Instance;
        PassiveStatManager statManager = PassiveStatManager.Instance;

        // 후보 종류를 섞어서 서로 다른 3종을 뽑는다.
        List<RewardOption> candidates = new(passives.Count + traits.Count + 1)
        {
            RewardOption.ForRuneStone(runeCount, _runeStoneIcon, _runeStoneTitleFormat, _runeStoneDescriptionFormat)
        };
        HashSet<PassiveStatType> usedStats = new();
        foreach (PassiveData passive in passives)
        {
            if (passive == null || !usedStats.Add(passive.StatType))
            {
                continue;
            }

            // 최대 레벨 도달 패시브는 추첨에서 제외한다.
            int maxLevel = Mathf.Max(1, passive.MaxLevel);
            int stacks = statManager != null ? statManager.GetStacks(passive.StatType) : 0;
            if (stacks >= maxLevel)
            {
                continue;
            }

            candidates.Add(RewardOption.ForPassive(passive, stacks));
        }

        HashSet<string> usedTraitIds = new(System.StringComparer.Ordinal);
        foreach (TraitData trait in traits)
        {
            if (trait == null || string.IsNullOrWhiteSpace(trait.Id))
            {
                continue;
            }

            if (manager != null && manager.Has(trait.Id))
            {
                continue;
            }

            if (usedTraitIds.Add(trait.Id))
            {
                candidates.Add(RewardOption.ForTrait(trait));
            }
        }

        Shuffle(candidates);

        // 방금 보여준 카드는 뒤로 미뤄, 다른 후보가 모자랄 때만 쓰이게 한다.
        if (exclude != null && exclude.Count > 0)
        {
            List<RewardOption> fresh = candidates.FindAll(option => !ContainsSame(exclude, option));
            List<RewardOption> shown = candidates.FindAll(option => ContainsSame(exclude, option));
            candidates.Clear();
            candidates.AddRange(fresh);
            candidates.AddRange(shown);
        }

        List<RewardOption> options = new(3);
        int take = Mathf.Min(3, candidates.Count);
        for (int i = 0; i < take; i++)
        {
            options.Add(candidates[i]);
        }

        // 후보가 3종 미만이면 중복 허용으로 3개를 채운다.
        int guard = 0;
        while (options.Count < 3 && candidates.Count > 0 && guard < 30)
        {
            guard++;
            options.Add(candidates[Random.Range(0, candidates.Count)]);
        }

        // 최종 순서도 섞는다 (룬석 위치 고정 방지).
        Shuffle(options);

        // 테스트용: 룬석 보장은 하되 자리는 섞는다.
        if (_alwaysIncludeRuneStone && !options.Exists(o => o.Type == RewardType.RuneStone))
        {
            options[Random.Range(0, options.Count)] = RewardOption.ForRuneStone(
                runeCount, _runeStoneIcon, _runeStoneTitleFormat, _runeStoneDescriptionFormat);
        }

        Debug.Log($"[RewardGenerator] 후보 {candidates.Count}종(패시브 {passives.Count}, 특성 {traits.Count}) → 선택: {string.Join(", ", options.ConvertAll(o => o.Title))}", this);
        return options;
    }

    private static bool ContainsSame(IReadOnlyList<RewardOption> list, RewardOption option)
    {
        foreach (RewardOption other in list)
        {
            if (IsSame(other, option))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>같은 보상인지 (종류와 대상이 같으면 같다. 룬석 개수, 패시브 레벨은 보지 않는다)</summary>
    private static bool IsSame(RewardOption a, RewardOption b)
    {
        if (a == null || b == null || a.Type != b.Type)
        {
            return false;
        }

        return a.Type switch
        {
            RewardType.Trait => a.Trait != null && b.Trait != null && a.Trait.Id == b.Trait.Id,
            RewardType.Passive => a.Passive != null && b.Passive != null && a.Passive.StatType == b.Passive.StatType,
            _ => true
        };
    }

    private List<PassiveData> GetEffectivePassivePool()
    {
        List<PassiveData> pool = new();
        if (_passivePool != null)
        {
            foreach (PassiveData data in _passivePool)
            {
                if (data != null)
                {
                    pool.Add(data);
                }
            }
        }

        if (pool.Count == 0)
        {
            pool = BuildFallbackPassives();
        }

        return pool;
    }

    /// <summary>이어하기 : ID로 특성 데이터를 찾는다. 없으면 null</summary>
    public TraitData FindTrait(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        foreach (TraitData data in GetEffectiveTraitPool())
        {
            if (data != null && data.Id == id)
            {
                return data;
            }
        }

        return null;
    }

    private List<TraitData> GetEffectiveTraitPool()
    {
        List<TraitData> pool = new();
        if (_traitPool != null)
        {
            foreach (TraitData data in _traitPool)
            {
                if (data != null)
                {
                    pool.Add(data);
                }
            }
        }

        if (pool.Count == 0)
        {
            pool = TraitFallback.Build();
        }

        return pool;
    }

    /// <summary>
    /// PassiveData 에셋이 아직 없을 때 쓰는 내장 기본 4종. 에셋으로 교체하면 쓰이지 않는다.
    /// </summary>
    private List<PassiveData> BuildFallbackPassives()
    {
        return new List<PassiveData>
        {
            MakeFallbackPassive("passive_damage", PassiveStatType.Damage, "공격 피해량", "공격 피해량이 5% 증가합니다."),
            MakeFallbackPassive("passive_attack_speed", PassiveStatType.AttackSpeed, "공격 속도", "공격 속도가 5% 증가합니다."),
            MakeFallbackPassive("passive_move_speed", PassiveStatType.MoveSpeed, "이동 속도", "이동 속도가 5% 증가합니다."),
            MakeFallbackPassive("passive_magazine_size", PassiveStatType.MagazineSize, "탄창 크기", "탄창 크기가 1발 증가합니다.")
        };
    }

    private PassiveData MakeFallbackPassive(string id, PassiveStatType type, string displayName, string description)
    {
        PassiveData data = ScriptableObject.CreateInstance<PassiveData>();
        data.Id = id;
        data.StatType = type;
        data.DisplayName = displayName;
        data.Description = description;
        data.ValuePerStack = type == PassiveStatType.MagazineSize ? 1f : 0.05f;
        return data;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
