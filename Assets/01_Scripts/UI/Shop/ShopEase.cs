using UnityEngine;

/// <summary>
/// 상점 연출에 쓰는 이징 함수. 입력은 0~1로 잘라서 쓴다.
/// </summary>
public static class ShopEase
{
    public static float OutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float u = 1f - t;
        return 1f - u * u * u;
    }

    public static float InCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    /// <summary>목표를 살짝 넘었다가 돌아오는 곡선 (튀어나오는 느낌)</summary>
    public static float OutBack(float t, float overshoot = 1.70158f)
    {
        t = Mathf.Clamp01(t);
        float u = t - 1f;
        return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
    }

    /// <summary>0 → 1 → 0 으로 한 번 부풀었다 돌아오는 곡선</summary>
    public static float Bump(float t)
    {
        t = Mathf.Clamp01(t);
        return Mathf.Sin(t * Mathf.PI);
    }

    /// <summary>프레임 수와 상관없이 일정하게 목표로 다가가는 보간 계수</summary>
    public static float Damp(float sharpness, float deltaTime)
    {
        return 1f - Mathf.Exp(-sharpness * deltaTime);
    }
}
