using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점 효과음. ShopUI 루트에 붙이고, 상점 스크립트들이 상황마다 부른다.
/// - 소리는 상점 밖에 따로 만든 오브젝트에서 낸다. (상점 창이 꺼져도 문 닫히는 소리가 끊기지 않게)
/// - 상점이 열린 동안 게임이 멈춰 있으므로 지연과 페이드는 실제 시간으로 잰다.
/// - 볼륨은 GameSettings(마스터 x 효과음)를 따른다.
/// - 배경 소리(벽난로)는 열 때 서서히 커지고, 나갈 때 서서히 작아진다.
/// 클립이 비어 있는 항목은 조용히 넘어간다.
/// </summary>
[DisallowMultipleComponent]
public class ShopSfx : MonoBehaviour
{
    [Serializable]
    public class Cue
    {
        [Tooltip("여러 개면 무작위로 하나를 고른다")]
        public AudioClip[] Clips;
        [Range(0f, 1f)] public float Volume = 1f;
        [Tooltip("재생 속도 범위 (최소, 최대). 같은 소리가 반복될 때 덜 단조롭게 들린다")]
        public Vector2 Pitch = Vector2.one;
    }

    [Header("입장 / 퇴장")]
    [SerializeField] private Cue _doorBell = new();
    [Tooltip("매대가 밀려 들어올 때")]
    [SerializeField] private Cue _shelfSlide = new();
    [SerializeField, Min(0f)] private float _shelfSlideDelay = 0.1f;
    [SerializeField] private Cue _doorClose = new();

    [Header("칸")]
    [Tooltip("열 때 칸이 하나씩 튀어나올 때")]
    [SerializeField] private Cue _slotAppear = new();
    [Tooltip("새로고침으로 칸이 뒤집힐 때")]
    [SerializeField] private Cue _slotFlip = new();
    [SerializeField] private Cue _select = new();

    [Header("구매")]
    [SerializeField] private Cue _purchase = new();
    [Tooltip("룬 문자 / 룬석을 샀을 때 동전 소리에 겹친다")]
    [SerializeField] private Cue _purchaseRune = new();
    [Tooltip("포션을 샀을 때 동전 소리에 겹친다")]
    [SerializeField] private Cue _purchasePotion = new();
    [Tooltip("마지막 품목까지 다 샀을 때")]
    [SerializeField] private Cue _soldOut = new();
    [SerializeField, Min(0f)] private float _soldOutDelay = 0.25f;
    [Tooltip("골드가 부족하거나 살 수 없을 때")]
    [SerializeField] private Cue _denied = new();
    [Tooltip("골드 숫자가 줄어드는 동안 똑딱이는 소리")]
    [SerializeField] private Cue _goldTick = new();

    [Header("새로고침")]
    [SerializeField] private Cue _reroll = new();

    [Header("상점 주인")]
    [Tooltip("말풍선 글자가 찍힐 때")]
    [SerializeField] private Cue _talk = new();
    [SerializeField] private Cue _poke = new();
    [SerializeField] private Cue _annoyed = new();
    [SerializeField] private Cue _secret = new();

    [Header("배경 소리")]
    [SerializeField] private AudioClip _ambienceClip;
    [SerializeField, Range(0f, 1f)] private float _ambienceVolume = 0.5f;
    [SerializeField, Min(0f)] private float _ambienceFadeIn = 1.5f;
    [SerializeField, Min(0f)] private float _ambienceFadeOut = 0.8f;

    private const int PoolSize = 8;

    private struct Pending
    {
        public Cue Cue;
        public float Time;
    }

    private readonly List<AudioSource> _pool = new();
    private readonly List<Pending> _pending = new();
    private GameObject _audioRoot;
    private AudioSource _ambienceSource;
    private int _poolIndex;
    private float _ambienceLevel;
    private float _ambienceTarget;
    private float _ambienceFadeDuration = 1f;

    private void OnEnable()
    {
        GameSettings.Changed += HandleSettingsChanged;
    }

    private void OnDisable()
    {
        GameSettings.Changed -= HandleSettingsChanged;

        // 창이 꺼지면 남은 예약과 배경 소리를 정리한다. (이미 나고 있는 효과음은 끝까지 난다)
        _pending.Clear();
        _ambienceLevel = 0f;
        _ambienceTarget = 0f;
        if (_ambienceSource != null)
        {
            _ambienceSource.Stop();
        }
    }

    private void OnDestroy()
    {
        if (_audioRoot != null)
        {
            Destroy(_audioRoot);
        }
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            if (_pending[i].Time <= now)
            {
                Cue cue = _pending[i].Cue;
                _pending.RemoveAt(i);
                Play(cue);
            }
        }

