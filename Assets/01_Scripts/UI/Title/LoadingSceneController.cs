using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 로딩 화면 (Jang/LoadingScene). 타이틀(StartScene)에서 시작 버튼을 누르면 이 씬으로 온다.
/// 같은 씬의 GameBootstrap이 데이터베이스를 모두 불러올 때까지 로딩 화면을 보여준 뒤 마을(TownScene)로 넘어간다.
/// 마을 씬을 불러오는 동안에도 로딩 화면이 그대로 보인다. (비동기로 불러온 뒤 전환)
/// - 너무 빨리 끝나 화면이 깜빡이지 않도록 최소 표시 시간 동안은 유지한다.
/// - 초기화에 실패하면(CSV 다운로드 실패 등) 잠시 후 다시 시도한다.
/// </summary>
[DisallowMultipleComponent]
public class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private GameBootstrap _bootstrap;
    [SerializeField] private string _nextSceneName = "TownScene";
    [SerializeField, Min(0f)] private float _minimumDisplaySeconds = 1.5f;
    [SerializeField, Min(0.5f)] private float _retryDelaySeconds = 3f;

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
                    SceneManager.LoadSceneAsync(_nextSceneName, LoadSceneMode.Single);
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
}
