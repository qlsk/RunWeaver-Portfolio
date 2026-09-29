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

    [Header("씬 전환 설정 (비워둘 경우 현재 씬 또는 기본 씬으로 안전하게 전환)")]
    [Tooltip("계속 진행 시 로드할 다음 스테이지 씬 이름")]
    [SerializeField] private string nextStageSceneName = "StageTest_Scene";
    [Tooltip("복귀 시 로드할 로비/마을 씬 이름")]
    [SerializeField] private string lobbySceneName = "LobbyScene";

    private GameObject currentSpawnedPortal;

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
        if (currentSpawnedPortal != null)
        {
            Debug.LogWarning("[StageManager] 이미 포탈이 생성되어 있습니다.");
            return;
        }

        if (portalPrefab != null)
        {
            currentSpawnedPortal = Instantiate(portalPrefab, portalSpawnPosition, Quaternion.identity);
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
        LoadSceneSafely(nextStageSceneName, "다음 스테이지");
    }

    /// <summary>
    /// [복귀] 선택 시 호출
    /// </summary>
    public void ReturnToLobby()
    {
        Debug.Log("[StageManager] 로비/마을로 복귀합니다.");
        LoadSceneSafely(lobbySceneName, "로비");
    }

    /// <summary>
    /// 씬 이름 미설정 또는 빌드 세팅 누락 시 안전하게 예외를 처리하는 씬 로더
    /// </summary>
    private void LoadSceneSafely(string sceneName, string targetDescription)
    {
        if (!string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        // 빌드 세팅에 없거나 씬 이름이 누락된 경우의 안전 장치
        Debug.LogWarning($"[StageManager] {targetDescription} 씬 '{sceneName}'을(를) 로드할 수 없습니다. (Build Settings 등록 여부 확인 필요)");

        // 개발/테스트 편의: 현재 활성화된 씬을 다시 로드하여 루프 테스트 유지
        int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
        if (currentBuildIndex >= 0)
        {
            Debug.Log("[StageManager] 안전 장치: 현재 씬을 다시 로드합니다.");
            SceneManager.LoadScene(currentBuildIndex);
        }
    }
}
