using UnityEngine;

/// <summary>
/// [테스트 전용] HUD 확인용 키 입력.
/// 체력은 실제 PlayerHealth의 공개 메서드(TakeDamage, Heal)를 호출하고,
/// 미션 두루마리는 MissionManager가 없는 씬에서 확인할 수 있도록 가짜 진행 내용을 넣는다.
///
/// 테스트 키
/// 4: 체력 감소 / 5: 체력 회복 / 8: 미션 진행 +1
/// </summary>
public class HudDebugKeys : MonoBehaviour
{
    [Header("체력")]
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField, Min(0f)] private float _healthStep = 10f;
    [SerializeField] private KeyCode _damageKey = KeyCode.Alpha4;
    [SerializeField] private KeyCode _healKey = KeyCode.Alpha5;

    [Header("미션")]
    [SerializeField] private MissionScrollView _missionView;
    [SerializeField, Min(1)] private int _missionTarget = 5;
    [SerializeField] private KeyCode _missionProgressKey = KeyCode.Alpha8;

    private int _missionProgress;

    private void Start()
    {
        ShowMission();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_damageKey) && _playerHealth != null)
        {
            _playerHealth.TakeDamage(_healthStep);
        }

        if (Input.GetKeyDown(_healKey) && _playerHealth != null)
        {
            _playerHealth.Heal(_healthStep);
        }

        if (Input.GetKeyDown(_missionProgressKey))
        {
            _missionProgress = _missionProgress >= _missionTarget ? 0 : _missionProgress + 1;
            ShowMission();
        }
    }

    private void ShowMission()
    {
        if (_missionView != null)
        {
            _missionView.SetMission("Defeat Enemies", $"Enemies defeated: {_missionProgress} / {_missionTarget}");
        }
    }
}
