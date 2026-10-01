using UnityEngine;

public class AltarDefenseMission : MissionBase
{
    [Header("제단 방어 미션 설정")]
    [SerializeField] private float defenseDuration = 20f; // 목표 제단 생존 시간 (20초)
    private float currentTimer = 0f;
    private int lastLoggedSecond = -1;
    private DefenseAltar targetAltar;

    public float DefenseDuration => defenseDuration;
    public float CurrentTimer => currentTimer;

    /// <summary>
    /// MissionManager에서 생성된 신규 제단 오브젝트를 직접 전달받습니다.
    /// </summary>
    public void SetTargetAltar(DefenseAltar altar)
    {
        targetAltar = altar;
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

        DefenseAltar.OnAltarDestroyed -= HandleAltarDestroyed;
        DefenseAltar.OnAltarDestroyed += HandleAltarDestroyed;

        Debug.Log($"[AltarDefenseMission] 제단 방어 미션 시작! {defenseDuration}초 동안 제단을 수호하세요.");
    }

    public override void UpdateMission(float deltaTime)
    {
        if (IsCompleted || IsFailed) return;

        // 제단 파괴 유무 검사
        if (targetAltar == null || targetAltar.IsDestroyed)
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