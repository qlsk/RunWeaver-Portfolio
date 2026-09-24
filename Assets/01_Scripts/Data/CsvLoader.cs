using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using UnityEngine;
using UnityEngine.Networking;

public static class CsvLoader
{
    public static IEnumerator DownloadCsv(string url, Action<string> onSuccess)
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

    public static bool TryParseCsv<T>(string csv, out IReadOnlyList<T> parseResult)
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
}
