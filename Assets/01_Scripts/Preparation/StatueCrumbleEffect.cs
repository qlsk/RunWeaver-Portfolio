using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 시작 석상이 위에서부터 알갱이로 풀리며 모래처럼 떨어지는 연출. (순수 C# 클래스. ExpeditionStatue가 만들어서 구동한다)
/// - 스프라이트 마스크를 위에서 아래로 내려 비석을 지운다.
/// - 지워진 자리에서 비석 텍스처의 실제 색을 가진 알갱이를 만든다. 알갱이는 ParticleSystem 하나에 담아 직접 움직인다.
/// - 먼지는 풀리는 경계와 알갱이가 쌓이는 밑동에서 점진적으로 생성한다.
/// 만든 오브젝트는 모두 효과 루트 아래에 두고 Cleanup으로 한꺼번에 치운다.
/// 사용: Begin → (매 프레임) Tick → Finish. 도중에 끊기면 Cleanup.
/// </summary>
public sealed class StatueCrumbleEffect
{
    /// <summary>연출 설정. ExpeditionStatue의 인스펙터 값을 옮겨 담는다.</summary>
    public struct Settings
    {
        /// <summary>비석이 위에서 아래까지 다 풀리는 데 걸리는 시간</summary>
        public float Duration;

        public int GrainCount;
        public Vector2 GrainSize;
        public float GrainGravity;
        public float GrainSpread;
        public float GroundJitter;
        public float GrainFadeDuration;

        public Sprite DustSprite;
        public Color DustColor;
        public int DustCount;
        public Vector2 DustSize;
        public float DustLifetime;
    }

    // 위에서 아래로 풀리는 속도 곡선. 1보다 크면 처음엔 천천히, 갈수록 빨라진다.
    private const float CrumbleCurve = 1.35f;
    private const float MaskMargin = 0.2f;
    private const byte MinGrainAlpha = 80;
    private const int GrainSampleAttempts = 6;
    private const float GrainDrag = 1.2f;
    private const float GrainLifetime = 1000f;

    private static Sprite s_whiteSprite;

    private struct GrainState
    {
        public Vector3 Position;
        public Vector2 Velocity;
        public float GroundY;
        public bool IsResting;
        public Color32 Color;
    }

    /// <summary>먼지 하나. 스스로 움직이고 커지다 사라진다. (알갱이는 GrainState로 따로 다룬다)</summary>
    private sealed class Dust
    {
        private readonly Transform _transform;
        private readonly SpriteRenderer _renderer;
        private readonly Vector3 _baseScale;
        private readonly float _life;
        private readonly Color _color;

        private Vector2 _velocity;
        private float _age;

        public Dust(Transform transform, SpriteRenderer renderer, Vector2 velocity, float life, Color color)
        {
            _transform = transform;
            _renderer = renderer;
            _baseScale = transform.localScale;
            _velocity = velocity;
            _life = life;
            _color = color;
        }

        public void Update(float deltaTime)
        {
            if (_transform == null)
            {
                return;
            }

            _age += deltaTime;
            float t = Mathf.Clamp01(_age / _life);
            _velocity *= Mathf.Exp(-2.5f * deltaTime);
            _transform.position += (Vector3)(_velocity * deltaTime);
            _transform.localScale = _baseScale * Mathf.Lerp(0.7f, 1.6f, 1f - (1f - t) * (1f - t));

            Color color = _color;
            color.a *= Mathf.Min(1f, t / 0.15f) * (1f - t);
            _renderer.color = color;
        }
    }

    private readonly Settings _settings;
    private readonly SpriteRenderer _statueRenderer;
    private readonly Transform _owner;
    private readonly List<Dust> _dusts = new();

    private GameObject _root;
    private SpriteMask _mask;
    private Material _fallbackMaterial;
    private Bounds _bounds;
    private Color _baseColor;
    private Color _flashColor;

    private Color32[] _pixels;
    private int _pixelWidth;
    private int _pixelHeight;
    private Vector2 _sampleOffset;

    private GrainState[] _grains;
    private ParticleSystem.Particle[] _grainParticles;
    private ParticleSystem _grainSystem;
    private int _grainActive;

    private float _previousProgress;
    private float _grainBudget;
    private float _dustBudget;

    public StatueCrumbleEffect(Settings settings, SpriteRenderer statueRenderer, Transform owner)
    {
        _settings = settings;
        _statueRenderer = statueRenderer;
        _owner = owner;
    }

    /// <summary>연출이 끝나기까지 걸리는 시간. 먼지는 풀리는 마지막까지 만들어지므로 그 수명도 더한다.</summary>
    public float Duration => Mathf.Max(_settings.Duration + _settings.GrainFadeDuration,
        _settings.Duration + _settings.DustLifetime);

    private int SortingLayerId => _statueRenderer != null ? _statueRenderer.sortingLayerID : 0;
    private int SortingOrder => _statueRenderer != null ? _statueRenderer.sortingOrder : 0;

    public bool IsCollapsed => _previousProgress >= 1f;

    /// <summary>연출을 준비한다. 마스크와 알갱이 시스템을 만들고, 알갱이 색을 얻으려고 석상 색을 읽어 둔다.</summary>
    /// <param name="baseColor">석상의 원래 색 (알갱이 색에 곱한다)</param>
    /// <param name="flashColor">풀리기 직전까지 석상이 빛나던 색 (풀리기 시작하면 원래 색으로 돌아온다)</param>
    public void Begin(Color baseColor, Color flashColor)
    {
        _baseColor = baseColor;
        _flashColor = flashColor;
        _bounds = _statueRenderer != null
            ? _statueRenderer.bounds
            : new Bounds(_owner.position, new Vector3(2f, 3f, 0f));

        _root = new GameObject("StatueCrumbleEffects");
        _mask = CreateMask();
        if (_statueRenderer != null)
        {
            _statueRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        TryReadStatuePixels();
        CreateGrainSystem();
    }

    /// <summary>연출을 한 프레임 진행한다. elapsed는 Begin 이후 지난 시간이다.</summary>
    public void Tick(float elapsed, float deltaTime)
    {
        float progress = Mathf.Pow(Mathf.Clamp01(elapsed / _settings.Duration), CrumbleCurve);
        if (progress > _previousProgress)
        {
            Advance(progress);
        }

        // 빛나던 석상 색은 풀리기 시작하면 돌 색으로 돌아온다.
        if (_statueRenderer != null && progress < 1f)
        {
            float colorT = Mathf.Clamp01(elapsed / (_settings.Duration * 0.25f));
            _statueRenderer.color = Color.Lerp(_flashColor, _baseColor, colorT);
        }

        float fade = Mathf.Clamp01((elapsed - _settings.Duration) / _settings.GrainFadeDuration);
        UpdateGrains(deltaTime, fade);
        UpdateDust(deltaTime);
    }

    /// <summary>연출을 마친다. 석상을 숨기고 만든 오브젝트를 모두 치운다.</summary>
    public void Finish()
    {
        if (_statueRenderer != null)
        {
            _statueRenderer.enabled = false;
        }

        Cleanup();
    }

    /// <summary>만든 마스크, 알갱이, 먼지를 모두 치운다. (석상의 마스크 설정과 색은 건드리지 않는다)</summary>
    public void Cleanup()
    {
        if (_root != null)
        {
            UnityEngine.Object.Destroy(_root);
        }

        if (_fallbackMaterial != null)
        {
            UnityEngine.Object.Destroy(_fallbackMaterial);
        }

        _fallbackMaterial = null;
        _root = null;
        _mask = null;
        _grainSystem = null;
        _grains = null;
        _grainParticles = null;
        _grainActive = 0;
        _pixels = null;
        _dusts.Clear();
    }

    // ───────────────────────── 진행 ─────────────────────────

    /// <summary>풀린 정도가 progress까지 늘었을 때 새로 풀린 띠에서 알갱이와 먼지를 만들고 마스크를 내린다.</summary>
    private void Advance(float progress)
    {
        float step = progress - _previousProgress;
        float previousY = Mathf.Lerp(_bounds.max.y, _bounds.min.y, _previousProgress);
        float cutY = Mathf.Lerp(_bounds.max.y, _bounds.min.y, progress);

        _grainBudget += _settings.GrainCount * step;
        int grainsToSpawn = (int)_grainBudget;
        _grainBudget -= grainsToSpawn;
        SpawnGrains(grainsToSpawn, cutY, previousY);

        if (_settings.DustSprite != null)
        {
            _dustBudget += _settings.DustCount * step;
            int dustToSpawn = (int)_dustBudget;
            _dustBudget -= dustToSpawn;
            for (int i = 0; i < dustToSpawn; i++)
            {
                // 절반은 풀리는 경계에서, 절반은 알갱이가 쌓이는 밑동에서 인다.
                float dustY = i % 2 == 0
                    ? Random.Range(cutY, previousY)
                    : _bounds.min.y + Random.Range(0f, 0.35f);
                _dusts.Add(CreateDust(dustY));
            }
        }

        UpdateMask(cutY);
        _previousProgress = progress;
    }

    // ───────────────────────── 마스크 ─────────────────────────

    private static Sprite GetWhiteSprite()
    {
        if (s_whiteSprite == null)
        {
            Texture2D white = Texture2D.whiteTexture;
            s_whiteSprite = Sprite.Create(white, new Rect(0f, 0f, white.width, white.height), new Vector2(0.5f, 0.5f),
                white.width);
        }

        return s_whiteSprite;
    }

    /// <summary>비석 윗부분을 가리는 마스크를 만든다. 마스크 안쪽(아래쪽)만 비석이 보인다.</summary>
    private SpriteMask CreateMask()
    {
        var go = new GameObject("StatueCrumbleMask");
        go.transform.SetParent(_root.transform, false);
        SpriteMask mask = go.AddComponent<SpriteMask>();
        mask.sprite = GetWhiteSprite();
        _mask = mask;
        UpdateMask(_bounds.max.y);
        return mask;
    }

    /// <summary>마스크의 윗변을 cutY로 맞춘다. 아랫변은 비석 밑동보다 조금 아래에 둔다.</summary>
    private void UpdateMask(float cutY)
    {
        float bottom = _bounds.min.y - MaskMargin;
        float height = Mathf.Max(0.0001f, cutY - bottom);
        _mask.transform.position = new Vector3(_bounds.center.x, (cutY + bottom) * 0.5f, _owner.position.z);
        _mask.transform.localScale = new Vector3(_bounds.size.x + MaskMargin * 2f, height, 1f);
    }

    // ───────────────────────── 석상 색 읽기 ─────────────────────────

    /// <summary>석상 스프라이트의 색을 읽어 온다. (텍스처가 읽기 불가여도 GPU를 거쳐 한 번 복사한다)</summary>
    /// <returns>읽지 못하면 false. 이때 알갱이는 석상의 단색으로 만든다.</returns>
    private bool TryReadStatuePixels()
    {
        _pixels = null;
        _pixelWidth = 0;
        _pixelHeight = 0;
        if (_statueRenderer == null || _statueRenderer.sprite == null)
        {
            return false;
        }

        Sprite sprite = _statueRenderer.sprite;
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = null;
        Texture2D readable = null;
        try
        {
            if (sprite.packed && sprite.packingRotation != SpritePackingRotation.None)
            {
                Debug.LogWarning("[StatueCrumbleEffect] 회전되어 패킹된 스프라이트는 색을 읽지 못해 알갱이를 단색으로 만듭니다.");
                return false;
            }

            Texture2D texture = sprite.texture;
            Rect rect = sprite.textureRect;
            _sampleOffset = sprite.textureRectOffset;
            target = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            Graphics.Blit(texture, target);
            RenderTexture.active = target;
            readable = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(rect.x, rect.y, rect.width, rect.height), 0, 0);
            _pixels = readable.GetPixels32();
            _pixelWidth = readable.width;
            _pixelHeight = readable.height;
            return true;
        }
        catch (Exception exception)
        {
            _pixels = null;
            Debug.LogWarning($"[StatueCrumbleEffect] 석상 색을 읽지 못해 알갱이를 단색으로 만듭니다. {exception.Message}");
            return false;
        }
        finally
        {
            RenderTexture.active = previous;
            if (target != null)
            {
                RenderTexture.ReleaseTemporary(target);
            }

            if (readable != null)
            {
                UnityEngine.Object.Destroy(readable);
            }
        }
    }

    /// <summary>월드 좌표에 해당하는 석상 스프라이트의 색을 구한다. 투명한 곳이면 false.</summary>
    private bool TrySampleStatueColor(Vector3 worldPosition, out Color32 color)
    {
        color = default;
        Sprite sprite = _statueRenderer.sprite;
        Vector3 local = _statueRenderer.transform.InverseTransformPoint(worldPosition);
        if (_statueRenderer.flipX)
        {
            local.x = -local.x;
        }

        if (_statueRenderer.flipY)
        {
            local.y = -local.y;
        }

        // 읽어 온 배열은 textureRect 영역이므로, 원래 스프라이트 기준 좌표에서 오프셋을 뺀다.
        int x = Mathf.FloorToInt(local.x * sprite.pixelsPerUnit + sprite.pivot.x - _sampleOffset.x);
        int y = Mathf.FloorToInt(local.y * sprite.pixelsPerUnit + sprite.pivot.y - _sampleOffset.y);
        if (x < 0 || x >= _pixelWidth || y < 0 || y >= _pixelHeight)
        {
            return false;
        }

        color = _pixels[y * _pixelWidth + x];
        return color.a >= MinGrainAlpha;
    }

    // ───────────────────────── 알갱이 ─────────────────────────

    /// <summary>알갱이를 그릴 ParticleSystem을 만든다. 생성과 움직임은 코드가 직접 맡고, 시스템은 그리기만 한다.</summary>
    private void CreateGrainSystem()
    {
        int capacity = Mathf.Max(1, _settings.GrainCount);
        _grains = new GrainState[capacity];
        _grainParticles = new ParticleSystem.Particle[capacity];
        _grainActive = 0;

        var go = new GameObject("StatueGrains");
        go.transform.SetParent(_root.transform, false);
        _grainSystem = go.AddComponent<ParticleSystem>();
        _grainSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = _grainSystem.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = capacity;
        main.gravityModifier = 0f;
        ParticleSystem.EmissionModule emission = _grainSystem.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = _grainSystem.shape;
        shape.enabled = false;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (_statueRenderer != null && _statueRenderer.sharedMaterial != null)
        {
            renderer.sharedMaterial = _statueRenderer.sharedMaterial;
        }
        else
        {
            // 석상 머티리얼이 없을 때만 직접 만들고, Cleanup에서 함께 치운다.
            _fallbackMaterial = new Material(Shader.Find("Sprites/Default"));
            renderer.sharedMaterial = _fallbackMaterial;
        }

        renderer.sortingLayerID = SortingLayerId;
        renderer.sortingOrder = SortingOrder + 1;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        _grainSystem.Play();
    }

    /// <summary>풀려 나간 띠(lowerY~upperY)에서 알갱이를 만든다. 알갱이 색은 그 자리의 비석 색이다.</summary>
    private void SpawnGrains(int count, float lowerY, float upperY)
    {
        if (_grains == null)
        {
            return;
        }

        bool hasPixels = _pixels != null;
        float z = _owner.position.z;
        for (int i = 0; i < count && _grainActive < _grains.Length; i++)
        {
            Vector3 position = default;
            Color32 color = _baseColor;
            bool found = false;
            for (int attempt = 0; attempt < GrainSampleAttempts && !found; attempt++)
            {
                position = new Vector3(Random.Range(_bounds.min.x, _bounds.max.x), Random.Range(lowerY, upperY), z);
                if (!hasPixels)
                {
                    found = true;
                }
                else if (TrySampleStatueColor(position, out Color32 sampled))
                {
                    color = (Color)sampled * _baseColor;
                    found = true;
                }
            }

            if (!found)
            {
                continue;
            }

            color.a = 255;
            float groundY = _bounds.min.y + Random.Range(0f, _settings.GroundJitter);
            int index = _grainActive++;
            _grains[index] = new GrainState
            {
                Position = position,
                Velocity = new Vector2(Random.Range(-1f, 1f) * _settings.GrainSpread, Random.Range(-0.3f, 0.1f)),
                // 생성된 자리보다 높은 곳에는 쌓이지 않는다. (알갱이가 위로 튀어 오르지 않게)
                GroundY = Mathf.Min(groundY, position.y),
                Color = color,
            };

            _grainParticles[index] = new ParticleSystem.Particle
            {
                position = position,
                startColor = color,
                startSize = Random.Range(_settings.GrainSize.x, _settings.GrainSize.y),
                startLifetime = GrainLifetime,
                remainingLifetime = GrainLifetime,
            };
        }
    }

    /// <summary>알갱이를 모래처럼 떨어뜨린다. 바닥에 닿으면 쌓여 있다가 fade가 올라가면 사라진다.</summary>
    private void UpdateGrains(float deltaTime, float fade)
    {
        if (_grainSystem == null || _grainActive == 0)
        {
            return;
        }

        byte alpha = (byte)Mathf.RoundToInt(255f * (1f - fade));
        for (int i = 0; i < _grainActive; i++)
        {
            ref GrainState grain = ref _grains[i];
            if (!grain.IsResting)
            {
                grain.Velocity.y -= _settings.GrainGravity * deltaTime;
                grain.Velocity.x *= Mathf.Exp(-GrainDrag * deltaTime);
                grain.Position += (Vector3)(grain.Velocity * deltaTime);
                if (grain.Position.y <= grain.GroundY)
                {
                    grain.Position.y = grain.GroundY;
                    grain.IsResting = true;
                }

                _grainParticles[i].position = grain.Position;
            }

            _grainParticles[i].remainingLifetime = GrainLifetime;
            if (fade > 0f)
            {
                _grainParticles[i].startColor = new Color32(grain.Color.r, grain.Color.g, grain.Color.b, alpha);
            }
        }

        _grainSystem.SetParticles(_grainParticles, _grainActive);
    }

    // ───────────────────────── 먼지 ─────────────────────────

    private Dust CreateDust(float y)
    {
        var go = new GameObject("StatueDust");
        go.transform.SetParent(_root.transform, false);
        float x = Mathf.Lerp(_bounds.min.x, _bounds.max.x, Random.value);
        go.transform.position = new Vector3(x, y, _owner.position.z);
        float size = Random.Range(_settings.DustSize.x, _settings.DustSize.y);
        go.transform.localScale = Vector3.one * size;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = _settings.DustSprite;
        renderer.sortingLayerID = SortingLayerId;
        renderer.sortingOrder = SortingOrder + 2;
        Color dustColor = _settings.DustColor;
        renderer.color = new Color(dustColor.r, dustColor.g, dustColor.b, 0f);

        float side = (x - _bounds.center.x) / Mathf.Max(0.01f, _bounds.extents.x);
        var velocity = new Vector2(side * Random.Range(0.8f, 2f), Random.Range(0.2f, 0.9f));
        float life = _settings.DustLifetime * Random.Range(0.7f, 1f);
        return new Dust(go.transform, renderer, velocity, life, dustColor);
    }

    private void UpdateDust(float deltaTime)
    {
        foreach (Dust dust in _dusts)
        {
            dust.Update(deltaTime);
        }
    }
}