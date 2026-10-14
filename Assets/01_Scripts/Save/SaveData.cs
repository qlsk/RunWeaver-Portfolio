using System;
using System.Collections.Generic;

// 저장 파일(save.json)에 들어가는 내용. 값만 담고 로직은 없다. (JsonUtility로 읽고 쓴다)
// JsonUtility는 Dictionary와 Nullable을 저장하지 못하므로 목록과 빈 문자열로 표현한다.
// ID는 모두 문자열로 저장한다. (룬 효과 ID, 특성 ID, 룬 문자, enum 이름) 순서나 숫자가 바뀌어도 안전하다.

/// <summary>저장 파일 전체</summary>
[Serializable]
public class SaveFile
{
    /// <summary>현재 저장 형식 번호. 항목이 바뀌면 올리고 SaveSystem에서 옛 형식을 변환한다.</summary>
    /// 1 : Meta, Run
    /// 2 : Settings, Tutorial, Records 추가 (PlayerPrefs에서 옮김)
    public const int CurrentVersion = 2;

    public int Version = CurrentVersion;

    /// <summary>판과 상관없이 남는 것</summary>
    public MetaSaveData Meta = new();

    /// <summary>설정 (볼륨, 전체 화면)</summary>
    public SettingsSaveData Settings = new();

    /// <summary>튜토리얼 완료 여부와 본 기록</summary>
    public TutorialSaveData Tutorial = new();

    /// <summary>엔딩 기록 (호문쿨루스 번호, 최단 정화 기록)</summary>
    public RecordsSaveData Records = new();

    /// <summary>예전 PlayerPrefs 저장을 이 파일로 옮겼는지. (한 번만 옮긴다)</summary>
    public bool PlayerPrefsMigrated;

    /// <summary>튜토리얼 PlayerPrefs 값(완료, 본 기록)을 이 파일로 옮겼는지. 한 번만 옮긴다.</summary>
    public bool IsTutorialPlayerPrefsImported;

    /// <summary>진행 중인 판이 있는지. 있으면 타이틀의 "이어하기"가 켜진다.</summary>
    public bool HasRun;

    /// <summary>진행 중인 판 (층 시작 체크포인트). HasRun이 false면 쓰지 않는다.</summary>
    public RunSaveData Run = new();
}

/// <summary>판과 상관없이 남는 것</summary>
[Serializable]
public class MetaSaveData
{
    /// <summary>룬북에서 자세한 설명까지 공개된 룬 효과 ID</summary>
    public List<string> RevealedRuneWords = new();
}

/// <summary>설정. 타이틀과 일시정지의 설정 창이 같이 쓴다. (GameSettings)</summary>
[Serializable]
public class SettingsSaveData
{
    public float MasterVolume = 1f;
    public float BgmVolume = 1f;
    public float SfxVolume = 1f;
    public bool IsFullscreen = true;
}

/// <summary>튜토리얼 (TutorialProgressStore)</summary>
[Serializable]
public class TutorialSaveData
{
    public bool Completed;
    /// <summary>2층 룬북 튜토리얼을 마쳤는지</summary>
    public bool IsRuneBookTutorialCompleted;
    /// <summary>협회 지침서에서 본 기록 ID</summary>
    public List<string> SeenRecords = new();
}

/// <summary>엔딩 기록 (EndingRecordStore)</summary>
[Serializable]
public class RecordsSaveData
{
    /// <summary>지금까지 투입한 호문쿨루스 수. 보고서의 "제 N호"</summary>
    public int HomunculusNumber;
    /// <summary>최단 정화 기록(초). 기록이 없으면 -1</summary>
    public float BestClearSeconds = -1f;
}

/// <summary>진행 중인 판. 층에 막 들어왔을 때(준비 단계)의 상태</summary>
[Serializable]
public class RunSaveData
{
    /// <summary>저장한 시각 (UTC, ISO 8601)</summary>
    public string SavedAtUtc = string.Empty;

    // ── 층과 미션 ──
    public int Depth = 1;
    /// <summary>이 층의 미션 종류 (MissionType 이름). 보스 층이면 빈 문자열</summary>
    public string MissionType = string.Empty;
    /// <summary>다음 층부터 꺼낼 미션 셔플 백 (MissionType 이름, 마지막 원소부터 꺼낸다)</summary>
    public List<string> MissionBag = new();

    // ── 상점 ──
    /// <summary>층 사이 상점에서 저장했는지. 이어하면 상점부터 다시 열고, 나가면 Depth 층이 시작된다.</summary>
    public bool InShop;
    /// <summary>상점 칸별 진열 (산 칸은 Id가 빈 문자열)</summary>
    public List<ShopOfferSaveEntry> ShopOffers = new();
    /// <summary>이번 상점에서 리롤한 횟수 (리롤 비용 계산)</summary>
    public int ShopRerollCount;

    // ── 플레이어 ──
    public float Health;
    public int Gold;

    // ── 룬 ──
    public List<RuneStoneSaveEntry> RuneStones = new();
    /// <summary>장착한 룬 효과 ID (0번 칸이 주 원소)</summary>
    public List<string> EquippedEffectIds = new();
    public int ActiveSlotCount = 2;
    /// <summary>상점에서 산 룬 워드 완성품</summary>
    public List<CountSaveEntry> OwnedRuneWords = new();

    // ── 보상 ──
    public List<string> TraitIds = new();
    public List<PassiveSaveEntry> Passives = new();

    // ── 결과창 기록 ──
    public float ElapsedSeconds;
    public int KillCount;
    public float TotalDamage;
}

[Serializable]
public class RuneStoneSaveEntry
{
    /// <summary>룬 문자 한 글자</summary>
    public string Rune;
    public int Count;
}

[Serializable]
public class ShopOfferSaveEntry
{
    /// <summary>상점 품목 ID. 산 칸이나 빈 칸은 빈 문자열</summary>
    public string Id;
    public int Price;
}

[Serializable]
public class CountSaveEntry
{
    public string Id;
    public int Count;
}

[Serializable]
public class PassiveSaveEntry
{
    /// <summary>PassiveStatType 이름</summary>
    public string Type;
    public int Stacks;
    public float ValuePerStack;
}
