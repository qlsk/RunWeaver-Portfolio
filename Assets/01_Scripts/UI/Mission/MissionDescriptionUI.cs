using TMPro;
using UnityEngine;
using System.Collections;

public class MissionDescriptionUI : MonoBehaviour
{
    [SerializeField] private StageManager _stageManager;
    [SerializeField] private TextMeshProUGUI _missionDescription;
    [SerializeField] private FadePanelView _depthPanel;
    [SerializeField, Min(0f)] private float _displayDuration = 10f;

    private FadePanelView _hintPanel;
    private Coroutine _showRoutine;

    private void Awake()
    {
        if (_missionDescription == null) return;

        if (!_missionDescription.TryGetComponent(out _hintPanel))
        {
            _hintPanel = _missionDescription.gameObject.AddComponent<FadePanelView>();
        }
    }

    public void OnEnable()
    {
        ClearMissionDescription(0);
        if (_stageManager == null) return;

        _stageManager.PhaseChanged += HandlePhaseChanged;
        HandlePhaseChanged(_stageManager.Phase);
    }

    public void OnDisable()
    {
        ClearMissionDescription(0);
        if (_stageManager == null) return;

        _stageManager.PhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(StagePhase phase)
    {
        if (phase == StagePhase.InMission && _stageManager != null && !_stageManager.IsBossStage)
        {
            ClearMissionDescription(0);
            _showRoutine = StartCoroutine(ShowAfterDepthPanel());
        }
        else
        {
            ClearMissionDescription(0);
        }
    }

    private IEnumerator ShowAfterDepthPanel()
    {
        while (_depthPanel != null && _depthPanel.gameObject.activeInHierarchy)
        {
            yield return null;
        }

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            yield return null;
            if (!PauseController.IsGamePaused)
            {
                elapsed += Time.unscaledDeltaTime;
            }
        }

        _showRoutine = null;
        if (_stageManager != null && _stageManager.Phase == StagePhase.InMission)
        {
            UpdateMissionDescription(_stageManager.CurrentDepth);
        }
    }

    public void UpdateMissionDescription(int currentDepth)
    {
        if (_stageManager != null && _stageManager.IsBossStage)
        {
            ClearMissionDescription(0);
            return;
        }

        if (_missionDescription == null || _stageManager == null || _hintPanel == null) return;

        _missionDescription.text = _stageManager.ObjectiveDescription;
        _missionDescription.enabled = true;
        _hintPanel.ConfigureLike(_depthPanel, _displayDuration);
        _hintPanel.Play();
    }

    public void ClearMissionDescription(int depth)
    {
        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        if (_hintPanel != null) _hintPanel.Hide();
        if (_missionDescription == null) return;

        _missionDescription.text = string.Empty;
        _missionDescription.enabled = false;
    }
}
