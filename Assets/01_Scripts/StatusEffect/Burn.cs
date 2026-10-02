using System;
using System.Collections;
using UnityEngine;

public class Burn : MonoBehaviour
{
    // 지속시간 3초, 1초마다 피해, 최초 피해는 부여 1초 후 발생.
    // 1회 피해량은 화상 부여 시점 탄환 최종 피해량의 20%.
    // 기본적으로 총 3회 피해 발생.
    // 화상은 중첩되지 않으며, 재적용 시 지속시간만 3초로 갱신하고 기존 틱 타이머는 유지.
    // 재적용 시 새 화상 피해량이 기존보다 높을 경우에만 피해량 갱신.
    // 적용 이후 원본 탄환과 독립적으로 동작하며 화상 피해로 적 처치 가능.
    // 적 사망 또는 스테이지 종료 시 제거.
    // 화상 틱이 OnHit, 흡수 등 추가 효과를 발동시키는지는 구현 단계에서 추후 결정.
    private const float Duration = 3f;
    private const float DamageInterval = 1f;
    private const float BurnDamageMultiplier = 0.2f;

    private float _remainingDuration;
    private float _tickTimer;
    private float _damage;

    private Coroutine _timerRoutine;

    public event Action<float> OnBurnTriggered;

    public void Activate(float damage)
    {
        _damage = BurnDamageMultiplier * damage;
        _remainingDuration = Duration;
        if (_timerRoutine == null)
            _timerRoutine = StartCoroutine(Timer());
    }

    private IEnumerator Timer()
    {
        _tickTimer = 0f;
        while (_remainingDuration > 0)
        {
            _remainingDuration -= Time.deltaTime;
            _tickTimer += Time.deltaTime;
            if (_tickTimer >= DamageInterval)
            {
                _tickTimer -= DamageInterval;
                OnBurnTriggered?.Invoke(_damage);
            }
            yield return null;
        }
        _timerRoutine = null;
    }

    public void Reset()
    {
        _remainingDuration = Duration;
    }
}