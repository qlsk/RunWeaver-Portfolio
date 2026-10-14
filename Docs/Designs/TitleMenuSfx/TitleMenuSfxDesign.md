# 타이틀 메뉴 SFX — 구현 설계

> 상태: **설계 승인됨 (미구현)** · 기준 브랜치 `feature/growth` · 작성일 2026-10-14
> 범위는 StartScene 메뉴와 설정 창의 효과음 3종(이동·확정·취소)이다. 타이틀 BGM은 `Docs/Designs/TitleBgm/TitleBgmDesign.md`에서 다룬다.

## 1. 기능 개요 및 요구사항

### 목적

타이틀 메뉴를 조작할 때 이동·확정·취소 효과음을 낸다. 기존 SFX 재생 구조(`Sfx`, `SfxLibrary`)를 그대로 쓰고 새 클래스는 만들지 않는다.

### 정상 동작

- **이동음**: 사용자 입력으로 메뉴 선택이 실제로 바뀔 때 재생한다. 키보드와 마우스 호버 모두 해당한다. 메뉴가 처음 나타날 때의 초기 선택은 제외한다.
- **확정음**: 메뉴 항목을 확정할 때(새 게임, 설정, 게임 종료)와 설정 창의 Apply에서 재생한다.
- **취소음**: 설정 창의 Cancel(취소 버튼, Esc)에서 재생한다.
- 설정 창은 타이틀과 일시정지 화면이 공유하므로, Apply·Cancel 효과음은 일시정지 화면에서도 같이 난다.

### 제외 범위

- 비활성 항목('이어하기') 거부음, 슬라이더 조작음, 새 게임 시작 전용음. 추가 효과음 질문에서 선택하지 않아 이번 범위에서 뺐다.
- 설정 창 안에서 슬라이더·토글 사이를 이동할 때의 소리.
- 클립 자체의 선정과 믹싱(볼륨 균형)은 에디터에서 청취하며 조정한다.

## 2. 기존 코드 분석 및 변경 범위

### 확인된 기존 구조

- `Sfx`(`Assets/01_Scripts/Audio/Sfx.cs`)는 `Sfx.Play(id)`로 효과음을 재생한다. 처음 사용할 때 `Resources`의 `SfxLibrary`를 읽어 id별 클립 표를 채우며, 클립이 비어 있으면 조용히 무시한다. 볼륨은 전체 볼륨 × SFX 볼륨이고, 설정이 바뀌면 재생용 소스들의 볼륨이 갱신된다. 새 효과음은 `SfxLibrary`에 클립 필드, `Sfx`에 id 상수와 표 등록 한 줄을 추가하는 방식이다.
- 기존 SFX 클립은 `Assets/09_Audio/`에 모여 있고 `SfxLibrary.asset`(`Assets/09_Audio/Resources/`)이 참조한다.
- `TitleMenuView`(`Assets/01_Scripts/UI/Title/TitleMenuView.cs`)는 위/아래 키와 `W`/`S`, 마우스 호버로 선택을 바꾸고, 확정 키나 클릭으로 항목을 확정한다. 선택 변경은 내부의 선택 처리에서 일어난다. 이 처리는 메뉴가 활성화될 때 한 번 즉시 전환 방식으로 불리고(초기 선택), 그 밖의 입력에서는 즉시 전환 없이 불린다. 확정은 항목 순서를 담은 `Confirmed` 이벤트로 전달된다. 선택이 바뀌었다는 이벤트는 없다. 비활성 항목은 호버로 선택되지 않고 확정도 되지 않는다.
- 메뉴의 선택 이동은 순환하며 비활성 항목은 건너뛴다.
- `TitleMenuController`(`Assets/01_Scripts/UI/Title/TitleMenuController.cs`)는 메뉴의 `Confirmed`를 구독해 항목별로 새 게임, 설정 열기, 게임 종료를 처리한다. 구독과 해제는 각각 `Awake`, `OnDestroy`에서 한다. 설정 창이 열려 있을 때 Esc를 누르면 설정 창의 취소를 부른다.
- `SettingsPanelView`(`Assets/01_Scripts/UI/Settings/SettingsPanelView.cs`)의 `Apply`는 열려 있을 때만 설정을 저장하고 닫으며, `Cancel`은 열려 있을 때만 창을 열기 전 값으로 되돌리고 닫는다. 값을 되돌릴 때 설정 변경 알림이 동기적으로 전달된다(`SafeEventNotifier`가 즉시 호출). 이 창은 `PauseController`도 사용한다.

