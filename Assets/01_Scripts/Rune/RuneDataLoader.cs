using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        StartCoroutine(LoadRuneStonesData());
        StartCoroutine(LoadRuneEffectsData());
    }

    private bool TryCreateRuneStoneList(IReadOnlyList<RuneStoneDataCsvHeader> runeStoneCsvRows,
        out IReadOnlyList<RuneStoneData> runeStoneList)
    {
        runeStoneList = new List<RuneStoneData>();
        List<RuneStoneData> runeStones = new();
        Sprite[] icons = Resources.LoadAll<Sprite>(RuneStoneLoadPath);
        foreach (RuneStoneDataCsvHeader row in runeStoneCsvRows)
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
                Debug.LogError($"Rune stone '{row.Id}': icon '{row.IconName}' was not found in '{RuneStoneLoadPath}'.");
                return false;
            }

            runeStones.Add(new RuneStoneData(row.Id, row.RuneCharacter, runeStoneIcon));
        }

        runeStoneList = runeStones;
        Debug.Log($"Loaded {runeStones.Count} rune stones.");
        return true;
    }

    private IEnumerator LoadRuneStonesData()
    {
        yield return StartCoroutine(CsvLoader.DownloadCsv(_runeStoneCsvUrl, csv => _runeStoneCsv = csv));
        if (string.IsNullOrWhiteSpace(_runeStoneCsv))
        {
            Debug.LogError("Failed to download the rune stone CSV, or its content is empty.");
            yield break;
        }
        Debug.Log("Rune stone CSV downloaded successfully.");

        if (!CsvLoader.TryParseCsv(_runeStoneCsv, out IReadOnlyList<RuneStoneDataCsvHeader> rows)) yield break;
        if (!TryCreateRuneStoneList(rows, out IReadOnlyList<RuneStoneData> runeStonesData)) yield break;

        _runeStoneDatabase.Initialize(runeStonesData);
    }

    private bool TryCreateRuneEffectList(IReadOnlyList<RuneEffectDataCsvHeader> runeEffectCsvRows,
        out IReadOnlyList<RuneEffectData> runeEffectList)
    {
        runeEffectList = new List<RuneEffectData>();
        List<RuneEffectData> runeEffects = new();
        foreach (RuneEffectDataCsvHeader row in runeEffectCsvRows)
        {
            if (!Enum.TryParse(row.RuneEffectCategory, out RuneEffectCategory runeEffectCategory))
            {
                Debug.LogError($"Rune effect '{row.Id}': invalid category '{row.RuneEffectCategory}'.");
                return false;
            }

            string iconName = RuneEffectLoadPath + row.IconName;
            Sprite icon = Resources.Load<Sprite>(iconName);
            if (icon == null)
            {
                Debug.LogError($"Rune effect '{row.Id}': icon was not found at '{iconName}'.");
                return false;
            }

            runeEffects.Add(new RuneEffectData(row.Id, row.RuneWord, row.EnglishName, row.DisplayName,
                runeEffectCategory,
                row.Description, icon));
        }

        runeEffectList = runeEffects;
        Debug.Log($"Loaded {runeEffects.Count} rune effects.");
        return true;
    }

    private IEnumerator LoadRuneEffectsData()
    {
        yield return StartCoroutine(CsvLoader.DownloadCsv(_runeEffectCsvUrl, csv => _runeEffectCsv = csv));
        if (string.IsNullOrWhiteSpace(_runeEffectCsv))
        {
            Debug.LogError("Failed to download the rune effect CSV, or its content is empty.");
            yield break;
        }
        Debug.Log("Rune effect CSV downloaded successfully.");

        if (!CsvLoader.TryParseCsv(_runeEffectCsv, out IReadOnlyList<RuneEffectDataCsvHeader> rows)) yield break;
        if (!TryCreateRuneEffectList(rows, out IReadOnlyList<RuneEffectData> runeEffectsData)) yield break;

        _runeEffectDatabase.Initialize(runeEffectsData);
    }
}
