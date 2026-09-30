using TMPro;
using UnityEngine;

/// <summary>
/// 스테이지에 입장하면 현재 심도를 심도 패널에 표시한다.
/// 패널은 페이드 인 → 유지 → 페이드 아웃으로 잠시 보였다가 사라진다. (연출 값은 패널의 FadePanelView에서 조정)
/// 패널이 꺼져 있는 동안에도 이벤트를 받아야 하므로 항상 켜져 있는 오브젝트에 붙인다.
/// </summary>
[DisallowMultipleComponent]
public class DepthPanelPresenter : MonoBehaviour
{
    [SerializeField] private StageStatusSource _source;
    [SerializeField] private FadePanelView _panel;
    [SerializeField] private TMP_Text _depthText;

    [Tooltip("{0}: 스테이지 이름, {1}: 심도")]
    [SerializeField] private string _depthFormat = "{0} - {1}";

    private void Awake()
    {
        // 씬에 켜진 상태로 저장되어 있어도 입장 전에는 보이지 않게 한다.
        if (_panel != null)
        {
            _panel.Hide();
        }
    }

    private void OnEnable()
    {
        if (_source == null)
        {
            Debug.LogWarning("[DepthPanelPresenter] 스테이지 상태 소스가 할당되지 않았습니다.", this);
            return;
        }

        _source.StageEntered += HandleStageEntered;
    }

    private void OnDisable()
    {
        if (_source != null)
        {
            _source.StageEntered -= HandleStageEntered;
        }
    }

    private void HandleStageEntered(int depth, string stageName)
    {
        if (_depthText != null)
        {
            _depthText.text = string.Format(_depthFormat, stageName, depth);
        }

        if (_panel != null)
        {
            _panel.Play();
        }
    }
}
