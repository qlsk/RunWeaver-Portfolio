using System.Collections;
using UnityEngine;

/// <summary>
/// 바위 문 포탈의 보조 연출. (포탈 Portal, 도착 연출 PortalArrival이 같이 쓴다)
/// - RiseIn : 바닥에서 살짝 솟아오르며 서서히 나타난다. 흙먼지가 옅게 인다.
/// - SinkOut : 소용돌이가 닫힌 뒤 바위 문이 가라앉으며 흐려진다. 흙먼지가 옅게 인다.
/// - Flash : 들어갈 때 소용돌이가 한 번 밝게 부풀었다 가라앉는다. (같은 그림을 위에 겹쳐 밝힌다)
/// 바위 문 그림의 기준점(pivot)이 바닥이라 transform 위치를 바닥으로 본다. 게임 시간으로 움직인다.
/// </summary>
public static class PortalGateFx
{
    public const float RiseDuration = 0.45f;
    public const float RiseDistance = 0.3f;
    public const float SinkDuration = 0.6f;
    public const float SinkDistance = 0.25f;

    /// <summary>나타나기 : 처음 프레임부터 투명하게 숨긴 뒤 솟아오르며 나타난다.</summary>
    public static IEnumerator RiseIn(SpriteRenderer sprite, float duration = RiseDuration, float distance = RiseDistance)
    {
        if (sprite == null)
        {
            yield break;
        }

        Transform target = sprite.transform;
        Vector3 end = target.position;
        Color color = sprite.color;
        float alpha = color.a;
        Apply(sprite, end + Vector3.down * distance, color, 0f);
        PortalGateDust.Spawn(end, sprite.sortingLayerID, sprite.sortingOrder + 1, 0.7f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            if (sprite == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float k = Smooth(elapsed / duration);
            Apply(sprite, end + Vector3.down * (distance * (1f - k)), color, alpha * k);
        }

        Apply(sprite, end, color, alpha);
    }

    /// <summary>사라지기 : 가라앉으며 흐려진다. 끝나면 투명한 상태로 남는다. (지우는 것은 부른 쪽이 한다)</summary>
    public static IEnumerator SinkOut(SpriteRenderer sprite, float duration = SinkDuration, float distance = SinkDistance)
    {
        if (sprite == null)
        {
            yield break;
        }

        Transform target = sprite.transform;
        Vector3 start = target.position;
        Color color = sprite.color;
        float alpha = color.a;
        PortalGateDust.Spawn(start, sprite.sortingLayerID, sprite.sortingOrder + 1, 0.9f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            if (sprite == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float k = Smooth(elapsed / duration);
            Apply(sprite, start + Vector3.down * (distance * k), color, alpha * (1f - k));
        }

        Apply(sprite, start + Vector3.down * distance, color, 0f);
    }

    /// <summary>들어갈 때 반짝임. source 위에 같은 그림을 겹쳐 밝게 했다가 가라앉히고, 살짝 부풀린다.</summary>
    public static void Flash(SpriteRenderer source)
    {
        if (source == null)
        {
            return;
        }

        var go = new GameObject("PortalGateFlash");
        go.transform.SetParent(source.transform, false);
        go.AddComponent<PortalGateFlash>().Initialize(source);
    }

    public static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private static void Apply(SpriteRenderer sprite, Vector3 position, Color color, float alpha)
    {
        sprite.transform.position = position;
        color.a = alpha;
        sprite.color = color;
    }
}
