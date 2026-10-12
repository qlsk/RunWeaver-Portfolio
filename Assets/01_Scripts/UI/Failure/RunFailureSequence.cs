using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 런 실패 연출. 연출이 끝나면 넘겨받은 콜백으로 결과창(EndingUI)을 연다. MissionResultPresenter가 부른다.
/// - 피격 사망 : 맞는 순간 히트스톱(흰색 → 빨간색 번쩍임) → 흔들림, 색 빠짐, 확대 → 하얗게 변해 푸른 입자로 흩어짐
/// - 탐사 포기 : 발밑에 회수 마법진 → 빛기둥 → 발부터 빛 입자로 회수
/// - 임무 실패 : "임무 실패" 띠 → 바닥에 오염 줄기가 퍼지고 화면 가장자리가 잠식됨 → 발부터 보랏빛으로 물들고 위에서부터 부서져 떨어짐
/// 셋 다 문구를 보여준 뒤 화면이 검게 닫히고 결과창이 열린다. 결과창이 열리면 검은 화면을 걷는다.
///
/// 화면 연출(비네트, 잠식, 번쩍임, 띠, 문구, 검은 화면)은 사유별 애니메이션 클립(RunFailureUI 컨트롤러)이 맡는다.
/// 발밑 회수 마법진·빛기둥, 오염 줄기, 흩어지는 입자는 프리팹(08_VFX/Failure)과 클립으로 만들어 두고 위치·크기만 맞춰 띄운다.
/// 코드에는 캐릭터 분해(셰이더 값), 카메라, 장면 조명, 게임 시간 조절만 남아 있다.
/// 플레이어와 카메라 스크립트는 고치지 않는다. 연출용 머티리얼을 잠시 끼우고(PlayerFailureFx),
/// 카메라에는 실행 중에 FailureCameraFx를 붙인다. 장면의 전역 2D 조명은 어둡게 바꾼다.
/// 일시정지와 상관없이 실제 시간으로 진행하고, 진행에 맞춰 게임 시간(timeScale)을 늦추다가 멈춘다.
/// </summary>
[DisallowMultipleComponent]
public class RunFailureSequence : MonoBehaviour
{
    [Serializable]
    private class Caption
    {
        public string Title;
        public string Subtitle;
    }

    private const float DiedDuration = 4.3f;
    private const float AbandonedDuration = 4f;
    private const float MissionFailedDuration = 5.2f;

    [Header("대상")]
    [Tooltip("비워두면 장면에서 찾는다")]
    [SerializeField] private PlayerHealth _playerHealth;
    [Tooltip("RuneWeaver/FailureSprite")]
    [SerializeField] private Shader _shader;

    [Header("화면")]
    [Tooltip("화면 연출 애니메이터 (Hidden / Died / Abandoned / MissionFailed / FadeOut)")]
    [SerializeField] private Animator _animator;
    [SerializeField] private TMP_Text _bannerText;
    [SerializeField] private TMP_Text _captionTitle;
    [SerializeField] private TMP_Text _captionSubtitle;

    [Header("월드 연출 프리팹")]
    [Tooltip("탐사 포기 : 회수 마법진 + 빛기둥 (FailureRecallFx)")]
    [SerializeField] private GameObject _recallFxPrefab;
    [Tooltip("임무 실패 : 바닥 오염 줄기 (FailureVeinsFx)")]
    [SerializeField] private GameObject _veinsFxPrefab;
    [Tooltip("피격 사망 : 흩어지는 푸른 입자")]
    [SerializeField] private GameObject _diedMotesPrefab;
    [Tooltip("탐사 포기 : 위로 회수되는 빛 입자")]
    [SerializeField] private GameObject _abandonedMotesPrefab;
    [Tooltip("임무 실패 : 부서져 떨어지는 조각")]
    [SerializeField] private GameObject _missionFailedMotesPrefab;
    [Tooltip("바닥에 그리는 연출(회수 마법진, 오염 줄기)의 정렬 순서. 바닥 위, 캐릭터 아래")]
    [SerializeField] private int _groundSortingOrder = 1;

