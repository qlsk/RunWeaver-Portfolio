using UnityEngine;
using UnityEngine.Serialization;

public class AltarDefenseMission : MissionBase
{
    public override MissionType Type => MissionType.AltarDefense;

    [Header("제단 방어 미션 설정")]
    [SerializeField, FormerlySerializedAs("defenseDuration")] private float _defenseDuration = 40f; // 목표 제단 생존 시간 (15초)
    private float _currentTimer = 0f;
    private int _lastLoggedSecond = -1;
    private DefenseAltar _targetAltar;
    private bool _hasAltarBeenRegistered = false;

    public float DefenseDuration => _defenseDuration;
    public float CurrentTimer => _currentTimer;
    public DefenseAltar TargetAltar => _targetAltar;

    /// <summary>
    /// MissionManager에서 생성된 신규 제단 오브젝트를 직접 전달받습니다.
    /// </summary>
    public void SetTargetAltar(DefenseAltar altar)
    {
        _targetAltar = altar;
        if (_targetAltar != null)
        {
            _hasAltarBeenRegistered = true;
        }
    }

    public override void Initialize()
    {
        _currentTimer = 0f;
        _lastLoggedSecond = -1;
        IsCompleted = false;
        IsFailed = false;

        // 직접 전달받지 않은 경우에만 폴백으로 씬 전체 탐색
        if (_targetAltar == null)
        {
            _targetAltar = FindAnyObjectByType<DefenseAltar>();
        }

        if (_targetAltar != null)
        {
            _hasAltarBeenRegistered = true;
        }

        DefenseAltar.OnAltarDestroyed -= HandleAltarDestroyed;
        DefenseAltar.OnAltarDestroyed += HandleAltarDestroyed;

        Debug.Log($"[AltarDefenseMission] 제단 방어 미션 시작! {_defenseDuration}초 동안 제단을 수호하세요. (제단 등록 상태: {_hasAltarBeenRegistered})");
    }

    public override void UpdateMission(float deltaTime)
    {
        if (IsCompleted || IsFailed) return;

        // 1. 아직 제단이 등록되지 않았거나 지연 생성 중인 경우 재탐색 (바로 실패시키지 않고 대기)
        if (!_hasAltarBeenRegistered || _targetAltar == null)
        {
            _targetAltar = FindAnyObjectByType<DefenseAltar>();
            if (_targetAltar != null)
            {
                _hasAltarBeenRegistered = true;
                Debug.Log($"[AltarDefenseMission] 제단 오브젝트 탐색/등록 완료: {_targetAltar.name}");
            }
            else
            {
                // 아직 씬에 제단이 스폰되지 않았으므로 다음 프레임까지 대기
                return;
            }
        }

        // 2. 제단이 정상 등록된 상태에서 체력이 0이 되어 파괴된 경우에만 실패 처리
        if (_targetAltar.IsDestroyed)
        {
            HandleAltarDestroyed();
            return;
        }

        _currentTimer += deltaTime;

        // 1초 단위 디버그 로그 및 제단 HP 상태 출력
        int currentSecond = Mathf.FloorToInt(_currentTimer);
        if (currentSecond > _lastLoggedSecond && currentSecond <= (int)_defenseDuration)
        {
            _lastLoggedSecond = currentSecond;
            Debug.Log($"[AltarDefenseMission] 제단 방어 진행 중... {currentSecond}s / {_defenseDuration:F0}s | 제단 HP: {_targetAltar.CurrentHp}/{_targetAltar.MaxHp}");
        }

        // 15초 생존 달성 시 처리
        if (_currentTimer >= _defenseDuration)
        {
            Debug.Log($"[AltarDefenseMission] ★ 제단 방어 성공({_defenseDuration}초 생존)! 미션 클리어");

            DefenseAltar.OnAltarDestroyed -= HandleAltarDestroyed;

            // 제단은 삭제하지 않고 남겨 둔다. (포탈이 제단 아래에 나오고, 다음 층을 시작할 때 MissionManager가 정리한다)
            _targetAltar = null;

            CompleteMission();
        }
    }

    private void HandleAltarDestroyed()
    {
        if (IsCompleted || IsFailed) return;

        Debug.Log("[AltarDefenseMission] 제단이 파괴되어 미션 실패!");
        FailMission();
    }

    private void OnDisable()
    {
        DefenseAltar.OnAltarDestroyed -= HandleAltarDestroyed;
    }

    public override float GetProgressRatio()
    {
        if (IsFailed) return 0f;
        if (_defenseDuration <= 0f) return 1f;
        return Mathf.Clamp01(_currentTimer / _defenseDuration);
    }

    public override string GetProgressText()
    {
        if (IsFailed) return "제단 파괴됨!";
        if (IsCompleted) return "방어 성공!";
        string hp = _targetAltar != null ? $" | 제단 HP: {_targetAltar.CurrentHp:F0}/{_targetAltar.MaxHp:F0}" : string.Empty;
        return $"제단 방어: {Mathf.Min(_currentTimer, _defenseDuration):F1}s / {_defenseDuration:F0}s{hp}";
    }
}
