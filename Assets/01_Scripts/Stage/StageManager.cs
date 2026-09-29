using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("포탈 설정")]
    [Tooltip("미션 완료 시 생성할 포탈 프리팹")]
    [SerializeField] private GameObject portalPrefab;
    [Tooltip("포탈이 생성될 맵 좌표 (기본 중앙: 0, 0, 0)")]
    [SerializeField] private Vector3 portalSpawnPosition = Vector3.zero;

    [Header("씬 전환 설정")]
    [Tooltip("계속 진행 시 로드할 다음 스테이지 씬 이름")]
    [SerializeField] private string nextStageSceneName = "";
    [Tooltip("복귀 시 로드할 로비/마을 씬 이름")]
    [SerializeField] private string lobbySceneName = "";

    private GameObject _currentSpawnedPortal;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 미션 완료 시 맵 중앙에 포탈을 생성합니다.
    /// </summary>
    public void SpawnPortal()
    {
        if (_currentSpawnedPortal != null)
        {
            Debug.LogWarning("[StageManager] 이미 포탈이 생성되어 있습니다.");
            return;
        }

        if (portalPrefab != null)
        {
            _currentSpawnedPortal = Instantiate(portalPrefab, portalSpawnPosition, Quaternion.identity);
            Debug.Log($"[StageManager] ★ 포탈 생성 완료! 좌표: {portalSpawnPosition} ★");
        }
        else
        {
            Debug.LogError("[StageManager] portalPrefab이 인스펙터에 할당되지 않았습니다!");
        }
    }

    /// <summary>
    /// [계속 진행] 선택 시 호출
    /// </summary>
    public void ProceedToNextStage()
    {
        Debug.Log("[StageManager] 다음 심도/스테이지로 이동합니다.");

        if (!string.IsNullOrEmpty(nextStageSceneName))
        {
            SceneManager.LoadScene(nextStageSceneName);
        }
        else
        {
            Debug.LogWarning("[StageManager] nextStageSceneName이 설정되지 않아 씬을 이동하지 못했습니다. (인스펙터 확인 필요)");
        }
    }

    /// <summary>
    /// [복귀] 선택 시 호출
    /// </summary>
    public void ReturnToLobby()
    {
        Debug.Log("[StageManager] 로비/마을로 복귀합니다.");

        if (!string.IsNullOrEmpty(lobbySceneName))
        {
            SceneManager.LoadScene(lobbySceneName);
        }
        else
        {
            Debug.LogWarning("[StageManager] lobbySceneName이 설정되지 않아 씬을 이동하지 못했습니다. (인스펙터 확인 필요)");
        }
    }
}
