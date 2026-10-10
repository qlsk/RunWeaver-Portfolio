using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 체력바. Feel의 MMProgressBar로 표시한다.
/// 체력이 줄면 앞쪽 바는 즉시 줄고, 뒤쪽 흰색 지연 바(DelayedBarDecreasing)가 잠시 남아 있다가 천천히 따라 줄어든다.
/// 지연 시간과 줄어드는 속도는 MMProgressBar의 "Delayed Bar Decreasing" 항목에서 조정한다.
/// </summary>
[DisallowMultipleComponent]
public class HealthBarView : MonoBehaviour
{
    [SerializeField] private MMProgressBar _progressBar;

    private bool _hasShownOnce;

    /// <summary>앞쪽 체력 바 이미지 (체력 위험 연출이 색을 바꾼다)</summary>
    public Image FillImage =>
        _progressBar != null && _progressBar.ForegroundBar != null ? _progressBar.ForegroundBar.GetComponent<Image>() : null;

    private void Awake()
    {
        if (_progressBar == null)
        {
            Debug.LogError("[HealthBarView] MMProgressBar가 할당되지 않았습니다.", this);
        }
    }

    public void SetHealth(float currentHealth, float maxHealth)
    {
        if (_progressBar == null)
        {
            return;
        }

        float ratio = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        // 처음 표시할 때는 연출 없이 바로 맞추고, 이후 변화부터 지연 감소 연출을 보여준다.
        if (!_hasShownOnce)
        {
            _hasShownOnce = true;
            _progressBar.SetBar01(ratio);
            return;
        }

        _progressBar.UpdateBar01(ratio);
    }
}
