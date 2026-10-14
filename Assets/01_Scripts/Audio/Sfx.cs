using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 중앙 SFX 재생기. SfxLibrary(Resources)를 읽어 id 한 줄로 재생한다.
/// 클립·라이브러리가 없으면 조용히 무시된다. 볼륨은 GameSettings를 따른다.
/// </summary>
public static class Sfx
{
    public const string RunePickedUp = "RunePickedUp";
    public const string EnemyDeath = "EnemyDeath";
    public const string EnemyHit = "EnemyHit";
    public const string PlayerHit = "PlayerHit";
    public const string ShieldInit = "ShieldInit";
    public const string PortalMove = "PortalMove";
    public const string CleanseDone = "CleanseDone";
    public const string ShieldHit = "ShieldHit";
    public const string TornadoLoop = "TornadoLoop";
    public const string RewardShow = "RewardShow";
    public const string Reload = "Reload";
    public const string TraitSelect = "TraitSelect";
    public const string Dash = "Dash";
    public const string CleanseSpell = "CleanseSpell";
    public const string MissionStart = "MissionStart";
    public const string ElementFire = "ElementFire";
    public const string ElementWater = "ElementWater";
    public const string ElementWind = "ElementWind";
    public const string ElementEarth = "ElementEarth";
    public const string StatueBreak = "StatueBreak";
    public const string UiClick = "UiClick";
    public const string UiHover = "UiHover";

    private const int PoolSize = 8;

    private static GameObject _root;
    private static SfxRunner _runner;
    private static readonly List<AudioSource> _pool = new();
    private static int _poolIndex;
    private static readonly Dictionary<string, AudioSource> _loops = new();
    private static readonly Dictionary<string, AudioClip> _clips = new();
    private static readonly List<AudioClip> _footstepVariants = new();
    private static bool _loaded;
    private static bool _warned;
    private static bool _subscribed;

    public static void Play(string id, float volumeScale = 1f, float pitch = 1f)
    {
        if (!EnsureLibrary()) return;
        if (!_clips.TryGetValue(id, out AudioClip clip) || clip == null) return;
        EnsureRoot();
        for (int i = 0; i < PoolSize; i++)
        {
            AudioSource source = _pool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % PoolSize;
            if (source == null) continue;
            source.pitch = pitch;
            source.PlayOneShot(clip, volumeScale);
            return;
        }
    }

    /// <summary>발걸음 1회. 변주 샘플 중 무작위 + 피치 변주로 반복감을 줄인다.</summary>
    public static void PlayFootstep(float volumeScale = 0.45f)
    {
        if (!EnsureLibrary() || _footstepVariants.Count == 0) return;
        AudioClip clip = _footstepVariants[Random.Range(0, _footstepVariants.Count)];
        if (clip == null) return;
        EnsureRoot();
        for (int i = 0; i < PoolSize; i++)
        {
            AudioSource source = _pool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % PoolSize;
            if (source == null) continue;
            source.pitch = Random.Range(0.78f, 0.88f);
            source.PlayOneShot(clip, volumeScale);
            return;
        }
    }

    public static void PlayLoop(string id)
    {
        if (!EnsureLibrary()) return;
        if (_loops.ContainsKey(id)) return;
        if (!_clips.TryGetValue(id, out AudioClip clip) || clip == null) return;
        EnsureRoot();
        AudioSource source = _root.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.clip = clip;
        ApplyVolume(source);
        source.Play();
        _loops[id] = source;
    }

    public static void StopLoop(string id)
    {
        if (!_loops.TryGetValue(id, out AudioSource source)) return;
        _loops.Remove(id);
        if (source != null)
        {
            source.Stop();
            Object.Destroy(source);
        }
    }

    /// <summary>시전 동안 반복이 필요한 음성용. seconds 뒤 자동 정지한다.</summary>
    public static void PlayLoopFor(string id, float seconds)
    {
        PlayLoop(id);
        if (_runner != null && _loops.TryGetValue(id, out AudioSource source) && source != null)
        {
            _runner.StartCoroutine(LoopLife(id, source, Mathf.Max(0.1f, seconds), source.volume));
        }
    }

    private static IEnumerator LoopLife(string id, AudioSource source, float seconds, float baseVolume)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (source == null) yield break;
            if (!_loops.ContainsKey(id)) yield break;
            elapsed += Time.deltaTime;
            source.pitch = 1f + 0.05f * Mathf.Sin(elapsed * 7.3f);
            source.volume = baseVolume * (1f + 0.12f * Mathf.Sin(elapsed * 5.1f + 1f));
            yield return null;
        }
        StopLoop(id);
    }

    private static bool EnsureLibrary()
    {
        if (_loaded) return _clips.Count > 0;
        _loaded = true;
        SfxLibrary library = Resources.Load<SfxLibrary>("SfxLibrary");
        if (library == null)
        {
            if (!_warned)
            {
                _warned = true;
                Debug.LogWarning("[Sfx] SfxLibrary를 찾지 못했습니다. 무음으로 동작합니다.");
            }
            return false;
        }
        Add(RunePickedUp, library.RunePickedUp);
        if (library.FootstepVariants != null)
        {
            foreach (AudioClip clip in library.FootstepVariants)
            {
                if (clip != null) _footstepVariants.Add(clip);
            }
        }
        Add(EnemyDeath, library.EnemyDeath);
        Add(EnemyHit, library.EnemyHit);
        Add(PlayerHit, library.PlayerHit);
        Add(ShieldInit, library.ShieldInit);
        Add(PortalMove, library.PortalMove);
        Add(CleanseDone, library.CleanseDone);
        Add(ShieldHit, library.ShieldHit);
        Add(TornadoLoop, library.TornadoLoop);
        Add(RewardShow, library.RewardShow);
        Add(Reload, library.Reload);
        Add(TraitSelect, library.TraitSelect);
        Add(Dash, library.Dash);
        Add(CleanseSpell, library.CleanseSpell);
        Add(MissionStart, library.MissionStart);
        Add(ElementFire, library.ElementFire);
        Add(ElementWater, library.ElementWater);
        Add(ElementWind, library.ElementWind);
        Add(ElementEarth, library.ElementEarth);
        Add(StatueBreak, library.StatueBreak);
        Add(UiClick, library.UiClick);
        Add(UiHover, library.UiHover);
        return _clips.Count > 0;
    }

    private static void Add(string id, AudioClip clip)
    {
        if (clip != null) _clips[id] = clip;
    }

    private static void EnsureRoot()
    {
        if (_root != null) return;
        // Play 재진입 시 static 풀이 파괴된 소스를 들고 있을 수 있어서 전부 버린다.
        _pool.Clear();
        _loops.Clear();
        _poolIndex = 0;
        _root = new GameObject("Sfx");
        Object.DontDestroyOnLoad(_root);
        _runner = _root.AddComponent<SfxRunner>();
        for (int i = 0; i < PoolSize; i++)
        {
            AudioSource source = _root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            ApplyVolume(source);
            _pool.Add(source);
        }
        if (!_subscribed)
        {
            _subscribed = true;
            GameSettings.Changed += ApplyAllVolumes;
        }
    }

    private static void ApplyVolume(AudioSource source)
    {
        if (source != null) source.volume = GameSettings.MasterVolume * GameSettings.SfxVolume;
    }

    private static void ApplyAllVolumes()
    {
        foreach (AudioSource source in _pool) ApplyVolume(source);
        foreach (AudioSource source in _loops.Values) ApplyVolume(source);
    }
}

public sealed class SfxRunner : MonoBehaviour
{
}
