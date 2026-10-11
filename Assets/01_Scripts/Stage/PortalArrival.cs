using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 도착 연출. 플레이어 머리 위 공중에 균열이 열리고 플레이어를 뱉어낸 뒤 닫힌다.
/// - 런 시작(시작 석상 앞)과 포탈로 다음 층에 내려왔을 때 쓴다.
/// - 균열이 열리는 동안 플레이어는 숨겨지고 조작이 막힌다. (PortalPlayerHider)
/// - 균열 프레임은 Resources/PortalRiftFrames에서 불러온다. 없으면 플레이어를 바로 보여준다.
/// - 페이드 방식(PortalRiftFrames.Style = Fade) : 플레이어 뒤에 균열이 열린 채로 화면이 밝아지고,
///   완전히 밝아지면 조작을 돌려주고 균열이 닫힌다. 포탈에 들어갈 때도 플레이어를 숨기지 않는다.
/// 게임 시간으로 움직인다. (일시정지하면 멈춘다)
/// </summary>
public class PortalArrival : MonoBehaviour
{
    private const float OpenFramesPerSecond = 16f;
    private const float CloseFramesPerSecond = 14f;
    private const float IdleHold = 0.15f;
    private const float SpitDuration = 0.45f;
    /// <summary>페이드 방식 : 완전히 밝아진 뒤 균열이 닫히기까지 기다리는 시간</summary>
    private const float FadeCloseDelay = 0.15f;
    /// <summary>페이드 방식 : 균열 바닥을 플레이어 발보다 이만큼 위(뒤)에 둔다. 플레이어가 균열 앞에 선다.</summary>
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

    /// <summary>층 이동을 페이드 방식으로 하는지 (균열 프레임이 없으면 false)</summary>
    public static bool UseFadeStyle
    {
        get
        {
            PortalRiftFrames frames = Frames;
            return frames != null && frames.Style == PortalTransitionStyle.Fade;
        }
    }

    /// <summary>플레이어를 숨기고(페이드 방식은 조작만 막고) delay 뒤 도착 연출을 시작한다. (런 시작)</summary>
    public static void PlayFor(GameObject player, float delay)
    {
        PortalPlayerHider hider = PortalPlayerHider.Hide(player, false, UseFadeStyle);
        Play(hider, delay);
    }

    /// <summary>
    /// 페이드 방식으로 포탈에 들어갈 때 : 포탈은 층 이동과 함께 바로 사라지므로,
    /// 같은 모양의 균열을 따로 남겨 화면이 완전히 검게 될 때까지 계속 일렁이게 한다.
    /// </summary>
    public static void LingerExitRift(SpriteRenderer source, Sprite[] idleFrames, float framesPerSecond)
    {
        if (source == null)
        {
            return;
        }

        var go = new GameObject("PortalExitRift");
        go.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        go.transform.localScale = source.transform.lossyScale;
        var rift = go.AddComponent<SpriteRenderer>();
        rift.sprite = source.sprite;
        rift.color = source.color;
        rift.sharedMaterial = source.sharedMaterial;
        rift.sortingLayerID = source.sortingLayerID;
        rift.sortingOrder = source.sortingOrder;
        rift.spriteSortPoint = source.spriteSortPoint;
        rift.flipX = source.flipX;
        go.AddComponent<PortalArrival>().StartCoroutine(LingerRoutine(go, rift, idleFrames, framesPerSecond));
    }

    private static IEnumerator LingerRoutine(GameObject owner, SpriteRenderer rift, Sprite[] idleFrames,
        float framesPerSecond)
    {
        float time = 0f;
        float waited = 0f;
        while (!ScreenFader.IsBlack && waited < ExitLingerTimeout)
        {
            time += Time.deltaTime;
            waited += Time.unscaledDeltaTime;
            if (idleFrames != null && idleFrames.Length > 0)
            {
                rift.sprite = idleFrames[(int)(time * framesPerSecond) % idleFrames.Length];
            }

            yield return null;
        }

        Destroy(owner);
    }

    /// <summary>이미 숨겨진 플레이어로 도착 연출을 시작한다. (층 이동 뒤)</summary>
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

