using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using CsvHelper;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class CsvLoader
{
    public static async UniTask<string> DownloadCsv(string url)
    {
        using var request = UnityWebRequest.Get(url);
        await request.SendWebRequest();
        return request.result != UnityWebRequest.Result.Success
            ? throw new HttpRequestException($"Failed to download CSV: {request.error}")
            : request.downloadHandler.text;
    }

    public static IReadOnlyList<T> ParseCsv<T>(string csv)
    {
        try
        {
            using StringReader reader = new(csv.TrimStart('\uFEFF'));
            using CsvReader csvReader = new(reader, CultureInfo.InvariantCulture);
            if (!csvReader.Read()) throw new InvalidDataException("CSV Header is missing");
            csvReader.ReadHeader();

            List<T> parsedRows = new();
            while (csvReader.Read())
            {
                parsedRows.Add(csvReader.GetRecord<T>());
            }
            return parsedRows;
        }
        catch (CsvHelperException exception)
        {
            Debug.LogError($"CSV parsing failed: {exception.Message}");
            throw;
        }
    }
}
