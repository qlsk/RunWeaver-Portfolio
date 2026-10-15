#ifndef RUNEWEAVER_MAGIC_CIRCLE_REVEAL_INCLUDED
#define RUNEWEAVER_MAGIC_CIRCLE_REVEAL_INCLUDED

float4 MagicCircleLayerMasks(float distanceFromCenter, float3 stageRadii, float feather)
{
    float3 innerMasks = 1.0 - smoothstep(stageRadii - feather, stageRadii, distanceFromCenter);
    return float4(innerMasks.x, innerMasks.y - innerMasks.x,
        innerMasks.z - innerMasks.y, 1.0 - innerMasks.z);
}

float MagicCircleRevealAlpha(float distanceFromCenter, float progress, float3 stageRadii, float feather)
{
    float4 layerAlpha = saturate(saturate(progress) * 4.0 - float4(0.0, 1.0, 2.0, 3.0));
    return dot(MagicCircleLayerMasks(distanceFromCenter, stageRadii, feather), layerAlpha);
}

float4 MagicCircleRotatedSample(float2 uv, float angle)
{
    float sine, cosine;
    sincos(angle, sine, cosine);
    float2 offset = uv - 0.5;
    float2 rotatedUV = float2(cosine * offset.x - sine * offset.y,
        sine * offset.x + cosine * offset.y) + 0.5;
    float inBounds = step(0.0, rotatedUV.x) * step(rotatedUV.x, 1.0)
        * step(0.0, rotatedUV.y) * step(rotatedUV.y, 1.0);
    return SampleSpriteTexture(saturate(rotatedUV)) * inBounds;
}

float4 MagicCircleRotatedTexture(float2 uv, float3 stageRadii, float feather, float4 speeds, float time)
{
    float4 masks = MagicCircleLayerMasks(length(uv - 0.5) * 1.41421356, stageRadii, feather);
    float4 angles = -speeds * time * 0.01745329252;
    return MagicCircleRotatedSample(uv, angles.x) * masks.x
        + MagicCircleRotatedSample(uv, angles.y) * masks.y
        + MagicCircleRotatedSample(uv, angles.z) * masks.z
        + MagicCircleRotatedSample(uv, angles.w) * masks.w;
}

#endif
