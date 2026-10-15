using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Settings 창 (StartScene의 SettingUIPanel을 프리팹으로 만든 것). 타이틀과 일시정지에서 같이 쓴다.
///
/// - 슬라이더 / 전체 화면 토글을 바꾸면 그 자리에서 바로 적용된다.
/// - Apply : 저장하고 닫는다.
/// - Cancel : 창을 열기 전 값으로 되돌리고 닫는다. (Esc도 Cancel과 같다. Esc는 창을 연 쪽에서 Cancel을 부른다)
/// - _group을 넣으면 열릴 때 서서히 나타난다. (타이틀 확인 창과 같은 연출, 실제 시간)
/// </summary>
[DisallowMultipleComponent]
public class SettingsPanelView : MonoBehaviour
{
    [Header("Volume (0~1)")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;

    [Header("Display")]
    [SerializeField] private Toggle _fullscreenToggle;

    [Header("Buttons")]
    [SerializeField] private Button _applyButton;
    [SerializeField] private Button _cancelButton;

    [Header("등장")]
    [Tooltip("창 전체. 넣으면 열릴 때 서서히 나타난다 (비워 두면 바로 나타난다)")]
    [SerializeField] private CanvasGroup _group;
    [SerializeField, Min(0.01f)] private float _fadeDuration = 0.2f;

    private float _savedMaster;
    private float _savedBgm;
    private float _savedSfx;
    private bool _savedFullscreen;
    private bool _isInitialized;
    private float _openTime;

    /// <summary>Apply 또는 Cancel로 창이 닫혔을 때</summary>
    public event Action Closed;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (_masterSlider != null) _masterSlider.onValueChanged.RemoveListener(GameSettings.SetMasterVolume);
        if (_bgmSlider != null) _bgmSlider.onValueChanged.RemoveListener(GameSettings.SetBgmVolume);
        if (_sfxSlider != null) _sfxSlider.onValueChanged.RemoveListener(GameSettings.SetSfxVolume);
        if (_fullscreenToggle != null) _fullscreenToggle.onValueChanged.RemoveListener(GameSettings.SetFullscreen);
        if (_applyButton != null) _applyButton.onClick.RemoveListener(Apply);
        if (_cancelButton != null) _cancelButton.onClick.RemoveListener(Cancel);
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        if (_masterSlider != null) _masterSlider.onValueChanged.AddListener(GameSettings.SetMasterVolume);
        if (_bgmSlider != null) _bgmSlider.onValueChanged.AddListener(GameSettings.SetBgmVolume);
        if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(GameSettings.SetSfxVolume);
        if (_fullscreenToggle != null) _fullscreenToggle.onValueChanged.AddListener(GameSettings.SetFullscreen);
        if (_applyButton != null) _applyButton.onClick.AddListener(Apply);
        if (_cancelButton != null) _cancelButton.onClick.AddListener(Cancel);
    }

    /// <summary>현재 설정 값을 보여주며 창을 연다.</summary>
    public void Open()
    {
        Initialize();

        _savedMaster = GameSettings.MasterVolume;
        _savedBgm = GameSettings.BgmVolume;
        _savedSfx = GameSettings.SfxVolume;
        _savedFullscreen = GameSettings.IsFullscreen;

        SetSliderWithoutNotify(_masterSlider, _savedMaster);
        SetSliderWithoutNotify(_bgmSlider, _savedBgm);
        SetSliderWithoutNotify(_sfxSlider, _savedSfx);
        if (_fullscreenToggle != null)
        {
            _fullscreenToggle.SetIsOnWithoutNotify(_savedFullscreen);
        }

        gameObject.SetActive(true);
        _openTime = Time.unscaledTime;
        if (_group != null)
        {
            _group.alpha = 0f;
        }

        if (EventSystem.current != null && _masterSlider != null)
        {
            EventSystem.current.SetSelectedGameObject(_masterSlider.gameObject);
        }
    }

    private void Update()
    {
        if (_group != null && _group.alpha < 1f)
        {
            _group.alpha = Mathf.Clamp01((Time.unscaledTime - _openTime) / _fadeDuration);
        }
    }

    /// <summary>저장하고 닫는다.</summary>
    public void Apply()
    {
        // 창이 열려 있지 않으면 종료한다.
        if (!IsOpen)
        {
            return;
        }

        // 현재 설정을 저장한다.
        GameSettings.Save();

        // 확정음을 재생한다.
        Sfx.Play(Sfx.MenuConfirm);

        // 창을 닫는다.
        Close();
    }

    /// <summary>창을 열기 전 값으로 되돌리고 닫는다.</summary>
    public void Cancel()
    {
        // 창이 열려 있지 않으면 종료한다.
        if (!IsOpen)
        {
            return;
        }

        // 각 볼륨과 전체 화면 여부를 창을 열기 전 값으로 되돌린다.
        GameSettings.SetMasterVolume(_savedMaster);
        GameSettings.SetBgmVolume(_savedBgm);
        GameSettings.SetSfxVolume(_savedSfx);
        if (GameSettings.IsFullscreen != _savedFullscreen)
        {
            GameSettings.SetFullscreen(_savedFullscreen);
        }

        // 취소음을 재생한다.
        Sfx.Play(Sfx.MenuCancel);

        // 창을 닫는다.
        Close();
    }

    private void Close()
    {
        gameObject.SetActive(false);
        SafeEventNotifier.Notify(Closed);
    }

    private static void SetSliderWithoutNotify(Slider slider, float normalizedValue)
    {
        if (slider == null)
        {
            return;
        }

        slider.SetValueWithoutNotify(Mathf.Lerp(slider.minValue, slider.maxValue, normalizedValue));
    }
}