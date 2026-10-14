using System;
using UnityEngine;

/// <summary>
/// 게임 설정 값(볼륨, 전체 화면)을 보관하고 저장 파일(SaveSystem.Settings)에 저장한다.
/// 타이틀 화면과 일시정지 화면의 Settings 창이 같은 값을 공유한다.
///
/// - 볼륨은 0~1 값만 저장한다. 실제 소리 크기 연결은 오디오 시스템이 생기면 Changed 이벤트로 받아서 적용한다.
/// - 전체 화면은 바뀌는 즉시 Screen.fullScreen에 적용한다.
/// - Set 함수는 값만 바꾸고(즉시 반영), Save를 불러야 다음 실행에도 유지된다.
/// - 예전에 PlayerPrefs에 저장한 값은 SaveSystem이 처음 읽을 때 옮겨 온다.
/// </summary>
public static class GameSettings
{
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
        SettingsSaveData saved = SaveSystem.Settings;
        saved.MasterVolume = _masterVolume;
        saved.BgmVolume = _bgmVolume;
        saved.SfxVolume = _sfxVolume;
        saved.IsFullscreen = _isFullscreen;
        SaveSystem.Commit();
    }

    private static void EnsureLoaded()
    {
        if (_isLoaded)
        {
            return;
        }

        _isLoaded = true;
        SettingsSaveData saved = SaveSystem.Settings;
        _masterVolume = Mathf.Clamp01(saved.MasterVolume);
        _bgmVolume = Mathf.Clamp01(saved.BgmVolume);
        _sfxVolume = Mathf.Clamp01(saved.SfxVolume);
        _isFullscreen = saved.IsFullscreen;
    }

    private static void Notify()
    {
        SafeEventNotifier.Notify(Changed);
    }

    // 도메인 리로드를 끈 플레이 모드에서도 플레이할 때마다 저장 파일에서 다시 읽는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        _isLoaded = false;
    }
}