        UpdateAmbience(Time.unscaledDeltaTime);
    }

    // ---- 상점 ----

    /// <summary>상점이 열릴 때. 문 종, 매대 소리, 배경 소리</summary>
    public void PlayOpen()
    {
        Play(_doorBell);
        PlayDelayed(_shelfSlide, _shelfSlideDelay);
        StartAmbience();
    }

    /// <summary>상점을 나갈 때. 문 닫히는 소리, 배경 소리 줄이기</summary>
    public void PlayLeave()
    {
        _pending.Clear();
        Play(_doorClose);
        FadeAmbience(0f, _ambienceFadeOut);
    }

    public void PlaySlotAppear() => Play(_slotAppear);
    public void PlaySlotFlip() => Play(_slotFlip);
    public void PlaySelect() => Play(_select);
    public void PlayDenied() => Play(_denied);
    public void PlayGoldTick() => Play(_goldTick);
    public void PlayReroll() => Play(_reroll);

    /// <summary>구매했을 때. 동전 소리에 품목별 소리를 겹치고, 다 팔렸으면 조금 뒤에 축하 소리를 낸다.</summary>
    public void PlayPurchase(ShopItemType type, bool isSoldOut)
    {
        Play(_purchase);
        switch (type)
        {
            case ShopItemType.RuneWord:
            case ShopItemType.RuneStone:
                Play(_purchaseRune);
                break;
            case ShopItemType.Potion:
                Play(_purchasePotion);
                break;
        }

        if (isSoldOut)
        {
            PlayDelayed(_soldOut, _soldOutDelay);
        }
    }

    // ---- 상점 주인 ----

    public void PlayTalk() => Play(_talk);
    public void PlayPoke() => Play(_poke);
    public void PlayAnnoyed() => Play(_annoyed);
    public void PlaySecret() => Play(_secret);

    // ---- 재생 ----

    private void PlayDelayed(Cue cue, float delay)
    {
        if (delay <= 0f)
        {
            Play(cue);
            return;
        }

        _pending.Add(new Pending { Cue = cue, Time = Time.unscaledTime + delay });
    }

    private void Play(Cue cue)
    {
        if (cue == null || cue.Clips == null || cue.Clips.Length == 0)
        {
            return;
        }

        AudioClip clip = cue.Clips[UnityEngine.Random.Range(0, cue.Clips.Length)];
        if (clip == null)
        {
            return;
        }

        EnsureAudio();
        AudioSource source = _pool[_poolIndex];
        _poolIndex = (_poolIndex + 1) % _pool.Count;
        source.pitch = UnityEngine.Random.Range(cue.Pitch.x, Mathf.Max(cue.Pitch.x, cue.Pitch.y));
        source.volume = GetSettingsVolume();
        source.PlayOneShot(clip, cue.Volume);
    }

    private void StartAmbience()
    {
        if (_ambienceClip == null)
        {
            return;
        }

        EnsureAudio();
        if (!_ambienceSource.isPlaying)
        {
            _ambienceLevel = 0f;
            _ambienceSource.clip = _ambienceClip;
            // 매번 같은 곳부터 시작하지 않게 한다.
            _ambienceSource.time = UnityEngine.Random.Range(0f, _ambienceClip.length * 0.9f);
            _ambienceSource.Play();
        }

        FadeAmbience(1f, _ambienceFadeIn);
        ApplyAmbienceVolume();
    }

    private void FadeAmbience(float target, float duration)
    {
        _ambienceTarget = target;
        _ambienceFadeDuration = Mathf.Max(0.01f, duration);
    }

    private void UpdateAmbience(float deltaTime)
    {
        if (_ambienceSource == null || !_ambienceSource.isPlaying)
        {
            return;
        }

        _ambienceLevel = Mathf.MoveTowards(_ambienceLevel, _ambienceTarget, deltaTime / _ambienceFadeDuration);
        ApplyAmbienceVolume();
        if (_ambienceLevel <= 0f && _ambienceTarget <= 0f)
        {
            _ambienceSource.Stop();
        }
    }

    private void ApplyAmbienceVolume()
    {
        if (_ambienceSource != null)
        {
            _ambienceSource.volume = GetSettingsVolume() * _ambienceVolume * _ambienceLevel;
        }
    }

    private void HandleSettingsChanged()
    {
        ApplyAmbienceVolume();
    }

    private static float GetSettingsVolume()
    {
        return GameSettings.MasterVolume * GameSettings.SfxVolume;
    }

    /// <summary>소리를 낼 오브젝트를 상점 밖에 만든다. (상점 창이 꺼져도 소리가 끊기지 않게)</summary>
    private void EnsureAudio()
    {
        if (_audioRoot != null)
        {
            return;
        }

        _audioRoot = new GameObject("ShopSfxAudio");
        _pool.Clear();
        for (int i = 0; i < PoolSize; i++)
        {
            _pool.Add(CreateSource(false));
        }

        _ambienceSource = CreateSource(true);
        _poolIndex = 0;
    }

    private AudioSource CreateSource(bool loop)
    {
        AudioSource source = _audioRoot.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = GetSettingsVolume();
        return source;
    }
}
