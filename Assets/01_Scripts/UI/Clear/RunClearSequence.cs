using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 보스 처치 연출. 연출이 끝나면 넘겨받은 콜백으로 결과창(EndingUI)을 연다. MissionResultPresenter가 부른다.
/// 1. 히트스톱 : 마지막 공격에 맞는 순간 게임이 멈추고 보스가 하얗게 번쩍인다.
/// 2. 고동 : 느려진 시간 속에서 보스 가슴의 결정이 점점 빨라지는 세 번의 박동으로 고동친다.
///    빛이 결정에서 몸 바깥쪽으로 차오르고, 박동마다 겉돌 조각이 떨어져 나간다. 장면 조명은 어두워진다.
/// 3. 정화 : 하얀 섬광과 함께 보스가 빛 입자로 흩어지고, 정화 파동(PurificationWave)이 퍼지며 남은 적과 탄환을 정화한다.
///    장면 조명이 밝게 돌아오고 빛 입자가 떠오르며 문구가 나온다.
/// 4. 화면이 하얗게 닫히고 결과창이 열린다. 결과창이 열리면 하얀 화면을 걷는다.
///
/// 화면 연출(비네트, 섬광, 문구, 하얀 화면)은 애니메이션 클립(RunClearUI 컨트롤러의 Boss / NoBoss)이 맡는다.
/// 결정의 빛·주변 불꽃·겉돌 조각(ClearCoreFx), 흩어지는 빛 입자(ClearBurstMotes), 떠오르는 빛(ClearAmbientMotes)은
/// 프리팹(08_VFX/Clear)과 클립으로 만들어 두고 위치·크기만 맞춰 띄운다. 클립은 히트스톱 0.22초 기준으로 맞춰져 있다.
/// 코드에는 보스 복제와 셰이더 값, 카메라, 장면 조명, 게임 시간 조절만 남아 있다.
/// 보스 프리팹을 고치지 않는다. 처치된 보스(StageManager.BossHealth)의 스프라이트를 그 자리에서 복제해 연출에 쓰고,
/// 원래 보스는 숨긴다. (보스는 처치된 프레임에 사라진다) 보스를 찾지 못하면 플레이어 위치에서 정화만 보여준다.
/// 연출 동안 플레이어 조작 컴포넌트를 끄고, 일시정지와 상관없이 실제 시간으로 진행한다.
/// </summary>
[DisallowMultipleComponent]
public class RunClearSequence : MonoBehaviour
{
    private const float BreakDuration = 1.5f;
    private const float AftermathDuration = 2.7f;
    private const float WhiteDuration = 0.6f;
    // 떠오르는 빛(ClearAmbientMotes)이 기준으로 삼는 카메라 크기 (orthographicSize)
    private const float AmbientReferenceSize = 5f;

    private static readonly int HiddenHash = Animator.StringToHash("Hidden");
    private static readonly int BossHash = Animator.StringToHash("Boss");
    private static readonly int NoBossHash = Animator.StringToHash("NoBoss");
    private static readonly int FadeOutHash = Animator.StringToHash("FadeOut");

    private static readonly float[] BeatTimes = { 0.2f, 0.75f, 1.12f };
    private static readonly float[] BeatStrengths = { 0.5f, 0.75f, 1f };

    [Header("대상")]
    [Tooltip("비워두면 장면에서 찾는다")]
    [SerializeField] private StageManager _stageManager;
    [Tooltip("비워두면 장면에서 찾는다")]
    [SerializeField] private PlayerHealth _playerHealth;
    [Tooltip("연출 동안 끌 플레이어 조작 컴포넌트 (룬북 일시정지와 같은 목록)")]
    [SerializeField] private Behaviour[] _behavioursToDisable;
    [Tooltip("RuneWeaver/FailureSprite")]
    [SerializeField] private Shader _shader;
    [Tooltip("보스가 흩어질 때 퍼지는 정화 파동 (미션 클리어와 같은 프리팹)")]
    [SerializeField] private PurificationWave _wavePrefab;
    [Tooltip("보스 몸에서 빛이 터져 나오는 결정의 높이. 발 0, 머리 1")]
    [SerializeField, Range(0f, 1f)] private float _coreHeight = 0.55f;

