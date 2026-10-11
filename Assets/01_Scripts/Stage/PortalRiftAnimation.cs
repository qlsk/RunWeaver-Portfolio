using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 균열(포탈) 애니메이션을 Animator로 재생하고 끝나길 기다리는 도우미.
/// 스프라이트는 애니메이션 클립이 바꾼다. 스크립트는 상태를 고르고 끝날 때까지 기다리기만 한다.
/// 컨트롤러 구조 : Open(끝나면 자동으로 Idle) → Idle(반복) → Close
/// - 필요한 상태가 컨트롤러에 없으면 오류를 남기고 기다리지 않고 바로 돌려준다. (잘못 연결해도 연출이 멈추지 않는다)
/// - 기다리는 최대 시간은 재생할 상태의 실제 길이로 정한다. 일시정지(timeScale 0)에는 같이 멈춘다.
/// </summary>
public static class PortalRiftAnimation
{
    public const string OpenState = "Open";
    public const string IdleState = "Idle";
    public const string CloseState = "Close";

    /// <summary>클립을 만든 프레임 속도. 재생 속도 배율(원하는 fps / 이 값)을 구할 때 쓴다.</summary>
    public const float ClipFramesPerSecond = 12f;

    /// <summary>예상 재생 시간에 곱하는 여유 배율과 더하는 여유 시간(초). 이 안에 끝나지 않으면 다음으로 넘어간다.</summary>
    private const float TimeoutScale = 2f;
    private const float TimeoutMargin = 1f;

    /// <summary>요청한 상태로 들어가길 기다리는 최대 시간(실제 시간, 초). 일시정지 중에는 세지 않는다.</summary>
    private const float EnterTimeout = 0.5f;

    /// <summary>오브젝트에 Animator를 붙이고 컨트롤러를 연결한다. 이미 있으면 컨트롤러만 바꾼다.</summary>
    public static Animator Attach(GameObject target, RuntimeAnimatorController controller)
    {
        if (!target.TryGetComponent(out Animator animator))
        {
            animator = target.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        return animator;
    }

    /// <summary>균열이 열리는 클립을 처음부터 재생하고, 끝나 Idle로 넘어갈 때까지 기다린다.</summary>
    public static IEnumerator PlayOpen(Animator animator, float speed = 1f)
    {
        // Open이 끝나면 Idle로 넘어가는 구조이므로 두 상태가 모두 있어야 한다.
        if (!CanPlay(animator, OpenState) || !CanPlay(animator, IdleState))
        {
            yield break;
        }

        animator.speed = speed;
        animator.Play(OpenState, 0, 0f);
        yield return WaitUntil(animator, OpenState, info =>
            info.IsName(IdleState) || (info.IsName(OpenState) && info.normalizedTime >= 1f));
        if (animator != null)
        {
            animator.speed = 1f;
        }
    }

    /// <summary>열린 채로 일렁이는 Idle을 재생한다. normalizedTime으로 이어서 재생할 수 있다.</summary>
    public static void PlayIdle(Animator animator, float normalizedTime = 0f)
    {
        if (!CanPlay(animator, IdleState))
        {
            return;
        }

        animator.speed = 1f;
        animator.Play(IdleState, 0, normalizedTime);
    }

    /// <summary>균열이 닫히는 클립을 재생하고 끝날 때까지 기다린다.</summary>
    public static IEnumerator PlayClose(Animator animator, float speed = 1f)
    {
        if (!CanPlay(animator, CloseState))
        {
            yield break;
        }

        animator.speed = speed;
        animator.Play(CloseState, 0, 0f);
        yield return WaitUntil(animator, CloseState, info => info.IsName(CloseState) && info.normalizedTime >= 1f);
        if (animator != null)
        {
            animator.speed = 1f;
        }
    }

    /// <summary>컨트롤러에 해당 상태가 있는지</summary>
    public static bool HasState(Animator animator, string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return false;
        }

        // 방금 붙인 Animator는 첫 갱신 전이라 아직 초기화되지 않았을 수 있다. 그때만 초기화한 뒤 확인한다.
        if (!animator.isInitialized)
        {
            animator.Rebind();
        }

        return animator.HasState(0, Animator.StringToHash(stateName));
    }

    private static bool CanPlay(Animator animator, string stateName)
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
        {
            return false;
        }

        if (!HasState(animator, stateName))
        {
            Debug.LogError($"[PortalRiftAnimation] 컨트롤러 '{animator.runtimeAnimatorController.name}'에 '{stateName}' 상태가 없어 재생하지 못했습니다.", animator);
            return false;
        }

        return true;
    }

    private static IEnumerator WaitUntil(Animator animator, string stateName, Func<AnimatorStateInfo, bool> isDone)
    {
        // 1. 요청한 상태로 들어간 것을 확인한다. Play 직후에는 상태 정보가 다음 갱신에 반영되므로,
        //    이전 상태의 길이나 완료 조건으로 잘못 판단하지 않도록 들어갈 때까지 기다린다.
        //    일시정지(timeScale 0) 중에는 시간을 세지 않는다.
        int stateHash = Animator.StringToHash(stateName);
        float enterWaited = 0f;
        AnimatorStateInfo info;
        while (true)
        {
            yield return null;
            if (animator == null || !animator.isActiveAndEnabled)
            {
                yield break;
            }

            info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.shortNameHash == stateHash)
            {
                break;
            }

            if (Time.timeScale > 0f)
            {
                enterWaited += Time.unscaledDeltaTime;
            }

            if (enterWaited >= EnterTimeout)
            {
                Debug.LogWarning($"[PortalRiftAnimation] '{stateName}' 상태로 {EnterTimeout:F1}초 안에 들어가지 못해 넘어갑니다.", animator);
                yield break;
            }
        }

        // 2. 기다리는 최대 시간 : 들어간 상태의 실제 길이(재생 속도 반영)에 여유를 더한다.
        float expected = info.length / Mathf.Max(0.01f, animator.speed);
        float timeout = expected * TimeoutScale + TimeoutMargin;
        float elapsed = 0f;
        while (animator != null && animator.isActiveAndEnabled && !isDone(animator.GetCurrentAnimatorStateInfo(0)))
        {
            if (elapsed >= timeout)
            {
                Debug.LogWarning($"[PortalRiftAnimation] '{stateName}' 재생이 {timeout:F1}초 안에 끝나지 않아 넘어갑니다.", animator);
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
