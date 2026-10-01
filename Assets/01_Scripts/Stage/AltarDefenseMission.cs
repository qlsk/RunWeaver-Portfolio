using UnityEngine;

public class AltarDefenseMission : MissionBase
{
    [Header("제단 방어 미션 설정")]
    [SerializeField] private float defenseDuration = 20f; // 목표 제단 생존 시간 (20초)
    private float currentTimer = 0f;
    private int lastLoggedSecond = -1;
    private DefenseAltar targetAltar;
    private bool hasAltarBeenRegistered = false;

    public float DefenseDuration => defenseDuration;
    public float CurrentTimer => currentTimer;

    /// <summary>
    /// MissionManager에서 생성된 신규 제단 오브젝트를 직접 전달받습니다.
    /// </summary>
    public void SetTargetAltar(DefenseAltar altar)
    {
        targetAltar = altar;
        if (targetAltar != null)
        {
            hasAltarBeenRegistered = true;
        }
    }

    public override void Initialize()
    {
        currentTimer = 0f;
        lastLoggedSecond = -1;
        IsCompleted = false;
        IsFailed = false;

        // 직접 전달받지 않은 경우에만 폴백으로 씬 전체 탐색
        if (targetAltar == null)
        {
            targetAltar = FindAnyObjectByType<DefenseAltar>();
        }

        if (targetAltar != null)
        {
            hasAltarBeenRegistered = true;
        }

        DefenseAltar.OnAltarDestroyed -= HandleAltarDestroyed;
        DefenseAltar.OnAltarDestroyed += HandleAltarDestroyed;

        Debug.Log($"[AltarDefenseMission] 제단 방어 미션 시작! {defenseDuration}초 동안 제단을 수호하세요. (제단 등록 상태: {hasAltarBeenRegistered})");
    }

    public override void UpdateMission(float deltaTime)
    {
        if (IsCompleted || IsFailed) return;

        // 1. 아직 제단이 등록되지 않았거나 지연 생성 중인 경우 재탐색 (바로 실패시키지 않고 대기)
        if (!hasAltarBeenRegistered || targetAltar == null)
        {
            targetAltar = FindAnyObjectByType<DefenseAltar>();
            if (targetAltar != null)
            {
                hasAltarBeenRegistered = true;
                Debug.Log($"[AltarDefenseMission] 제단 오브젝트 탐색/등록 완료: {targetAltar.name}");
            }
            else
            {
                // 아직 씬에 제단이 스폰되지 않았으므로 다음 프레임까지 대기
                return;
            }
        }

        // 2. 제단이 정상 등록된 상태에서 체력이 0이 되어 파괴된 경우에만 실패 처리
        if (targetAltar.IsDestroyed)
        {
            HandleAltarDestroyed();
            return;
        }

        currentTimer += deltaTime;

        // 1초 단위 디버그 로그 및 제단 HP 상태 출력
        int currentSecond = Mathf.FloorToInt(currentTimer);
        if (currentSecond > lastLoggedSecond && currentSecond <= (int)defenseDuration)
        {
            lastLoggedSecond = currentSecond;
            Debug.Log($"[AltarDefenseMission] 제단 방어 진행 중... {currentSecond}s / {defenseDuration:F0}s | 제단 HP: {targetAltar.CurrentHp}/{targetAltar.MaxHp}");
        }

        // 20초 생존 달성 시 처리
        if (currentTimer >= defenseDuration)
        {
            Debug.Log($"[AltarDefenseMission] ★ 제단 방어 성공({defenseDuration}초 생존)! 미션 클리어");

            DefenseAltar.OnAltarDestroyed -= HandleAltarDestroyed;

            if (targetAltar != null)
            {
                Destroy(targetAltar.gameObject);
                targetAltar = null;
            }

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
        if (defenseDuration <= 0f) return 1f;
        return Mathf.Clamp01(currentTimer / defenseDuration);
    }

    public override string GetProgressText()
    {
        if (IsFailed) return "제단 파괴됨!";
        if (IsCompleted) return "방어 성공!";
        return $"제단 방어: {Mathf.Min(currentTimer, defenseDuration):F1}s / {defenseDuration:F0}s";
    }
}