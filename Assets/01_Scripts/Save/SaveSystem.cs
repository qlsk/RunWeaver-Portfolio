using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 저장 파일(persistentDataPath/save.json)을 읽고 쓴다.
/// - Meta     : 판과 상관없이 남는 것 (룬북 공개 상태)
/// - Settings : 설정 (GameSettings)
/// - Tutorial : 튜토리얼 완료 여부와 본 기록 (TutorialProgressStore)
/// - Records  : 엔딩 기록 (EndingRecordStore)
/// - Run      : 진행 중인 판의 체크포인트 (층 시작, 상점). 판이 끝나면(사망, 포기, 잠식, 클리어) 지운다.
/// 쓸 때는 임시 파일에 먼저 쓰고 바꿔 끼운다. 쓰는 도중 게임이 꺼져도 이전 파일이 남는다. (.bak 사본도 남긴다)
/// 파일이 깨져 있으면 .corrupt로 옮겨 두고 빈 저장으로 시작한다.
/// 예전에 PlayerPrefs에 저장하던 설정, 튜토리얼, 엔딩 기록은 처음 읽을 때 한 번 이 파일로 옮긴다. (PlayerPrefs는 지우지 않는다)
/// </summary>
public static class SaveSystem
{
    private const string FileName = "save.json";

    private static SaveFile s_file;
    private static bool s_isContinueRequested;
    private static RuneBook s_boundRuneBook;

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
    private static string TempPath => FilePath + ".tmp";
    private static string BackupPath => FilePath + ".bak";

    /// <summary>이어할 판이 있는지 (타이틀의 이어하기)</summary>
    public static bool HasRun => Data.HasRun;

    /// <summary>저장된 판. 없으면 null</summary>
    public static RunSaveData Run => Data.HasRun ? Data.Run : null;

    /// <summary>설정. 값을 바꾼 뒤 Commit으로 파일에 쓴다. (GameSettings)</summary>
    public static SettingsSaveData Settings => Data.Settings;

    /// <summary>튜토리얼. 값을 바꾼 뒤 Commit으로 파일에 쓴다. (TutorialProgressStore)</summary>
    public static TutorialSaveData Tutorial => Data.Tutorial;

    /// <summary>엔딩 기록. 값을 바꾼 뒤 Commit으로 파일에 쓴다. (EndingRecordStore)</summary>
    public static RecordsSaveData Records => Data.Records;

    private static SaveFile Data
    {
        get
        {
            if (s_file == null)
            {
                s_file = Load();
                bool isMigrated = MigratePlayerPrefs(s_file);
                bool isTutorialImported = ImportTutorialPlayerPrefs(s_file);
                if (isMigrated || isTutorialImported)
                {
                    Write();
                }
            }

            return s_file;
        }
    }

    /// <summary>Settings, Tutorial, Records를 바꾼 뒤 파일에 쓴다.</summary>
    public static void Commit()
    {
        Write();
    }

    // ───────────────────────── 진행 중인 판 ─────────────────────────

    /// <summary>층 시작 체크포인트를 저장한다.</summary>
    public static void SaveRun(RunSaveData run)
    {
        if (run == null)
        {
            return;
        }

        run.SavedAtUtc = DateTime.UtcNow.ToString("o");
        Data.Run = run;
        Data.HasRun = true;
        Write();
    }

    /// <summary>판이 끝났거나 새 게임을 시작할 때 저장된 판을 지운다.</summary>
    public static void DeleteRun()
    {
        if (!Data.HasRun)
        {
            return;
        }

        Data.HasRun = false;
        Data.Run = new RunSaveData();
        s_isContinueRequested = false;
        Write();
    }

    /// <summary>타이틀에서 이어하기를 골랐을 때. 전투 씬이 열리면 RunSaveController가 확인하고 지운다.</summary>
    public static void RequestContinue()
    {
        s_isContinueRequested = HasRun;
    }

    /// <summary>이어하기 요청이 있었는지 확인하고 지운다.</summary>
    public static bool ConsumeContinueRequest()
    {
        bool isRequested = s_isContinueRequested && HasRun;
        s_isContinueRequested = false;
        return isRequested;
    }

