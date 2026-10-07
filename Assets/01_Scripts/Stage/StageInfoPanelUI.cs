using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class StageInfoPanelUI : MonoBehaviour
{
    [Header("UI 패널")]
    [Tooltip("비워두거나 본인을 연결하면 CanvasGroup을 통해 안전하게 표시/숨김 처리됩니다.")]
    [SerializeField, FormerlySerializedAs("infoPanel")] private GameObject _infoPanel;

    [Header("스테이지 정보 텍스트 (TMP) - 비워두면 자식 오브젝트에서 이름으로 자동 탐색")]
    [SerializeField, FormerlySerializedAs("depthText")] private TMP_Text _depthText;
    [SerializeField, FormerlySerializedAs("stageNameText")] private TMP_Text _stageNameText;
    [SerializeField, FormerlySerializedAs("elementText")] private TMP_Text _elementText;
    [SerializeField, FormerlySerializedAs("dangerLevelText")] private TMP_Text _dangerLevelText;
    [SerializeField, FormerlySerializedAs("monsterInfoText")] private TMP_Text _monsterInfoText;

    [Header("기본 더미 데이터 (비워둘 경우 자동 디폴트값 적용)")]
    [SerializeField, FormerlySerializedAs("defaultDepth")] private string _defaultDepth = "Depth 2";
    [SerializeField, FormerlySerializedAs("defaultStageName")] private string _defaultStageName = "Blazing Corridor";
    [SerializeField, FormerlySerializedAs("defaultElement")] private string _defaultElement = "Fire Element";
    [SerializeField, FormerlySerializedAs("defaultDangerLevel")] private string _defaultDangerLevel = "Danger ★★★☆☆";
    [SerializeField, FormerlySerializedAs("defaultMonsterInfo")] private string _defaultMonsterInfo = "Fire Slime, Flame Imp";

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        // 인스펙터에 빈 문자열("")이 저장되어 있는 경우 디폴트 영문값으로 복구
        EnsureDefaultValues();

        // CanvasGroup을 통해 투명도/상호작용으로 제어하여 GameObject 비활성화로 인한 이벤트 수신 중단을 방지합니다.
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 인스펙터 슬롯이 비어(Null) 있다면 자식 오브젝트에서 이름(DepthText, ElementText 등)으로 자동 연결!
        AutoFindTextReferences();

        SetVisible(false);
    }

    private void EnsureDefaultValues()
    {
        if (string.IsNullOrWhiteSpace(_defaultDepth)) _defaultDepth = "Depth 2";
        if (string.IsNullOrWhiteSpace(_defaultStageName)) _defaultStageName = "Blazing Corridor";
        if (string.IsNullOrWhiteSpace(_defaultElement)) _defaultElement = "Fire Element";
        if (string.IsNullOrWhiteSpace(_defaultDangerLevel)) _defaultDangerLevel = "Danger ★★★☆☆";
        if (string.IsNullOrWhiteSpace(_defaultMonsterInfo)) _defaultMonsterInfo = "Fire Slime, Flame Imp";
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
        SetStageInfo(_defaultDepth, _defaultStageName, _defaultElement, _defaultDangerLevel, _defaultMonsterInfo);
        SetVisible(true);
    }

    private void HandlePortalExited(Portal portal)
    {
        Debug.Log("[StageInfoPanelUI] 포탈 이탈 이벤트 수신 -> 정보 UI 숨김");
        SetVisible(false);
    }

    public void SetVisible(bool isVisible)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.interactable = isVisible;
            _canvasGroup.blocksRaycasts = isVisible;
        }

        if (_infoPanel != null && _infoPanel != gameObject)
        {
            _infoPanel.SetActive(isVisible);
        }
    }

    /// <summary>
    /// 향후 StageDataSO 등 외부 데이터로부터 스테이지 정보를 주입할 때 사용
    /// </summary>
    public void SetStageInfo(string depth, string stageName, string element, string dangerLevel, string monsterInfo)
    {
        if (string.IsNullOrWhiteSpace(depth)) depth = "Depth 5";
        if (string.IsNullOrWhiteSpace(stageName)) stageName = "Blazing Corridor";
        if (string.IsNullOrWhiteSpace(element)) element = "Fire Element";
        if (string.IsNullOrWhiteSpace(dangerLevel)) dangerLevel = "Danger ★★★☆☆";
        if (string.IsNullOrWhiteSpace(monsterInfo)) monsterInfo = "Fire Slime, Flame Imp";

        if (_depthText == null || _elementText == null)
        {
            AutoFindTextReferences();
        }

        if (_depthText != null)
        {
            _depthText.text = depth;
            // 텍스트가 흰색 패널에 묻히지 않도록 가시성 보장
            if (_depthText.color == Color.white) _depthText.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            Debug.Log($"[StageInfoPanelUI] DepthText 적용 완료: '{depth}'");
        }

        if (_stageNameText != null)
        {
            _stageNameText.text = stageName;
            if (_stageNameText.color == Color.white) _stageNameText.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        }

        if (_elementText != null)
        {
            _elementText.text = element;
            if (_elementText.color == Color.white) _elementText.color = new Color(0.8f, 0.2f, 0.1f, 1f); // 속성 강조색
            Debug.Log($"[StageInfoPanelUI] ElementText 적용 완료: '{element}'");
        }

        if (_dangerLevelText != null) _dangerLevelText.text = dangerLevel;
        if (_monsterInfoText != null) _monsterInfoText.text = monsterInfo;
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
            if (_depthText == null && lowerName.Contains("depth"))
            {
                _depthText = t;
            }
            else if (_elementText == null && lowerName.Contains("element"))
            {
                _elementText = t;
            }
            else if (_stageNameText == null && (lowerName.Contains("stagename") || lowerName.Contains("title")))
            {
                _stageNameText = t;
            }
            else if (_dangerLevelText == null && lowerName.Contains("danger"))
            {
                _dangerLevelText = t;
            }
            else if (_monsterInfoText == null && lowerName.Contains("monster"))
            {
                _monsterInfoText = t;
            }
        }
    }
}
