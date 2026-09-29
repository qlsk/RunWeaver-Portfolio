using TMPro;
using UnityEngine;

public class StageInfoPanelUI : MonoBehaviour
{
    [Header("UI 패널")]
    [Tooltip("비워두거나 본인을 연결하면 CanvasGroup을 통해 안전하게 표시/숨김 처리됩니다.")]
    [SerializeField] private GameObject infoPanel;

    [Header("스테이지 정보 텍스트 (TMP)")]
    [SerializeField] private TMP_Text depthText;
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private TMP_Text elementText;
    [SerializeField] private TMP_Text dangerLevelText;
    [SerializeField] private TMP_Text monsterInfoText;

    [Header("기본 더미 데이터")]
    [SerializeField] private string defaultDepth = "Depth 2";
    [SerializeField] private string defaultStageName = "작열하는 회랑";
    [SerializeField] private string defaultElement = "화염 (Fire)";
    [SerializeField] private string defaultDangerLevel = "위험도 ★★★☆☆";
    [SerializeField] private string defaultMonsterInfo = "화염 슬라임, 불꽃 임프";

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // CanvasGroup을 통해 투명도/상호작용으로 제어하여 GameObject 비활성화로 인한 이벤트 수신 중단을 방지합니다.
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        SetVisible(false);
    }

    private void OnEnable()
    {
        Portal.OnPlayerEntered += HandlePortalEntered;
        Portal.OnPlayerExited += HandlePortalExited;
    }

    private void OnDisable()
    {
        Portal.OnPlayerEntered -= HandlePortalEntered;
        Portal.OnPlayerExited -= HandlePortalExited;
    }

    private void HandlePortalEntered(Portal portal)
    {
        Debug.Log("[StageInfoPanelUI] 포탈 진입 이벤트 수신 -> 정보 UI 표시");
        SetStageInfo(defaultDepth, defaultStageName, defaultElement, defaultDangerLevel, defaultMonsterInfo);
        SetVisible(true);
    }

    private void HandlePortalExited(Portal portal)
    {
        Debug.Log("[StageInfoPanelUI] 포탈 이탈 이벤트 수신 -> 정보 UI 숨김");
        SetVisible(false);
    }

    public void SetVisible(bool isVisible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }

        if (infoPanel != null && infoPanel != gameObject)
        {
            infoPanel.SetActive(isVisible);
        }
    }

    /// <summary>
    /// 향후 StageDataSO 등 외부 데이터로부터 스테이지 정보를 주입할 때 사용
    /// </summary>
    public void SetStageInfo(string depth, string stageName, string element, string dangerLevel, string monsterInfo)
    {
        if (depthText != null) depthText.text = depth;
        if (stageNameText != null) stageNameText.text = stageName;
        if (elementText != null) elementText.text = element;
        if (dangerLevelText != null) dangerLevelText.text = dangerLevel;
        if (monsterInfoText != null) monsterInfoText.text = monsterInfo;
    }
}