    // ───────────────────────── 메타 (룬북) ─────────────────────────

    /// <summary>
    /// 룬북에 저장된 공개 상태를 넣고, 이후 바뀔 때마다 저장한다. (GameSession.Initialize에서 한 번 부른다)
    /// </summary>
    public static void BindRuneBook(RuneBook book)
    {
        if (book == null || !book.IsInitialized)
        {
            return;
        }

        if (s_boundRuneBook != null)
        {
            s_boundRuneBook.RuneBookChanged -= HandleRuneBookChanged;
        }

        s_boundRuneBook = null;
        foreach (string id in Data.Meta.RevealedRuneWords)
        {
            try
            {
                book.RevealDetails(id);
            }
            catch (ArgumentException)
            {
                // 데이터에서 빠진 룬 효과. 건너뛴다. (다음 저장 때 목록에서도 빠진다)
            }
        }

        s_boundRuneBook = book;
        book.RuneBookChanged += HandleRuneBookChanged;
    }

    private static void HandleRuneBookChanged()
    {
        if (s_boundRuneBook == null)
        {
            return;
        }

        Data.Meta.RevealedRuneWords.Clear();
        foreach (RuneWordInfo info in s_boundRuneBook.GetAll())
        {
            if (info != null && info.RevealLevel == RuneWordRevealLevel.Detailed)
            {
                Data.Meta.RevealedRuneWords.Add(info.RuneEffectId);
            }
        }

        Write();
    }

    // ───────────────────────── 파일 ─────────────────────────

    private static SaveFile Load()
    {
        SaveFile file = TryRead(FilePath) ?? TryRead(BackupPath);
        if (file == null)
        {
            return new SaveFile();
        }

        file.Meta ??= new MetaSaveData();
        file.Meta.RevealedRuneWords ??= new();
        file.Settings ??= new SettingsSaveData();
        file.Tutorial ??= new TutorialSaveData();
        file.Tutorial.SeenRecords ??= new();
        file.Records ??= new RecordsSaveData();
        file.Run ??= new RunSaveData();
        if (file.Version > SaveFile.CurrentVersion)
        {
            Debug.LogWarning($"[SaveSystem] 더 새로운 형식의 저장 파일입니다 ({file.Version}). 읽을 수 있는 항목만 씁니다.");
        }

        file.Version = SaveFile.CurrentVersion;
        return file;
    }

    // 예전 PlayerPrefs 키. 옮긴 뒤에도 지우지 않는다. (이전 빌드로 돌아가도 기록이 남도록)
    private const string LegacyMasterVolumeKey = "Settings.MasterVolume";
    private const string LegacyBgmVolumeKey = "Settings.BgmVolume";
    private const string LegacySfxVolumeKey = "Settings.SfxVolume";
    private const string LegacyFullscreenKey = "Settings.Fullscreen";
    private const string LegacyTutorialCompletedKey = "Tutorial.Completed";
    private const string LegacyTutorialSeenRecordsKey = "Tutorial.SeenRecords";
    private const string LegacyHomunculusNumberKey = "RuneWeaver.HomunculusNumber";
    private const string LegacyBestClearSecondsKey = "RuneWeaver.BestClearSeconds";

