using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 플레이어 도착 연출. (런 시작, 포탈로 다음 층에 내려왔을 때)
/// 화면이 어두운 동안 플레이어 뒤에 열린 균열(PortalRiftFrames의 프리팹)을 띄워 두고, 화면이 완전히 밝아지면
/// 조작을 돌려주고 균열을 닫는다. 닫힌 바위 문은 가라앉으며 흐려진다. (모두 프리팹 Animator의 클립)
/// 포탈에 들어갈 때도 같은 균열을 남겨 화면이 다 덮일 때까지 일렁이게 한다. (LingerExitRift)
/// 게임 시간으로 움직인다. (일시정지하면 멈춘다)
/// </summary>
public class PortalArrival : MonoBehaviour
{
    private const float CloseFramesPerSecond = 14f;
    /// <summary>완전히 밝아진 뒤 균열이 닫히기까지 기다리는 시간</summary>
    private const float FadeCloseDelay = 0.15f;
    /// <summary>균열 바닥을 플레이어 발보다 이만큼 위(뒤)에 둔다. 플레이어가 균열 앞에 선다.</summary>
    private const float FadeRiftLift = 0.5f;
    /// <summary>포탈에 들어갈 때 남겨 두는 균열이 화면이 검게 되지 않아도 사라지는 시간 (실제 시간)</summary>
    private const float ExitLingerTimeout = 4f;

    /// <summary>
    /// 바닥 기준점(groundY)으로 정렬 순서를 정한다. 씬의 YSort와 같은 식(10000 - y*100)에
    /// 공중에 떠 있는 만큼 조금 앞으로 당긴다. (석상, 미션 오브젝트 등 YSort를 쓰는 것과 앞뒤가 맞는다)
    /// </summary>
    public static int RiftSortingOrder(float groundY)
    {
        return Mathf.Clamp(10000 - Mathf.RoundToInt(groundY * 100f) + 5, -32768, 32766);
    }

    private static PortalRiftFrames s_frames;

    private PortalPlayerHider _hider;
    private float _delay;
    private GameObject _rift;

    /// <summary>균열 연출을 쓸 수 있는지 (설정과 프리팹이 있어야 한다)</summary>
    public static bool UseFadeStyle => Frames != null && Frames.RiftPrefab != null;

    /// <summary>조작을 막고 delay 뒤 도착 연출을 시작한다. (런 시작)</summary>
    public static void PlayFor(GameObject player, float delay)
    {
        PortalPlayerHider hider = PortalPlayerHider.Hide(player, false, true);
        Play(hider, delay);
    }

    /// <summary>
    /// 포탈에 들어갈 때 : 포탈은 층 이동과 함께 바로 사라지므로, 같은 모양의 균열을 따로 남겨
    /// 화면이 완전히 검게 될 때까지 일렁이게 한다. 남긴 균열의 소용돌이는 한 번 밝게 부풀었다 가라앉는다.
    /// </summary>
    public static void LingerExitRift(Animator source, Vector3 ground, int sortingOrder)
    {
        if (!UseFadeStyle)
        {
            return;
        }

        RuntimeAnimatorController controller = source != null ? source.runtimeAnimatorController : null;
        Animator animator = SpawnRift(ground, sortingOrder, controller, out GameObject rift);
        float time = source != null ? Mathf.Repeat(source.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f) : 0f;
        PortalRiftAnimation.PlayIdle(animator, time);
        PortalRiftAnimation.PlayLayer(animator, PortalRiftAnimation.FlashLayer, PortalRiftAnimation.FlashState);
        rift.AddComponent<PortalArrival>().StartCoroutine(LingerRoutine(rift));
    }

    private static IEnumerator LingerRoutine(GameObject owner)
    {
        float waited = 0f;
        while (!ScreenFader.IsBlack && waited < ExitLingerTimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        Destroy(owner);
    }

    /// <summary>균열 프리팹을 바닥 위치에 만들고 정렬 순서를 맞춘다.</summary>
    private static Animator SpawnRift(Vector3 ground, int sortingOrder, RuntimeAnimatorController controller, out GameObject rift)
    {
        rift = Instantiate(Frames.RiftPrefab, ground, Quaternion.identity);
        foreach (SortingGroup group in rift.GetComponentsInChildren<SortingGroup>())
        {
            group.sortingOrder = sortingOrder;
        }

        Animator animator = rift.GetComponentInChildren<Animator>();
        if (animator != null && controller != null)
        {
            animator.runtimeAnimatorController = controller;
        }

        return animator;
    }

    /// <summary>이미 조작을 막은 플레이어로 도착 연출을 시작한다. (층 이동 뒤)</summary>
    public static void Play(PortalPlayerHider hider, float delay = 0f)
    {
        if (hider == null)
        {
            return;
        }

        var arrival = new GameObject("PortalArrival").AddComponent<PortalArrival>();
        arrival._hider = hider;
        arrival._delay = delay;
    }

    private static PortalRiftFrames Frames
    {
        get
        {
            if (s_frames == null)
            {
                s_frames = Resources.Load<PortalRiftFrames>(PortalRiftFrames.ResourcePath);
            }

            return s_frames;
        }
    }

    private void Start()
    {
        if (_hider == null)
        {
            return; // LingerExitRift로 만든 경우
        }

        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        if (!UseFadeStyle)
        {
            _hider.RestoreNow();
            Destroy(gameObject);
            yield break;
        }

        // 바닥 기준점이 플레이어보다 위라서 YSort상 플레이어 뒤에 그려진다.
        Vector3 ground = _hider.PlayerPosition + Vector3.up * FadeRiftLift;
        Animator animator = SpawnRift(ground, RiftSortingOrder(ground.y), null, out _rift);
        PortalRiftAnimation.PlayIdle(animator);

        float waited = 0f;
        while (waited < _delay || ScreenFader.IsFading || SceneTransition.IsRunning)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        float hold = 0f;
        while (hold < FadeCloseDelay)
        {
            hold += Time.deltaTime;
            yield return null;
        }

        _hider.RestoreNow();
        yield return PortalRiftAnimation.PlayClose(animator, CloseFramesPerSecond / PortalRiftAnimation.ClipFramesPerSecond);

        // 닫힌 바위 문이 한 번에 사라지지 않고 가라앉으며 흐려진다.
        yield return PortalRiftAnimation.PlayLayerAndWait(animator, PortalRiftAnimation.GateLayer, PortalRiftAnimation.SinkState);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_rift != null)
        {
            Destroy(_rift);
        }
    }
}
