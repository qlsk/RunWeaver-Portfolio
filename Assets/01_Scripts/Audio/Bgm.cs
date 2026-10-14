using Unity.VisualScripting;
using UnityEngine;

public static class Bgm
{
    private enum State
    {
        Stopped,
        Playing,
        Gap,
        FadingOut
    }

    private static GameObject s_rootObject;
    private static AudioSource s_audioSource;
    private static AudioClip s_currentClip;
    private static State s_playbackState;
    private static float s_cooldownSeconds;
    private static float s_cooldownElapsedSeconds;
    private static float s_fadeInDurationSeconds;
    private static float s_fadeRatio;
    private static float s_fadeTargetRatio;
    private static float s_fadeDurationSeconds;
    private static bool s_isSettingsSubscribed;

    public static void Play(AudioClip clip, float cooldownSeconds, float fadeInDurationSeconds)
    {
        EnsureRoot();

        s_currentClip = clip;
        s_cooldownSeconds = cooldownSeconds;
        s_fadeInDurationSeconds = fadeInDurationSeconds;

        StartClip();
    }

    public static void FadeOut(float fadeOutDurationSeconds)
    {
        s_fadeTargetRatio = 0;
        s_fadeDurationSeconds = fadeOutDurationSeconds;
        s_playbackState = State.FadingOut;
    }

    public static void Stop()
    {
        s_audioSource.Stop();
        s_currentClip = null;
        s_playbackState = State.Stopped;
    }

    internal static void Tick(float unscaledDeltaTime)
    {
        // 정지 상태면 아무것도 하지 않는다.
        if (s_playbackState == State.Stopped)
            return;

        // 페이드 비율을 목표를 향해 이동시킨다. 이동 시간이 0 이하이면 즉시 목표에 도달한 것으로 본다. 그다음 볼륨을 반영한다.
        s_fadeRatio = s_fadeDurationSeconds <= 0f
            ? s_fadeTargetRatio
            : Mathf.MoveTowards(s_fadeRatio, s_fadeTargetRatio, unscaledDeltaTime / s_fadeDurationSeconds);
        ApplyVolume();

        // 재생 상태이면 곡이 끝났는지 확인한다. 소스가 더 이상 재생 중이 아니면 곡이 끝난 것으로 보고, 쿨타임 경과 시간을 0으로 두고 쿨타임 상태로 바꾼다.
        if (s_playbackState == State.Playing)
        {
            if (!s_audioSource.isPlaying)
            {
                s_cooldownElapsedSeconds = 0f;
                s_playbackState = State.Gap;
            }
        }

        // 쿨타임 상태이면 경과 시간에 프레임 간격을 더한다. 쿨타임에 도달하면 곡 재생을 다시 시작한다.
        if (s_playbackState == State.Gap)
        {
            s_cooldownElapsedSeconds += unscaledDeltaTime;

            if (s_cooldownElapsedSeconds >= s_cooldownSeconds)
                StartClip();
        }

        // 페이드아웃 상태이면 페이드 비율이 0에 도달했는지 확인한다. 도달했다면 즉시 정지한다.
        if (s_playbackState == State.FadingOut)
        {
            if (s_fadeRatio <= 0f)
                Stop();
        }
    }

    private static void EnsureRoot()
    {
        if (s_rootObject != null)
            return;

        s_playbackState = State.Stopped;
        s_currentClip = null;

        s_rootObject = new GameObject("BgmRoot");
        s_rootObject.transform.SetParent(null);
        Object.DontDestroyOnLoad(s_rootObject);

        s_audioSource = s_rootObject.AddComponent<AudioSource>();
        s_audioSource.loop = false;
        s_audioSource.playOnAwake = false;

        s_rootObject.AddComponent<BgmRunner>();

        if (!s_isSettingsSubscribed)
        {
            GameSettings.Changed += ApplyVolume;
            s_isSettingsSubscribed = true;
        }
    }

    private static void StartClip()
    {
        s_audioSource.clip = s_currentClip;
        s_audioSource.Play();

        s_fadeRatio = 0f;
        s_fadeTargetRatio = 1f;
        s_fadeDurationSeconds = s_fadeInDurationSeconds;

        s_playbackState = State.Playing;
        s_cooldownElapsedSeconds = 0f;

        ApplyVolume();
    }

    private static void ApplyVolume()
    {
        if (s_audioSource == null)
            return;

        s_audioSource.volume = GameSettings.MasterVolume * GameSettings.BgmVolume * s_fadeRatio;
    }
}