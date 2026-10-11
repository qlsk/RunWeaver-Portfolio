using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// 런 실패 연출. 연출이 끝나면 넘겨받은 콜백으로 결과창(EndingUI)을 연다. MissionResultPresenter가 부른다.
/// - 피격 사망 : 맞는 순간 히트스톱(흰색 → 빨간색 번쩍임) → 흔들림, 색 빠짐, 확대 → 하얗게 변해 푸른 입자로 흩어짐
/// - 탐사 포기 : 발밑에 회수 마법진 → 빛기둥 → 발부터 빛 입자로 회수
/// - 임무 실패 : "임무 실패" 띠 → 바닥에 오염 줄기가 퍼지고 화면 가장자리가 잠식됨 → 발부터 보랏빛으로 물들고 위에서부터 부서져 떨어짐
/// 셋 다 문구를 보여준 뒤 화면이 검게 닫히고 결과창이 열린다. 결과창이 열리면 검은 화면을 걷는다.
///
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
    [SerializeField] private Image _vignette;
    [SerializeField] private Image _miasma;
    [SerializeField] private Image _flash;
    [SerializeField] private Image _fade;
    [SerializeField] private CanvasGroup _banner;
    [SerializeField] private TMP_Text _bannerText;
    [SerializeField] private CanvasGroup _caption;
    [SerializeField] private TMP_Text _captionTitle;
    [SerializeField] private TMP_Text _captionSubtitle;

    [Header("월드 스프라이트")]
    [SerializeField] private Sprite _moteSprite;
    [SerializeField] private Sprite _recallCircleSprite;
    [SerializeField] private Sprite _lightColumnSprite;
    [SerializeField] private Sprite _veinsSprite;
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
    [Tooltip("결과창이 열린 뒤 검은 화면을 걷는 시간")]
    [SerializeField, Min(0f)] private float _fadeOutAfterEnding = 0.6f;

    private readonly List<Light2D> _sceneLights = new();
    private readonly List<Color> _sceneLightColors = new();
    private readonly List<float> _sceneLightIntensities = new();

    private Coroutine _routine;
    private Action _onFinished;
    private PlayerFailureFx _player;
    private FailureCameraFx _cameraFx;
    private FailureMotes _motes;
    private GameObject _worldRoot;
    private Material _alphaMaterial;
    private Material _additiveMaterial;
    private Material _veinsMaterial;
    private Color _baseTint = Color.white;
    private Color _sceneTint = Color.white;
    private float _lastTime;
    private float _emitCarry;
    private Vector3 _bannerScale = Vector3.one;

    private Transform _circleHolder;
    private SpriteRenderer _circle;
    private Transform _columnHolder;
    private SpriteRenderer _column;
    private Transform _veinsHolder;
    private float _veinsRadius;

    public bool IsPlaying => _routine != null;

    private void Awake()
    {
        if (_banner != null)
        {
            _bannerScale = _banner.transform.localScale;
        }

        HideScreen();
    }

    private void OnDestroy()
    {
        DestroyMaterials();
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
        _lastTime = 0f;
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
        SetGroupAlpha(_caption, 0f);
        float elapsed = 0f;
        while (elapsed < _fadeOutAfterEnding)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(_fade, 1f - elapsed / _fadeOutAfterEnding);
            SetTimeScale(0f);
            yield return null;
        }

        SetAlpha(_fade, 0f);
        _routine = null;
    }

    // ───────────────────────── 준비 / 정리 ─────────────────────────

    private void Prepare(RunEndReason reason)
    {
        if (_playerHealth == null)
        {
            _playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        _alphaMaterial = FailureSpriteMaterial.Create(_shader, false, "FailureFx Alpha (Instance)");
        _additiveMaterial = FailureSpriteMaterial.Create(_shader, true, "FailureFx Additive (Instance)");
        if (_shader == null)
        {
            Debug.LogWarning("[RunFailureSequence] 셰이더가 할당되지 않아 캐릭터 효과 없이 진행합니다.", this);
        }

        CollectSceneLights();
        _sceneTint = Color.white;

        _worldRoot = new GameObject("RunFailureFx");
        if (_playerHealth != null && _shader != null)
        {
            _player = new PlayerFailureFx(_playerHealth.transform, _shader);
            _player.SetTint(_baseTint);
        }

        int overLayer = _player != null ? _player.SortingLayerId : 0;
        int overOrder = _player != null ? _player.SortingOrder + 5 : 32000;
        _motes = _worldRoot.AddComponent<FailureMotes>();
        _motes.Setup(_moteSprite, _alphaMaterial, _additiveMaterial, overLayer, overOrder);

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

            if (reason == RunEndReason.Abandoned)
            {
                _circle = CreateWorldSprite("RecallCircle", _recallCircleSprite, _additiveMaterial, feet, height * 1.4f, 0.42f, 0, _groundSortingOrder, out _circleHolder);
                _column = CreateWorldSprite("LightColumn", _lightColumnSprite, _additiveMaterial, feet, height * 0.9f, 1f, overLayer, overOrder - 1, out _columnHolder);
            }
            else if (reason == RunEndReason.MissionFailed && _shader != null)
            {
                _veinsMaterial = FailureSpriteMaterial.Create(_shader, false, "FailureFx Veins (Instance)");
                _veinsRadius = height * 2.6f;
                const float squash = 0.5f;
                CreateWorldSprite("Veins", _veinsSprite, _veinsMaterial, feet, _veinsRadius * 2f, squash, 0, _groundSortingOrder, out _veinsHolder);
                _veinsMaterial.SetVector(FailureSpriteMaterial.RevealCenter, new Vector4(feet.x, feet.y, 1f, 1f / squash));
                _veinsMaterial.SetFloat(FailureSpriteMaterial.RevealRadius, 0f);
                _veinsMaterial.SetFloat(FailureSpriteMaterial.RevealSoftness, height * 0.35f);
            }
        }

        ShowScreen(reason);
    }

    private SpriteRenderer CreateWorldSprite(string name, Sprite sprite, Material material, Vector3 position, float worldWidth, float squashY, int sortingLayerId, int sortingOrder, out Transform holder)
    {
        holder = null;
        if (sprite == null || material == null)
        {
            return null;
        }

        holder = new GameObject(name).transform;
        holder.SetParent(_worldRoot.transform, false);
        holder.position = position;
        holder.localScale = new Vector3(1f, squashY, 1f);

        GameObject go = new("Sprite");
        go.transform.SetParent(holder, false);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.sortingLayerID = sortingLayerId;
        renderer.sortingOrder = sortingOrder;
        renderer.color = new Color(1f, 1f, 1f, 0f);
        go.transform.localScale = Vector3.one * (worldWidth / Mathf.Max(0.0001f, sprite.bounds.size.x));
        return renderer;
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

        _motes = null;
        _circle = null;
        _column = null;
        _circleHolder = null;
        _columnHolder = null;
        _veinsHolder = null;
        DestroyMaterials();
    }

    private void DestroyMaterials()
    {
        DestroyMaterial(ref _alphaMaterial);
        DestroyMaterial(ref _additiveMaterial);
        DestroyMaterial(ref _veinsMaterial);
    }

    private static void DestroyMaterial(ref Material material)
    {
        if (material != null)
        {
            Destroy(material);
            material = null;
        }
    }

    // ───────────────────────── 화면 ─────────────────────────

    private void ShowScreen(RunEndReason reason)
    {
        SetActive(_vignette, true);
        SetActive(_miasma, reason == RunEndReason.MissionFailed);
        SetActive(_flash, true);
        SetActive(_fade, true);
        SetAlpha(_vignette, 0f);
        SetAlpha(_miasma, 0f);
        SetAlpha(_flash, 0f);
        SetAlpha(_fade, 0f);

        if (_banner != null)
        {
            _banner.gameObject.SetActive(reason == RunEndReason.MissionFailed);
            _banner.alpha = 0f;
        }

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

        if (_caption != null)
        {
            _caption.gameObject.SetActive(true);
            _caption.alpha = 0f;
        }

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
        SetActive(_vignette, false);
        SetActive(_miasma, false);
        SetActive(_flash, false);
        SetActive(_fade, false);

        if (_banner != null)
        {
            _banner.gameObject.SetActive(false);
        }

        if (_caption != null)
        {
            _caption.gameObject.SetActive(false);
        }
    }

    // ───────────────────────── 진행 ─────────────────────────

    private void Evaluate(RunEndReason reason, float time)
    {
        float deltaTime = Mathf.Max(0f, time - _lastTime);
        _lastTime = time;

        switch (reason)
        {
            case RunEndReason.Abandoned:
                EvaluateAbandoned(time, deltaTime);
                break;
            case RunEndReason.MissionFailed:
                EvaluateMissionFailed(time, deltaTime);
                break;
            default:
                EvaluateDied(time, deltaTime);
                break;
        }
    }

    /// <summary>피격 사망</summary>
    private void EvaluateDied(float time, float deltaTime)
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

            if (dissolve > 0f && dissolve < 1f)
            {
                EmitMotes(deltaTime, 70f, dissolve, false, () => new MoteSettings(
                    new Vector2(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(0.5f, 1.3f)),
                    new Color(0.82f, 0.92f, 1f, 1f), UnityEngine.Random.Range(0.05f, 0.11f), UnityEngine.Random.Range(0.8f, 1.5f), 0.3f, true));
            }
        }

        SetAlpha(_flash, time < 0.05f ? 0.5f : 0.5f * (1f - Segment(time, 0.05f, 0.2f)));
        SetColor(_vignette, _hitColor, time < hitStop ? 0.75f : Mathf.Lerp(0.75f, 0.3f, Segment(time, hitStop, 0.8f)));

        if (_cameraFx != null)
        {
            _cameraFx.Shake = time < hitStop ? 0f : 0.22f * (1f - Segment(time, hitStop, 0.45f));
            _cameraFx.Zoom = 1f - 0.16f * EaseOut(Segment(time, hitStop, 1.8f));
        }

        SetGroupAlpha(_caption, Segment(time, 2.4f, 0.5f) * (1f - Segment(time, 3.6f, 0.5f)));
        SetAlpha(_fade, black);
    }

    /// <summary>탐사 포기</summary>
    private void EvaluateAbandoned(float time, float deltaTime)
    {
        float black = Segment(time, 3.2f, 0.7f);
        SetTimeScale(black >= 1f ? 0f : Mathf.Lerp(1f, 0.15f, Segment(time, 0f, 0.4f)));
        SetSceneLight(_abandonedSceneLight, 0.75f, Segment(time, 0f, 0.8f));
        SetColor(_vignette, new Color(0.02f, 0.05f, 0.12f, 1f), 0.55f * Segment(time, 0f, 0.8f));

        if (_circleHolder != null)
        {
            float appear = EaseOut(Segment(time, 0.1f, 0.5f));
            _circleHolder.localScale = new Vector3(appear, appear * 0.42f, 1f);
            _circle.transform.localRotation = Quaternion.Euler(0f, 0f, -time * 30f);
            _circle.color = WithAlpha(_recallColor, 0.9f * Segment(time, 0.1f, 0.3f) * (1f - Segment(time, 2.4f, 0.6f)));
        }

        float flare = Segment(time, 1.9f, 0.2f) * (1f - Segment(time, 2.1f, 0.4f));
        if (_columnHolder != null)
        {
            float grow = EaseOut(Segment(time, 0.55f, 0.45f));
            _columnHolder.localScale = new Vector3(1f + flare * 0.3f, Mathf.Max(0.001f, grow), 1f);
            _column.color = WithAlpha(_recallColor, (grow * 0.75f + flare * 0.25f) * (1f - Segment(time, 2.3f, 0.6f)));
        }

        float dissolve = Segment(time, 0.9f, 1.3f);
        if (_player != null)
        {
            _player.SetTint(_baseTint * _sceneTint);
            _player.SetFlash(_recallColor, 0.6f * Segment(time, 0.6f, 0.4f));
            _player.SetDissolve(dissolve, false, 0.35f, new Color(0.85f, 0.95f, 1f, 1f), 0.1f);
            _player.SetLightFactor(1f - Segment(time, 0.9f, 1f));

            if (dissolve > 0f && dissolve < 1f)
            {
                EmitMotes(deltaTime, 55f, dissolve, false, () => new MoteSettings(
                    new Vector2(UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(1.4f, 2.8f)),
                    _recallColor, UnityEngine.Random.Range(0.04f, 0.09f), UnityEngine.Random.Range(0.6f, 1.1f), 0.8f, true));
            }
        }

        SetAlpha(_flash, 0.25f * flare);

        if (_cameraFx != null)
        {
            _cameraFx.Zoom = 1f - 0.06f * EaseOut(Segment(time, 0.2f, 2.5f));
        }

        SetGroupAlpha(_caption, Segment(time, 2.1f, 0.5f) * (1f - Segment(time, 3.2f, 0.4f)));
        SetAlpha(_fade, black);
    }

    /// <summary>임무 실패 (오염에 잠식)</summary>
    private void EvaluateMissionFailed(float time, float deltaTime)
    {
        float black = Segment(time, 4.4f, 0.7f);
        float timeScale = time < 2.4f
            ? Mathf.Lerp(1f, 0.3f, Segment(time, 0f, 0.5f))
            : Mathf.Lerp(0.3f, 0.06f, Segment(time, 2.4f, 1f));
        SetTimeScale(black >= 1f ? 0f : timeScale);

        if (_banner != null)
        {
            float bannerIn = Segment(time, 0f, 0.3f);
            _banner.alpha = bannerIn * (1f - Segment(time, 2.4f, 0.4f));
            _banner.transform.localScale = _bannerScale * Mathf.Lerp(1.15f, 1f, EaseOut(bannerIn));
        }

        if (_veinsMaterial != null)
        {
            _veinsMaterial.SetFloat(FailureSpriteMaterial.RevealRadius, _veinsRadius * 1.1f * EaseOut(Segment(time, 0.3f, 2.2f)));
            SpriteRenderer veins = _veinsHolder != null ? _veinsHolder.GetComponentInChildren<SpriteRenderer>() : null;
            if (veins != null)
            {
                veins.color = WithAlpha(Color.white, 0.92f * Segment(time, 0.3f, 0.3f));
            }
        }

        SetAlpha(_miasma, 0.9f * Smooth(Segment(time, 0.4f, 2.4f)));
        SetColor(_vignette, Color.black, 0.35f * Segment(time, 0.4f, 2f));
        SetSceneLight(_missionSceneLight, 0.5f, Segment(time, 0.3f, 2f));

        float dissolve = Segment(time, 2.6f, 1.4f);
        if (_player != null)
        {
            _player.SetTint(_baseTint * _sceneTint);
            _player.SetFlash(Color.white, 0f);
            _player.SetStain(Smooth(Segment(time, 0.8f, 1.7f)), _stainColor, _stainEdgeColor);
            _player.SetDissolve(dissolve, true, 0.4f, new Color(0.55f, 0.22f, 0.7f, 1f), 0.08f);
            _player.SetLightFactor(1f - Segment(time, 0.8f, 1.5f));

            if (dissolve > 0f && dissolve < 1f)
            {
                EmitMotes(deltaTime, 80f, dissolve, true, () =>
                {
                    bool isGlow = UnityEngine.Random.value < 0.3f;
                    return new MoteSettings(
                        new Vector2(UnityEngine.Random.Range(-0.35f, 0.35f), UnityEngine.Random.Range(-0.2f, 0.3f)),
                        isGlow ? new Color(0.6f, 0.25f, 0.8f, 1f) : new Color(0.16f, 0.05f, 0.2f, 1f),
                        isGlow ? UnityEngine.Random.Range(0.03f, 0.06f) : UnityEngine.Random.Range(0.05f, 0.1f),
                        UnityEngine.Random.Range(0.7f, 1.1f), -2.2f, isGlow);
                });
            }
        }

        if (_cameraFx != null)
        {
            _cameraFx.Zoom = 1f - 0.08f * Smooth(Segment(time, 0.3f, 3f));
            _cameraFx.Shake = dissolve > 0f && dissolve < 1f ? 0.04f : 0f;
        }

        SetGroupAlpha(_caption, Segment(time, 3.3f, 0.5f) * (1f - Segment(time, 4.4f, 0.5f)));
        SetAlpha(_fade, black);
    }

    // ───────────────────────── 입자 ─────────────────────────

    private readonly struct MoteSettings
    {
        public readonly Vector2 Velocity;
        public readonly Color Color;
        public readonly float Size;
        public readonly float Life;
        public readonly float Gravity;
        public readonly bool IsAdditive;

        public MoteSettings(Vector2 velocity, Color color, float size, float life, float gravity, bool isAdditive)
        {
            Velocity = velocity;
            Color = color;
            Size = size;
            Life = life;
            Gravity = gravity;
            IsAdditive = isAdditive;
        }
    }

    /// <summary>사라지는 경계에서 초당 rate개씩 입자를 뿌린다.</summary>
    private void EmitMotes(float deltaTime, float rate, float dissolve, bool fromTop, Func<MoteSettings> create)
    {
        if (_motes == null || _player == null)
        {
            return;
        }

        Bounds bounds = _player.Bounds;
        float frontY = _player.GetDissolveFrontY(dissolve, fromTop);
        _emitCarry += rate * deltaTime;
        while (_emitCarry >= 1f)
        {
            _emitCarry -= 1f;
            MoteSettings settings = create();
            Vector2 position = new(
                bounds.center.x + UnityEngine.Random.Range(-0.3f, 0.3f) * bounds.size.x,
                frontY + UnityEngine.Random.Range(-0.05f, 0.05f) * bounds.size.y);
            _motes.Emit(position, settings.Velocity, settings.Color, settings.Size, settings.Life, settings.Gravity, settings.IsAdditive);
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

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static void SetActive(Graphic graphic, bool isActive)
    {
        if (graphic != null)
        {
            graphic.gameObject.SetActive(isActive);
        }
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic != null)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }

    private static void SetColor(Graphic graphic, Color color, float alpha)
    {
        if (graphic != null)
        {
            color.a = alpha;
            graphic.color = color;
        }
    }

    private static void SetGroupAlpha(CanvasGroup group, float alpha)
    {
        if (group != null)
        {
            group.alpha = alpha;
        }
    }
}
