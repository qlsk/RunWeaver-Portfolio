using System;
using System.Collections.Generic;
using UnityEngine;

public class Cleanse : MonoBehaviour
{
    private static readonly ReactionElement[] Elements =
    {
        ReactionElement.Fire, ReactionElement.Water, ReactionElement.Earth, ReactionElement.Wind
    };

    [SerializeField] private PlayerInputHandler _playerInputHandler;
    private PlayerStatusEffectController _playerStatusEffectController;

    /// <summary>보상 특성용: 정화 시전 통지 (해제한 상태이상 개수).</summary>
    public event Action<int> Cleansed;

    /// <summary>연출용: 정화 시전 통지 (해제한 상태이상 원소 목록. 없으면 빈 목록).</summary>
    public event Action<IReadOnlyList<ReactionElement>> Used;

    /// <summary>연출용: 다음 정화 대기시간 감산이 쌓였을 때 (이번에 더한 비율). (clease_04)</summary>
    public event Action<float> CooldownRefunded;

    [Header("정화 재사용 대기시간 (보상 특성용)")]
    [SerializeField, Min(0f)] private float _cooldown = 8f;
    private float _cooldownTimer;
    private float _cooldownRefund;
    private float _lastCooldownDuration;
    private readonly List<ReactionElement> _removed = new();

    [Header("효과음")]
    [Tooltip("시전음에 겹치는 성공음 크기 (상태이상을 하나 이상 풀었을 때)")]
    [SerializeField, Range(0f, 1f)] private float _successVolume = 1f;
    [Tooltip("시전음에 겹치는 빈 정화음 크기 (풀 상태이상이 없을 때)")]
    [SerializeField, Range(0f, 1f)] private float _emptyVolume = 0.4f;
    [SerializeField, Range(0f, 1f)] private float _cooldownVolume = 0.8f;
    [Tooltip("재사용 대기 중에 연타해도 거절음이 겹치지 않게 하는 최소 간격(초)")]
    [SerializeField, Min(0f)] private float _cooldownSoundInterval = 0.25f;
    private float _lastCooldownSoundTime = float.NegativeInfinity;

    /// <summary>HUD용: 다시 정화할 수 있을 때까지 남은 시간(초). 쓸 수 있으면 0</summary>
    public float CooldownRemaining => _cooldownTimer;

    /// <summary>HUD용: 이번 쿨타임의 전체 시간(초). 쿨타임 감산이 적용된 값</summary>
    public float CooldownDuration => _lastCooldownDuration > 0f ? _lastCooldownDuration : _cooldown;

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
            PlayCooldownSound();
            return;
        }

        // 연출에서 색을 쓰도록, 풀기 전에 걸려 있던 상태이상을 기록한다.
        _removed.Clear();
        if (_playerStatusEffectController != null)
        {
            foreach (ReactionElement element in Elements)
            {
                if (_playerStatusEffectController.HasEffect(element))
                {
                    _removed.Add(element);
                }
            }
        }

        int cleared = _playerStatusEffectController != null
            ? _playerStatusEffectController.ClearAllEffects()
            : 0;
        _lastCooldownDuration = _cooldown * (1f - Mathf.Clamp01(_cooldownRefund));
        _cooldownTimer = _lastCooldownDuration;
        _cooldownRefund = 0f;
        Used?.Invoke(_removed);
        Cleansed?.Invoke(cleared);
        Sfx.Play(Sfx.CleanseSpell);
        if (cleared > 0)
        {
            Sfx.Play(Sfx.CleanseSuccess, _successVolume);
        }
        else
        {
            Sfx.Play(Sfx.CleanseEmpty, _emptyVolume);
        }
    }

    /// <summary>재사용 대기 중에 눌렀을 때. 연타하면 일정 간격으로만 낸다.</summary>
    private void PlayCooldownSound()
    {
        float now = Time.unscaledTime;
        if (now - _lastCooldownSoundTime < _cooldownSoundInterval)
        {
            return;
        }

        _lastCooldownSoundTime = now;
        Sfx.Play(Sfx.CleanseCooldown, _cooldownVolume);
    }

    /// <summary>보상 특성용: 다음 정화 1회의 대기시간을 비율만큼 감산한다.</summary>
    public void RefundCooldown(float ratio)
    {
        _cooldownRefund = Mathf.Clamp01(_cooldownRefund + Mathf.Max(0f, ratio));
        if (ratio > 0f)
        {
            CooldownRefunded?.Invoke(ratio);
        }
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer = Mathf.Max(0f, _cooldownTimer - Time.deltaTime);
        }
    }
}