    [Header("문구")]
    [SerializeField] private Caption _diedCaption = new() { Title = "개체 반응 소실", Subtitle = "호문쿨루스의 반응이 끊어졌습니다." };
    [SerializeField] private Caption _abandonedCaption = new() { Title = "조사 중단 - 개체 회수", Subtitle = "호문쿨루스가 협회로 회수됩니다." };
    [SerializeField] private Caption _missionFailedCaption = new() { Title = "개체 잠식 - 오염 한계 초과", Subtitle = "호문쿨루스가 오염에 잠식되었습니다." };
    [SerializeField] private string _missionFailedBanner = "임무 실패";

    [Header("피격 사망")]
    [Tooltip("맞는 순간 게임이 멈추는 시간 (실제 시간)")]
    [SerializeField, Min(0f)] private float _hitStopDuration = 0.14f;
    [SerializeField] private Color _hitColor = new(0.9f, 0.12f, 0.08f, 1f);
    [SerializeField] private Color _diedGlowColor = new(0.82f, 0.9f, 1f, 1f);
    [SerializeField] private Color _diedSceneLight = new(0.62f, 0.68f, 0.85f, 1f);

    [Header("탐사 포기")]
    [SerializeField] private Color _recallColor = new(0.75f, 0.9f, 1f, 1f);
    [SerializeField] private Color _abandonedSceneLight = new(0.78f, 0.86f, 1f, 1f);

    [Header("임무 실패")]
    [SerializeField] private Color _stainColor = new(0.2f, 0.06f, 0.26f, 1f);
    [SerializeField] private Color _stainEdgeColor = new(0.75f, 0.3f, 0.95f, 1f);
    [SerializeField] private Color _missionSceneLight = new(0.62f, 0.42f, 0.78f, 1f);

    [Header("끝")]
    [Tooltip("결과창이 열린 뒤 검은 화면을 걷는 시간 (FadeOut 클립 길이와 맞춘다)")]
    [SerializeField, Min(0f)] private float _fadeOutAfterEnding = 0.6f;

    private readonly List<Light2D> _sceneLights = new();
    private readonly List<Color> _sceneLightColors = new();
    private readonly List<float> _sceneLightIntensities = new();

    private static readonly int HiddenHash = Animator.StringToHash("Hidden");
    private static readonly int DiedHash = Animator.StringToHash("Died");
    private static readonly int AbandonedHash = Animator.StringToHash("Abandoned");
    private static readonly int MissionFailedHash = Animator.StringToHash("MissionFailed");
    private static readonly int FadeOutHash = Animator.StringToHash("FadeOut");

    // 프리팹 기준 캐릭터 크기(유닛). 입자 프리팹은 이 크기로 구워져 있어 실제 크기에 맞춰 늘린다.
    private const float MoteReferenceWidth = 0.8f;
    private const float MoteReferenceHeight = 1.2f;

    private readonly List<Animator> _worldAnimators = new();

    private Coroutine _routine;
    private Action _onFinished;
    private PlayerFailureFx _player;
    private FailureCameraFx _cameraFx;
    private GameObject _worldRoot;
    private Color _baseTint = Color.white;
    private Color _sceneTint = Color.white;

