using UnityEngine;

/// <summary>
/// 거점 점령 링의 안/밖 표시. 임무가 진행 중일 때만 동작한다. (시작 전과 끝난 뒤에는 결계도, 링 어둡게 하기도 없다)
/// 플레이어가 링 안에 있으면 링 둘레에서 금빛 결계가 바닥에서부터 천천히 솟아 감싸고,
/// 밖에 있으면 결계가 가라앉고 링이 어둡고 차가워진다.
/// - 결계 그림은 처음에 코드로 만든다. (링 타원을 따라 세운 빛의 벽, 12프레임으로 빛줄기가 흐른다)
///   위로 갈수록 5단계로 끊어지며 흐려지고, 바닥에는 밝은 테두리 선이 있다.
///   결계 전용 머티리얼(MAT_BarrierWall)로 배경 위에 빛을 더하고, 높이(_Reveal)만큼만 그려 솟아오르게 한다.
/// - 링 뒤쪽 절반과 앞쪽 절반을 따로 그려, 플레이어가 결계 안에 서 있는 것처럼 앞뒤가 맞는다. (YSort와 같은 기준)
/// - 결계를 따라 빛 알갱이가 위로 솟는다.
/// 안/밖은 같은 오브젝트의 CaptureZone.IsPlayerInside로 읽는다. 게임 시간으로 움직인다.
/// </summary>
[DisallowMultipleComponent]
public class CaptureZoneBarrier : MonoBehaviour
{
    private const int FrameCount = 12;
    private const int Bands = 5;

    [Tooltip("결계 전용 머티리얼 (MAT_BarrierWall)")]
    [SerializeField] private Material _wallMaterial;

    [Header("모양 (로컬)")]
    [Tooltip("결계가 서는 타원의 반지름. 금빛 칸이 지나는 자리")]
    [SerializeField] private Vector2 _radius = new(0.86f, 0.523f);
    [Tooltip("결계 높이 (로컬)")]
    [SerializeField] private float _height = 0.5f;
    [Tooltip("그림 픽셀 한 칸 (로컬). 링 그림과 같은 크기")]
    [SerializeField] private float _pixel = 4f / 546f;

    [Header("빛")]
    [SerializeField] private Color _wallColor = new(1f, 0.82f, 0.42f, 1f);
    [SerializeField] private Color _rimColor = new(1f, 0.96f, 0.78f, 1f);
    [SerializeField, Range(0f, 1f)] private float _frontAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float _backAlpha = 0.7f;
    [SerializeField] private float _framesPerSecond = 12f;
    [Tooltip("결계가 바닥에서 다 솟아오르는 데 걸리는 시간")]
    [SerializeField, Min(0.01f)] private float _riseDuration = 1.2f;
    [Tooltip("결계가 가라앉는 데 걸리는 시간")]
    [SerializeField, Min(0.01f)] private float _fallDuration = 0.6f;

    [Header("밖에 있을 때 링 색")]
    [SerializeField] private Color _outsideTint = new(0.7f, 0.73f, 0.82f, 1f);

    [Header("빛 알갱이")]
    [SerializeField] private float _moteRate = 28f;
    [SerializeField] private float _moteSize = 0.022f;

    private static readonly int RevealId = Shader.PropertyToID("_Reveal");
    private static readonly int WallColorId = Shader.PropertyToID("_WallColor");
    private static readonly int RimColorId = Shader.PropertyToID("_RimColor");

    private CaptureZone _zone;
    private MissionManager _manager;
    private MaterialPropertyBlock _block;
    private float _activeTint;
    private SpriteRenderer _body;
    private Color _bodyColor = Color.white;
    private SpriteRenderer _front;
    private SpriteRenderer _back;
    private Sprite[] _frontFrames;
    private Sprite[] _backFrames;
    private PixelParticles _frontMotes;
    private PixelParticles _backMotes;
    private float _weight;
    private float _frameTime;
    private float _moteTimer;

    /// <summary>안에 있는 정도 (0 : 밖, 1 : 안). 다른 연출이 같이 밝아질 때 쓴다.</summary>
    public float Weight => _weight;

    private void Awake()
    {
        _zone = GetComponent<CaptureZone>();
        _body = GetComponent<SpriteRenderer>();
        if (_body != null)
        {
            _bodyColor = _body.color;
        }

        Build();
    }

