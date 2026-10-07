using System;
using UnityEngine;

public class Cleanse : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    private PlayerStatusEffectController _playerStatusEffectController;

    /// <summary>보상 특성용: 정화 시전 통지 (해제한 상태이상 개수).</summary>
    public event Action<int> Cleansed;

    [Header("정화 재사용 대기시간 (보상 특성용)")]
    [SerializeField, Min(0f)] private float _cooldown = 8f;
    private float _cooldownTimer;
    private float _cooldownRefund;

    private void Awake()
    {
        _playerStatusEffectController = GetComponent<PlayerStatusEffectController>();
    }

    private void OnEnable()
    {
        _playerInputHandler.Skill2Pressed += UseCleanse;
    }

    private void OnDisable()
    {
        _playerInputHandler.Skill2Pressed -= UseCleanse;
    }

    private void UseCleanse()
    {
        if (_cooldownTimer > 0f)
        {
            return;
        }

        int cleared = _playerStatusEffectController != null
            ? _playerStatusEffectController.ClearAllEffects()
            : 0;
        _cooldownTimer = _cooldown * (1f - Mathf.Clamp01(_cooldownRefund));
        _cooldownRefund = 0f;
        Cleansed?.Invoke(cleared);
    }

    /// <summary>보상 특성용: 다음 정화 1회의 대기시간을 비율만큼 감산한다.</summary>
    public void RefundCooldown(float ratio)
    {
        _cooldownRefund = Mathf.Clamp01(_cooldownRefund + Mathf.Max(0f, ratio));
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer = Mathf.Max(0f, _cooldownTimer - Time.deltaTime);
        }
    }
}