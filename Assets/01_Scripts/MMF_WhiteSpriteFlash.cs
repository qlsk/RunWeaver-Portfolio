using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

/// 스프라이트의 알파를 유지한 흰색 머티리얼로 잠깐 전환하고 원래 머티리얼을 복원한다.
[System.Serializable]
[FeedbackPath("Renderer/White Sprite Flash")]
public class MMF_WhiteSpriteFlash : MMF_Feedback
{
    [MMFInspectorGroup("White Sprite Flash", true, 51)]
    public SpriteRenderer BoundSpriteRenderer;
    public SpriteRenderer[] AdditionalSpriteRenderers;
    public Material FlashMaterial;
    [Min(0.01f)] public float Duration = 0.08f;

    public override float FeedbackDuration
    {
        get => ApplyTimeMultiplier(Duration);
        set => Duration = value;
    }

    private readonly Dictionary<SpriteRenderer, Material> _originalMaterials = new();
    private Coroutine _flashCoroutine;

    protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
    {
        if (!Active || FlashMaterial == null)
            return;

        StopAndRestore();
        IsPlaying = true;
        Flash(BoundSpriteRenderer);
        if (AdditionalSpriteRenderers != null)
        {
            foreach (SpriteRenderer renderer in AdditionalSpriteRenderers)
                Flash(renderer);
        }

        _flashCoroutine = Owner.StartCoroutine(RestoreAfterFlash());
    }

    private void Flash(SpriteRenderer renderer)
    {
        if (renderer == null || _originalMaterials.ContainsKey(renderer))
            return;

        _originalMaterials.Add(renderer, renderer.sharedMaterial);
        renderer.sharedMaterial = FlashMaterial;
    }

    private IEnumerator RestoreAfterFlash()
    {
        yield return new WaitForSecondsRealtime(FeedbackDuration);
        _flashCoroutine = null;
        RestoreMaterials();
    }

    protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1f)
    {
        StopAndRestore();
    }

    protected override void CustomRestoreInitialValues()
    {
        StopAndRestore();
    }

    protected override void CustomSkipToTheEnd(Vector3 position, float feedbacksIntensity = 1f)
    {
        StopAndRestore();
    }

    private void StopAndRestore()
    {
        if (_flashCoroutine != null)
            Owner.StopCoroutine(_flashCoroutine);
        _flashCoroutine = null;
        RestoreMaterials();
    }

    private void RestoreMaterials()
    {
        foreach (var entry in _originalMaterials)
        {
            if (entry.Key != null)
                entry.Key.sharedMaterial = entry.Value;
        }
        _originalMaterials.Clear();
        IsPlaying = false;
    }
}