    private void Build()
    {
        _block = new MaterialPropertyBlock();
        Material material = _wallMaterial != null ? _wallMaterial : MissionFxSprites.UnlitMaterial;
        _frontFrames = BuildFrames(true);
        _backFrames = BuildFrames(false);
        _front = CreateRenderer("BarrierFront", material);
        _back = CreateRenderer("BarrierBack", material);
        _frontMotes = new GameObject("BarrierFrontMotes").AddComponent<PixelParticles>();
        _backMotes = new GameObject("BarrierBackMotes").AddComponent<PixelParticles>();
        Apply(0f);
    }

    private SpriteRenderer CreateRenderer(string objectName, Material material)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }

        return renderer;
    }

    /// <summary>앞(아래쪽 호) 또는 뒤(위쪽 호)의 빛의 벽 프레임들을 만든다. 그림 기준점은 링 가운데다.</summary>
    private Sprite[] BuildFrames(bool front)
    {
        int rx = Mathf.CeilToInt(_radius.x / _pixel);
        int ry = Mathf.CeilToInt(_radius.y / _pixel);
        int wallPixels = Mathf.CeilToInt(_height / _pixel);
        int width = rx * 2 + 3;
        int height = ry * 2 + wallPixels + 4;
        int centerX = rx + 1;
        int centerY = ry + 1;
        var frames = new Sprite[FrameCount];
        var pixels = new Color32[width * height];
        for (int f = 0; f < FrameCount; f++)
        {
            float phase = f / (float)FrameCount * Mathf.PI * 2f;
            System.Array.Clear(pixels, 0, pixels.Length);
            for (int i = -rx; i <= rx; i++)
            {
                float u = i / (float)rx;
                if (Mathf.Abs(u) >= 1f)
                {
                    continue;
                }

                int x = centerX + i;
                float yOffset = ry * Mathf.Sqrt(1f - u * u);
                int baseY = Mathf.RoundToInt(front ? centerY - yOffset : centerY + yOffset);
                // 양 끝은 옆면이라 조금 옅게
                float side = Mathf.Clamp01((1f - Mathf.Abs(u)) * 5f);
                // 위로 흐르는 밝은 빛줄기 두 겹 (한 바퀴에 정확히 한 주기라 끊김 없이 반복된다)
                float streak = 0.55f + 0.45f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(i * 0.46f + phase)), 6f)
                               + 0.35f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(i * 0.154f - phase)), 4f);
                int columnHeight = Mathf.RoundToInt(wallPixels * (0.8f + 0.2f * Mathf.Sin(i * 0.11f + phase)));
                for (int j = 0; j < columnHeight; j++)
                {
                    float q = j / (float)Mathf.Max(1, columnHeight);
                    float band = Mathf.Floor((1f - q) * Bands) / Bands;
                    float a = Mathf.Clamp01(band * (0.55f + 0.45f * side) * streak);
                    // 높이는 기둥마다가 아니라 벽 전체 높이 기준이라, 솟는 끝이 고르게 올라간다.
                    Put(pixels, width, height, x, baseY + j, a, j / (float)wallPixels, false);
                }

                // 바닥의 선명한 테두리 선 (높이 0이라 솟기 시작하자마자 보인다)
                Put(pixels, width, height, x, baseY, 1f, 0f, true);
                Put(pixels, width, height, x, baseY + 1, 1f, 0f, true);
                Put(pixels, width, height, x, baseY + 2, 0.6f, 0f, true);
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = front ? "BarrierFront" : "BarrierBack"
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var pivot = new Vector2(centerX / (float)width, centerY / (float)height);
            frames[f] = Sprite.Create(texture, new Rect(0, 0, width, height), pivot, 1f / _pixel);
        }

        return frames;
    }

    /// <summary>R = 밝기, G = 높이(0~1), B = 테두리 여부, A = 그릴 칸 으로 적는다. 테두리가 벽보다 우선한다.</summary>
    private static void Put(Color32[] pixels, int width, int height, int x, int y, float brightness, float height01, bool rim)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        int index = y * width + x;
        Color32 current = pixels[index];
        if (current.a > 0 && (current.b > 0 && !rim || current.r >= brightness * 255f && current.b == (rim ? 255 : 0)))
        {
            return;
        }

        pixels[index] = new Color32((byte)Mathf.RoundToInt(Mathf.Clamp01(brightness) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(height01) * 255f), (byte)(rim ? 255 : 0), 255);
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        bool active = IsMissionActive();
        bool inside = active && _zone != null && _zone.IsPlayerInside;
        float target = inside ? 1f : 0f;
        _weight = Mathf.MoveTowards(_weight, target, dt / (target > _weight ? _riseDuration : _fallDuration));
        // 링을 어둡게 하는 것은 임무 중에만 (시작 전, 끝난 뒤에는 원래 색)
        _activeTint = Mathf.MoveTowards(_activeTint, active ? 1f : 0f, dt / 0.5f);
        _frameTime += dt * _framesPerSecond;
        Apply(_weight);
        EmitMotes(dt);
    }

    /// <summary>거점 점령 임무가 시작됐고 아직 끝나지 않았는지</summary>
    private bool IsMissionActive()
    {
        if (_manager == null)
        {
            // Instance는 없으면 새로 만들기 때문에 씬에 있는 것만 찾는다.
            _manager = FindAnyObjectByType<MissionManager>();
            if (_manager == null)
            {
                return false;
            }
        }

        MissionBase mission = _manager.GetCurrentMission();
        return _manager.IsMissionRunning && mission != null && mission.Type == MissionType.CaptureZone &&
               !mission.IsCompleted && !mission.IsFailed;
    }

    private void Apply(float weight)
    {
        float reveal = weight * weight * (3f - 2f * weight);
        // 테두리는 솟기 시작하자마자 또렷하게, 가라앉을 때는 끝까지 남았다가 사라진다.
        float alpha = Mathf.Clamp01(weight * 5f);
        int frame = (int)_frameTime % FrameCount;
        int layer = _body != null ? _body.sortingLayerID : 0;
        Vector3 center = transform.position;
        float worldRy = _radius.y * Mathf.Abs(transform.lossyScale.y);

        // 앞뒤 순서는 씬의 YSort(10000 - 바닥 y * 100)와 같은 기준 : 링 앞쪽 호는 그보다 위(안)에 선 플레이어보다 앞에,
        // 뒤쪽 호는 링 안에 선 플레이어보다 뒤에 그려진다.
        Setup(_front, _frontFrames[frame], alpha * _frontAlpha, reveal, layer, SortOrder(center.y - worldRy));
        Setup(_back, _backFrames[frame], alpha * _backAlpha, reveal, layer, SortOrder(center.y + worldRy));
        _frontMotes.SetSorting(layer, SortOrder(center.y - worldRy) + 1);
        _backMotes.SetSorting(layer, SortOrder(center.y + worldRy) + 1);

        if (_body != null)
        {
            // 임무 중 밖에 있으면 어둡고 차갑게, 안에 들어오면(결계가 솟는 만큼) 원래 색으로
            float dark = _activeTint * (1f - reveal);
            _body.color = Color.Lerp(_bodyColor, _bodyColor * _outsideTint, dark);
        }
    }

    private void Setup(SpriteRenderer renderer, Sprite sprite, float alpha, float reveal, int layer, int order)
    {
        renderer.sprite = sprite;
        Color color = Color.white;
        color.a = alpha;
        renderer.color = color;
        renderer.enabled = alpha > 0.001f;
        renderer.sortingLayerID = layer;
        renderer.sortingOrder = order;
        renderer.GetPropertyBlock(_block);
        _block.SetFloat(RevealId, reveal);
        _block.SetColor(WallColorId, _wallColor);
        _block.SetColor(RimColorId, _rimColor);
        renderer.SetPropertyBlock(_block);
    }

    private void EmitMotes(float dt)
    {
        if (_weight <= 0.05f)
        {
            return;
        }

        _moteTimer += dt * _moteRate * _weight;
        float reveal = _weight * _weight * (3f - 2f * _weight);
        float rise = _height * Mathf.Abs(transform.lossyScale.y) * 1.1f * Mathf.Max(0.2f, reveal);
        while (_moteTimer >= 1f)
        {
            _moteTimer -= 1f;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 local = new(Mathf.Cos(angle) * _radius.x, Mathf.Sin(angle) * _radius.y);
            Vector3 origin = transform.TransformPoint(local);
            float life = Random.Range(0.8f, 1.2f);
            PixelParticles target = Mathf.Sin(angle) < 0f ? _frontMotes : _backMotes;
            target.Emit(origin, new Vector3(0f, rise / life, 0f), 0f, life, new Color(1f, 0.97f, 0.85f, 1f),
                _moteSize * (Random.value < 0.4f ? 2f : 1f), null, true);
        }
    }

    private static int SortOrder(float groundY)
    {
        return Mathf.Clamp(10000 - Mathf.RoundToInt(groundY * 100f), -32768, 32767);
    }

    private void OnDisable()
    {
        if (_body != null)
        {
            _body.color = _bodyColor;
        }
    }

    private void OnDestroy()
    {
        if (_frontMotes != null)
        {
            _frontMotes.DestroyWhenEmpty = true;
        }

        if (_backMotes != null)
        {
            _backMotes.DestroyWhenEmpty = true;
        }
    }
}
