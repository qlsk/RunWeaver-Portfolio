# 사운드 체크리스트

> 전수 조사 기준 (`Assets/01_Scripts`, `Assets/09_Audio`).
> 현황: `Assets/09_Audio` 비어 있음. 재생 코드 보유는 `Stage/RewardSelectUI.cs` 1곳만.
> 볼륨 인프라는 `UI/Settings/GameSettings.cs` (`Master/Bgm/SfxVolume` + `Changed` + PlayerPrefs) 사용.

## 0. 공통 인프라

- [ ] 공용 SFX 플레이어 마련 (`RewardSelectUI`의 `PlaySound`+`ApplySoundVolume` 패턴 승격)
- [ ] BGM 컨트롤러 신설 (`RunFlow` / `StageManager.PhaseChanged` / `BossPatternController.Phase` 구독, `GameSettings.BgmVolume` 연동)
- [ ] 전역 팝업 열림/닫힘음 (`UI/PopupController.cs` — `OpenPopup` / `ClosePopup`)

## 1. BGM (미구현, 큐·크로스페이드 없음)

- [ ] 타이틀 테마 (`UI/Title/TitleMenuController.cs`, `TitleSequence.cs`)
- [ ] 마을·거점 테마 (`Preparation/ExpeditionStatue.cs`, `UI/Shop/ShopPresenter.cs`)
- [ ] 전투 테마 — 미션 시작 시 전환 (`Stage/StageManager.cs`, `Stage/MissionManager.cs`)
- [ ] 보스 테마 + 페이즈 스팅어 (`Enemy/Attack/BossPatternController.cs` Phase 1/2/3)
- [ ] 결과 테마 — 클리어/실패 (`UI/Mission/MissionResultPresenter.cs`, `UI/Clear/RunClearSequence.cs`, `UI/Failure/RunFailureSequence.cs`)

## 2. Player

- [ ] 공격 발사음 — 원소별 (`Player/PlayerAttack.cs` — `Fire`, `Fired`)
- [ ] 재장전 시작/완료음 (`Player/PlayerAttack.cs` — `Reload`, `UpdateReload`)
- [ ] 피격음 — 쉴드 피격 vs 본체 피격 구분 (`Player/PlayerHealth.cs` — `TakeDamage`, `Damaged`)
- [ ] 사망음 (`Player/PlayerHealth.cs` — `Death`, `Died`)
- [ ] 이동 발소리/루프 (`Player/PlayerMove.cs`)
- [ ] 대시 시작 whoosh + 착지음 (`Skill/Dash.cs` — `DashStarted` / `DashFinished`)
- [ ] 쉴드 생성음/파괴음 (`Skill/Shield.cs` — `ShieldUp`, `ShieldBroken`)
- [ ] 정화 성공/빈 정화/쿨 거절음 (`Skill/Cleanse.cs` — `Cleansed(cleared)`)
- [ ] 상호작용 확정음 (`Player/PlayerInteraction.cs`)

## 3. Enemy

- [ ] 적 피격음 (`Enemy/EnemyHealth.cs` — `OnDamaged`, `OnDamageDealt`)
- [ ] 적 사망음 — 일반/보스 분리 (`Enemy/EnemyHealth.cs` — `OnDeath`)
- [ ] 원거리 발사음 (`Enemy/Attack/EnemyRangeAttack.cs` — `ProjectileFired`)
- [ ] 적 탄환 적중음 — 플레이어 피격음과 중복 방지 (`Enemy/Attack/EnemyBullet.cs`)
- [ ] 공격예고 차지업음 (`Enemy/Attack/EnemyTankAttack.cs`, `EnemyDashAttack.cs`, `EnemyRockfallAttack.cs`, `EnemyShockwaveAttack.cs`)
- [ ] 공격 타격음 — 성공/헛방 구분 (`AttackEnded`)
- [ ] 보스 패턴 발동음 + 페이즈 경고음 (`Enemy/Attack/BossPatternController.cs`)

## 4. 탄환·원소반응 (10종, 전부 무음)

