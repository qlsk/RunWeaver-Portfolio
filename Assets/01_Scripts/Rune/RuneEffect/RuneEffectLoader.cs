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

    private bool _isDownloadSuccess;

    private string _runeEffectCsv;

    private IEnumerator Start()
    {
        yield return StartCoroutine(DownloadCsv(_runeEffectCsvUrl));
        if (!_isDownloadSuccess)
        {
            Debug.LogError($"{nameof(_runeEffectCsvUrl)} 다운로드에 실패했습니다.");
            yield break;
        }

        if (!TryParseCsv(_runeEffectCsv, out IReadOnlyList<RuneEffect> runeEffects))
        {
            Debug.LogError($"룬 효과 CSV 파싱에 실패했습니다.");
            yield break;
        }

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
        Debug.Log("룬 효과 CSV 다운로드에 성공했습니다.");
        _isDownloadSuccess = true;
    }

    private bool TryParseCsv(string csv, out IReadOnlyList<RuneEffect> runeEffects)
    {
        runeEffects = new List<RuneEffect>();
        List<RuneEffect> parse = new();

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
            while (csvReader.Read())
            {
                RuneEffectCsvHeader row = csvReader.GetRecord<RuneEffectCsvHeader>();

                if (!Enum.TryParse(row.RuneEffectCategory, out RuneEffectCategory runeEffectCategory))
                {
                    Debug.LogError($"{row.Id}의 룬 효과 카테고리가 유효하지 않습니다.");
                    return false;
                }

                string iconPath = RuneEffectLoadPath + row.IconPath;
                Sprite icon = Resources.Load<Sprite>(iconPath);
                if (icon == null)
                {
                    Debug.LogError($"{row.Id}의 룬 효과 아이콘 경로가 유효하지 않습니다.");
                    return false;
                }

                parse.Add(new RuneEffect(row.Id, row.RuneWord, row.EnglishName, row.DisplayName, runeEffectCategory,
                    row.Description, icon));
            }

            Debug.Log($"룬 효과 {parse.Count}개를 로드했습니다.");
            runeEffects = parse;
            return true;
        }
        catch (CsvHelperException e)
        {
            Debug.LogError($"CSV 파싱 실패: {e.Message}");
            return false;
        }
    }
}
