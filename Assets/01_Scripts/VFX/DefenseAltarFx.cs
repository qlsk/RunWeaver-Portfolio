using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 제단 방어 임무의 제단 연출.
/// - 수정 광원(Light2D)과 은은한 후광이 천천히 맥동한다. 금이 갈수록 불안하게 깜빡인다.
/// - 수정 주위를 작은 빛 알갱이가 돈다.
/// - 피격 : 제단이 하얗게 번쩍이고 살짝 흔들리며, 수정에서 파편이 튄다. (체력이 줄어든 것을 보고 안다)
/// - 금이 가는 단계(Stage 2, 3)로 넘어갈 때 : 파편이 크게 튀고 빛이 한 번 일렁인다.
/// - 파괴 : 제단이 사라지는 순간 그 자리에 파편이 크게 흩어진다.
/// 단계는 같은 오브젝트의 Animator "Stage" 값(DefenseAltarStage가 바꾼다)으로 읽는다.
/// </summary>
[DisallowMultipleComponent]
public class DefenseAltarFx : MonoBehaviour
{
    private static readonly int StageHash = Animator.StringToHash("Stage");
    private const int OrbitCount = 8;

    [SerializeField] private DefenseAltar _altar;
    [Tooltip("하얗게 번쩍일 때 쓰는 머티리얼 (SpriteWhiteFlash)")]
    [SerializeField] private Material _flashMaterial;

    [Header("수정 위치 (로컬, 단계 1~3)")]
    [SerializeField] private Vector2[] _crystalOffsets = { new(0f, 2.32f), new(0.09f, 2.3f), new(0.12f, 1.86f) };

    [Header("광원")]
    [SerializeField] private Color _crystalColor = new(0.55f, 0.9f, 1f, 1f);
    [SerializeField] private float _lightIntensity = 0.7f;
    [SerializeField] private float _lightRadius = 2.4f;
    [SerializeField] private float _pulsePeriod = 1.8f;
    [SerializeField] private float _haloAlpha = 0.16f;
    [SerializeField] private float _haloSize = 1.1f;

    [Header("빛 알갱이 / 파편")]
    [Tooltip("그림 픽셀 한 칸의 월드 크기")]
    [SerializeField] private float _pixelSize = 4f / 170f;
    [SerializeField] private Vector2 _orbitRadius = new(0.5f, 0.22f);
    [SerializeField] private Color _stoneColor = new(0.55f, 0.6f, 0.58f, 1f);

    [Header("피격")]
    [SerializeField, Min(0.01f)] private float _flashDuration = 0.18f;
    [SerializeField, Min(0.01f)] private float _shakeDuration = 0.2f;
    [SerializeField] private float _shakeAmount = 0.05f;

    private Animator _animator;
    private SpriteRenderer _body;
    private SpriteRenderer _flash;
    private SpriteRenderer _halo;
    private Light2D _light;
    private readonly SpriteRenderer[] _orbits = new SpriteRenderer[OrbitCount];
    private PixelParticles _particles;
    private float _lastHp = -1f;
    private int _stage = 1;
    private float _flashTimer;
    private float _shakeTimer;
    private Vector3 _shakeBase;
    private float _flare;
    private float _seed;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _body = GetComponent<SpriteRenderer>();
        if (_altar == null)
        {
            _altar = GetComponent<DefenseAltar>();
        }

