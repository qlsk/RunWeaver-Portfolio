using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 도착 연출. 플레이어 머리 위 공중에 균열이 열리고 플레이어를 뱉어낸 뒤 닫힌다.
/// - 런 시작(시작 석상 앞)과 포탈로 다음 층에 내려왔을 때 쓴다.
/// - 균열이 열리는 동안 플레이어는 숨겨지고 조작이 막힌다. (PortalPlayerHider)
/// - 균열 프레임은 Resources/PortalRiftFrames에서 불러온다. 없으면 플레이어를 바로 보여준다.
/// 게임 시간으로 움직인다. (일시정지하면 멈춘다)
/// </summary>
public class PortalArrival : MonoBehaviour
{
    private const float OpenFramesPerSecond = 16f;
    private const float CloseFramesPerSecond = 14f;
    private const float IdleHold = 0.15f;
    private const float SpitDuration = 0.45f;
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

    /// <summary>플레이어를 숨기고 delay 뒤 도착 연출을 시작한다. (런 시작)</summary>
    public static void PlayFor(GameObject player, float delay)
    {
        PortalPlayerHider hider = PortalPlayerHider.Hide(player, false);
        Play(hider, delay);
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
        StartCoroutine(PlayRoutine());
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
