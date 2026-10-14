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
    public const string Footsteps = "Footsteps";
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
    public const string CleanseSuccess = "CleanseSuccess";
    public const string CleanseEmpty = "CleanseEmpty";
    public const string CleanseCooldown = "CleanseCooldown";
    public const string MissionStart = "MissionStart";
    public const string ElementFire = "ElementFire";
    public const string ElementWater = "ElementWater";
    public const string ElementWind = "ElementWind";
    public const string ElementEarth = "ElementEarth";
    public const string EndingStamp = "EndingStamp";
    public const string MenuMove = "MenuMove";
    public const string MenuConfirm = "MenuConfirm";
    public const string MenuCancel = "MenuCancel";

    private const int PoolSize = 8;

    private static GameObject s_root;
    private static SfxRunner s_runner;
    private static readonly List<AudioSource> s_pool = new();
    private static int s_poolIndex;
    private static readonly Dictionary<string, AudioSource> s_loops = new();
    private static readonly Dictionary<string, AudioClip> s_clips = new();
    private static bool s_loaded;
    private static bool s_warned;
    private static bool s_subscribed;

    public static void Play(string id, float volumeScale = 1f, float pitch = 1f)
    {
        if (!EnsureLibrary()) return;
        if (!s_clips.TryGetValue(id, out AudioClip clip) || clip == null) return;
        EnsureRoot();
        for (int i = 0; i < PoolSize; i++)
        {
            AudioSource source = s_pool[s_poolIndex];
            s_poolIndex = (s_poolIndex + 1) % PoolSize;
            if (source == null) continue;
            source.pitch = pitch;
            source.PlayOneShot(clip, volumeScale);
            return;
        }
    }

    public static void PlayLoop(string id)
    {
        if (!EnsureLibrary()) return;
        if (s_loops.ContainsKey(id)) return;
        if (!s_clips.TryGetValue(id, out AudioClip clip) || clip == null) return;
        EnsureRoot();
        AudioSource source = s_root.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.clip = clip;
        ApplyVolume(source);
        source.Play();
        s_loops[id] = source;
    }

    public static void StopLoop(string id)
    {
        if (!s_loops.TryGetValue(id, out AudioSource source)) return;
        s_loops.Remove(id);
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
        if (s_runner != null && s_loops.TryGetValue(id, out AudioSource source) && source != null)
        {
            s_runner.StartCoroutine(LoopLife(id, source, Mathf.Max(0.1f, seconds), source.volume));
        }
    }

    private static IEnumerator LoopLife(string id, AudioSource source, float seconds, float baseVolume)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (source == null) yield break;
            if (!s_loops.ContainsKey(id)) yield break;
            elapsed += Time.deltaTime;
            source.pitch = 1f + 0.05f * Mathf.Sin(elapsed * 7.3f);
            source.volume = baseVolume * (1f + 0.12f * Mathf.Sin(elapsed * 5.1f + 1f));
            yield return null;
        }
        StopLoop(id);
    }

    private static bool EnsureLibrary()
    {
        if (s_loaded) return s_clips.Count > 0;
        s_loaded = true;
        SfxLibrary library = Resources.Load<SfxLibrary>("SfxLibrary");
        if (library == null)
        {
            if (!s_warned)
            {
                s_warned = true;
                Debug.LogWarning("[Sfx] SfxLibrary를 찾지 못했습니다. 무음으로 동작합니다.");
            }
            return false;
        }
        Add(RunePickedUp, library.RunePickedUp);
        Add(Footsteps, library.Footsteps);
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
        Add(CleanseSuccess, library.CleanseSuccess);
        Add(CleanseEmpty, library.CleanseEmpty);
        Add(CleanseCooldown, library.CleanseCooldown);
        Add(MissionStart, library.MissionStart);
        Add(ElementFire, library.ElementFire);
        Add(ElementWater, library.ElementWater);
        Add(ElementWind, library.ElementWind);
        Add(ElementEarth, library.ElementEarth);
        Add(EndingStamp, library.EndingStamp);
        Add(MenuMove, library.MenuMove);
        Add(MenuConfirm, library.MenuConfirm);
        Add(MenuCancel, library.MenuCancel);
        return s_clips.Count > 0;
    }

    private static void Add(string id, AudioClip clip)
    {
        if (clip != null) s_clips[id] = clip;
    }

    private static void EnsureRoot()
    {
        if (s_root != null) return;
        // Play 재진입 시 static 풀이 파괴된 소스를 들고 있을 수 있어서 전부 버린다.
        s_pool.Clear();
        s_loops.Clear();
        s_poolIndex = 0;
        s_root = new GameObject("Sfx");
        Object.DontDestroyOnLoad(s_root);
        s_runner = s_root.AddComponent<SfxRunner>();
        for (int i = 0; i < PoolSize; i++)
        {
            AudioSource source = s_root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            ApplyVolume(source);
            s_pool.Add(source);
        }
        if (!s_subscribed)
        {
            s_subscribed = true;
            GameSettings.Changed += ApplyAllVolumes;
        }
    }

    private static void ApplyVolume(AudioSource source)
    {
        if (source != null) source.volume = GameSettings.MasterVolume * GameSettings.SfxVolume;
    }

    private static void ApplyAllVolumes()
    {
        foreach (AudioSource source in s_pool) ApplyVolume(source);
        foreach (AudioSource source in s_loops.Values) ApplyVolume(source);
    }
}

public sealed class SfxRunner : MonoBehaviour { }