        _seed = Random.Range(0f, 100f);
        Build();
    }

    private void Build()
    {
        Material unlit = MissionFxSprites.UnlitMaterial;

        var lightObject = new GameObject("AltarLight");
        lightObject.transform.SetParent(transform, false);
        _light = lightObject.AddComponent<Light2D>();
        _light.lightType = Light2D.LightType.Point;
        _light.color = _crystalColor;
        _light.pointLightInnerRadius = 0f;
        _light.pointLightOuterRadius = _lightRadius;
        _light.falloffIntensity = 0.7f;

        _halo = CreateChild("AltarHalo", MissionFxSprites.SoftCircle, unlit);
        _halo.transform.localScale = new Vector3(_haloSize, _haloSize * 1.2f, 1f);

        if (_flashMaterial != null && _body != null)
        {
            _flash = CreateChild("AltarFlash", null, _flashMaterial);
            _flash.color = new Color(1f, 1f, 1f, 0f);
        }

        for (int i = 0; i < OrbitCount; i++)
        {
            _orbits[i] = CreateChild("AltarMote", MissionFxSprites.Pixel, unlit);
            _orbits[i].transform.localScale = Vector3.one * _pixelSize * (i % 3 == 0 ? 2f : 1f);
        }

        var particleObject = new GameObject("AltarParticles");
        _particles = particleObject.AddComponent<PixelParticles>();
    }

    private SpriteRenderer CreateChild(string objectName, Sprite sprite, Material material)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }

        return renderer;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        int stage = _animator != null ? Mathf.Clamp(_animator.GetInteger(StageHash), 1, 4) : 1;
        if (stage > _stage && stage <= 3)
        {
            Burst(16, 3.2f);
            _flare = 1f;
        }

        _stage = stage;
        DetectHit();

        _flare = Mathf.MoveTowards(_flare, 0f, dt / 0.5f);
        _flashTimer = Mathf.Max(0f, _flashTimer - dt);
        UpdateShake(dt);

        Vector2 crystal = CrystalOffset;
        float time = Time.time + _seed;
        float pulse = 0.5f + 0.5f * Mathf.Sin(time * 2f * Mathf.PI / _pulsePeriod);
        // 금이 갈수록 빛이 불안하게 깜빡인다.
        float unstable = stage <= 1 ? 0f : stage == 2 ? 0.2f : 0.45f;
        float flicker = 1f - unstable * Mathf.PerlinNoise(time * 9f, 0.5f);
        float strength = (0.75f + 0.25f * pulse) * flicker + _flare * 0.8f;

        int order = _body != null ? _body.sortingOrder : 0;
        int layer = _body != null ? _body.sortingLayerID : 0;

        _light.transform.localPosition = crystal;
        _light.intensity = _lightIntensity * strength;

        _halo.transform.localPosition = crystal;
        Color halo = _crystalColor;
        halo.a = _haloAlpha * strength;
        _halo.color = halo;
        _halo.sortingLayerID = layer;
        _halo.sortingOrder = order + 1;

        if (_flash != null)
        {
            _flash.sprite = _body.sprite;
            _flash.flipX = _body.flipX;
            float k = _flashTimer / _flashDuration;
            _flash.color = new Color(1f, 1f, 1f, 0.85f * k * k);
            _flash.sortingLayerID = layer;
            _flash.sortingOrder = order + 2;
        }

        for (int i = 0; i < OrbitCount; i++)
        {
            SpriteRenderer mote = _orbits[i];
            float angle = time * (0.9f + 0.15f * (i % 3)) + i * (Mathf.PI * 2f / OrbitCount);
            float bob = Mathf.Sin(time * 1.7f + i) * 0.06f;
            Vector2 position = crystal + new Vector2(Mathf.Cos(angle) * _orbitRadius.x, Mathf.Sin(angle) * _orbitRadius.y + bob);
            mote.transform.localPosition = Snap(position);
            float twinkle = 0.5f + 0.5f * Mathf.Sin(time * 5f + i * 1.7f);
            Color color = Color.Lerp(_crystalColor, Color.white, twinkle * 0.6f);
            color.a = (0.35f + 0.6f * twinkle) * flicker;
            mote.color = color;
            mote.sortingLayerID = layer;
            // 수정 뒤를 돌 때는 제단 뒤로 숨긴다.
            mote.sortingOrder = Mathf.Sin(angle) > 0f ? order - 1 : order + 3;
        }

        _particles.SetSorting(layer, order + 3);
    }

    private Vector2 CrystalOffset
    {
        get
        {
            if (_crystalOffsets == null || _crystalOffsets.Length == 0)
            {
                return Vector2.up * 2f;
            }

            return _crystalOffsets[Mathf.Clamp(_stage - 1, 0, _crystalOffsets.Length - 1)];
        }
    }

    private void DetectHit()
    {
        if (_altar == null)
        {
            return;
        }

        float hp = _altar.CurrentHp;
        if (_lastHp >= 0f && hp < _lastHp - 0.001f)
        {
            _flashTimer = _flashDuration;
            if (_shakeTimer <= 0f)
            {
                _shakeBase = transform.position;
            }

            _shakeTimer = _shakeDuration;
            Burst(6, 2.2f);
        }

        _lastHp = hp;
    }

    private void UpdateShake(float dt)
    {
        if (_shakeTimer <= 0f)
        {
            return;
        }

        _shakeTimer = Mathf.Max(0f, _shakeTimer - dt);
        if (_shakeTimer <= 0f)
        {
            transform.position = _shakeBase;
            return;
        }

        float k = _shakeTimer / _shakeDuration;
        float offset = Mathf.Sin(Time.time * 90f) * _shakeAmount * k;
        transform.position = _shakeBase + new Vector3(offset, 0f, 0f);
    }

    /// <summary>수정에서 파편이 위로 튀었다가 바닥으로 떨어진다.</summary>
    private void Burst(int count, float speed)
    {
        Vector3 origin = transform.TransformPoint(CrystalOffset);
        EmitShards(_particles, origin, transform.position.y, count, speed);
    }

    private void EmitShards(PixelParticles particles, Vector3 origin, float floorY, int count, float speed)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(20f, 160f) * Mathf.Deg2Rad;
            float s = speed * Random.Range(0.5f, 1f);
            var velocity = new Vector3(Mathf.Cos(angle) * s, Mathf.Sin(angle) * s, 0f);
            Color color = Random.value < 0.6f ? _crystalColor : _stoneColor;
            float size = _pixelSize * (Random.value < 0.3f ? 2f : 1f);
            particles.Emit(origin, velocity, 9f, Random.Range(0.6f, 1.1f), color, size,
                floorY + Random.Range(-0.15f, 0.25f));
        }
    }

    private void OnDestroy()
    {
        if (_shakeTimer > 0f)
        {
            transform.position = _shakeBase;
        }

        // 체력이 다해 사라질 때만 그 자리에 파편을 크게 흩뿌린다. (씬을 닫을 때는 남기지 않는다)
        if (_altar != null && _altar.IsDestroyed && gameObject.scene.isLoaded)
        {
            int layer = _body != null ? _body.sortingLayerID : 0;
            int order = _body != null ? _body.sortingOrder + 3 : 0;
            PixelParticles detached = PixelParticles.CreateDetached("AltarDebris", layer, order);
            EmitShards(detached, transform.TransformPoint(CrystalOffset), transform.position.y, 26, 4f);
            Vector3 middle = transform.TransformPoint(new Vector3(0f, 1f, 0f));
            EmitShards(detached, middle, transform.position.y, 14, 2.6f);
        }

        if (_particles != null)
        {
            _particles.DestroyWhenEmpty = true;
        }
    }

    private Vector2 Snap(Vector2 local)
    {
        float unit = _pixelSize / Mathf.Max(0.0001f, transform.lossyScale.x);
        return new Vector2(Mathf.Round(local.x / unit) * unit, Mathf.Round(local.y / unit) * unit);
    }
}
