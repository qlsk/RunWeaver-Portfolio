using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using UnityEngine;

/// 검증을 마친 CSV 묶음을 실행 간 로컬 파일로 보관한다.
public sealed class CsvCacheStore
{
    // CSV 헤더나 정의 구조가 변경되면 버전을 올려 이전 캐시를 무효화한다.
    private const int FormatVersion = 2;
    private const int RequiredCsvCount = 9;
    private readonly string _filePath;

    [Serializable]
    private sealed class Snapshot
    {
        [SerializeField] private int _version;
        [SerializeField] private string _savedAtUtc;
        [SerializeField] private List<Entry> _entries;

        public int Version => _version;
        public List<Entry> Entries => _entries;

        public Snapshot(int version, string savedAtUtc, List<Entry> entries)
        {
            _version = version;
            _savedAtUtc = savedAtUtc;
            _entries = entries;
        }
    }

    [Serializable]
    private sealed class Entry
    {
        [SerializeField] private string _url;
        [SerializeField] private string _csv;

        public string Url => _url;
        public string Csv => _csv;

        public Entry(string url, string csv)
        {
            _url = url;
            _csv = csv;
        }
    }

    public CsvCacheStore(string directoryPath)
    {
        _filePath = Path.Combine(directoryPath, "snapshot.json");
    }

    /// 사용 가능한 전체 캐시를 읽는다. 없거나 손상되면 다운로드를 사용한다.
    public bool TryRead(out Dictionary<string, string> csvByUrl)
    {
        csvByUrl = null;
        try
        {
            if (!File.Exists(_filePath)) return false;

            var snapshot = JsonUtility.FromJson<Snapshot>(File.ReadAllText(_filePath));
            if (!IsValidSnapshot(snapshot))
                throw new InvalidDataException("CSV cache version or required entries do not match.");

            Dictionary<string, string> entries = new(StringComparer.Ordinal);
            bool hasInvalidEntries =
                snapshot.Entries.Any(entry => !IsValidEntry(entry) || !entries.TryAdd(entry.Url, entry.Csv));
            if (hasInvalidEntries)
                throw new InvalidDataException("CSV cache contains an invalid entry.");

            csvByUrl = entries;
            return true;
        }
        catch (Exception exception) when (IsFileError(exception))
        {
            Debug.LogWarning($"[CsvCacheStore] 캐시를 사용할 수 없어 다운로드합니다: {exception.Message}");
            return false;
        }
    }

    /// 전체 초기화 성공 후 저장한다. 저장 실패는 게임 시작을 막지 않는다.
    public bool TryWrite(IReadOnlyDictionary<string, string> csvByUrl)
    {
        if (csvByUrl.Count != RequiredCsvCount)
        {
            Debug.LogWarning("[CsvCacheStore] 전체 CSV가 준비되지 않아 캐시를 저장하지 않습니다.");
            return false;
        }

        try
        {
            Snapshot snapshot = new(FormatVersion, DateTime.UtcNow.ToString("O"), new List<Entry>(csvByUrl.Count));
            foreach (KeyValuePair<string, string> csv in csvByUrl)
            {
                snapshot.Entries.Add(new Entry(csv.Key, csv.Value));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
            string temporaryPath = _filePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(snapshot));
            if (File.Exists(_filePath))
                File.Replace(temporaryPath, _filePath, null);
            else
                File.Move(temporaryPath, _filePath);

            Debug.Log("[CsvCacheStore] CSV 캐시를 저장했습니다.");
            return true;
        }
        catch (Exception exception) when (IsFileError(exception))
        {
            Debug.LogWarning($"[CsvCacheStore] 캐시 저장에 실패했습니다. 로딩은 계속합니다: {exception.Message}");
            return false;
        }
    }

    private static bool IsValidSnapshot(Snapshot snapshot) =>
        snapshot != null && snapshot.Version == FormatVersion &&
        snapshot.Entries != null && snapshot.Entries.Count == RequiredCsvCount;

    private static bool IsValidEntry(Entry entry) =>
        entry != null && !string.IsNullOrWhiteSpace(entry.Url) && !string.IsNullOrWhiteSpace(entry.Csv);

    private static bool IsFileError(Exception exception) =>
        exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException
            or SecurityException;
}