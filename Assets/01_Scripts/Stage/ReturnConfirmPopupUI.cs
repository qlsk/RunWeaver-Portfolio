using UnityEngine;
using UnityEngine.UI;

public class ReturnConfirmPopupUI : MonoBehaviour
{
    [Header("UI 패널")]
    [Tooltip("비워두거나 본인을 연결하면 CanvasGroup을 통해 안전하게 표시/숨김 처리됩니다.")]
    [SerializeField] private GameObject popupPanel;

    [Header("조작 버튼")]
    [SerializeField] private Button proceedButton;  // [계속 진행] 버튼
    [SerializeField] private Button returnButton;   // [복귀] 버튼

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

        if (proceedButton != null)
        {
            proceedButton.onClick.AddListener(OnProceedClicked);
        }

        if (returnButton != null)
        {
            returnButton.onClick.AddListener(OnReturnClicked);
        }
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

    private void OnDestroy()
    {
        if (proceedButton != null)
        {
            proceedButton.onClick.RemoveListener(OnProceedClicked);
        }

        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(OnReturnClicked);
        }
    }

    private void HandlePortalEntered(Portal portal)
    {
        Debug.Log("[ReturnConfirmPopupUI] 포탈 진입 이벤트 수신 -> 팝업 표시");
        SetVisible(true);
    }

    private void HandlePortalExited(Portal portal)
    {
        Debug.Log("[ReturnConfirmPopupUI] 포탈 이탈 이벤트 수신 -> 팝업 숨김");
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

        if (popupPanel != null && popupPanel != gameObject)
        {
            popupPanel.SetActive(isVisible);
        }
    }

    private void OnProceedClicked()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.ProceedToNextStage();
        }
    }

    private void OnReturnClicked()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.ReturnToLobby();
        }
    }
}