### 생성·수정 대상

| 구분 | 대상 | 내용 |
|---|---|---|
| 수정 | [SfxLibrary.cs](../../../Assets/01_Scripts/Audio/SfxLibrary.cs) | 클립 필드 3개 추가 |
| 수정 | [Sfx.cs](../../../Assets/01_Scripts/Audio/Sfx.cs) | id 상수 3개, 표 등록 3줄 추가 |
| 수정 | [TitleMenuView.cs](../../../Assets/01_Scripts/UI/Title/TitleMenuView.cs) | 선택 이동 이벤트 추가 |
| 수정 | [TitleMenuController.cs](../../../Assets/01_Scripts/UI/Title/TitleMenuController.cs) | 이동음·확정음 재생 연결 |
| 수정 | [SettingsPanelView.cs](../../../Assets/01_Scripts/UI/Settings/SettingsPanelView.cs) | Apply·Cancel 효과음 재생 |
| 에셋 | `Assets/09_Audio/Menu_Move`, `Menu_Confirm`, `Menu_Cancel` | 선정한 클립 3개를 복사 |
| 에셋 | `Assets/09_Audio/Resources/SfxLibrary.asset` | 필드 3개에 클립 연결 |

## 3. 전체 실행 흐름 및 설계 구조

### 책임

- 입력 감지는 `TitleMenuView`가, 어떤 소리를 낼지는 `TitleMenuController`와 `SettingsPanelView`가 정한다. 뷰는 오디오를 모르고 "선택이 바뀌었다"는 사실만 알린다.
- 설정 창은 타이틀과 일시정지가 공유하는 하나의 창이므로, 창 안의 Apply·Cancel 소리는 창 자신이 낸다. 한쪽 화면의 컨트롤러가 대신 내지 않는다.
- 소리 재생과 볼륨 반영은 모두 기존 `Sfx`가 맡는다.

### 흐름

1. 키보드나 마우스 호버로 선택이 바뀌면 메뉴가 이동 알림을 보내고, 컨트롤러가 이동음을 낸다.
2. 항목을 확정하면 메뉴가 확정 알림을 보내고, 컨트롤러가 확정음을 낸 뒤 항목별 기존 처리(새 게임 시작, 설정 열기, 게임 종료)로 이어진다.
3. 설정 창에서 Apply를 누르면 설정을 저장하고 확정음을 낸 뒤 닫는다. Cancel(취소 버튼, Esc)이면 값을 되돌린 뒤 취소음을 내고 닫는다.

Cancel에서 값을 되돌린 **뒤에** 소리를 내는 이유는, 설정을 되돌리면 SFX 볼륨이 창을 열기 전 값으로 복원되는데 취소음이 그 복원된 볼륨으로 나야 하기 때문이다.

마우스가 호버 없이 곧바로 클릭하는 경우(터치 등)에는 선택 변경과 확정이 연달아 일어나 이동음과 확정음이 겹쳐 들릴 수 있다. 일반 마우스는 호버가 클릭보다 먼저 선택을 바꾸므로 이 경우를 따로 처리하지 않는다.

## 4. 단계별 구현 가이드

### 단계 1. 효과음 id와 클립 슬롯 (선행 준비)

효과음 id와 클립 슬롯이 먼저 있어야 이후 단계의 호출이 컴파일된다.

**`SfxLibrary.cs`** (기존 클래스에 필드 추가)

```csharp
public AudioClip MenuMove;
public AudioClip MenuConfirm;
public AudioClip MenuCancel;
```

