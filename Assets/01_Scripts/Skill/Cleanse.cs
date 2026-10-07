using System;
using UnityEngine;

public class Cleanse : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    private PlayerStatusEffectController _playerStatusEffectController;

    /// <summary>보상 특성용: 정화 시전 통지 (해제한 상태이상 개수).</summary>
    public event Action<int> Cleansed;

    [Header("정화 재사용 대기시간 (보상 특성용, 0이면 없음)")]
    [SerializeField, Min(0f)] private float _cooldown = 0f;
    private float _cooldownTimer;

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
        _cooldownTimer = _cooldown;
        Cleansed?.Invoke(cleared);
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer = Mathf.Max(0f, _cooldownTimer - Time.deltaTime);
        }
    }
}