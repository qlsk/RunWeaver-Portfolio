# 🎮 Unity 2D 탑다운 액션 - 스테이지/미션 시스템 인수인계 & 협업 지침서

> **문서 목적**: 본 문서는 프로젝트의 진행 상태와 아키텍처를 즉시 파악하고, 작업자 간 원활하게 협업할 수 있도록 업데이트된 종합 컨텍스트 문서입니다.

---

## 1. 프로젝트 개요 및 협업 환경 규칙

### 1) 프로젝트 환경
* **엔진 버전**: Unity 6 (6000.3.22f1 LTS)
* **장르**: 2D 탑다운 액션 슈팅 (WASD 이동, 마우스 조작, 심도/스테이지 이동 루프)
* **협업 방식**: 팀 단위 협업 진행 중 (레벨 디자인 / 스테이지 로직 / 임무 매니저 담당)

### 2) ★ 절대 준수 협업 지침
1. **작업 폴더 제한**:
   * 팀원들과의 Git 충돌을 방지하기 위해 **오직 `Assets/01_Scripts/Stage/` 폴더 내부의 스크립트만 수정하거나 추가**합니다.
   * 외부 폴더(Player, Enemy, UI 등)의 코드는 건드리지 않고, 정적 이벤트 및 `EnemyHealth` / `EnemyDataSO` 인터페이스로 느슨하게 연동합니다.
2. **코드 제공 방식**:
   * 유니티 및 IDE에 바로 복사-붙여넣기할 수 있는 생략 없는 완전한 C# 코드 블록을 제공합니다.
3. **언어**: 한국어로 작성합니다.

---

## 2. 현재까지 완료된 작업 내역 (100% 검증 완료)

현재 `Assets/01_Scripts/Stage/` 내 핵심 스크립트 및 시스템 연결 구조입니다.

```text
Assets/01_Scripts/Stage/
├── MissionBase.cs              // 5종 미션 공통 추상 클래스 (Strategy Pattern)
├── MissionManager.cs           // 미션 라이프사이클 총괄 & 동적 미션 오브젝트 주입/정리
├── EnemyKillMission.cs         // 미션 1: 일반 및 엘리트 적 처치 (EnemyHealth.OnDeath 수신)
├── SoulCollectMission.cs       // 미션 2: 지정 구역(SoulHarvestZone) 내 적 처치 시 영혼 수집
├── SoulHarvestZone.cs          // 영혼 수집 물리 트리거 구역 (Gizmo 시각화)
├── AltarDefenseMission.cs      // 미션 3: 중앙 제단 HP 방어 (제한시간 버티기)
├── DefenseAltar.cs             // 중앙 제단 컴포넌트 (몬스터 접촉 피격 및 생존 관리)
├── SurvivalMission.cs          // 미션 4: 제한시간 동안 생존
├── CaptureZoneMission.cs       // 미션 5: 거점 밟기 100% 점령 (SetTargetZone 직접 주입)
├── CaptureZone.cs              // 거점 점령 물리 트리거 구역
├── StageManager.cs             // 싱글톤 매니저, Depth 관리, 포탈 생성 및 씬 전환 루프
├── MonsterSpawner.cs           // EnemyDataSO 기반 동적 예산/가중치 추첨 스포너
├── StageEnemyInfo.cs           // 엘리트 몬스터 판정용 메타 어댑터
├── Portal.cs                   // 포탈 Collider 감지 및 OnDestroy 강제 이탈 이벤트 보장
├── StageInfoPanelUI.cs         // 우측 상단 정보 UI (CanvasGroup 제어)
└── ReturnConfirmPopupUI.cs     // 우측 하단 [복귀] / [계속 진행] 선택 팝업 (CanvasGroup 제어)
### 최근 완료된 주요 개선 및 버그 픽스 (PR 리뷰 피드백 반영 완료)
1. **정적 이벤트 중복 구독 방지**: `Initialize()` 호출 시 `DummyEnemy.OnEnemyDied` 및 `DummyPlayer.OnDummyPlayerDied`의 중복 구독 방지(`-=` 후 `+=`) 및 `OnDestroy()` 정리 구현 완료.
2. **안전한 씬 전환 (Fail-safe)**: `StageManager`에 기본 씬 상수 및 `Application.CanStreamedLevelBeLoaded` 예외 처리를 추가하여 씬 이름 누락 시에도 루프가 깨지지 않도록 보강.
3. **제단 디펜스 진행률 보정**: 제단 파괴/실패 시 `GetProgressRatio()`가 0%를 반환하도록 상태 불일치 해결.
4. **UI 편의성 극대화**: `StageInfoPanelUI`에 자식 오브젝트 자동 탐색(`AutoFindTextReferences`) 및 인스펙터 빈칸 시 영문 디폴트 자동 복원 기능 탑재.

---

## 3. 전체 시스템 아키텍처 및 데이터 흐름

flowchart TD
    subgraph Data & Enemy System
        ED[EnemyDataSO / EnemyInfoData] --> EH[EnemyHealth / Prefab]
    end

    subgraph Stage & Spawner
        SM[StageManager] -->|StartStage / Depth| MM[MissionManager]
        SM -->|RestartSpawner| MS[MonsterSpawner]
        MS -->|Cache & Weight Pick| EH
    end

    subgraph Mission Strategies
        MM --> MB[MissionBase]
        MB --> M1[EnemyKillMission]
        MB --> M2[SoulCollectMission]
        MB --> M3[AltarDefenseMission]
        MB --> M4[SurvivalMission]
        MB --> M5[CaptureZoneMission]
    end

    subgraph Portal & UI Loop
        MB -->|CompleteMission| MM
        MM -->|SpawnPortalAtCenter| SM
        SM -->|DespawnPortal & Clear| Portal[Portal Prefab]
        Portal -->|OnPlayerEntered/Exited| UI[ReturnConfirmPopupUI & StageInfoPanelUI]
    end

---

## 4. Gemini AI의 작업 수행 지침 (How to Work)

Gemini는 사용자와 대화할 때 항상 다음 프로세스를 따릅니다:

1. **사전 확인**: 사용자가 전달한 기획 텍스트를 분석하고 요구사항을 명확히 요약.
2. **코드 작성 (복사-붙여넣기 전용)**:
   - 반드시 **`Assets/01_Scripts/Stage/` 폴더 내에 위치할 단일/완성형 C# 코드**를 작성.
   - 코드에 클래스 역할과 주요 함수에 대한 주석을 충실히 기재.
3. **인스펙터 가이드 제공**:
   - 유니티 에디터에서 사용자가 생성해야 할 GameObject 이름.
   - 붙여야 할 컴포넌트 목록 및 `Collider2D` 설정(Is Trigger 여부 등).
   - 인스펙터 슬롯에 연결할 오브젝트/프리팹 안내.
4. **검증(테스트) 절차 안내**:
   - `Play` 후 어떤 키를 누르고 어떤 로그를 확인해야 성공인지 1-2-3 단계로 제시.
