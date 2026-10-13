using System;
using UnityEngine;

public class Shield : MonoBehaviour
{
    [Header("쉴드 쿨타임")]
    [SerializeField] private float _shieldCoolTime = 25f;
    private float _shieldCoolTimer = 0f;
    [Header("최대 Shield stack (기본 3, 보상 특성 shield_01로 4)")]
    [SerializeField] private int _maxShield = 3;
    [Header("현재 Shield stack")]
    [SerializeField] private int _currentShield = 0;
    public int CurrentShield => _currentShield;

    /// <summary>HUD용: 최대 쉴드 스택</summary>
    public int MaxShield => _maxShield;

    /// <summary>HUD용: 다음 쉴드 스택이 생길 때까지의 진행도 (0 ~ 1)</summary>
    public float RechargeProgress => _shieldCoolTime > 0f ? Mathf.Clamp01(_shieldCoolTimer / _shieldCoolTime) : 1f;

    /// <summary>HUD용: 지금 쉴드를 충전하고 있는지 (최대 스택이 아니고, 미션 진행 중일 때)</summary>
    public bool IsRecharging => _currentShield < _maxShield
                                && (_stageManager == null || _stageManager.Phase == StagePhase.InMission);

    /// <summary>HUD용: 다음 쉴드 스택이 생길 때까지 남은 시간(초)</summary>
    public float RechargeRemaining => Mathf.Max(0f, _shieldCoolTime - _shieldCoolTimer);

    /// <summary>보상 특성용: 쉴드 스택이 실제로 깎일 때 발행.</summary>
    public event Action ShieldBroken;

    /// <summary>보상 특성용: 충전 시간 외부 조정.</summary>
    public float ShieldCoolTime
    {
        get => _shieldCoolTime;
        set => _shieldCoolTime = Mathf.Max(0.1f, value);
    }

    private StageManager _stageManager;

    private void Start()
    {
        // StageManager.Instance는 없으면 새로 만들므로 찾기만 한다.
        _stageManager = FindAnyObjectByType<StageManager>();
    }

    private void Update()
    {
        // 미션 진행 중에만 충전한다. (미션 시작 전에 쉴드를 최대로 쌓아 두는 것을 막는다)
        // StageManager가 없는 씬(단독 테스트)에서는 항상 충전한다.
        if (_stageManager != null && _stageManager.Phase != StagePhase.InMission)
        {
            return;
        }

        // 최대 스택이면 쿨타임을 쌓지 않는다. (쉴드가 깨지면 그때부터 다시 충전)
        if (_currentShield >= _maxShield)
        {
            _shieldCoolTimer = 0f;
            return;
        }

        _shieldCoolTimer += Time.deltaTime;
        if (_shieldCoolTimer >= _shieldCoolTime)
        {
            ShieldUp();
            _shieldCoolTimer = 0f;
        }
    }

    // 쉴드 스택 하나 증가 (스킬 쿨타임 지나면 자동 사용)
    public void ShieldUp()
    {
        Debug.Log("ShieldUp");
        _currentShield++;
        Sfx.Play(Sfx.ShieldInit);
    }

    // 쉴드 하나 파괴 (피격 때 사용)
    public void DestroyShield()
    {
        if (_currentShield > 0)
        {
            _currentShield--;
            Sfx.Play(Sfx.ShieldHit);
            ShieldBroken?.Invoke();
        }
    }

    // 쉴드 최대 스택 하나 증가 (업그레이드 때 사용)
    public void ShieldMaxStackUp()
    {
        _maxShield++;
    }
}