using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보상 패시브 스택 보관 + 최종 스탯 계산. 런 한정 효과.
///
/// - 스택은 합연산: 최종값 = 기본값 × (1 + valuePerStack × 스택 수)
/// - 증가량(valuePerStack)은 PassiveData에서 받아오며 하드코딩하지 않는다.
/// - 각 스탯 시스템(공격/이동/재장전/탄창)은 GetMultiplier를 조회하거나 OnStatChanged를 구독한다.
///   보상 시스템은 스탯 계산 코드를 직접 건드리지 않는다.
/// - 씬 오브젝트로 두면 런 종료(씬 전환) 시 자동 소멸되어 스택이 사라진다.
///   플레이어 사망 시에는 PlayerHealth.Died를 구독해 자동 초기화한다 (외부 읽기 전용, 수정 없음).
/// </summary>
[DisallowMultipleComponent]
public class PassiveStatManager : MonoBehaviour
{
    private static PassiveStatManager s_instance;

    public static PassiveStatManager Instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = FindAnyObjectByType<PassiveStatManager>();
                if (s_instance == null)
                {
                    GameObject go = new GameObject("PassiveStatManager");
                    s_instance = go.AddComponent<PassiveStatManager>();
                    Debug.Log("[PassiveStatManager] 씬에 없어 자동으로 생성하였습니다.");
                }
            }
            return s_instance;
        }
    }

    private const float DefaultValuePerStack = 0.05f;

    private readonly Dictionary<PassiveStatType, int> _stacks = new();
    private readonly Dictionary<PassiveStatType, float> _valuePerStack = new();

    private PlayerHealth _playerHealth;

    /// <summary>스택이 바뀐 스탯 종류를 알린다.</summary>
    public event Action<PassiveStatType> OnStatChanged;

    private void Awake()
    {
        if (s_instance == null)
        {
            s_instance = this;
        }
        else if (s_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 사망 시 초기화용으로만 구독한다 (읽기 전용).
        _playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (_playerHealth != null)
        {
            _playerHealth.Died += HandlePlayerDied;
        }
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.Died -= HandlePlayerDied;
        }

        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    /// <summary>
    /// 선택 확정 이후에만 호출한다. 최대 레벨에 도달하면 무시하고 false.
    /// </summary>
    public bool AddStack(PassiveStatType type, float valuePerStack = DefaultValuePerStack, int maxStacks = int.MaxValue)
    {
        _stacks.TryGetValue(type, out int count);
        if (count >= Mathf.Max(1, maxStacks))
        {
            return false;
        }

        _stacks[type] = count + 1;

        if (valuePerStack > 0f)
        {
            _valuePerStack[type] = valuePerStack;
        }
        else if (!_valuePerStack.ContainsKey(type))
        {
            _valuePerStack[type] = DefaultValuePerStack;
        }

        Debug.Log($"[PassiveStatManager] {type} 스택 {_stacks[type]} (배율 {GetMultiplier(type):F3})");
        OnStatChanged?.Invoke(type);
        return true;
    }

    public int GetStacks(PassiveStatType type)
    {
        return _stacks.TryGetValue(type, out int count) ? count : 0;
    }

    /// <summary>
    /// 최종 배율 = 1 + valuePerStack × 스택 수. 없으면 1.
    /// </summary>
    public float GetMultiplier(PassiveStatType type)
    {
        if (!_stacks.TryGetValue(type, out int count) || count <= 0)
        {
            return 1f;
        }

        float perStack = _valuePerStack.TryGetValue(type, out float v) ? v : DefaultValuePerStack;
        return 1f + perStack * count;
    }

    /// <summary>
    /// 탄창 보너스 탄수. 올림 처리 + 최소 +1발 보장. 스택이 없으면 0.
    /// </summary>
    public int GetMagazineBonus(int baseMagazine)
    {
        if (GetStacks(PassiveStatType.MagazineSize) <= 0 || baseMagazine <= 0)
        {
            return 0;
        }

        int increased = Mathf.CeilToInt(baseMagazine * GetMultiplier(PassiveStatType.MagazineSize));
        return Mathf.Max(1, increased - baseMagazine);
    }

    /// <summary>런 종료/사망 시 초기화.</summary>
    public void ResetAll()
    {
        _stacks.Clear();
        Debug.Log("[PassiveStatManager] 전 스택 초기화");
    }

    private void HandlePlayerDied()
    {
        ResetAll();
    }
}
