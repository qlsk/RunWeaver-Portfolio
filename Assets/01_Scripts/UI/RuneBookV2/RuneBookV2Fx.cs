using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 새 룬북(RuneBookV2) 연출. RuneBookV2Presenter가 붙이고 호출한다. 게임이 멈춘 동안에도 움직인다. (실제 시간)
/// - 열기 : 어둡게 깔리고, 두 페이지가 아래에서 살짝 떠오르며 나타난다.
/// - 닫기 : 반대로 아래로 살짝 가라앉으며 사라진다. (RuneBookV2Toggle이 끝난 뒤 끈다)
/// - 룬 워드 장착 : 들어간 칸이 커졌다 돌아오며 빛나고, 쓰게 될 룬석 칸이 통 튄다.
/// - 룬 워드 해제 : 칸이 움츠렸다 돌아오며 어두운 빛이 번진다.
/// - 원소 없음 경고 : MAIN 칸이 붉게 세 번 깜빡인다.
/// - 룬석 부족 : 모자란 룬석 칸이 붉게 빛나며 좌우로 떤다.
/// - Confirm : 장착된 칸이 왼쪽부터 차례로 강하게 빛난다.
/// 꺼질 때 모든 연출을 멈추고 크기, 회전, 투명도를 되돌린다.
/// </summary>
[DisallowMultipleComponent]
public class RuneBookV2Fx : MonoBehaviour
{
    [Header("열기")]
    [SerializeField, Min(0.01f)] private float _openDuration = 0.4f;
    [SerializeField] private float _openRise = 36f;
    [SerializeField, Min(0.01f)] private float _closeDuration = 0.25f;

    [Header("색")]
    [SerializeField] private Color _equipColor = new(1f, 0.86f, 0.55f, 0.9f);
    [SerializeField] private Color _removeColor = new(0.35f, 0.25f, 0.2f, 0.7f);
    [SerializeField] private Color _warningColor = new(0.9f, 0.2f, 0.16f, 0.85f);
    [SerializeField] private Color _confirmColor = new(1f, 0.9f, 0.6f, 1f);

    private UiRiseParts _parts;
    private readonly Dictionary<Transform, (Vector3 Scale, Quaternion Rotation)> _touched = new();
    private readonly List<GameObject> _overlays = new();

    public void PlayOpen()
    {
        _parts ??= new UiRiseParts(transform, _openRise);
        _parts.Apply(0f);
        StartCoroutine(OpenRoutine());
    }

    /// <summary>닫기 연출. 다른 오브젝트(항상 켜진 토글)에서 코루틴으로 돌린다.</summary>
    public IEnumerator PlayClose()
    {
        StopAllCoroutines();
        _parts ??= new UiRiseParts(transform, _openRise);
        float elapsed = 0f;
        while (elapsed < _closeDuration && isActiveAndEnabled)
        {
            elapsed += Time.unscaledDeltaTime;
            _parts.Apply(1f - UiRiseParts.EaseOut(elapsed / _closeDuration));
            yield return null;
        }
    }

    private IEnumerator OpenRoutine()
    {
        float elapsed = 0f;
        while (elapsed < _openDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _parts.Apply(UiRiseParts.EaseOut(elapsed / _openDuration));
            yield return null;
        }

        _parts.Apply(1f);
    }

    /// <summary>룬 워드를 넣은 칸</summary>
    public void PlayEquip(RuneSocketView socket, Color? elementColor)
    {
        if (socket == null)
        {
            return;
        }

        Color color = elementColor ?? _equipColor;
        color.a = 0.9f;
        StartCoroutine(PunchRoutine(socket.transform, 1.22f, 0.3f));
        StartCoroutine(FlashRoutine(socket.transform, socket.FlashSprite, color, 1f, 1.8f, 0.45f, 0f));
    }

    /// <summary>룬 워드를 뺀 칸</summary>
    public void PlayRemove(RuneSocketView socket)
    {
        if (socket == null)
        {
            return;
        }

        StartCoroutine(PunchRoutine(socket.transform, 0.82f, 0.25f));
        StartCoroutine(FlashRoutine(socket.transform, socket.FlashSprite, _removeColor, 0.9f, 1.4f, 0.35f, 0f));
    }

    /// <summary>쓰게 될 룬석 칸</summary>
    public void PlayStoneUse(Component cell)
    {
        if (cell != null)
        {
            StartCoroutine(PunchRoutine(cell.transform, 1.25f, 0.28f));
        }
    }

