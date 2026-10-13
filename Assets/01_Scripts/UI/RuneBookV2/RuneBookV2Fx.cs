using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 새 룬북(RuneBookV2) 연출을 고르는 곳. RuneBookV2Presenter가 붙이고 호출한다. 게임이 멈춘 동안에도 움직인다. (실제 시간)
/// 움직임은 모두 프리팹의 Animator 클립이 한다.
/// - 열기 / 닫기 : 룬북 루트 Animator(RuneBookV2.controller)의 Open / Close. 어둡게 깔리고 두 페이지가 떠오르거나 가라앉는다.
/// - 칸(RuneSocketView)마다 Animator(RuneSocketFx.controller) : Equip, Remove, Warning, Confirm
///   빛 색은 원소마다 달라서 재생하기 전에 FxFlash 이미지의 색만 정해 준다. (투명도와 크기는 클립이 정한다)
/// - 룬석 칸(RuneStoneCellView)마다 Animator(RuneStoneCellFx.controller) : Use, Shortage
/// </summary>
[DisallowMultipleComponent]
public class RuneBookV2Fx : MonoBehaviour
{
    private const string FlashName = "FxFlash";
    private const string Flash2Name = "FxFlash2";

    [Header("색")]
    [SerializeField] private Color _equipColor = new(1f, 0.86f, 0.55f, 0.9f);
    [SerializeField] private Color _removeColor = new(0.35f, 0.25f, 0.2f, 0.7f);
    [SerializeField] private Color _warningColor = new(0.9f, 0.2f, 0.16f, 0.85f);
    [SerializeField] private Color _confirmColor = new(1f, 0.9f, 0.6f, 1f);

    [Header("Confirm")]
    [Tooltip("칸이 왼쪽부터 차례로 빛나는 간격 (초)")]
    [SerializeField, Min(0f)] private float _confirmStagger = 0.07f;
    [SerializeField, Min(0.01f)] private float _closeDuration = 0.25f;

    private Animator _animator;

    private Animator BookAnimator => _animator != null ? _animator : _animator = GetComponent<Animator>();

    public void PlayOpen()
    {
        if (BookAnimator != null)
        {
            BookAnimator.Play("Open", 0, 0f);
        }
    }

    /// <summary>닫기 연출. 다른 오브젝트(항상 켜진 토글)에서 코루틴으로 돌린다.</summary>
    public IEnumerator PlayClose()
    {
        StopAllCoroutines();
        if (BookAnimator == null || !isActiveAndEnabled)
        {
            yield break;
        }

        BookAnimator.Play("Close", 0, 0f);
        float elapsed = 0f;
        while (elapsed < _closeDuration && isActiveAndEnabled)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    /// <summary>룬 워드를 넣은 칸</summary>
    public void PlayEquip(RuneSocketView socket, Color? elementColor)
    {
        Play(socket, "Equip", elementColor ?? _equipColor, Color.white);
    }

    /// <summary>룬 워드를 뺀 칸</summary>
    public void PlayRemove(RuneSocketView socket)
    {
        Play(socket, "Remove", _removeColor, Color.white);
    }

    /// <summary>쓰게 될 룬석 칸</summary>
    public void PlayStoneUse(Component cell)
    {
        Play(cell, "Use", null, null);
    }

    /// <summary>모자란 룬석 칸</summary>
    public void PlayStoneShortage(Component cell, Sprite flashSprite)
    {
        Play(cell, "Shortage", _warningColor, null);
    }

    /// <summary>비어 있으면 안 되는 칸 (원소 없음)</summary>
    public void PlayWarning(RuneSocketView socket)
    {
        Play(socket, "Warning", _warningColor, Color.white);
    }

    /// <summary>Confirm 성공 : 장착된 칸이 왼쪽부터 차례로 빛난다.</summary>
    public void PlayConfirm(IReadOnlyList<RuneSocketView> sockets, IReadOnlyList<Color?> colors)
    {
        for (int i = 0; i < sockets.Count; i++)
        {
            Color color = colors != null && i < colors.Count && colors[i].HasValue ? colors[i].Value : _confirmColor;
            StartCoroutine(PlayLater(sockets[i], color, i * _confirmStagger));
        }
    }

    private IEnumerator PlayLater(RuneSocketView socket, Color color, float delay)
    {
        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Play(socket, "Confirm", color, Color.white);
    }

    /// <summary>대상의 Animator 상태를 처음부터 재생한다. 빛 이미지 색(투명도 제외)을 먼저 정한다.</summary>
    private static void Play(Component target, string state, Color? flashColor, Color? flash2Color)
    {
        if (target == null || !target.TryGetComponent(out Animator animator) || !animator.isActiveAndEnabled)
        {
            return;
        }

        SetColor(target.transform.Find(FlashName), flashColor);
        SetColor(target.transform.Find(Flash2Name), flash2Color);
        animator.Play(state, 0, 0f);
    }

    private static void SetColor(Transform flash, Color? color)
    {
        if (flash == null || !color.HasValue || !flash.TryGetComponent(out Image image))
        {
            return;
        }

        Color current = image.color;
        Color next = color.Value;
        next.a = current.a;
        image.color = next;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