    /// <summary>예전 PlayerPrefs 값이 있으면 한 번만 옮긴다. 옮겼으면 true (파일에 써야 함)</summary>
    private static bool MigratePlayerPrefs(SaveFile file)
    {
        if (file.PlayerPrefsMigrated)
        {
            return false;
        }

        file.PlayerPrefsMigrated = true;

        SettingsSaveData settings = file.Settings;
        if (PlayerPrefs.HasKey(LegacyMasterVolumeKey))
            settings.MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(LegacyMasterVolumeKey, 1f));
        if (PlayerPrefs.HasKey(LegacyBgmVolumeKey))
            settings.BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(LegacyBgmVolumeKey, 1f));
        if (PlayerPrefs.HasKey(LegacySfxVolumeKey))
            settings.SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(LegacySfxVolumeKey, 1f));
        if (PlayerPrefs.HasKey(LegacyFullscreenKey))
            settings.IsFullscreen = PlayerPrefs.GetInt(LegacyFullscreenKey, 1) == 1;

        TutorialSaveData tutorial = file.Tutorial;
        if (PlayerPrefs.GetInt(LegacyTutorialCompletedKey, 0) == 1)
            tutorial.Completed = true;
        string seen = PlayerPrefs.GetString(LegacyTutorialSeenRecordsKey, string.Empty);
        foreach (string id in seen.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!tutorial.SeenRecords.Contains(id))
                tutorial.SeenRecords.Add(id);
        }

        file.IsTutorialPlayerPrefsImported = true;

        RecordsSaveData records = file.Records;
        records.HomunculusNumber = Mathf.Max(records.HomunculusNumber,
            PlayerPrefs.GetInt(LegacyHomunculusNumberKey, 0));
        if (PlayerPrefs.HasKey(LegacyBestClearSecondsKey))
        {
            float best = PlayerPrefs.GetFloat(LegacyBestClearSecondsKey, -1f);
            if (best > 0f && (records.BestClearSeconds < 0f || best < records.BestClearSeconds))
                records.BestClearSeconds = best;
        }

        return true;
    }

    /// <summary>
    /// 저장 파일을 처음 읽을 때 튜토리얼 PlayerPrefs 값을 한 번 합친다. 합쳤으면 true (파일에 써야 함)
    /// 튜토리얼 저장소가 PlayerPrefs를 쓰던 동안 쌓인 완료와 본 기록을 잃지 않기 위해서다.
    /// </summary>
    private static bool ImportTutorialPlayerPrefs(SaveFile file)
    {
        if (file.IsTutorialPlayerPrefsImported)
        {
            return false;
        }

        file.IsTutorialPlayerPrefsImported = true;

        TutorialSaveData tutorial = file.Tutorial;
        if (PlayerPrefs.GetInt(LegacyTutorialCompletedKey, 0) == 1)
            tutorial.Completed = true;
        string seen = PlayerPrefs.GetString(LegacyTutorialSeenRecordsKey, string.Empty);
        foreach (string id in seen.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!tutorial.SeenRecords.Contains(id))
                tutorial.SeenRecords.Add(id);
        }

        return true;
    }

    private static SaveFile TryRead(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            return null;
        }

        try
        {
            string json = System.IO.File.ReadAllText(path);
            SaveFile file = JsonUtility.FromJson<SaveFile>(json);
            if (file == null)
            {
                throw new InvalidDataException("빈 저장 파일");
            }

            return file;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SaveSystem] 저장 파일을 읽지 못했습니다 ({path}): {exception.Message}");
            try
            {
                System.IO.File.Copy(path, path + ".corrupt", true);
            }
            catch (Exception)
            {
                // 사본을 못 남겨도 진행한다.
            }

            return null;
        }
    }

    private static void Write()
    {
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            System.IO.File.WriteAllText(TempPath, JsonUtility.ToJson(Data, true));
            if (System.IO.File.Exists(FilePath))
            {
                System.IO.File.Replace(TempPath, FilePath, BackupPath);
            }
            else
            {
                System.IO.File.Move(TempPath, FilePath);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveSystem] 저장하지 못했습니다: {exception.Message}");
        }
    }

    /// <summary>[테스트용] 저장 파일을 모두 지운다.</summary>
    public static void DeleteAllForDebug()
    {
        foreach (string path in new[] { FilePath, TempPath, BackupPath })
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }

        // 지운 뒤 예전 PlayerPrefs 값을 다시 가져오지 않는다.
        s_file = new SaveFile { PlayerPrefsMigrated = true, IsTutorialPlayerPrefsImported = true };
        s_isContinueRequested = false;
    }

    // 도메인 리로드를 끈 플레이 모드에서도 플레이할 때마다 파일을 다시 읽는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        s_file = null;
        s_isContinueRequested = false;
        s_boundRuneBook = null;
    }
}
