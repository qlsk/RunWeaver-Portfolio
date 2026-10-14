using UnityEngine;

/// <summary>
/// 원소 반응 장판의 판정 모양.
/// 장판 이펙트는 바닥에 눕힌 타원(가로 : 세로 = 1 : VerticalRatio)으로 그려지므로 판정도 같은 타원을 쓴다.
/// radius는 타원의 가로 반지름이며, ReactionAreaVisuals가 이펙트 바닥 그림의 가로 반지름을 radius에 맞춘다.
/// </summary>
public static class ReactionAreaShape
{
    /// <summary>장판 이펙트 바닥 그림의 세로 / 가로 비율</summary>
    public const float VerticalRatio = 0.4f;

    /// <summary>타원에 걸친 콜라이더를 모두 돌려준다. (Physics2D.OverlapCircleAll 대신 쓴다)</summary>
    public static Collider2D[] OverlapAll(Vector2 center, float radius)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(center, radius);
        int count = 0;
        for (int i = 0; i < colliders.Length; i++)
        {
            // 콜라이더에서 중심에 가장 가까운 점이 타원 안이면 걸친 것으로 본다. (중심이 콜라이더 안이면 중심 자신)
            if (Contains(center, radius, colliders[i].ClosestPoint(center)))
                colliders[count++] = colliders[i];
        }

        System.Array.Resize(ref colliders, count);
        return colliders;
    }

    /// <summary>점이 타원 안에 있는지</summary>
    public static bool Contains(Vector2 center, float radius, Vector2 point)
    {
        if (radius <= 0f)
            return false;

        Vector2 offset = point - center;
        float x = offset.x / radius;
        float y = offset.y / (radius * VerticalRatio);
        return x * x + y * y <= 1f;
    }

    /// <summary>타원 안의 무작위 점 (중심 기준 오프셋)</summary>
    public static Vector2 RandomOffset(float radius)
    {
        Vector2 offset = Random.insideUnitCircle * radius;
        offset.y *= VerticalRatio;
        return offset;
    }
}