**`Sfx.cs`** (기존 클래스에 상수 추가, 라이브러리 읽는 메서드 수정)

```csharp
public const string MenuMove = "MenuMove";
public const string MenuConfirm = "MenuConfirm";
public const string MenuCancel = "MenuCancel";

private static bool EnsureLibrary();   // 수정
```

라이브러리를 읽는 처리에서, 기존 클립들을 표에 등록하는 부분의 끝에 새 클립 3개의 등록을 같은 방식으로 추가한다. 등록 순서나 반환 조건은 바꾸지 않는다.

**Unity 에디터 작업**

1. 사용할 클립 3개를 고른다. 후보는 이동음은 Universal Sound FX의 `BUTTONS` 폴더(`BUTTON_Clean_Tap`, `BUTTON_Click_01` 등) 같은 짧고 가벼운 소리, 확정음은 Fantasy UI SFX의 `Interface 3-x`(약 0.5초)나 `Magical Interface 5-2`(약 1.1초), 취소음은 `Interface 4-x`(약 0.5초)나 이동음보다 낮은 클릭 계열이다. 직접 들어 보고 정한다.
2. 고른 클립을 `Assets/09_Audio/`에 `Menu_Move`, `Menu_Confirm`, `Menu_Cancel`로 복사한다. 임포트 설정은 기존 SFX와 같은 기본값을 쓴다.
3. `Assets/09_Audio/Resources/SfxLibrary.asset`을 선택하고 새 필드 3개에 클립을 연결한다.

### 단계 2. 메뉴 선택 이동 알림 (`TitleMenuView`)

**`TitleMenuView.cs`** (기존 클래스에 이벤트 추가, 선택 처리 수정)

```csharp
/// <summary>사용자 입력으로 선택 항목이 바뀌었을 때</summary>
public event Action Moved;

private void Select(int index, bool snap);   // 수정
```

선택 처리의 순서는 다음과 같다.

1. 바뀌기 전의 선택 항목을 기억해 둔다.
2. 새 선택 항목을 기록한다.
3. 모든 항목의 선택 표시를 갱신한다. 즉시 전환이 지정되었으면 표시를 곧바로 맞춘다. (기존 동작)
4. 즉시 전환이 아니고, 선택 항목이 이전과 다르다면 이동 알림을 보낸다.

즉시 전환은 메뉴가 활성화될 때 초기 선택에만 쓰이므로, 4번의 조건이 초기 선택을 이동음에서 제외한다. 호버 처리는 이미 같은 항목이면 선택을 부르지 않고, 키보드 이동은 선택 가능한 항목이 하나뿐일 때만 같은 항목이 나오므로, 4번의 비교로 중복이 걸러진다. 이 이벤트는 다른 구독자가 생길 수 있어 확정 이벤트와 같은 방식으로 단순 호출한다.

### 단계 3. 메뉴 반응 연결 (`TitleMenuController`)

**`TitleMenuController.cs`** (기존 클래스 수정, 핸들러 추가)

```csharp
private void Awake();                       // 수정: 이동 알림 구독 추가
private void OnDestroy();                   // 수정: 이동 알림 구독 해제 추가
private void HandleMoved();                 // 신규
private void HandleConfirmed(int index);    // 수정: 맨 앞에 확정음 재생
```

**`Awake`**: 기존에 확정 알림을 구독하는 곳에서, 같은 조건(메뉴가 연결되어 있을 때)으로 이동 알림도 구독한다. **`OnDestroy`**에서는 같은 조건으로 이를 해제한다.

**`HandleMoved`**: 메뉴 이동음을 한 번 재생한다.

**`HandleConfirmed`**의 순서는 다음과 같다.

1. 확정음을 한 번 재생한다.
2. 이후 항목별 분기(새 게임 시작, 설정 열기, 게임 종료, 그 외)는 기존 그대로 수행한다.

