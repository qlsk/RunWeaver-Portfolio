using System;
using UnityEngine;

/// <summary>
/// 게임 설정 값(볼륨, 전체 화면)을 보관하고 PlayerPrefs에 저장한다.
/// 타이틀 화면과 일시정지 화면의 Settings 창이 같은 값을 공유한다.
///
/// - 볼륨은 0~1 값만 저장한다. 실제 소리 크기 연결은 오디오 시스템이 생기면 Changed 이벤트로 받아서 적용한다.
/// - 전체 화면은 바뀌는 즉시 Screen.fullScreen에 적용한다.
/// - Set 함수는 값만 바꾸고(즉시 반영), Save를 불러야 다음 실행에도 유지된다.
/// </summary>
public static class GameSettings
{
    private const string MasterVolumeKey = "Settings.MasterVolume";
    private const string BgmVolumeKey = "Settings.BgmVolume";
    private const string SfxVolumeKey = "Settings.SfxVolume";
    private const string FullscreenKey = "Settings.Fullscreen";

    private static bool _isLoaded;
    private static float _masterVolume = 1f;
    private static float _bgmVolume = 1f;
    private static float _sfxVolume = 1f;
    private static bool _isFullscreen = true;

    /// <summary>설정 값이 바뀌었을 때 (슬라이더를 움직이는 중에도 발생)</summary>
    public static event Action Changed;

    public static float MasterVolume { get { EnsureLoaded(); return _masterVolume; } }
    public static float BgmVolume { get { EnsureLoaded(); return _bgmVolume; } }
    public static float SfxVolume { get { EnsureLoaded(); return _sfxVolume; } }
    public static bool IsFullscreen { get { EnsureLoaded(); return _isFullscreen; } }

    public static void SetMasterVolume(float value)
    {
        EnsureLoaded();
        _masterVolume = Mathf.Clamp01(value);
        Notify();
    }

    public static void SetBgmVolume(float value)
    {
        EnsureLoaded();
        _bgmVolume = Mathf.Clamp01(value);
        Notify();
    }

    public static void SetSfxVolume(float value)
    {
        EnsureLoaded();
        _sfxVolume = Mathf.Clamp01(value);
        Notify();
    }

    public static void SetFullscreen(bool isFullscreen)
    {
        EnsureLoaded();
        _isFullscreen = isFullscreen;
        Screen.fullScreen = isFullscreen;
        Notify();
    }

    /// <summary>현재 값을 저장한다. (다음 실행에도 유지)</summary>
    public static void Save()
    {
        EnsureLoaded();
        PlayerPrefs.SetFloat(MasterVolumeKey, _masterVolume);
        PlayerPrefs.SetFloat(BgmVolumeKey, _bgmVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, _sfxVolume);
        PlayerPrefs.SetInt(FullscreenKey, _isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private static void EnsureLoaded()
    {
        if (_isLoaded)
        {
            return;
        }

        _isLoaded = true;
        _masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        _bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, 1f));
        _sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
        _isFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
    }

    private static void Notify()
    {
        SafeEventNotifier.Notify(Changed);
    }
}