    [Header("화면")]
    [Tooltip("화면 연출 애니메이터 (Hidden / Boss / NoBoss / FadeOut)")]
    [SerializeField] private Animator _animator;
    [SerializeField] private TMP_Text _captionTitle;
    [SerializeField] private TMP_Text _captionSubtitle;

    [Header("월드 연출 프리팹")]
    [Tooltip("보스 가슴의 결정 빛, 주변 불꽃, 박동마다 떨어지는 조각 (보스 키에 맞춰 커진다)")]
    [SerializeField] private GameObject _coreFxPrefab;
    [Tooltip("정화되는 순간 보스 몸에서 흩어지는 빛 입자")]
    [SerializeField] private GameObject _burstMotesPrefab;
    [Tooltip("정화 뒤 화면 아래쪽에서 떠오르는 빛")]
    [SerializeField] private GameObject _ambientMotesPrefab;

    [Header("문구")]
    [SerializeField] private string _title = "오염의 근원 정화";
    [SerializeField] private string _subtitle = "최심부의 수호자가 소멸했습니다.";

    [Header("색")]
    [SerializeField] private Color _lightColor = new(0.85f, 0.93f, 1f, 1f);
    [Tooltip("고동치는 동안 장면 조명")]
    [SerializeField] private Color _corruptedSceneLight = new(0.62f, 0.5f, 0.78f, 1f);
    [Tooltip("정화 직후 잠깐 밝아지는 장면 조명")]
    [SerializeField] private Color _purifiedSceneLight = new(1f, 0.97f, 0.9f, 1f);

    [Header("히트스톱")]
    [Tooltip("마지막 공격에 맞는 순간 게임이 멈추는 시간 (실제 시간). 바꾸면 Boss 클립과 ClearCore 클립의 시간도 맞춰야 한다.")]
    [SerializeField, Min(0f)] private float _hitStopDuration = 0.22f;

    [Header("끝")]
    [Tooltip("결과창이 열린 뒤 하얀 화면을 걷는 시간")]
    [SerializeField, Min(0f)] private float _fadeOutAfterEnding = 0.6f;

    private readonly List<Light2D> _sceneLights = new();
    private readonly List<Color> _sceneLightColors = new();
    private readonly List<float> _sceneLightIntensities = new();
    private readonly List<Behaviour> _disabledBehaviours = new();
    private readonly List<Animator> _worldAnimators = new();

    private Coroutine _routine;
    private Action _onFinished;
    private GameObject _worldRoot;
    private Transform _ghost;
    private PlayerFailureFx _ghostFx;
    private FailureCameraFx _cameraFx;
    private Rigidbody2D _playerBody;
    private Vector3 _ghostBasePosition;
    private Vector2 _core;
    private Vector3 _feet;
    private float _height = 2f;
    private bool _hasBoss;
    private bool _hasBurst;
    private float _burstTime;
    private float _whiteTime;
    private Color _baseTint = Color.white;
    private int _sortingLayer;
    private int _overOrder;

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