확정 알림은 비활성 항목에서는 발생하지 않으므로, 확정음이 비활성 항목에서 나는 경우는 없다. 새 게임 확정 때는 확정음이 난 뒤 기존 시작 연출이 이어진다. 게임 종료 확정 때는 확정음 재생을 요청한 직후 기존 즉시 종료 처리가 이어지며, 효과음은 재생 완료를 기다리지 않으므로 종료 직전의 소리는 들리지 않거나 잘릴 수 있다. 요구사항에 종료 지연이 없으므로 기존 종료 동작은 바꾸지 않는다.

### 단계 4. 설정 창 확정·취소음 (`SettingsPanelView`)

**`SettingsPanelView.cs`** (기존 메서드 두 개에 재생 추가)

```csharp
public void Apply();    // 수정
public void Cancel();   // 수정
```

**`Apply`**의 순서는 다음과 같다.

1. 창이 열려 있지 않으면 종료한다. (기존 동작)
2. 현재 설정을 저장한다. (기존 동작)
3. 확정음을 재생한다.
4. 창을 닫는다. (기존 동작)

**`Cancel`**의 순서는 다음과 같다.

1. 창이 열려 있지 않으면 종료한다. (기존 동작)
2. 전체·BGM·SFX 볼륨과 전체 화면 여부를 창을 열기 전 값으로 되돌린다. (기존 동작)
3. 취소음을 재생한다. 되돌려진 SFX 볼륨으로 재생된다.
4. 창을 닫는다. (기존 동작)

Esc 입력은 타이틀 컨트롤러가 창의 취소를 부르는 기존 경로를 그대로 타므로 별도 처리가 없다. 일시정지 화면에서 이 창을 쓸 때도 같은 두 메서드를 거치므로 같은 소리가 난다. 효과음 재생은 게임 시간 배율과 무관하다.

## 5. 검증 기준 및 테스트 시나리오

| # | 실행 조건 | 수행 행동 | 예상 결과 |
|---|---|---|---|
| 1 | 메뉴가 나타난 직후 | 아무 입력 없이 대기 | 소리가 나지 않는다. (초기 선택 제외) |
| 2 | 메뉴 표시 완료 | 아래/위 키(또는 `W`/`S`)로 이동 | 선택이 바뀔 때마다 이동음이 한 번 난다. 마지막 항목에서 계속 누르면 순환하며 이동음이 난다. |
| 3 | 메뉴 표시 완료 | 마우스를 다른 항목으로 올림 | 이동음이 한 번 난다. 이미 선택된 항목에 다시 올려도 나지 않는다. |
| 4 | '이어하기'가 비활성 | 해당 항목에 마우스를 올리거나 클릭 | 이동음도 확정음도 나지 않는다. |
| 5 | 메뉴 표시 완료 | 새 게임·설정을 확정(키보드 또는 클릭) | 확정음이 한 번 나고 기존 동작(시작 연출, 설정 창 열림)이 이어진다. |
| 6 | 설정 창 열림 | Apply 버튼 | 확정음이 난 뒤 창이 닫히고 값이 저장된다. |
| 7 | 설정 창 열림 | Cancel 버튼 또는 Esc | 취소음이 난 뒤 창이 닫히고 값이 열기 전으로 돌아간다. |
| 8 | 설정 창에서 SFX 슬라이더를 0으로 내림 | Cancel | 값이 원래대로 복원되고 취소음이 복원된 볼륨으로 들린다. |
| 9 | SFX 볼륨 0으로 저장됨 | 메뉴 조작 | 어떤 효과음도 들리지 않는다. |
| 10 | 일시정지 화면의 설정 창 | Apply·Cancel | 각각 확정음·취소음이 난다. |
| 11 | 게임 종료 항목 | 확정 | 확정음 재생을 요청한 직후 종료 처리가 진행된다. (에디터에서는 플레이 종료) 종료 직전의 소리가 들리는지는 보장하지 않는다. |
| 12 | 새 게임 시작 연출 중 | 연출 동안 대기 | 확정음은 한 번만 나고, 연출 중 이동음은 나지 않는다. (메뉴 잠김) |