- [ ] 탄환 비행 루프음 — 원소별 (`Bullet/Bullet.cs`)
- [ ] 탄환 적중음 + 소멸음 (`Bullet/Bullet.cs` — `Ended`, `BulletCastEffect.cs` — `PlayAt`)
- [ ] 체인 발동음 (`Bullet/BulletHitEffects.cs` — `ApplyChain`)
- [ ] OverHeat — 화+화 (`StatusEffect/Reactions/OverHeatReaction.cs`)
- [ ] Steam — 화+수 (`StatusEffect/Reactions/SteamReaction.cs`)
- [ ] Molten — 화+지 (`StatusEffect/Reactions/MoltenReaction.cs`)
- [ ] Firestorm — 화+풍 (`StatusEffect/Reactions/FirestormReaction.cs`)
- [ ] Deluge — 수+수 (`StatusEffect/Reactions/DelugeReaction.cs`)
- [ ] Mire — 수+지 (`StatusEffect/Reactions/MireReaction.cs`)
- [ ] Tempest — 수+풍 (`StatusEffect/Reactions/TempestReaction.cs`)
- [ ] Petrify — 지+지 (`StatusEffect/Reactions/PetrifyReaction.cs`)
- [ ] Cyclone — 풍+풍 (`StatusEffect/Reactions/CycloneReaction.cs`)
- [ ] Sandstorm — 지+풍 (`StatusEffect/Reactions/SandstormReaction.cs`)
- [ ] 장판 틱음 (`StatusEffect/Areas/` 11종)

## 5. Stage

- [ ] 임무 시작 징글 (`Stage/MissionManager.cs` — `OnMissionInitialized` / `OnMissionStarted`)
- [ ] 임무 클리어 팡파레 / 실패 스팅어 (`Stage/MissionBase.cs` — `CompleteMission` / `FailMission`)
- [ ] 정화웨이브 발동 whoosh + 정화 틱음 (`Stage/PurificationWave.cs` — `PlayDetached`, `PurifyInside`, `Finished`)
- [ ] 보상UI — 열림/호버/선택/확정/리롤 (보유: `Stage/RewardSelectUI.cs`)
- [ ] 보상 확정 팡 — 특성/패시브 (`Stage/RewardController.cs` — `ApplyReward`)
- [ ] 룬석 살포음 (`Stage/RuneStoneDropper.cs` — `Scatter`)
- [ ] 룬석/골드 획득음 (`Enemy/EnemyDrop.cs`, `Rune/RuneStone/RunePickup.cs`)
- [ ] 포탈 생성 등장음 (`Stage/PortalSpawner.cs` — `SpawnPortal`)
- [ ] 포탈 진입음 (`Stage/Portal.cs` — `Interact`)
- [ ] 포탈 도착음 (`Stage/PortalArrival.cs`)
- [ ] 층전환 whoosh + 도착 징글 (`Stage/StageManager.cs`, `Core/SceneTransition.cs`)
- [ ] 상점 오픈/닫힘 + 구매/거절음 (`UI/Shop/ShopPresenter.cs`, `ShopSlotView.cs`)

## 6. UI

- [ ] 타이틀 메뉴 이동틱/확정음/시작 징글 (`UI/Title/TitleMenuView.cs`, `TitleMenuController.cs`)
- [ ] 일시정지 열림/재개 카운트다운 틱 (`UI/Pause/PauseController.cs`, `CountdownView.cs`)
- [ ] 설정/확인 다이얼로그음 (`SettingsPanelView.cs`, `ConfirmDialogView.cs`)
- [ ] 룬북 열림/장착/해제/경고/확정음 (`UI/RuneBookV2/RuneBookV2Presenter.cs`, `RuneBookV2Fx.cs`)
- [ ] 원소 선택음 (`UI/RuneBookV2`, `MainElementSelectView.cs`)
- [ ] 미션 결과 징글 (`UI/Mission/MissionResultPresenter.cs` — `ShowSuccess` / `ShowFailure` / `ShowClear`)
- [ ] 클리어 시퀀스음 — 고동/파열/섬광 (`UI/Clear/RunClearSequence.cs`)
- [ ] 실패 시퀀스음 (`UI/Failure/RunFailureSequence.cs`)
