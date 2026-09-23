using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using UnityEngine;
using UnityEngine.Networking;

public class RuneEffectLoader : MonoBehaviour
{
    private const string RuneEffectLoadPath = "Icons/RuneEffect/";

    [Header("CSV URL")]
    [SerializeField] private string _runeEffectCsvUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vR9F5MSLHEOJg7A8euPIs8WimRo-nZ9S7YwK5yHixCZMNlnPSUysFEyxwX9ZUw92XTNw127RO24LYVm/pub?gid=0&single=true&output=csv";

    [Header("References")]
    [SerializeField] private RuneEffectDatabase _runeEffectDatabase;

    private string _runeEffectCsv;

    private IEnumerator Start()
    {
        yield return StartCoroutine(DownloadCsv(_runeEffectCsvUrl));
        IReadOnlyList<RuneEffect> runeEffects = ParseCsv(_runeEffectCsv);
        _runeEffectDatabase.Initialize(runeEffects);
    }

    private IEnumerator DownloadCsv(string url)
    {
        using var request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(request.error);
            yield break;
        }

        _runeEffectCsv = request.downloadHandler.text;
        Debug.Log("Rune effect CSV download successful.");
    }

    private IReadOnlyList<RuneEffect> ParseCsv(string csv)
    {
        List<RuneEffect> runeEffects = new();
        using StringReader reader = new(csv.TrimStart('\uFEFF'));
        using CsvReader csvReader = new(reader, CultureInfo.InvariantCulture);
        if (!csvReader.Read())
        {
            Debug.LogError("The CSV does not have a header.");
            return Array.Empty<RuneEffect>();
        }

        csvReader.ReadHeader();
        while (csvReader.Read())
        {
            RuneEffectCsvHeader row = csvReader.GetRecord<RuneEffectCsvHeader>();

            if (!Enum.TryParse(row.RuneEffectCategory, out RuneEffectCategory runeEffectCategory))
            {
                Debug.LogError($"The string for the rune category with id {row.Id} is invalid.");
                return Array.Empty<RuneEffect>();
            }

            string iconPath = RuneEffectLoadPath + row.IconPath;
            Sprite icon = Resources.Load<Sprite>(iconPath);
            if (icon == null)
            {
                Debug.LogError($"The icon for the rune category with id {row.Id} is invalid.");
                return Array.Empty<RuneEffect>();
            }

            runeEffects.Add(new RuneEffect(row.Id, row.RuneWord, row.EnglishName, row.DisplayName, runeEffectCategory,
                row.Description, icon));
        }

        Debug.Log($"Loaded {runeEffects.Count} rune effects.");
        return runeEffects;
    }
}