    public bool IsPlaying => _routine != null;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        HideScreen();
    }

    private void Update()
    {
        // 일시정지 메뉴가 열려 있는 동안에는 화면·월드 연출도 멈춘다.
        float speed = PauseController.IsGamePaused ? 0f : 1f;
        if (_animator != null)
        {
            _animator.speed = speed;
        }

        foreach (Animator animator in _worldAnimators)
        {
            if (animator != null)
            {
                animator.speed = speed;
            }
        }
    }

    /// <summary>종료 사유에 맞는 연출을 재생하고, 화면이 검게 닫히면 onFinished를 부른다.</summary>
    public void Play(RunEndReason reason, Action onFinished)
    {
        if (_routine != null)
        {
            return;
        }

        _onFinished = onFinished;
        _routine = StartCoroutine(PlayRoutine(reason));
    }

    /// <summary>[테스트용] 연출을 멈추고 장면을 원래대로 되돌린다.</summary>
    public void ResetForTest()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        _onFinished = null;
        Cleanup();
        HideScreen();
        Time.timeScale = 1f;
    }

    private IEnumerator PlayRoutine(RunEndReason reason)
    {
        Prepare(reason);

        float duration = reason switch
        {
            RunEndReason.Abandoned => AbandonedDuration,
            RunEndReason.MissionFailed => MissionFailedDuration,
            _ => DiedDuration
        };

        float time = 0f;
        while (time < duration)
        {
            Evaluate(reason, time);
            yield return null;

            if (!PauseController.IsGamePaused)
            {
                time += Time.unscaledDeltaTime;
            }
        }

        Evaluate(reason, duration);

        Action callback = _onFinished;
        _onFinished = null;
        callback?.Invoke();

        // 결과창에는 자체 어두운 배경이 있으므로 검은 화면을 걷는다.
        PlayScreen(FadeOutHash);
        float elapsed = 0f;
        while (elapsed < _fadeOutAfterEnding)
        {
            elapsed += Time.unscaledDeltaTime;
            SetTimeScale(0f);
            yield return null;
        }

        _routine = null;
    }

    // ───────────────────────── 준비 / 정리 ─────────────────────────

    private void Prepare(RunEndReason reason)
    {
        if (_playerHealth == null)
        {
            _playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        if (_shader == null)
        {
            Debug.LogWarning("[RunFailureSequence] 셰이더가 할당되지 않아 캐릭터 효과 없이 진행합니다.", this);
        }

        CollectSceneLights();
        _sceneTint = Color.white;

        // 포탈 연출 중이면 숨겨진 플레이어 그림을 먼저 되돌린다. (숨긴 채로는 분해 연출이 걸리지 않는다)
        PortalPlayerHider.ReleaseAll();

        _worldRoot = new GameObject("RunFailureFx");
        if (_playerHealth != null && _shader != null)
        {
            _player = new PlayerFailureFx(_playerHealth.transform, _shader);
            _player.SetTint(_baseTint);
        }

        int overLayer = _player != null ? _player.SortingLayerId : 0;
        int overOrder = _player != null ? _player.SortingOrder + 5 : 32000;

        Camera camera = Camera.main;
        if (camera != null)
        {
            _cameraFx = camera.GetComponent<FailureCameraFx>();
            if (_cameraFx == null)
            {
                _cameraFx = camera.gameObject.AddComponent<FailureCameraFx>();
            }
        }

        if (_player != null)
        {
            Bounds bounds = _player.Bounds;
            Vector3 feet = new(bounds.center.x, bounds.min.y, 0f);
            float height = Mathf.Max(0.5f, bounds.size.y);

            if (reason == RunEndReason.Abandoned && _recallFxPrefab != null)
            {
                // 마법진은 바닥에, 빛기둥은 캐릭터 위에 그린다.
                GameObject recall = SpawnWorld(_recallFxPrefab, feet, new Vector3(height, height, 1f));
                SetSorting(recall.transform.Find("CircleHolder"), 0, _groundSortingOrder);
                SetSorting(recall.transform.Find("ColumnHolder"), overLayer, overOrder - 1);
            }
            else if (reason == RunEndReason.MissionFailed && _veinsFxPrefab != null)
            {
                GameObject veins = SpawnWorld(_veinsFxPrefab, feet, Vector3.one);
                veins.GetComponent<FailureVeinsFx>().Setup(feet, height);
                SetSorting(veins.transform, 0, _groundSortingOrder);
            }

            GameObject motesPrefab = reason switch
            {
                RunEndReason.Abandoned => _abandonedMotesPrefab,
                RunEndReason.MissionFailed => _missionFailedMotesPrefab,
                _ => _diedMotesPrefab
            };

            if (motesPrefab != null)
            {
                Vector3 scale = new(
                    Mathf.Max(0.1f, bounds.size.x) / MoteReferenceWidth,
                    Mathf.Max(0.1f, bounds.size.y) / MoteReferenceHeight,
                    1f);
                GameObject motes = SpawnWorld(motesPrefab, feet, scale);
                SetSorting(motes.transform, overLayer, overOrder);
            }
        }

        ShowScreen(reason);
    }

    /// <summary>연출 프리팹을 월드 루트 아래에 만든다. (정리할 때 한꺼번에 없어진다)</summary>
    private GameObject SpawnWorld(GameObject prefab, Vector3 position, Vector3 scale)
    {
        GameObject instance = Instantiate(prefab, position, Quaternion.identity, _worldRoot.transform);
        instance.transform.localScale = scale;
        _worldAnimators.AddRange(instance.GetComponentsInChildren<Animator>(true));
        return instance;
    }

    private static void SetSorting(Transform root, int sortingLayerId, int sortingOrder)
    {
        if (root == null)
        {
            return;
        }

        foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder;
        }
    }

    private void CollectSceneLights()
    {
        _sceneLights.Clear();
        _sceneLightColors.Clear();
        _sceneLightIntensities.Clear();
        _baseTint = Color.white;

        foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (light.lightType != Light2D.LightType.Global)
            {
                continue;
            }

            _sceneLights.Add(light);
            _sceneLightColors.Add(light.color);
            _sceneLightIntensities.Add(light.intensity);
        }

        // 연출 머티리얼은 조명을 받지 않으므로, 지금 장면 밝기와 비슷한 색으로 시작한다.
        if (_sceneLights.Count > 0)
        {
            Color lit = _sceneLightColors[0] * _sceneLightIntensities[0];
            _baseTint = new Color(Mathf.Clamp01(lit.r + 0.1f), Mathf.Clamp01(lit.g + 0.1f), Mathf.Clamp01(lit.b + 0.1f), 1f);
        }
    }

    private void Cleanup()
    {
        _player?.Restore();
        _player = null;

        for (int i = 0; i < _sceneLights.Count; i++)
        {
            if (_sceneLights[i] != null)
            {
                _sceneLights[i].color = _sceneLightColors[i];
                _sceneLights[i].intensity = _sceneLightIntensities[i];
            }
        }

        _sceneLights.Clear();

        if (_cameraFx != null)
        {
            _cameraFx.Remove();
            _cameraFx = null;
        }

        if (_worldRoot != null)
        {
            Destroy(_worldRoot);
            _worldRoot = null;
        }

        _worldAnimators.Clear();
    }

    // ───────────────────────── 화면 ─────────────────────────

    private void ShowScreen(RunEndReason reason)
    {
        PlayScreen(reason switch
        {
            RunEndReason.Abandoned => AbandonedHash,
            RunEndReason.MissionFailed => MissionFailedHash,
            _ => DiedHash
        });

        if (_bannerText != null)
        {
            _bannerText.text = _missionFailedBanner;
        }

        Caption caption = reason switch
        {
            RunEndReason.Abandoned => _abandonedCaption,
            RunEndReason.MissionFailed => _missionFailedCaption,
            _ => _diedCaption
        };

        if (_captionTitle != null)
        {
            _captionTitle.text = caption.Title;
        }

        if (_captionSubtitle != null)
        {
            _captionSubtitle.text = caption.Subtitle;
        }
    }

    private void HideScreen()
    {
        PlayScreen(HiddenHash);
    }

    private void PlayScreen(int stateHash)
    {
        if (_animator == null)
        {
            return;
        }

        _animator.Play(stateHash, 0, 0f);
        _animator.Update(0f);
    }

    // ───────────────────────── 진행 ─────────────────────────

    private void Evaluate(RunEndReason reason, float time)
    {
        switch (reason)
        {
            case RunEndReason.Abandoned:
                EvaluateAbandoned(time);
                break;
            case RunEndReason.MissionFailed:
                EvaluateMissionFailed(time);
                break;
            default:
                EvaluateDied(time);
                break;
        }
    }

    /// <summary>피격 사망</summary>
    private void EvaluateDied(float time)
    {
        float hitStop = _hitStopDuration;
        float black = Segment(time, 3.5f, 0.7f);
        bool isFrozen = time < hitStop || black >= 1f;
        SetTimeScale(isFrozen ? 0f : Mathf.Lerp(0.3f, 0.05f, Segment(time, hitStop, 1.4f)));

        float drain = Segment(time, hitStop, 1f);
        SetSceneLight(_diedSceneLight, 0.55f, drain);

        float dissolve = Segment(time, 1.3f, 1.3f);
        if (_player != null)
        {
            if (time < hitStop * 0.5f)
            {
                _player.SetFlash(Color.white, 1f);
            }
            else if (time < hitStop)
            {
                _player.SetFlash(_hitColor, 1f);
            }
            else
            {
                float red = 1f - Segment(time, hitStop, 0.25f);
                float glow = Segment(time, 0.9f, 0.5f) * 0.85f;
                _player.SetFlash(glow > red ? _diedGlowColor : _hitColor, Mathf.Max(glow, red));
            }

            _player.SetTint(_baseTint * _sceneTint);
            _player.SetDesaturate(Segment(time, hitStop, 0.7f));
            _player.SetDissolve(dissolve, false, 0.75f, _diedGlowColor, 0.08f);
            _player.SetLightFactor(1f - Segment(time, 1.2f, 1f));
        }

        if (_cameraFx != null)
        {
            _cameraFx.Shake = time < hitStop ? 0f : 0.22f * (1f - Segment(time, hitStop, 0.45f));
            _cameraFx.Zoom = 1f - 0.16f * EaseOut(Segment(time, hitStop, 1.8f));
        }
    }

    /// <summary>탐사 포기</summary>
    private void EvaluateAbandoned(float time)
    {
        float black = Segment(time, 3.2f, 0.7f);
        SetTimeScale(black >= 1f ? 0f : Mathf.Lerp(1f, 0.15f, Segment(time, 0f, 0.4f)));
        SetSceneLight(_abandonedSceneLight, 0.75f, Segment(time, 0f, 0.8f));

        float dissolve = Segment(time, 0.9f, 1.3f);
        if (_player != null)
        {
            _player.SetTint(_baseTint * _sceneTint);
            _player.SetFlash(_recallColor, 0.6f * Segment(time, 0.6f, 0.4f));
            _player.SetDissolve(dissolve, false, 0.35f, new Color(0.85f, 0.95f, 1f, 1f), 0.1f);
            _player.SetLightFactor(1f - Segment(time, 0.9f, 1f));
        }

        if (_cameraFx != null)
        {
            _cameraFx.Zoom = 1f - 0.06f * EaseOut(Segment(time, 0.2f, 2.5f));
        }
    }

    /// <summary>임무 실패 (오염에 잠식)</summary>
    private void EvaluateMissionFailed(float time)
    {
        float black = Segment(time, 4.4f, 0.7f);
        float timeScale = time < 2.4f
            ? Mathf.Lerp(1f, 0.3f, Segment(time, 0f, 0.5f))
            : Mathf.Lerp(0.3f, 0.06f, Segment(time, 2.4f, 1f));
        SetTimeScale(black >= 1f ? 0f : timeScale);

        SetSceneLight(_missionSceneLight, 0.5f, Segment(time, 0.3f, 2f));

        float dissolve = Segment(time, 2.6f, 1.4f);
        if (_player != null)
        {
            _player.SetTint(_baseTint * _sceneTint);
            _player.SetFlash(Color.white, 0f);
            _player.SetStain(Smooth(Segment(time, 0.8f, 1.7f)), _stainColor, _stainEdgeColor);
            _player.SetDissolve(dissolve, true, 0.4f, new Color(0.55f, 0.22f, 0.7f, 1f), 0.08f);
            _player.SetLightFactor(1f - Segment(time, 0.8f, 1.5f));
        }

        if (_cameraFx != null)
        {
            _cameraFx.Zoom = 1f - 0.08f * Smooth(Segment(time, 0.3f, 3f));
            _cameraFx.Shake = dissolve > 0f && dissolve < 1f ? 0.04f : 0f;
        }
    }

    // ───────────────────────── 도움 ─────────────────────────

    /// <summary>장면 전역 조명을 color 쪽으로, 밝기를 intensityScale배로 바꾼다. (amount 0 → 1)</summary>
    private void SetSceneLight(Color color, float intensityScale, float amount)
    {
        for (int i = 0; i < _sceneLights.Count; i++)
        {
            if (_sceneLights[i] == null)
            {
                continue;
            }

            _sceneLights[i].color = Color.Lerp(_sceneLightColors[i], color, amount);
            _sceneLights[i].intensity = _sceneLightIntensities[i] * Mathf.Lerp(1f, intensityScale, amount);
        }

        Color target = color * intensityScale;
        target.a = 1f;
        _sceneTint = Color.Lerp(Color.white, target, amount);
    }

    private static void SetTimeScale(float value)
    {
        if (!PauseController.IsGamePaused)
        {
            Time.timeScale = value;
        }
    }

    private static float Segment(float time, float start, float duration)
    {
        return duration <= 0f ? (time >= start ? 1f : 0f) : Mathf.Clamp01((time - start) / duration);
    }

    private static float EaseOut(float t)
    {
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    private static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
