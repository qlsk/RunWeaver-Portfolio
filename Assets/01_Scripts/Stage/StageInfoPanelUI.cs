using TMPro;
using UnityEngine;

public class StageInfoPanelUI : MonoBehaviour
{
    [Header("UI 패널")]
    [Tooltip("비워두거나 본인을 연결하면 CanvasGroup을 통해 안전하게 표시/숨김 처리됩니다.")]
    [SerializeField] private GameObject infoPanel;

    [Header("스테이지 정보 텍스트 (TMP) - 비워두면 자식 오브젝트에서 이름으로 자동 탐색")]
    [SerializeField] private TMP_Text depthText;
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private TMP_Text elementText;
    [SerializeField] private TMP_Text dangerLevelText;
    [SerializeField] private TMP_Text monsterInfoText;

    [Header("기본 더미 데이터 (비워둘 경우 자동 디폴트값 적용)")]
    [SerializeField] private string defaultDepth = "Depth 2";
    [SerializeField] private string defaultStageName = "Blazing Corridor";
    [SerializeField] private string defaultElement = "Fire Element";
    [SerializeField] private string defaultDangerLevel = "Danger ★★★☆☆";
    [SerializeField] private string defaultMonsterInfo = "Fire Slime, Flame Imp";

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // 인스펙터에 빈 문자열("")이 저장되어 있는 경우 디폴트 영문값으로 복구
        EnsureDefaultValues();

        // CanvasGroup을 통해 투명도/상호작용으로 제어하여 GameObject 비활성화로 인한 이벤트 수신 중단을 방지합니다.
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 인스펙터 슬롯이 비어(Null) 있다면 자식 오브젝트에서 이름(DepthText, ElementText 등)으로 자동 연결!
        AutoFindTextReferences();

        SetVisible(false);
    }

    private void EnsureDefaultValues()
    {
        if (string.IsNullOrWhiteSpace(defaultDepth)) defaultDepth = "Depth 2";
        if (string.IsNullOrWhiteSpace(defaultStageName)) defaultStageName = "Blazing Corridor";
        if (string.IsNullOrWhiteSpace(defaultElement)) defaultElement = "Fire Element";
        if (string.IsNullOrWhiteSpace(defaultDangerLevel)) defaultDangerLevel = "Danger ★★★☆☆";
        if (string.IsNullOrWhiteSpace(defaultMonsterInfo)) defaultMonsterInfo = "Fire Slime, Flame Imp";
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
        EnsureDefaultValues();
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
        if (string.IsNullOrWhiteSpace(depth)) depth = "Depth 2";
        if (string.IsNullOrWhiteSpace(stageName)) stageName = "Blazing Corridor";
        if (string.IsNullOrWhiteSpace(element)) element = "Fire Element";
        if (string.IsNullOrWhiteSpace(dangerLevel)) dangerLevel = "Danger ★★★☆☆";
        if (string.IsNullOrWhiteSpace(monsterInfo)) monsterInfo = "Fire Slime, Flame Imp";

        if (depthText == null || elementText == null)
        {
            AutoFindTextReferences();
        }

        if (depthText != null)
        {
            depthText.text = depth;
            // 텍스트가 흰색 패널에 묻히지 않도록 가시성 보장
            if (depthText.color == Color.white) depthText.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            Debug.Log($"[StageInfoPanelUI] DepthText 적용 완료: '{depth}'");
        }

        if (stageNameText != null)
        {
            stageNameText.text = stageName;
            if (stageNameText.color == Color.white) stageNameText.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        }

        if (elementText != null)
        {
            elementText.text = element;
            if (elementText.color == Color.white) elementText.color = new Color(0.8f, 0.2f, 0.1f, 1f); // 속성 강조색
            Debug.Log($"[StageInfoPanelUI] ElementText 적용 완료: '{element}'");
        }

        if (dangerLevelText != null) dangerLevelText.text = dangerLevel;
        if (monsterInfoText != null) monsterInfoText.text = monsterInfo;
    }

    /// <summary>
    /// 인스펙터 슬롯이 비어있을 때 자식 오브젝트(DepthText, ElementText 등)를 자동으로 찾아 바인딩
    /// </summary>
    private void AutoFindTextReferences()
    {
        var texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts)
        {
            string lowerName = t.gameObject.name.ToLower();
            if (depthText == null && lowerName.Contains("depth"))
            {
                depthText = t;
            }
            else if (elementText == null && lowerName.Contains("element"))
            {
                elementText = t;
            }
            else if (stageNameText == null && (lowerName.Contains("stagename") || lowerName.Contains("title")))
            {
                stageNameText = t;
            }
            else if (dangerLevelText == null && lowerName.Contains("danger"))
            {
                dangerLevelText = t;
            }
            else if (monsterInfoText == null && lowerName.Contains("monster"))
            {
                monsterInfoText = t;
            }
        }
    }
}
