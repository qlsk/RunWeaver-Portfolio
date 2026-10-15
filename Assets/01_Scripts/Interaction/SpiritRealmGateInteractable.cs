using UnityEngine;

public class SpiritRealmGateInteractable : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private PopupController _popupController;

    [Header("Settings")]
    [SerializeField] private string _interactionName;
    [SerializeField] private Transform _interactionGuidePoint;
    [SerializeField] private GameObject _primaryElementSelectionPopup;

    [Header("ReStart")]
    [Tooltip("결과창에서 ReStart로 마을에 왔을 때 플레이어를 옮길 위치 (제단 기준)")]
    [SerializeField] private Vector3 _restartPlayerOffset = new(0f, -1.5f, 0f);

    public string InteractionName => _interactionName;
    public Transform InteractionGuidePoint => _interactionGuidePoint;

    /// <summary>
    /// 결과창에서 ReStart로 마을에 왔으면 플레이어를 제단 옆으로 옮기고 원소 선택 창을 바로 연다.
    /// </summary>
    private void Start()
    {
        if (!RunFlow.ConsumeRestartRequest())
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = transform.position + _restartPlayerOffset;
        }

        Interact();
    }

    public void Interact()
    {
        _popupController.OpenPopup(_primaryElementSelectionPopup);
    }
}