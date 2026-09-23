using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using UnityEngine;
using UnityEngine.Networking;

public class RuneDataLoader : MonoBehaviour
{
    private const string RuneStoneLoadPath = "Icons/RuneStone/Icon_RuneStones";
    private const string RuneEffectLoadPath = "Icons/RuneEffect/";

    [Header("CSV URL")]
    [SerializeField] private string _runeStoneCsvUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vR9F5MSLHEOJg7A8euPIs8WimRo-nZ9S7YwK5yHixCZMNlnPSUysFEyxwX9ZUw92XTNw127RO24LYVm/pub?gid=1083085027&single=true&output=csv";
    [SerializeField] private string _runeEffectCsvUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vR9F5MSLHEOJg7A8euPIs8WimRo-nZ9S7YwK5yHixCZMNlnPSUysFEyxwX9ZUw92XTNw127RO24LYVm/pub?gid=0&single=true&output=csv";

    [Header("References")]
    [SerializeField] private RuneStoneDatabase _runeStoneDatabase;
    [SerializeField] private RuneEffectDatabase _runeEffectDatabase;

    private bool _isRuneStoneCsvDownloadSuccess;
    private bool _isRuneEffectCsvDownloadSuccess;

    private string _runeStoneCsv;
    private string _runeEffectCsv;

    private void Start()
    {
        StartCoroutine(LoadRuneStones());
        StartCoroutine(LoadRuneEffects());
    }

    private IEnumerator DownloadCsv(string url, Action<string> onSuccess)
    {
        using var request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(request.error);
            yield break;
        }

        onSuccess(request.downloadHandler.text);
    }

    private bool TryParseCsv<T>(string csv, out IReadOnlyList<T> parseResult)
    {
        parseResult = new List<T>();

        try
        {
            using StringReader reader = new(csv.TrimStart('\uFEFF'));
            using CsvReader csvReader = new(reader, CultureInfo.InvariantCulture);
            if (!csvReader.Read())
            {
                Debug.LogError("CSV가 헤더를 가지고 있지 않습니다.");
                return false;
            }

            csvReader.ReadHeader();

            List<T> csvRows = new();
            while (csvReader.Read())
            {
                csvRows.Add(csvReader.GetRecord<T>());
            }

            parseResult = csvRows;
            return true;
        }
        catch (CsvHelperException e)
        {
            Debug.LogError($"CSV 파싱 실패: {e.Message}");
            return false;
        }
    }

    private bool TryCreateRuneStoneList(IReadOnlyList<RuneStoneCsvHeader> runeStoneCsvRows,
        out IReadOnlyList<RuneStone> runeStoneList)
    {
        runeStoneList = new List<RuneStone>();
        List<RuneStone> runeStones = new();
        Sprite[] icons = Resources.LoadAll<Sprite>(RuneStoneLoadPath);
        foreach (RuneStoneCsvHeader row in runeStoneCsvRows)
        {
            Sprite runeStoneIcon = null;
            foreach (Sprite icon in icons)
            {
                if (icon.name != row.IconName) continue;
                runeStoneIcon = icon;
                break;
            }

            if (runeStoneIcon == null)
            {
                Debug.LogError($"{row.Id}의 룬석 아이콘 경로가 유효하지 않습니다.");
                return false;
            }

            runeStones.Add(new RuneStone(row.Id, row.RuneCharacter, runeStoneIcon));
        }

        runeStoneList = runeStones;
        Debug.Log($"룬석 {runeStones.Count}개를 로드했습니다.");
        return true;
    }

    private IEnumerator LoadRuneStones()
    {
        Action<string> onSuccess = csv =>
        {
            _runeStoneCsv = csv;
            _isRuneStoneCsvDownloadSuccess = true;
        };

        yield return StartCoroutine(DownloadCsv(_runeStoneCsvUrl, onSuccess));
        if (!_isRuneStoneCsvDownloadSuccess)
        {
            Debug.LogError($"{nameof(_runeStoneCsvUrl)} 다운로드에 실패했습니다.");
            yield break;
        }
        Debug.Log("룬석 CSV 다운로드에 성공했습니다.");

        if (!TryParseCsv(_runeStoneCsv, out IReadOnlyList<RuneStoneCsvHeader> runeStoneCsvRows))
        {
            Debug.LogError($"룬석 CSV 파싱에 실패했습니다.");
            yield break;
        }

        if (!TryCreateRuneStoneList(runeStoneCsvRows, out IReadOnlyList<RuneStone> runeStoneList))
        {
            Debug.LogError("룬석 리스트 생성에 실패했습니다.");
            yield break;
        }

        _runeStoneDatabase.Initialize(runeStoneList);
    }

    private bool TryCreateRuneEffectList(IReadOnlyList<RuneEffectCsvHeader> runeEffectCsvRows,
        out IReadOnlyList<RuneEffect> runeEffectList)
    {
        runeEffectList = new List<RuneEffect>();
        List<RuneEffect> runeEffects = new();
        foreach (RuneEffectCsvHeader row in runeEffectCsvRows)
        {
            if (!Enum.TryParse(row.RuneEffectCategory, out RuneEffectCategory runeEffectCategory))
            {
                Debug.LogError($"{row.Id}의 룬 효과 카테고리가 유효하지 않습니다.");
                return false;
            }

            string iconName = RuneEffectLoadPath + row.IconName;
            Sprite icon = Resources.Load<Sprite>(iconName);
            if (icon == null)
            {
                Debug.LogError($"{row.Id}의 룬 효과 아이콘 경로가 유효하지 않습니다.");
                return false;
            }

            runeEffects.Add(new RuneEffect(row.Id, row.RuneWord, row.EnglishName, row.DisplayName, runeEffectCategory,
                row.Description, icon));
        }

        runeEffectList = runeEffects;
        Debug.Log($"룬 효과 {runeEffects.Count}개를 로드했습니다.");
        return true;
    }

    private IEnumerator LoadRuneEffects()
    {
        Action<string> onSuccess = csv =>
        {
            _runeEffectCsv = csv;
            _isRuneEffectCsvDownloadSuccess = true;
        };

        yield return StartCoroutine(DownloadCsv(_runeEffectCsvUrl, onSuccess));
        if (!_isRuneEffectCsvDownloadSuccess)
        {
            Debug.LogError($"{nameof(_runeEffectCsvUrl)} 다운로드에 실패했습니다.");
            yield break;
        }
        Debug.Log("룬 효과 CSV 다운로드에 성공했습니다.");

        if (!TryParseCsv(_runeEffectCsv, out IReadOnlyList<RuneEffectCsvHeader> runeEffectCsvRows))
        {
            Debug.LogError($"룬 효과 CSV 파싱에 실패했습니다.");
            yield break;
        }

        if (!TryCreateRuneEffectList(runeEffectCsvRows, out IReadOnlyList<RuneEffect> runeEffectList))
        {
            Debug.LogError("룬 효과 리스트 생성에 실패했습니다.");
            yield break;
        }

        _runeEffectDatabase.Initialize(runeEffectList);
    }
}
