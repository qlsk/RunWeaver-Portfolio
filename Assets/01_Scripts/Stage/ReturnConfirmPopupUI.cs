using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ReturnConfirmPopupUI : MonoBehaviour
{
    [Header("UI 패널")]
    [Tooltip("비워두거나 본인을 연결하면 CanvasGroup을 통해 안전하게 표시/숨김 처리됩니다.")]
    [SerializeField, FormerlySerializedAs("popupPanel")] private GameObject _popupPanel;

    [Header("조작 버튼")]
    [SerializeField, FormerlySerializedAs("proceedButton")] private Button _proceedButton;  // [계속 진행] 버튼
    [SerializeField, FormerlySerializedAs("returnButton")] private Button _returnButton;   // [복귀] 버튼

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        // CanvasGroup을 통해 투명도/상호작용으로 제어하여 GameObject 비활성화로 인한 이벤트 수신 중단을 방지합니다.
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        SetVisible(false);

        if (_proceedButton != null)
        {
            _proceedButton.onClick.AddListener(OnProceedClicked);
        }

        if (_returnButton != null)
        {
            _returnButton.onClick.AddListener(OnReturnClicked);
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
        if (_proceedButton != null)
        {
            _proceedButton.onClick.RemoveListener(OnProceedClicked);
        }

        if (_returnButton != null)
        {
            _returnButton.onClick.RemoveListener(OnReturnClicked);
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
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.interactable = isVisible;
            _canvasGroup.blocksRaycasts = isVisible;
        }

        if (_popupPanel != null && _popupPanel != gameObject)
        {
            _popupPanel.SetActive(isVisible);
        }
    }

    private void OnProceedClicked()
    {
        SetVisible(false); // 클릭 즉시 팝업 닫기
        if (StageManager.Instance != null)
        {
            StageManager.Instance.ProceedToNextStage();
        }
    }

    private void OnReturnClicked()
    {
        SetVisible(false); // 클릭 즉시 팝업 닫기
        // 귀환 포탈은 폐기되었다. 기존 테스트 UI 호환을 위해 런을 정리하고 마을로 이동한다.
        RunFlow.GoToLobby();
    }
}
