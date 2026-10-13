using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 로딩 화면 (Jang/LoadingScene). 타이틀(StartScene)에서 시작 버튼을 누르면 이 씬으로 온다.
/// 같은 씬의 GameBootstrap이 데이터베이스를 모두 불러올 때까지 로딩 화면을 보여준 뒤 전투 씬(CombatScene)으로 넘어간다. (마을 씬은 쓰지 않는다)
/// 다음 씬을 불러오는 동안에도 로딩 화면이 그대로 보인다. (비동기로 미리 불러 둔 뒤 전환)
/// - 다 불러오면 화면을 검게 덮은 뒤 넘어가고, 전투 씬에서 검은 화면이 서서히 걷힌다. (SceneTransition)
///   전투 씬의 도착 연출(PortalArrival)은 화면이 다 밝아진 뒤 균열을 닫고 가라앉힌다.
/// - 너무 빨리 끝나 화면이 깜빡이지 않도록 최소 표시 시간 동안은 유지한다.
/// - 초기화에 실패하면(CSV 다운로드 실패 등) 잠시 후 다시 시도한다.
/// </summary>
[DisallowMultipleComponent]
public class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private GameBootstrap _bootstrap;
    [SerializeField] private string _nextSceneName = "CombatScene";
    [SerializeField, Min(0f)] private float _minimumDisplaySeconds = 1.5f;
    [SerializeField, Min(0.5f)] private float _retryDelaySeconds = 3f;

    [Header("전환")]
    [Tooltip("로딩 화면을 검게 덮는 시간(초)")]
    [SerializeField, Min(0f)] private float _coverDuration = 0.65f;
    [Tooltip("전투 씬에서 검은 화면이 걷히는 시간(초)")]
    [SerializeField, Min(0f)] private float _revealDuration = 0.8f;

    private float _startTime;
    private float _retryTime = -1f;
    private bool _isLoadingNextScene;

    private void Start()
    {
        _startTime = Time.unscaledTime;
        if (_bootstrap == null)
        {
            _bootstrap = FindAnyObjectByType<GameBootstrap>();
        }

        if (_bootstrap == null)
        {
            Debug.LogError("[LoadingSceneController] GameBootstrap을 찾을 수 없습니다.", this);
        }
    }

    private void Update()
    {
        if (_isLoadingNextScene || _bootstrap == null)
        {
            return;
        }

        switch (_bootstrap.State)
        {
            case BootstrapState.Ready:
                if (Time.unscaledTime - _startTime >= _minimumDisplaySeconds)
                {
                    _isLoadingNextScene = true;
                    StartCoroutine(LoadNextSceneRoutine());
                }

                break;

            case BootstrapState.Failed:
                if (_retryTime < 0f)
                {
                    _retryTime = Time.unscaledTime + _retryDelaySeconds;
                    Debug.LogWarning($"[LoadingSceneController] 데이터 로딩 실패 ({_bootstrap.FailedStage}). " +
                                     $"{_retryDelaySeconds}초 후 다시 시도합니다.", this);
                }
                else if (Time.unscaledTime >= _retryTime)
                {
                    _retryTime = -1f;
                    _bootstrap.InitializeAsync().Forget();
                }

                break;
        }
    }

    /// <summary>
    /// 로딩 화면을 보여주는 동안 다음 씬을 미리 불러 두고, 다 불러오면 화면을 검게 덮은 뒤 넘어간다.
    /// 전환을 시작할 수 없으면(이미 다른 전환 중) 바로 넘어간다.
    /// </summary>
    private IEnumerator LoadNextSceneRoutine()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(_nextSceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogError($"[LoadingSceneController] {_nextSceneName} 씬을 불러올 수 없습니다.", this);
            yield break;
        }

        operation.allowSceneActivation = false;

        // allowSceneActivation이 꺼져 있으면 0.9에서 멈추고 전환을 기다린다.
        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        bool isStarted = SceneTransition.FadeAndLoad(() => operation.allowSceneActivation = true,
            0f, _coverDuration, _revealDuration);
        if (!isStarted)
        {
            operation.allowSceneActivation = true;
        }
    }
}