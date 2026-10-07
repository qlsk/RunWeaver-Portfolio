using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 스킬 칸에 대시 쿨타임과 쉴드(Barrier) 상태를 표시한다.
/// - Dash : 쿨타임 동안 아이콘이 어두워지고 시계 방향으로 다시 채워진다.
/// - Barrier : 쉴드 스택 수를 숫자로 보여주고, 다음 스택이 생길 때까지 아이콘이 채워진다. 스택이 0이면 어둡게 표시한다.
/// 플레이어(Dash, Shield)는 씬에서 찾는다. 없는 씬(마을)에서는 아이콘을 쓸 수 있는 상태로 둔다.
/// </summary>
[DisallowMultipleComponent]
public class SkillSlotsHud : MonoBehaviour
{
    private const float SearchInterval = 1f;

    [Header("Dash")]
    [SerializeField] private Image _dashIcon;

    [Header("Barrier (Shield)")]
    [SerializeField] private Image _barrierIcon;
    [Tooltip("쉴드 스택 수. 비워두면 숫자를 표시하지 않는다")]
    [SerializeField] private TMP_Text _barrierStackText;

    [Header("색")]
    [SerializeField] private Color _readyColor = Color.white;
    [SerializeField] private Color _coolingColor = new(0.4f, 0.4f, 0.4f, 1f);

    private Dash _dash;
    private Shield _shield;
    private float _nextSearchTime;

    private void Awake()
    {
        SetupFill(_dashIcon);
        SetupFill(_barrierIcon);
    }

    private void LateUpdate()
    {
        FindPlayerSkills();
        RefreshDash();
        RefreshBarrier();
    }

    private void FindPlayerSkills()
    {
        if ((_dash != null && _shield != null) || Time.unscaledTime < _nextSearchTime)
        {
            return;
        }

        _nextSearchTime = Time.unscaledTime + SearchInterval;
        if (_dash == null)
        {
            _dash = FindAnyObjectByType<Dash>();
        }

        if (_shield == null)
        {
            _shield = FindAnyObjectByType<Shield>();
        }
    }

    private void RefreshDash()
    {
        if (_dashIcon == null)
        {
            return;
        }

        float progress = 1f;
        if (_dash != null && _dash.CooldownDuration > 0f)
        {
            progress = 1f - _dash.CooldownRemaining / _dash.CooldownDuration;
        }

        SetIcon(_dashIcon, progress, progress >= 1f);
    }

    private void RefreshBarrier()
    {
        if (_shield == null)
        {
            SetIcon(_barrierIcon, 1f, true);
            SetText(string.Empty);
            return;
        }

        int stacks = _shield.CurrentShield;
        float progress = stacks >= _shield.MaxShield ? 1f : _shield.RechargeProgress;
        SetIcon(_barrierIcon, progress, stacks > 0);
        SetText(stacks > 0 ? stacks.ToString() : string.Empty);
    }

    private void SetIcon(Image icon, float fillAmount, bool isReady)
    {
        if (icon == null)
        {
            return;
        }

        icon.fillAmount = Mathf.Clamp01(fillAmount);
        icon.color = isReady ? _readyColor : _coolingColor;
    }

    private void SetText(string text)
    {
        if (_barrierStackText != null && _barrierStackText.text != text)
        {
            _barrierStackText.text = text;
        }
    }

    private static void SetupFill(Image icon)
    {
        if (icon == null)
        {
            return;
        }

        icon.type = Image.Type.Filled;
        icon.fillMethod = Image.FillMethod.Radial360;
        icon.fillOrigin = (int)Image.Origin360.Top;
        icon.fillClockwise = true;
    }
}
