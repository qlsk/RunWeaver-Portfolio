using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 타이틀 화면 연출 순서. 연출 자체는 TitleScreen의 Animator(TitleScreen.controller) 클립이 한다.
/// - 등장 : 검은 화면에서 배경이 드러나고(FadeIn), 잠시 뒤 메뉴가 차례로 떠오른다.
///   배경 숨쉬기, 룬/꽃/촛불 빛, 안개 흐름, 로고 빛, 빛 알갱이는 Ambience / Motes 레이어가 계속 반복한다.
/// - 시작(새 게임) : "Start" 트리거 하나로 석판 룬이 빛나며 알갱이가 룬으로 빨려 들어가고,
///   룬에서 푸른 빛이 번진 뒤 암전한다. (Light 레이어의 StartLight 클립) 끝나면 받은 콜백을 부른다.
/// 메뉴 글자의 등장과 눌림은 TitleMenuView가 한다. 실제 시간으로 움직인다.
/// </summary>
[DisallowMultipleComponent]
public class TitleSequence : MonoBehaviour
{
    private static readonly int s_startHash = Animator.StringToHash("Start");

    [SerializeField] private Animator _animator;
    [SerializeField] private TitleMenuView _menu;

    [Header("등장")]
    [SerializeField, Min(0f)] private float _menuDelay = 2.5f;

    [Header("시작")]
    [Tooltip("시작 연출 길이 (StartLight 클립이 암전까지 끝나는 시간)")]
    [SerializeField, Min(0f)] private float _startDuration = 2.9f;

    private bool _isStarting;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
    }

    private void Start()
    {
        StartCoroutine(ShowMenuLater());
    }

    private IEnumerator ShowMenuLater()
    {
        yield return new WaitForSecondsRealtime(_menuDelay);
        if (_menu != null && !_isStarting)
        {
            _menu.PlayAppear();
        }
    }

    /// <summary>시작 연출을 하고 끝나면 onFinished를 부른다.</summary>
    public void PlayStart(int itemIndex, Action onFinished)
    {
        if (_isStarting)
        {
            return;
        }

        _isStarting = true;
        if (_animator != null)
        {
            _animator.SetTrigger(s_startHash);
        }

        StartCoroutine(PlayStartRoutine(itemIndex, onFinished));
    }

    private IEnumerator PlayStartRoutine(int itemIndex, Action onFinished)
    {
        float start = Time.unscaledTime;
        while (true)
        {
            float press = Time.unscaledTime - start;
            if (_menu != null)
            {
                _menu.SetLocked(true);
                _menu.SetPress(itemIndex, press);
            }

            if (press >= _startDuration)
            {
                break;
            }

            yield return null;
        }

        onFinished?.Invoke();
    }
}