        StartCoroutine(UseFadeStyle ? FadeRoutine() : PlayRoutine());
    }

    /// <summary>
    /// 페이드 방식 : 플레이어 뒤에 열린 균열을 바로 띄워 두고, 화면이 완전히 밝아지면 조작을 돌려주고 닫는다.
    /// </summary>
    private IEnumerator FadeRoutine()
    {
        PortalRiftFrames frames = Frames;
        Vector3 ground = _hider.PlayerPosition + Vector3.up * FadeRiftLift;
        transform.position = ground;
        var rift = gameObject.AddComponent<SpriteRenderer>();
        if (frames.Material != null)
        {
            rift.sharedMaterial = frames.Material;
        }

        rift.spriteSortPoint = SpriteSortPoint.Pivot;
        // 바닥 기준점이 플레이어보다 위라서 YSort상 플레이어 뒤에 그려진다.
        rift.sortingOrder = RiftSortingOrder(ground.y);

        float time = 0f;
        float waited = 0f;
        while (waited < _delay || ScreenFader.IsFading || SceneTransition.IsRunning)
        {
            time += Time.deltaTime;
            waited += Time.deltaTime;
            SetIdle(rift, frames, time);
            yield return null;
        }

        float hold = 0f;
        while (hold < FadeCloseDelay)
        {
            hold += Time.deltaTime;
            time += Time.deltaTime;
            SetIdle(rift, frames, time);
            yield return null;
        }

        _hider.RestoreNow();
        yield return PlayFrames(rift, frames.CloseFrames, CloseFramesPerSecond);
        Destroy(gameObject);
    }

    private IEnumerator PlayRoutine()
    {
        PortalRiftFrames frames = Frames;
        if (frames == null || frames.OpenFrames == null || frames.OpenFrames.Length == 0 || _hider == null)
        {
            _hider?.RestoreNow();
            Destroy(gameObject);
            yield break;
        }

        if (_delay > 0f)
        {
            yield return new WaitForSeconds(_delay);
        }

        Vector3 ground = _hider.PlayerPosition;
        transform.position = ground;
        var rift = gameObject.AddComponent<SpriteRenderer>();
        if (frames.Material != null)
        {
            rift.sharedMaterial = frames.Material;
        }

        rift.spriteSortPoint = SpriteSortPoint.Pivot;
        // 공중에 떠 있으므로 석상, 미션 오브젝트 등 바닥에 선 것보다 앞에 그린다.
        rift.sortingOrder = RiftSortingOrder(ground.y);
        Vector3 center = ground + (Vector3)frames.CenterOffset;

        // 1. 플레이어 위 공중에 균열이 열린다.
        yield return PlayFrames(rift, frames.OpenFrames, OpenFramesPerSecond);
        float hold = 0f;
        while (hold < IdleHold)
        {
            hold += Time.deltaTime;
            SetIdle(rift, frames, hold);
            yield return null;
        }

        // 2. 균열 가운데에서 플레이어가 튀어나와 바닥에 내려선다.
        List<SpriteRenderer> ghosts = _hider.CreateGhosts(rift.sortingOrder + 1);
        Vector3[] targets = new Vector3[ghosts.Count];
        Vector3[] scales = new Vector3[ghosts.Count];
        Color[] colors = new Color[ghosts.Count];
        for (int i = 0; i < ghosts.Count; i++)
        {
            targets[i] = ghosts[i].transform.position;
            scales[i] = ghosts[i].transform.localScale;
            colors[i] = ghosts[i].color;
        }

        Color tint = new(0.75f, 0.92f, 1f, 1f);
        float elapsed = 0f;
        while (elapsed < SpitDuration)
        {
            elapsed += Time.deltaTime;
            hold += Time.deltaTime;
            SetIdle(rift, frames, hold);
            float t = Mathf.Clamp01(elapsed / SpitDuration);
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);
            // 살짝 위로 튀었다가 내려앉는다.
            float hop = Mathf.Sin(t * Mathf.PI) * 0.6f;
            for (int i = 0; i < ghosts.Count; i++)
            {
                if (ghosts[i] == null)
                {
                    continue;
                }

                Transform ghost = ghosts[i].transform;
                ghost.position = Vector3.Lerp(center, targets[i], eased) + Vector3.up * hop;
                ghost.localScale = new Vector3(scales[i].x * Mathf.Lerp(0.15f, 1f, eased),
                    scales[i].y * Mathf.Lerp(0.4f, 1f, eased), scales[i].z);
                Color color = Color.Lerp(tint, colors[i], Mathf.Clamp01((t - 0.3f) / 0.7f));
                color.a = colors[i].a * Mathf.Clamp01(t / 0.15f);
                ghosts[i].color = color;
            }

            yield return null;
        }

        foreach (SpriteRenderer ghost in ghosts)
        {
            if (ghost != null)
            {
                Destroy(ghost.gameObject);
            }
        }

        _hider.RestoreNow();

        // 3. 균열이 닫힌다.
        yield return PlayFrames(rift, frames.CloseFrames, CloseFramesPerSecond);
        Destroy(gameObject);
    }

    private static void SetIdle(SpriteRenderer rift, PortalRiftFrames frames, float time)
    {
        if (frames.IdleFrames != null && frames.IdleFrames.Length > 0)
        {
            rift.sprite = frames.IdleFrames[(int)(time * 12f) % frames.IdleFrames.Length];
        }
    }

    private static IEnumerator PlayFrames(SpriteRenderer target, Sprite[] frames, float framesPerSecond)
    {
        if (frames == null || frames.Length == 0)
        {
            yield break;
        }

        float elapsed = 0f;
        float duration = frames.Length / framesPerSecond;
        while (elapsed < duration)
        {
            target.sprite = frames[Mathf.Min(frames.Length - 1, (int)(elapsed * framesPerSecond))];
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.sprite = frames[frames.Length - 1];
    }
}
