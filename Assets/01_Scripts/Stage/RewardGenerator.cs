using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 보상 선택지 3개 생성. 3슬롯 완전 랜덤 (룬석 포함 6종 중 서로 다른 3종).
///
/// - 후보: 룬석 1종 + 패시브 풀. 한 UI에서 같은 종류 중복 금지.
/// - 룬석이 안 뜰 수도 있다. 룬석 개수 = _baseRuneStoneCount + (depth - 1) × _runeStonePerDepth
/// - 인스펙터에 PassiveData가 비어 있으면 코드 내장 기본 4종으로 동작한다 (에셋 교체 전 플레이스홀더).
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

    /// <summary>
    /// UI를 열기 직전에 한 번 호출한다. 항상 3개를 반환한다.
    /// </summary>
    public List<RewardOption> GenerateOptions(int depth)
    {
        int safeDepth = Mathf.Max(1, depth);
        int runeCount = _baseRuneStoneCount + (safeDepth - 1) * _runeStonePerDepth;

        List<PassiveData> pool = GetEffectivePool();

        // 후보 종류: 0 = 룬석, 1..N = 패시브 인덱스. 서로 다른 3종을 뽑는다.
        List<int> kinds = new(pool.Count + 1);
        kinds.Add(0);
        for (int i = 1; i <= pool.Count; i++)
        {
            kinds.Add(i);
        }

        Shuffle(kinds);

        List<RewardOption> options = new(3);
        HashSet<PassiveStatType> usedStats = new();
        foreach (int kind in kinds)
        {
            if (options.Count >= 3)
            {
                break;
            }

            if (kind == 0)
            {
                options.Add(RewardOption.ForRuneStone(runeCount, _runeStoneIcon));
            }
            else
            {
                PassiveData data = pool[kind - 1];
                // 같은 스탯 중복 금지 (풀이 1~2종뿐이면 부족분은 아래에서 중복 허용으로 채운다).
                if (usedStats.Add(data.StatType))
                {
                    options.Add(RewardOption.ForPassive(data));
                }
            }
        }

        // 후보가 3종 미만이면 중복 허용으로 3개를 채운다.
        int guard = 0;
        while (options.Count < 3 && guard < 30)
        {
            guard++;
            if (Random.Range(0, pool.Count + 1) == 0)
            {
                options.Add(RewardOption.ForRuneStone(runeCount, _runeStoneIcon));
            }
            else
            {
                options.Add(RewardOption.ForPassive(pool[Random.Range(0, pool.Count)]));
            }
        }

        // 최종 순서도 섞는다 (룬석 위치 고정 방지).
        Shuffle(options);
        return options;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private List<PassiveData> GetEffectivePool()
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
            pool = BuildFallbackPool();
        }

        // 풀이 1종뿐이면 중복 방지가 불가능하므로 그대로 반환 (호출 측 가드에서 처리)
        return pool;
    }

    /// <summary>
    /// PassiveData 에셋이 아직 없을 때 쓰는 내장 기본 4종. 에셋으로 교체하면 쓰이지 않는다.
    /// </summary>
    private List<PassiveData> BuildFallbackPool()
    {
        return new List<PassiveData>
        {
            MakeFallback("passive_damage", PassiveStatType.Damage, "데미지", "데미지 +5%"),
            MakeFallback("passive_attack_speed", PassiveStatType.AttackSpeed, "공격속도", "공격속도 +5%"),
            MakeFallback("passive_move_speed", PassiveStatType.MoveSpeed, "이동속도", "이동속도 +5%"),
            MakeFallback("passive_magazine_size", PassiveStatType.MagazineSize, "탄창 크기", "탄창 크기 +5% (올림, 최소 +1발)")
        };
    }

    private PassiveData MakeFallback(string id, PassiveStatType type, string displayName, string description)
    {
        PassiveData data = ScriptableObject.CreateInstance<PassiveData>();
        data.Id = id;
        data.StatType = type;
        data.DisplayName = displayName;
        data.Description = description;
        data.ValuePerStack = 0.05f;
        return data;
    }
}
