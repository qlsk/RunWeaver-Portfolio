using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상황별 BGM 전환기. BgmLibrary(Resources)에서 곡을 읽어 존 id 한 줄로 바꾼다.
/// Bgm 단일 소스라 겹재생이 없고, 전환은 페이드아웃 뒤 순차 재생한다.
/// 클립·라이브러리가 없으면 조용히 무시된다.
/// </summary>
public static class BgmDirector
{
    public const string Title = "Title";
    public const string Combat = "Combat";
    public const string Boss = "Boss";
    public const string Shop = "Shop";
    public const string ResultClear = "ResultClear";
    public const string ResultFailure = "ResultFailure";

    private const float SwitchFadeSeconds = 0.5f;
    private const float DefaultCooldownSeconds = 8f;
    private const float DefaultFadeInSeconds = 1.5f;
    private const float DefaultVolumeScale = 0.5f;

    private static GameObject _root;
    private static BgmDirectorRunner _runner;
    private static readonly Dictionary<string, AudioClip> _clips = new();
    private static bool _loaded;
    private static bool _warned;
    private static string _current;
    private static string _pending;

    public static void PlayZone(string zone)
    {
        if (_current == zone || _pending == zone) return;
        if (!EnsureLibrary()) return;
        if (!_clips.TryGetValue(zone, out AudioClip clip) || clip == null) return;
        EnsureRunner();
        _pending = zone;
        Bgm.FadeOut(SwitchFadeSeconds);
        _runner.StartCoroutine(SwitchAfter(SwitchFadeSeconds + 0.05f, zone, clip));
    }

    public static void Stop()
    {
        _current = null;
        _pending = null;
        Bgm.Stop();
    }

    private static IEnumerator SwitchAfter(float seconds, string zone, AudioClip clip)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (_pending != zone) yield break;
        _pending = null;
        _current = zone;
        Bgm.Play(clip, DefaultCooldownSeconds, DefaultFadeInSeconds, DefaultVolumeScale);
    }

    private static bool EnsureLibrary()
    {
        if (_loaded) return _clips.Count > 0;
        _loaded = true;
        BgmLibrary library = Resources.Load<BgmLibrary>("BgmLibrary");
        if (library == null)
        {
            if (!_warned)
            {
                _warned = true;
                Debug.LogWarning("[BgmDirector] BgmLibrary를 찾지 못했습니다. 무음으로 동작합니다.");
            }
            return false;
        }
        Add(Title, library.Title);
        Add(Combat, library.Combat);
        Add(Boss, library.Boss);
        Add(Shop, library.Shop);
        Add(ResultClear, library.ResultClear);
        Add(ResultFailure, library.ResultFailure);
        return _clips.Count > 0;
    }

    private static void Add(string id, AudioClip clip)
    {
        if (clip != null) _clips[id] = clip;
    }

    private static void EnsureRunner()
    {
        if (_root != null) return;
        _root = new GameObject("BgmDirector");
        Object.DontDestroyOnLoad(_root);
        _runner = _root.AddComponent<BgmDirectorRunner>();
    }
}

public sealed class BgmDirectorRunner : MonoBehaviour
{
}
