using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 룬북이 열려 있는 동안 게임을 멈춘다.
/// Time.timeScale을 0으로 만들어 물리 이동(플레이어, 적)과 시간 기반 처리(탄환, 스폰, 쿨타임)를 멈추고,
/// timeScale과 무관하게 매 프레임 입력을 읽는 컴포넌트(PlayerInputHandler, PlayerAttack 등)는 잠시 끈다.
/// 게임 전체 일시정지 시스템(GameManager 등)이 생기면 그쪽 API로 교체한다.
/// </summary>
[DisallowMultipleComponent]
public class RuneBookPauseController : MonoBehaviour
{
    [Tooltip("일시정지 동안 끌 컴포넌트 (입력을 직접 읽는 플레이어 컴포넌트)")]
    [SerializeField] private Behaviour[] _behavioursToDisable;

    private readonly List<Behaviour> _disabledBehaviours = new();
    private float _previousTimeScale = 1f;
    private bool _isPaused;

    public bool IsPaused => _isPaused;

    private void OnDisable()
    {
        // 룬북이 열린 채로 씬이 바뀌거나 오브젝트가 꺼져도 게임이 멈춘 채로 남지 않게 한다.
        Resume();
    }

    public void Pause()
    {
        if (_isPaused)
        {
            return;
        }

        _isPaused = true;
        // 피격 정지 프레임 도중이면 연출을 끝내고 연다. (끝난 연출이 일시정지를 풀거나, 닫을 때 멈춘 채 남지 않게)
        _previousTimeScale = GameTimeScale.BeginPause();
        Time.timeScale = 0f;

        _disabledBehaviours.Clear();
        if (_behavioursToDisable == null)
        {
            return;
        }

        foreach (Behaviour behaviour in _behavioursToDisable)
        {
            // 원래 꺼져 있던 컴포넌트(예: 사망으로 비활성화된 컴포넌트)는 건드리지 않는다.
            if (behaviour != null && behaviour.enabled)
            {
                behaviour.enabled = false;
                _disabledBehaviours.Add(behaviour);
            }
        }
    }

    public void Resume()
    {
        if (!_isPaused)
        {
            return;
        }

        _isPaused = false;
        Time.timeScale = _previousTimeScale;

        foreach (Behaviour behaviour in _disabledBehaviours)
        {
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        _disabledBehaviours.Clear();
    }
}
