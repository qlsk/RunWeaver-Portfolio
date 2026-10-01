using UnityEngine;

/// <summary>
/// 플레이어가 죽으면 실패 연출을 보여주고, 연출이 끝나면 결과창(EndingUI)이 열리게 한다.
/// (MissionResultPresenter의 실패 연출 → EndingUI 흐름을 그대로 사용한다)
/// </summary>
[DisallowMultipleComponent]
public class PlayerDeathResultTrigger : MonoBehaviour
{
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private MissionResultPresenter _missionResult;

    private void OnEnable()
    {
        if (_playerHealth == null || _missionResult == null)
        {
            Debug.LogWarning("[PlayerDeathResultTrigger] PlayerHealth 또는 MissionResultPresenter가 할당되지 않았습니다.", this);
            return;
        }

        _playerHealth.Died += HandlePlayerDied;
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.Died -= HandlePlayerDied;
        }
    }

    private void HandlePlayerDied()
    {
        _missionResult.ShowFailure();
    }
}
