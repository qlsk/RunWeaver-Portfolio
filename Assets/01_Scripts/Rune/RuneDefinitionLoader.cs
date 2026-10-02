using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class RuneDefinitionLoader : MonoBehaviour
{
    [Header("CSV URL")]
    [SerializeField] private string _runeStoneCsvUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vR9F5MSLHEOJg7A8euPIs8WimRo-nZ9S7YwK5yHixCZMNlnPSUysFEyxwX9ZUw92XTNw127RO24LYVm/pub?gid=1083085027&single=true&output=csv";
    [SerializeField] private string _runeEffectCsvUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vR9F5MSLHEOJg7A8euPIs8WimRo-nZ9S7YwK5yHixCZMNlnPSUysFEyxwX9ZUw92XTNw127RO24LYVm/pub?gid=0&single=true&output=csv";

    [Header("References")]
    [SerializeField] private RuneStoneIconCatalog _runeStoneIconCatalog;
    [SerializeField] private RuneEffectIconCatalog _runeEffectIconCatalog;

    private bool _isRuneStoneCsvDownloadSuccess;
    private bool _isRuneEffectCsvDownloadSuccess;

    private string _runeStoneCsv;
    private string _runeEffectCsv;

    private IReadOnlyList<RuneStoneDefinition> CreateRuneStoneList(IReadOnlyList<RuneStoneCsvHeader> parsedRows)
    {
        List<RuneStoneDefinition> runeStones = new();
        foreach (RuneStoneCsvHeader row in parsedRows)
        {
            Sprite icon = _runeStoneIconCatalog.GetIcon(row.Id).Icon;
            runeStones.Add(new RuneStoneDefinition(row.Id, row.RuneCharacter, icon));
        }
        Debug.Log($"Loaded {runeStones.Count} rune stones.");
        return runeStones;
    }

    private IReadOnlyList<RuneEffectDefinition> CreateRuneEffectList(IReadOnlyList<RuneEffectCsvHeader> parsedRows)
    {
        List<RuneEffectDefinition> runeEffects = new();
        foreach (RuneEffectCsvHeader row in parsedRows)
        {
            if (string.IsNullOrWhiteSpace(row.RuneWord))
            {
                throw new InvalidDataException($"Rune word of {row.Id} is empty");
            }
            if (!Enum.TryParse(row.RuneEffectCategory, out RuneEffectCategory runeEffectCategory))
            {
                throw new InvalidDataException($"Invalid rune effect category: {row.RuneEffectCategory}");
            }

            Sprite icon = _runeEffectIconCatalog.GetIcon(row.Id).Icon;
            if (icon == null)
            {
                throw new InvalidDataException($"Rune effect icon is not assigned: {row.Id}");
            }
            if (!bool.TryParse(row.CanOverlap, out bool canOverlap))
            {
                throw new InvalidDataException($"Invalid rune effect canOverlap: {row.CanOverlap}");
            }

            runeEffects.Add(new RuneEffectDefinition(row.Id, row.RuneWord, row.EnglishName, row.DisplayName,
                runeEffectCategory, row.Description, icon, canOverlap));
        }
        Debug.Log($"Loaded {runeEffects.Count} rune effects.");
        return runeEffects;
    }

    public async UniTask<IReadOnlyList<RuneStoneDefinition>> LoadRuneStoneDefinitions()
    {
        string csv = await CsvLoader.DownloadCsv(_runeStoneCsvUrl);
        Debug.Log("Rune stone CSV downloaded successfully.");
        IReadOnlyList<RuneStoneCsvHeader> parsedRows = CsvLoader.ParseCsv<RuneStoneCsvHeader>(csv);
        return CreateRuneStoneList(parsedRows);
    }

    public async UniTask<IReadOnlyList<RuneEffectDefinition>> LoadRuneEffectDefinitions()
    {
        string csv = await CsvLoader.DownloadCsv(_runeEffectCsvUrl);
        Debug.Log("Rune effect CSV downloaded successfully.");
        IReadOnlyList<RuneEffectCsvHeader> parsedRows = CsvLoader.ParseCsv<RuneEffectCsvHeader>(csv);
        return CreateRuneEffectList(parsedRows);
    }
}