    /// <summary>모자란 룬석 칸</summary>
    public void PlayStoneShortage(Component cell, Sprite flashSprite)
    {
        if (cell == null)
        {
            return;
        }

        StartCoroutine(ShakeRoutine(cell.transform, 0.45f));
        if (flashSprite != null)
        {
            StartCoroutine(FlashRoutine(cell.transform, flashSprite, _warningColor, 1f, 1.5f, 0.5f, 0f));
        }
    }

    /// <summary>비어 있으면 안 되는 칸 (원소 없음)</summary>
    public void PlayWarning(RuneSocketView socket)
    {
        if (socket == null)
        {
            return;
        }

        StartCoroutine(ShakeRoutine(socket.transform, 0.35f));
        for (int i = 0; i < 3; i++)
        {
            StartCoroutine(FlashRoutine(socket.transform, socket.FlashSprite, _warningColor, 1f, 1.25f, 0.22f, i * 0.2f));
        }
    }

    /// <summary>Confirm 성공 : 장착된 칸이 왼쪽부터 차례로 빛난다.</summary>
    public void PlayConfirm(IReadOnlyList<RuneSocketView> sockets, IReadOnlyList<Color?> colors)
    {
        for (int i = 0; i < sockets.Count; i++)
        {
            RuneSocketView socket = sockets[i];
            if (socket == null)
            {
                continue;
            }

            float delay = i * 0.07f;
            Color color = colors != null && i < colors.Count && colors[i].HasValue ? colors[i].Value : _confirmColor;
            color.a = 1f;
            StartCoroutine(DelayedPunch(socket.transform, delay));
            StartCoroutine(FlashRoutine(socket.transform, socket.FlashSprite, color, 1f, 2.1f, 0.5f, delay));
            StartCoroutine(FlashRoutine(socket.transform, socket.FlashSprite, Color.white, 0.6f, 1.3f, 0.3f, delay));
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _parts?.Restore();
        foreach (KeyValuePair<Transform, (Vector3 Scale, Quaternion Rotation)> pair in _touched)
        {
            if (pair.Key != null)
            {
                pair.Key.localScale = pair.Value.Scale;
                pair.Key.localRotation = pair.Value.Rotation;
            }
        }

        _touched.Clear();
        foreach (GameObject overlay in _overlays)
        {
            if (overlay != null)
            {
                Destroy(overlay);
            }
        }

        _overlays.Clear();
    }

    // ───────────────────────── 공통 ─────────────────────────

    private IEnumerator DelayedPunch(Transform target, float delay)
    {
        yield return Wait(delay);
        yield return PunchRoutine(target, 1.15f, 0.3f);
    }

    /// <summary>크기를 peak까지 바로 바꾼 뒤 원래 크기로 돌아온다.</summary>
    private IEnumerator PunchRoutine(Transform target, float peak, float duration)
    {
        Vector3 baseScale = Remember(target).Scale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = UiRiseParts.EaseOut(elapsed / duration);
            target.localScale = baseScale * Mathf.Lerp(peak, 1f, t);
            yield return null;
        }

        target.localScale = baseScale;
    }

    private IEnumerator ShakeRoutine(Transform target, float duration)
    {
        Quaternion baseRotation = Remember(target).Rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float angle = Mathf.Sin(elapsed * 55f) * 9f * (1f - t);
            target.localRotation = baseRotation * Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }

        target.localRotation = baseRotation;
    }

    /// <summary>대상 위에 빛 이미지를 띄워 커지며 사라지게 한다.</summary>
    private IEnumerator FlashRoutine(Transform target, Sprite sprite, Color color, float fromScale, float toScale,
        float duration, float delay)
    {
        if (sprite == null)
        {
            yield break;
        }

        yield return Wait(delay);

        var go = new GameObject("Fx_Flash", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        _overlays.Add(go);
        var rect = (RectTransform)go.transform;
        rect.SetParent(target, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        go.GetComponent<LayoutElement>().ignoreLayout = true;
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;

        float elapsed = 0f;
        while (elapsed < duration && go != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = UiRiseParts.EaseOut(t);
            rect.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, eased);
            Color current = color;
            current.a = color.a * (1f - t) * (1f - t);
            image.color = current;
            yield return null;
        }

        _overlays.Remove(go);
        if (go != null)
        {
            Destroy(go);
        }
    }

    private static IEnumerator Wait(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    /// <summary>처음 건드릴 때의 크기와 회전을 기억해 두고 돌려준다. (연출이 겹쳐도 원래 값으로 돌아오게)</summary>
    private (Vector3 Scale, Quaternion Rotation) Remember(Transform target)
    {
        if (!_touched.TryGetValue(target, out (Vector3 Scale, Quaternion Rotation) original))
        {
            original = (target.localScale, target.localRotation);
            _touched[target] = original;
        }

        return original;
    }
}
