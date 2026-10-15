# RuneWeaver

룬을 조합해 탄환의 원소 효과를 강화하며 전투하는 2D 탑다운 슈터 게임입니다. Unity로 제작한 팀 프로젝트입니다.

## 주요 특징

- **원소와 룬 시스템**: 탄환에 원소를 부여하고, 두 원소가 만나면 원소 반응이 발생합니다. 룬은 위력, 가속, 연장, 반복(Echo), 흡수(Absorb) 등으로 탄환과 반응 효과를 변형합니다.
- **전투와 적**: 일반 적과 보스(골렘 등)가 등장하며, 보스는 페이즈별 패턴을 사용합니다.
- **성장과 준비**: 마을(Town)에서 연구, 상점, 룬 장착 등 전투 전 준비를 합니다.
- **미션과 스테이지**: 미션 단위로 스테이지를 진행하고 보상을 얻습니다.
- **튜토리얼**: 룬북, 룬 슬롯 해금 등 시스템을 단계별로 안내합니다.

## 개발 환경

| 항목 | 내용 |
|---|---|
| 엔진 | Unity 6000.3.22f1 |
| 렌더 파이프라인 | Universal Render Pipeline 17.3.0 |
| 입력 | Input System 1.20.0 |
| 언어 | C# |

Unity Hub에서 위 버전의 에디터를 설치해야 합니다.

## 실행 방법

1. 저장소를 복제합니다.
   ```bash
   git clone https://github.com/GA1-Team-Project/2d-team07.git
   ```
2. Unity Hub에서 **Add → Add project from disk**로 폴더를 추가하고 Unity 6000.3.22f1로 엽니다. 처음 열 때 `Library`가 생성되어 시간이 걸립니다.
3. `Assets/00_Scenes/BootStrap.unity`를 열고 Play 합니다.

주요 씬은 다음과 같습니다.

- `BootStrap`: 시작 시 초기화
- `StartScene`: 타이틀
- `TownScene`: 마을 (전투 전 준비)
- `CombatScene`: 전투

## 폴더 구조

```text
Assets/
├─ 00_Scenes/      씬
├─ 01_Scripts/     게임 코드 (Player, Enemy, Bullet, Rune, Stage, Mission, Growth, Research, Save, UI 등)
├─ 02_Prefabs/     프리팹
├─ 03_Sprites/     스프라이트
├─ 05_Animations/  애니메이션
├─ 08_VFX/         이펙트
├─ 09_Audio/       오디오
├─ 10_UI/          UI 에셋
├─ 11_Data/        데이터 테이블
├─ 15_Settings/    렌더링·프로젝트 설정 에셋
└─ 19_ThirdParty/  외부 에셋
Docs/              설계 문서와 코드 리뷰 가이드
```

## 문서

- [Docs/Designs](Docs/Designs): 기능별 기술 설계 문서 (룬 시스템, 튜토리얼, 타이틀 BGM·메뉴 효과음 등)
- [Docs/code-review-guide.md](Docs/code-review-guide.md): 코드 리뷰 가이드
- [Docs/sound-checklist.md](Docs/sound-checklist.md): 사운드 체크리스트

## 빌드 파일

빌드 결과물(`Build/`, `*.zip`)은 저장소에 커밋하지 않습니다. 배포가 필요하면 GitHub Releases를 사용합니다.