    /// <summary>보스 처치 연출을 재생하고, 화면이 하얗게 닫히면 onFinished를 부른다.</summary>
    public void Play(Action onFinished)
    {
        if (_routine != null)
        {
            return;
        }

        _onFinished = onFinished;
        _routine = StartCoroutine(PlayRoutine());
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

    private IEnumerator PlayRoutine()
    {
        Prepare();

        float duration = _whiteTime + WhiteDuration;
        float time = 0f;
        while (time < duration)
        {
            Evaluate(time);
            yield return null;

            if (!PauseController.IsGamePaused)
            {
                time += Time.unscaledDeltaTime;
            }
        }

        Evaluate(duration);

        Action callback = _onFinished;
        _onFinished = null;
        callback?.Invoke();

        // 결과창이 열리면 하얀 화면을 걷는다.
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

    private void Prepare()
    {
        if (_stageManager == null)
        {
            _stageManager = FindAnyObjectByType<StageManager>();
        }

        if (_playerHealth == null)
        {
            _playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        _playerBody = _playerHealth != null ? _playerHealth.GetComponent<Rigidbody2D>() : null;
        DisablePlayerControl();
        CollectSceneLights();

        _worldRoot = new GameObject("RunClearFx");

        EnemyHealth boss = _stageManager != null ? _stageManager.BossHealth : null;
        _hasBoss = boss != null && _shader != null && CreateGhost(boss);

        int sortingLayer = 0;
        int overOrder = 32000;
        if (_hasBoss)
        {
            Bounds bounds = _ghostFx.Bounds;
            _height = Mathf.Max(0.5f, bounds.size.y);
            _feet = new Vector3(bounds.center.x, bounds.min.y, 0f);
            _core = new Vector2(bounds.center.x, bounds.min.y + _height * _coreHeight);
            sortingLayer = _ghostFx.SortingLayerId;
            overOrder = _ghostFx.SortingOrder + 10;
            _ghostFx.SetTint(_baseTint);

            if (_coreFxPrefab != null)
            {
                GameObject coreFx = SpawnWorld(_coreFxPrefab, new Vector3(_core.x, _core.y, 0f), new Vector3(_height, _height, 1f));
                SetSorting(coreFx.transform, sortingLayer, overOrder);
                Transform glow = coreFx.transform.Find("CoreGlow");
                if (glow != null)
                {
                    SetSorting(glow, sortingLayer, overOrder - 1);
                }
            }
        }
        else
        {
            Vector3 origin = _playerHealth != null ? _playerHealth.transform.position : Vector3.zero;
            _feet = new Vector3(origin.x, origin.y, 0f);
            _core = _feet;
        }

        _sortingLayer = sortingLayer;
        _overOrder = overOrder;

        Camera camera = Camera.main;
        if (camera != null)
        {
            _cameraFx = camera.GetComponent<FailureCameraFx>();
            if (_cameraFx == null)
            {
                _cameraFx = camera.gameObject.AddComponent<FailureCameraFx>();
            }
        }

        float hitStop = _hasBoss ? _hitStopDuration : 0f;
        _burstTime = _hasBoss ? hitStop + BreakDuration : 0.15f;
        _whiteTime = _burstTime + AftermathDuration;
        _hasBurst = false;

        ShowScreen();
    }

    /// <summary>보스의 스프라이트를 복제한다. 원래 보스는 숨긴다. 복제할 스프라이트가 없으면 false</summary>
    private bool CreateGhost(EnemyHealth boss)
    {
        _ghost = new GameObject("BossGhost").transform;
        _ghost.SetParent(_worldRoot.transform, false);
        _ghost.position = boss.transform.position;

        SortingGroup sourceGroup = boss.GetComponentInParent<SortingGroup>();
        if (sourceGroup == null)
        {
            sourceGroup = boss.GetComponentInChildren<SortingGroup>();
        }

        if (sourceGroup != null)
        {
            SortingGroup group = _ghost.gameObject.AddComponent<SortingGroup>();
            group.sortingLayerID = sourceGroup.sortingLayerID;
            group.sortingOrder = sourceGroup.sortingOrder;
        }

        int count = 0;
        foreach (SpriteRenderer source in boss.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!source.enabled || !source.gameObject.activeInHierarchy || source.sprite == null)
            {
                continue;
            }

            GameObject copy = new(source.name);
            copy.transform.SetParent(_ghost, false);
            copy.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            copy.transform.localScale = source.transform.lossyScale;

            SpriteRenderer renderer = copy.AddComponent<SpriteRenderer>();
            renderer.sprite = source.sprite;
            renderer.color = source.color;
            renderer.flipX = source.flipX;
            renderer.flipY = source.flipY;
            renderer.drawMode = source.drawMode;
            renderer.size = source.size;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder;
            renderer.sharedMaterial = source.sharedMaterial;

            source.enabled = false;
            count++;
        }

        if (count == 0)
        {
            Destroy(_ghost.gameObject);
            _ghost = null;
            return false;
        }

        _ghostBasePosition = _ghost.position;
        _ghostFx = new PlayerFailureFx(_ghost, _shader);
        return true;
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
        foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder;
        }
    }

    private void DisablePlayerControl()
    {
        _disabledBehaviours.Clear();
        if (_behavioursToDisable == null)
        {
            return;
        }

        foreach (Behaviour behaviour in _behavioursToDisable)
        {
            if (behaviour != null && behaviour.enabled)
            {
                behaviour.enabled = false;
                _disabledBehaviours.Add(behaviour);
            }
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

        // 복제한 보스는 조명을 받지 않으므로, 지금 장면 밝기와 비슷한 색으로 시작한다.
        if (_sceneLights.Count > 0)
        {
            Color lit = _sceneLightColors[0] * _sceneLightIntensities[0];
            _baseTint = new Color(Mathf.Clamp01(lit.r + 0.1f), Mathf.Clamp01(lit.g + 0.1f), Mathf.Clamp01(lit.b + 0.1f), 1f);
        }
    }

    private void Cleanup()
    {
        _ghostFx?.Restore();
        _ghostFx = null;

        for (int i = 0; i < _sceneLights.Count; i++)
        {
            if (_sceneLights[i] != null)
            {
                _sceneLights[i].color = _sceneLightColors[i];
                _sceneLights[i].intensity = _sceneLightIntensities[i];
            }
        }

        _sceneLights.Clear();

        foreach (Behaviour behaviour in _disabledBehaviours)
        {
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        _disabledBehaviours.Clear();

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

        _ghost = null;
        _worldAnimators.Clear();
    }

    // ───────────────────────── 화면 ─────────────────────────

    private void ShowScreen()
    {
        PlayScreen(_hasBoss ? BossHash : NoBossHash);

        if (_captionTitle != null)
        {
            _captionTitle.text = _title;
        }

        if (_captionSubtitle != null)
        {
            _captionSubtitle.text = _subtitle;
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

    private void Evaluate(float time)
    {
        float hitStop = _hasBoss ? _hitStopDuration : 0f;
        bool isFrozen = time < hitStop;
        float breakTime = time - hitStop;
        float afterBurst = time - _burstTime;
        float white = Smooth(Segment(time, _whiteTime, WhiteDuration));

        // 게임 시간 : 멈춤 → 느리게 → (정화 파동이 퍼지도록) 정상 → 하얗게 닫히며 멈춤
        if (isFrozen)
        {
            SetTimeScale(0f);
        }
        else if (time < _burstTime)
        {
            SetTimeScale(Mathf.Lerp(0.25f, 0.1f, Segment(breakTime, 0f, BreakDuration)));
        }
        else
        {
            SetTimeScale(Mathf.Lerp(1f, 0f, white));
        }

        // 조작을 꺼도 남은 속도로 미끄러지지 않게 한다.
        if (_playerBody != null)
        {
            _playerBody.linearVelocity = Vector2.zero;
        }

        if (time < _burstTime)
        {
            EvaluateBreak(time, isFrozen, breakTime);
        }
        else
        {
            if (!_hasBurst)
            {
                Burst();
            }

            EvaluateAftermath(afterBurst);
        }
    }

    /// <summary>히트스톱과 고동 (정화 전)</summary>
    private void EvaluateBreak(float time, bool isFrozen, float breakTime)
    {
        float progress = breakTime > 0f ? EaseOut(breakTime / BreakDuration) : 0f;
        float beat = GetBeat(breakTime);
        float corruption = Segment(breakTime, 0f, BreakDuration);

        SetSceneLight(corruption, 0f);

        if (_cameraFx != null)
        {
            _cameraFx.Zoom = isFrozen ? 0.98f : 1f - 0.07f * progress;
            _cameraFx.Shake = isFrozen ? 0f : (0.02f + 0.04f * corruption) * Mathf.Max(1f, _height * 0.5f);
        }

        if (!_hasBoss)
        {
            return;
        }

        _ghostFx.SetTint(_baseTint * GetSceneTint(corruption, 0f));
        if (isFrozen)
        {
            _ghostFx.SetFlash(Color.white, 1f);
            _ghostFx.SetInnerGlow(_core, 0f, 1f, _lightColor, 0f);
        }
        else
        {
            _ghostFx.SetFlash(_lightColor, 0.04f + 0.22f * beat);
            float reach = (0.06f + 0.85f * EaseIn(breakTime / BreakDuration) + 0.1f * beat) * _height;
            _ghostFx.SetInnerGlow(_core, reach, _height * 0.2f, _lightColor, Mathf.Min(0.72f, 0.35f + 0.3f * progress));

            float jitter = (0.006f + 0.012f * progress) * _height * (Mathf.Sin(time * 67f) + 0.5f * Mathf.Sin(time * 113f));
            _ghost.position = _ghostBasePosition + new Vector3(jitter, 0f, 0f);
        }
    }

    /// <summary>정화 : 보스가 흩어지고 정화 파동이 퍼진다.</summary>
    private void Burst()
    {
        _hasBurst = true;

        if (_ghost != null)
        {
            _ghost.gameObject.SetActive(false);
        }

        if (_hasBoss && _burstMotesPrefab != null)
        {
            Bounds bounds = _ghostFx.Bounds;
            GameObject burst = SpawnWorld(_burstMotesPrefab, new Vector3(bounds.center.x, bounds.min.y, 0f),
                new Vector3(Mathf.Max(0.1f, bounds.size.x), _height, 1f));
            SetSorting(burst.transform, _sortingLayer, _overOrder);
        }

        // 화면 아래쪽에서 빛이 떠오른다. (카메라 크기에 맞춘다)
        Camera camera = Camera.main;
        if (_ambientMotesPrefab != null && camera != null && camera.orthographic)
        {
            Vector3 center = camera.transform.position;
            float scale = camera.orthographicSize / AmbientReferenceSize;
            GameObject ambient = SpawnWorld(_ambientMotesPrefab, new Vector3(center.x, center.y, 0f), new Vector3(scale, scale, 1f));
            SetSorting(ambient.transform, _sortingLayer, _overOrder);
        }

        if (_wavePrefab != null)
        {
            PurificationWave wave = Instantiate(_wavePrefab, _feet, Quaternion.identity);
            wave.PurifyEnemies = true;
            wave.PlayDetached();
        }
    }

    /// <summary>정화 뒤 : 섬광이 걷히고 장면이 밝게 돌아오며 빛이 떠오른다.</summary>
    private void EvaluateAftermath(float afterBurst)
    {
        float corruption = 1f - Segment(afterBurst, 0f, 0.3f);
        float purified = Segment(afterBurst, 0f, 0.3f) * (1f - Segment(afterBurst, 0.3f, 2f));
        SetSceneLight(_hasBoss ? corruption : 0f, purified);

        if (_cameraFx != null)
        {
            _cameraFx.Zoom = 1f - 0.07f * (1f - EaseOut(afterBurst / 0.8f)) * (_hasBoss ? 1f : 0f);
            _cameraFx.Shake = 0.25f * Mathf.Exp(-afterBurst * 5f);
        }
    }

    // ───────────────────────── 도움 ─────────────────────────

    /// <summary>점점 빨라지는 세 번의 박동 세기 (0 → 1)</summary>
    private static float GetBeat(float breakTime)
    {
        float beat = 0f;
        for (int i = 0; i < BeatTimes.Length; i++)
        {
            if (breakTime >= BeatTimes[i])
            {
                beat = Mathf.Max(beat, BeatStrengths[i] * Mathf.Exp(-(breakTime - BeatTimes[i]) * 7f));
            }
        }

        return beat;
    }

    /// <summary>
    /// 장면 전역 조명. corruption만큼 어둡고 탁해지고, purified만큼 밝고 따뜻해진다. (둘 다 0이면 원래대로)
    /// </summary>
    private void SetSceneLight(float corruption, float purified)
    {
        for (int i = 0; i < _sceneLights.Count; i++)
        {
            if (_sceneLights[i] == null)
            {
                continue;
            }

            Color color = Color.Lerp(_sceneLightColors[i], _corruptedSceneLight, corruption);
            color = Color.Lerp(color, _purifiedSceneLight, purified * 0.5f);
            _sceneLights[i].color = color;
            _sceneLights[i].intensity = _sceneLightIntensities[i] * Mathf.Lerp(1f, 0.65f, corruption) * (1f + 0.2f * purified);
        }
    }

    private Color GetSceneTint(float corruption, float purified)
    {
        Color tint = Color.Lerp(Color.white, _corruptedSceneLight * 0.65f, corruption);
        tint = Color.Lerp(tint, _purifiedSceneLight * 1.2f, purified * 0.5f);
        tint.a = 1f;
        return tint;
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
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    private static float EaseIn(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t;
    }

    private static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
