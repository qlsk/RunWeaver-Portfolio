using System;
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
    private static CsvCacheStore s_cacheStore;
    private static Dictionary<string, string> s_cachedCsv;
    private static Dictionary<string, string> s_downloadedCsv;

    /// 이번 초기화에서 사용할 CSV의 획득 경로를 고정한다.
    internal static bool BeginInitialization(CsvCacheStore cacheStore, bool preferCache)
    {
        if (s_cacheStore != null)
            throw new InvalidOperationException("CSV initialization is already in progress.");

        Dictionary<string, string> cachedCsv = null;
        bool useCache = preferCache && cacheStore.TryRead(out cachedCsv);
        s_downloadedCsv = new Dictionary<string, string>(StringComparer.Ordinal);
        s_cachedCsv = cachedCsv;
        s_cacheStore = cacheStore;
        Debug.Log(useCache ? "[CsvLoader] 로컬 CSV 캐시를 사용합니다." : "[CsvLoader] CSV를 다운로드합니다.");
        return useCache;
    }

    internal static void SaveInitializationCache()
    {
        if (s_cachedCsv == null)
            s_cacheStore.TryWrite(s_downloadedCsv);
    }

    internal static void EndInitialization()
    {
        s_cacheStore = null;
        s_cachedCsv = null;
        s_downloadedCsv = null;
    }

    public static async UniTask<string> DownloadCsv(string url)
    {
        if (s_cachedCsv != null)
        {
            return s_cachedCsv.TryGetValue(url, out string cachedCsv)
                ? cachedCsv
                : throw new InvalidDataException("The requested URL does not exist in the CSV cache.");
        }

        using var request = UnityWebRequest.Get(url);
        await request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
            throw new HttpRequestException($"Failed to download CSV: {request.error}");

        string csv = request.downloadHandler.text;
        if (s_downloadedCsv != null)
            s_downloadedCsv[url] = csv;
        return csv;
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
