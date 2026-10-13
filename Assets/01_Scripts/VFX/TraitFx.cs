using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미션 보상 특성(Trait)의 발동 연출을 한곳에서 재생한다.
/// - 프리팹(Resources/TraitFx/TraitFx)을 처음 쓸 때 씬에 하나 만든다. 씬에 미리 두어도 된다.
/// - 그림은 꺼 둔 자식 SpriteRenderer(견본)에 할당한 이미지를 쓴다. 견본을 복제해 조각(Piece)으로 띄우고, 끝나면 다시 모아 쓴다.
///   기존 스킬 연출 그림(정화, 보호막, 연쇄, 길잡이 빛)과 원소 마법 텍스처(충격파, 혜성 꼬리, 빛줄기)를 재사용한다.
/// - 조각은 대상(Transform)을 따라가거나 제자리에 남는다. 대상이 사라지면 짧게 사라진다.
/// - 계속 붙어 있어야 하는 연출(낙인, 반격 대기)은 Handle을 돌려준다. Release로 거둔다.
/// - 땅에 깔리는 연출(발밑 고리, 발자국)은 바닥 바로 위, 나머지는 캐릭터보다 앞에 그린다.
/// 게임 시간으로 흐른다. (일시정지하면 멈춘다)
/// </summary>
[DisallowMultipleComponent]
public class TraitFx : MonoBehaviour
{
    private const string ResourcePath = "TraitFx/TraitFx";
    private const int TopSortingOrder = 32000;
    private const int GroundSortingOrder = -10;

    private static TraitFx s_instance;
    private static bool s_loadFailed;

    /// <summary>씬의 연출 관리자. 없으면 Resources에서 만든다. 프리팹이 없으면 null.</summary>
    public static TraitFx Instance
    {
        get
        {
            if (s_instance != null)
            {
                return s_instance;
            }

            s_instance = FindAnyObjectByType<TraitFx>();
            if (s_instance == null && !s_loadFailed)
            {
                TraitFx prefab = Resources.Load<TraitFx>(ResourcePath);
                if (prefab == null)
                {
                    s_loadFailed = true;
                    Debug.LogWarning($"[TraitFx] Resources/{ResourcePath} 프리팹이 없어 특성 연출을 재생하지 않습니다.");
                    return null;
                }

                s_instance = Instantiate(prefab);
                s_instance.name = prefab.name;
            }

            return s_instance;
        }
    }

    [Header("그림 견본 (꺼 둔 자식)")]
    [Tooltip("납작한 고리 (ChainFx_Ring)")]
    [SerializeField] private SpriteRenderer _ring;
    [Tooltip("번쩍임 (ChainFx_Flash)")]
    [SerializeField] private SpriteRenderer _flash;
    [Tooltip("네 갈래 별 (Cleanse_Star)")]
    [SerializeField] private SpriteRenderer _star;
    [Tooltip("작은 별 (Cleanse_Sparkle)")]
    [SerializeField] private SpriteRenderer _sparkle;
    [Tooltip("빛 알갱이 (Cleanse_Mote)")]
    [SerializeField] private SpriteRenderer _mote;
    [Tooltip("은은한 빛 (Cleanse_Glow)")]
    [SerializeField] private SpriteRenderer _glow;
    [Tooltip("초승달 (Cleanse_Crescent)")]
    [SerializeField] private SpriteRenderer _crescent;
    [Tooltip("장막 (TraitFx_Veil : Barrier_1을 흰 빛으로 바꾼 그림)")]
    [SerializeField] private SpriteRenderer _veil;
    [Tooltip("보호막 깨짐 고리 (Barrier_BreakRing)")]
    [SerializeField] private SpriteRenderer _breakRing;
    [Tooltip("보호막 물결 (Barrier_Ripple)")]
    [SerializeField] private SpriteRenderer _ripple;
    [Tooltip("보호막 파편 (Barrier_Shard)")]
    [SerializeField] private SpriteRenderer _shard;
    [Tooltip("작은 빛 (GuideFly_Glow)")]
    [SerializeField] private SpriteRenderer _fly;
    [Tooltip("가시 충격파 (TraitFx_Shockwave)")]
    [SerializeField] private SpriteRenderer _shockwave;
    [Tooltip("혜성 꼬리. 머리가 오른쪽, 기준점이 머리 (TraitFx_Comet)")]
    [SerializeField] private SpriteRenderer _comet;
    [Tooltip("빛줄기. 왼쪽이 굵다 (TraitFx_Streak)")]
    [SerializeField] private SpriteRenderer _streak;
    [Tooltip("빛기둥. 기준점이 가운데 (MissionFx_Pillar)")]
    [SerializeField] private SpriteRenderer _pillar;
    [Tooltip("속도선. 오른쪽이 밝다 (GuideFly_Tail)")]
    [SerializeField] private SpriteRenderer _tail;

    [Header("색")]
    [Tooltip("공격 특성 (메아리, 반격)")]
    [SerializeField] private Color _attackColor = new(1f, 0.55f, 0.18f, 1f);
    [Tooltip("사냥의 낙인")]
    [SerializeField] private Color _markColor = new(1f, 0.74f, 0.32f, 1f);
    [Tooltip("대시 특성 (장막, 돌파)")]
    [SerializeField] private Color _dashColor = new(0.72f, 0.93f, 1f, 1f);
    [Tooltip("보호막 특성")]
    [SerializeField] private Color _wardColor = new(1f, 0.9f, 0.62f, 1f);
    [Tooltip("정화 특성")]
    [SerializeField] private Color _cleanseColor = new(0.86f, 0.95f, 1f, 1f);
    [Tooltip("회복")]
    [SerializeField] private Color _healColor = new(0.55f, 1f, 0.75f, 1f);
    [Tooltip("막아 낸 피격의 번쩍임")]
    [SerializeField] private Color _blockColor = new(1f, 0.5f, 0.35f, 1f);

    [Header("위치 (대상 Transform 기준)")]
    [SerializeField] private Vector2 _playerBodyOffset = Vector2.zero;
    [SerializeField] private Vector2 _playerFeetOffset = new(0f, -0.62f);
    [SerializeField] private Vector2 _enemyBodyOffset = new(0f, 0.1f);
    [SerializeField] private Vector2 _enemyFeetOffset = new(0f, -0.4f);
    [SerializeField] private Vector2 _enemyHeadOffset = new(0f, 0.75f);
    [Tooltip("따라가던 대상이 사라지면 이 시간 동안 사라진다")]
    [SerializeField, Min(0.01f)] private float _lostFade = 0.12f;

    [Header("attack_04 메아리 탄환")]
    [Tooltip("혜성 꼬리 크기 (길이, 굵기)")]
    [SerializeField] private Vector2 _echoTrailSize = new(1.5f, 0.4f);
    [SerializeField, Range(0f, 1f)] private float _echoTrailAlpha = 1f;
    [SerializeField, Min(0f)] private float _echoGlowSize = 0.7f;
    [Tooltip("총구 고리 높이 (첫 고리 기준)")]
    [SerializeField, Min(0f)] private float _echoRingSize = 0.5f;
    [Tooltip("총구에서 진행 방향으로 번지는 고리 수")]
    [SerializeField, Min(1)] private int _echoRingCount = 3;
    [Tooltip("고리 사이 거리")]
    [SerializeField, Min(0f)] private float _echoRingSpacing = 0.18f;

    [Header("attack_05 사냥의 낙인")]
    [Tooltip("발밑 고리 너비")]
    [SerializeField, Min(0f)] private float _markRingWidth = 0.9f;
    [Tooltip("머리 위 별 크기")]
    [SerializeField, Min(0f)] private float _markStarSize = 0.28f;
    [Tooltip("낙인 적 피격 번쩍임 크기")]
    [SerializeField, Min(0f)] private float _markHitSize = 0.8f;

    [Header("dash_03 잔상의 장막")]
    [SerializeField, Min(0f)] private float _veilSize = 1.7f;
    [SerializeField, Range(0f, 1f)] private float _veilAlpha = 0.55f;

    [Header("dash_04 돌파의 각인 (돌진)")]
    [Tooltip("속도선 길이 (최소, 최대)")]
    [SerializeField] private Vector2 _dashLineLength = new(0.7f, 1.3f);
    [SerializeField, Min(0f)] private float _dashLineWidth = 0.12f;
    [SerializeField, Range(0f, 1f)] private float _dashLineAlpha = 0.85f;
    [Tooltip("속도선이 몸 둘레로 퍼지는 폭 (위아래)")]
    [SerializeField, Min(0f)] private float _dashLineSpread = 0.55f;
    [SerializeField, Min(0.01f)] private float _dashLineDuration = 0.22f;

    [Header("dash_05 반격의 발걸음")]
    [SerializeField, Min(0f)] private float _counterGlowSize = 0.6f;
    [SerializeField, Min(0f)] private float _counterStarSize = 0.42f;
    [Tooltip("강화탄 혜성 꼬리 크기 (길이, 굵기)")]
    [SerializeField] private Vector2 _counterTrailSize = new(1.7f, 0.46f);

    [Header("shield_04 파열의 고리")]
    [Tooltip("가시 충격파가 퍼지는 크기 (지름)")]
    [SerializeField, Min(0f)] private float _ruptureSize = 4.5f;
    [Tooltip("충격파의 세로 배율 (바닥에 비스듬히 눕힌다)")]
    [SerializeField, Range(0.1f, 1f)] private float _ruptureFlatten = 0.78f;
    [SerializeField, Min(0.01f)] private float _ruptureDuration = 0.5f;
    [SerializeField, Min(0)] private int _ruptureShardCount = 14;

    [Header("clease_01 정화의 잔향")]
    [Tooltip("초승달이 도는 반지름")]
    [SerializeField, Min(0f)] private float _immuneRadius = 0.45f;
    [SerializeField, Min(0f)] private float _immuneCrescentSize = 0.55f;

    [Header("clease_02 가벼워진 몸")]
    [Tooltip("바람 줄기 길이 (최소, 최대)")]
    [SerializeField] private Vector2 _swiftStreakLength = new(0.6f, 1f);
    [SerializeField, Range(0f, 1f)] private float _swiftAlpha = 0.55f;

    [Header("clease_03 맑은 숨결")]
    [SerializeField, Min(0)] private int _healParticleCount = 16;

    [Header("clease_05 정화의 파동")]
    [Tooltip("미션 클리어 정화 파동 프리팹 (08_VFX/PurificationWave). 반경에 맞게 줄여 빠르게 재생한다")]
    [SerializeField] private PurificationWave _novaWave;
    [Tooltip("정화 파동 클립이 퍼지는 최대 반지름 (PurificationWave_Wave 클립의 _radius 끝값)")]
    [SerializeField, Min(0.01f)] private float _novaWaveClipRadius = 18f;
    [Tooltip("정화 파동 클립 길이 (초)")]
    [SerializeField, Min(0.01f)] private float _novaWaveClipLength = 1.64f;
    [Tooltip("파동이 반경 끝까지 퍼지는 시간")]
    [SerializeField, Min(0.01f)] private float _novaTravelTime = 0.45f;
    [Tooltip("맞은 적에 내려오는 빛기둥 크기 (너비, 높이)")]
    [SerializeField] private Vector2 _novaPillarSize = new(0.4f, 1.8f);
    [SerializeField, Min(0.01f)] private float _novaPillarDuration = 0.5f;

    [Header("shield_02 지속의 문양")]
    [SerializeField, Min(0f)] private float _wardRingSize = 1.5f;
    [SerializeField, Min(0)] private int _wardShardCount = 12;

    [Header("shield_05 수호자의 걸음")]
    [Tooltip("발자국 크기 (가로, 세로)")]
    [SerializeField] private Vector2 _stepSize = new(0.45f, 0.17f);
    [SerializeField, Min(0.01f)] private float _stepDuration = 0.9f;

    /// <summary>계속 붙어 있는 연출 묶음. Release로 거둔다.</summary>
    public sealed class Handle
    {
        internal readonly List<Piece> Pieces = new();
        internal TraitFx Owner;

        public bool IsAlive
        {
            get
            {
                foreach (Piece piece in Pieces)
                {
                    if (piece.IsAlive)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>fade초 동안 사라지게 한다.</summary>
        public void Release(float fade = 0.15f)
        {
            if (Owner != null)
            {
                foreach (Piece piece in Pieces)
                {
                    piece.Release(fade);
                }
            }

            Pieces.Clear();
        }

        internal Piece Add(Piece piece)
        {
            Pieces.Add(piece);
            return piece;
        }
    }

    /// <summary>화면에 뜬 그림 한 장. 만든 뒤 이어서 부르는 함수로 움직임을 정한다.</summary>
    internal sealed class Piece
    {
        public SpriteRenderer Template;
        public SpriteRenderer Renderer;
        public bool IsAlive;
        public Vector2 SpriteSize = Vector2.one;

        public Transform Follow;
        public bool HasFollow;
        public bool FollowRotation;
        public Vector3 LastFollowPosition;
        public Vector3 Position;
        public Vector2 Velocity;
        public float Drag;
        public float Angle;
        public float Spin;
        public float OrbitRadius;
        public float OrbitSpeed;
        public float OrbitAngle;
        public float OrbitFlatten = 1f;
        public bool OrbitAlign;
        public Vector2 SizeFrom = Vector2.one;
        public Vector2 SizeTo = Vector2.one;
        public float SizeTime;
        public Color Color = Color.white;
        public float Delay;
        public float Duration = 0.3f;
        public float FadeIn;
        public float FadeOut = 0.2f;
        public bool Loop;
        public float MaxLife;
        public float PulseSpeed;
        public float PulseAmount;
        public bool Ground;
        public int OrderOffset;
        public float Age;

        public Piece At(Vector3 position)
        {
            HasFollow = false;
            Follow = null;
            Position = position;
            return this;
        }

        public Piece On(Transform target, Vector2 offset, bool followRotation = false)
        {
            if (target == null)
            {
                return At(offset);
            }

            Follow = target;
            HasFollow = true;
            FollowRotation = followRotation;
            LastFollowPosition = target.position;
            Position = offset;
            return this;
        }

        public Piece Size(float width) => Size(width, width, 0f);

        public Piece Size(float from, float to, float time = -1f) =>
            Size(new Vector2(from, 0f), new Vector2(to, 0f), time);

        public Piece Size(Vector2 size) => Size(size, size, 0f);

        /// <summary>월드 크기 (가로, 세로). 세로가 0이면 그림 비율을 따른다. time이 음수면 수명 동안 바뀐다.</summary>
        public Piece Size(Vector2 from, Vector2 to, float time = -1f)
        {
            SizeFrom = from;
            SizeTo = to;
            SizeTime = time;
            return this;
        }

        public Piece Tint(Color color, float alpha = 1f)
        {
            Color = new Color(color.r, color.g, color.b, color.a * alpha);
            return this;
        }

        public Piece Life(float duration, float fadeIn, float fadeOut)
        {
            Duration = Mathf.Max(0.01f, duration);
            FadeIn = fadeIn;
            FadeOut = fadeOut;
            Loop = false;
            return this;
        }

        /// <summary>Release하거나 대상이 사라질 때까지 남는다. maxLife가 0보다 크면 그 시간 뒤에 스스로 사라진다.</summary>
        public Piece Forever(float fadeIn, float maxLife = 0f)
        {
            Loop = true;
            FadeIn = fadeIn;
            MaxLife = maxLife;
            return this;
        }

        public Piece Wait(float delay)
        {
            Delay = Mathf.Max(0f, delay);
            return this;
        }

        public Piece Turn(float angle, float spin = 0f)
        {
            Angle = angle;
            Spin = spin;
            return this;
        }

        public Piece Move(Vector2 velocity, float drag = 0f)
        {
            Velocity = velocity;
            Drag = drag;
            return this;
        }

        public Piece Orbit(float radius, float speed, float angle, float flatten = 1f, bool align = true)
        {
            OrbitRadius = radius;
            OrbitSpeed = speed;
            OrbitAngle = angle;
            OrbitFlatten = flatten;
            OrbitAlign = align;
            return this;
        }

        public Piece Pulse(float speed, float amount)
        {
            PulseSpeed = speed;
            PulseAmount = amount;
            return this;
        }

        public Piece OnGround(int orderOffset = 0)
        {
            Ground = true;
            OrderOffset = orderOffset;
            return this;
        }

        public Piece Order(int orderOffset)
        {
            OrderOffset = orderOffset;
            return this;
        }

        internal void Release(float fade)
        {
            if (!IsAlive)
            {
                return;
            }

            float elapsed = Age - Delay;
            if (elapsed <= 0f)
            {
                Duration = 0f;
                Loop = false;
                return;
            }

            Loop = false;
            FadeOut = Mathf.Max(0.01f, fade);
            Duration = elapsed + FadeOut;
        }
    }

    private readonly List<Piece> _pieces = new();
    private readonly Dictionary<SpriteRenderer, Stack<SpriteRenderer>> _pools = new();

    private void Awake()
    {
        if (s_instance == null)
        {
            s_instance = this;
        }
        else if (s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        foreach (SpriteRenderer template in new[]
                 {
                     _ring, _flash, _star, _sparkle, _mote, _glow, _crescent, _veil, _breakRing, _ripple, _shard, _fly,
                     _shockwave, _comet, _streak, _pillar, _tail
                 })
        {
            if (template != null)
            {
                template.gameObject.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    // ───────────────────────── 공격 ─────────────────────────

    /// <summary>attack_04 : 메아리 탄환. 총구에서 진행 방향으로 고리가 여러 겹 번지고, 탄환에 금빛 혜성 꼬리가 붙는다.</summary>
    public void PlayEcho(Transform muzzle, IReadOnlyList<Transform> bullets)
    {
        if (muzzle != null)
        {
            float angle = FirstAngle(bullets, muzzle.eulerAngles.z);
            Vector3 forward = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
            for (int i = 0; i < _echoRingCount; i++)
            {
                float height = _echoRingSize * (1f + 0.35f * i);
                Spawn(_ring).At(muzzle.position + forward * (_echoRingSpacing * i)).Turn(angle + 90f)
                    .Size(new Vector2(height * 0.4f, 0f), new Vector2(height, 0f)).Tint(_attackColor, 0.9f - 0.2f * i)
                    .Wait(0.04f * i).Life(0.3f, 0f, 0.22f);
            }

            Spawn(_flash).At(muzzle.position).Size(0.3f, 0.6f).Tint(_attackColor).Life(0.15f, 0f, 0.12f);
        }

        TrailBullets(bullets, _echoTrailSize, _echoTrailAlpha, _echoGlowSize);
        if (bullets == null)
        {
            return;
        }

        foreach (Transform bullet in bullets)
        {
            if (bullet != null)
            {
                Spawn(_sparkle).On(bullet, Vector2.zero).Turn(0f, 180f).Size(0.35f).Pulse(4f, 0.2f).Forever(0.05f, 3f);
            }
        }
    }

    /// <summary>attack_05 : 낙인 표식. 발밑 고리와 머리 위 별이 적을 따라다닌다.</summary>
    public Handle AttachMark(Transform enemy)
    {
        Handle handle = NewHandle();
        if (enemy == null)
        {
            return handle;
        }

        handle.Add(Spawn(_ring).On(enemy, _enemyFeetOffset).OnGround()
            .Size(_markRingWidth * 1.5f, _markRingWidth, 0.25f).Tint(_markColor, 0.85f).Pulse(1.2f, 0.08f)
            .Forever(0.1f));
        handle.Add(Spawn(_glow).On(enemy, _enemyHeadOffset).Size(_markStarSize * 1.5f).Tint(_markColor, 0.5f)
            .Pulse(1.2f, 0.15f).Forever(0.1f));
        handle.Add(Spawn(_star).On(enemy, _enemyHeadOffset).Size(_markStarSize * 2f, _markStarSize, 0.25f)
            .Tint(_markColor).Pulse(1.2f, 0.1f).Forever(0.1f));
        return handle;
    }

    /// <summary>attack_05 : 낙인이 찍힌 적을 맞혔을 때의 번쩍임.</summary>
    public void PlayMarkHit(Vector3 position)
    {
        Spawn(_flash).At(position).Size(_markHitSize * 0.6f, _markHitSize).Life(0.18f, 0f, 0.14f);
        Spawn(_star).At(position).Turn(0f, 180f).Size(_markHitSize * 0.4f, _markHitSize * 0.75f).Tint(_markColor)
            .Life(0.25f, 0f, 0.18f);
    }

    // ───────────────────────── 대시 ─────────────────────────

    /// <summary>dash_03 : 피해 감소 동안 보호막 그림이 옅은 장막으로 몸을 감싼다.</summary>
    public void PlayVeil(Transform player, float duration)
    {
        Spawn(_veil).On(player, _playerBodyOffset).Size(_veilSize * 0.8f, _veilSize, 0.15f).Tint(_dashColor, _veilAlpha)
            .Life(duration, 0.08f, 0.3f);
        Spawn(_ripple).On(player, _playerBodyOffset).Size(_veilSize * 0.6f, _veilSize * 1.3f).Tint(_dashColor)
            .Life(0.35f, 0f, 0.3f);
    }

    /// <summary>dash_03 : 장막이 피격을 받아 냈을 때 물결이 인다.</summary>
    public void PlayVeilAbsorb(Transform player)
    {
        if (player == null)
        {
            return;
        }

        Vector2 side = new Vector2(Random.Range(-1f, 1f), Random.Range(0.2f, 1f)).normalized * (_veilSize * 0.35f);
        Vector2 offset = _playerBodyOffset + side;
        Spawn(_ripple).On(player, offset).Size(0.3f, 0.8f).Tint(_dashColor).Life(0.3f, 0f, 0.25f);
        Spawn(_flash).On(player, offset).Size(0.45f).Tint(_blockColor, 0.7f).Life(0.2f, 0f, 0.15f);
    }

    /// <summary>dash_04 : 돌진하는 동안 몸 뒤로 바람빛 속도선과 빛 알갱이가 흐른다. (대시 잔상과 함께 보인다)</summary>
    public void SpawnDashLine(Transform player, Vector2 moveDirection)
    {
        if (player == null || moveDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector2 direction = moveDirection.normalized;
        Vector2 side = new(-direction.y, direction.x);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector3 body = player.position + (Vector3)_playerBodyOffset;

        // 속도선 그림은 오른쪽이 밝다. 밝은 쪽을 진행 방향(몸 쪽)으로 둔다.
        float length = Random.Range(_dashLineLength.x, Mathf.Max(_dashLineLength.x, _dashLineLength.y));
        Vector3 position = body - (Vector3)(direction * (length * 0.5f + Random.Range(0.1f, 0.5f))) +
                           (Vector3)(side * Random.Range(-_dashLineSpread, _dashLineSpread));
        Spawn(_tail).At(position).Turn(angle).Size(new Vector2(length, _dashLineWidth))
            .Tint(_dashColor, _dashLineAlpha * Random.Range(0.6f, 1f)).Move(-direction * 1.5f, 4f)
            .Life(_dashLineDuration, 0f, _dashLineDuration * 0.8f);

        Vector3 motePosition = body - (Vector3)(direction * Random.Range(0.2f, 0.8f)) +
                               (Vector3)(side * Random.Range(-_dashLineSpread, _dashLineSpread));
        Spawn(_mote).At(motePosition).Move(-direction * 0.8f + Vector2.up * 0.2f).Size(Random.Range(0.06f, 0.1f))
            .Tint(_dashColor, 0.85f).Life(0.4f, 0f, 0.3f);
    }

    /// <summary>dash_04 : 돌진에 맞은 적이 바람빛으로 짧게 번쩍인다. position : 적 Transform 위치</summary>
    public void PlayDashHit(Vector3 position)
    {
        Vector3 body = position + (Vector3)_enemyBodyOffset;
        Spawn(_flash).At(body).Size(0.5f, 0.9f).Tint(_dashColor, 0.85f).Life(0.2f, 0f, 0.15f);
        Spawn(_sparkle).At(body).Turn(0f, 180f).Size(0.25f, 0.45f).Life(0.25f, 0f, 0.2f);
    }

    /// <summary>dash_05 : 반격 대기. 총구에 별빛이 맺힌다. 쏘거나 시간이 지나면 Release한다.</summary>
    public Handle PlayCounterReady(Transform muzzle)
    {
        Handle handle = NewHandle();
        if (muzzle == null)
        {
            return handle;
        }

        handle.Add(Spawn(_glow).On(muzzle, Vector2.zero).Size(_counterGlowSize).Tint(_attackColor, 0.9f).Pulse(2f, 0.15f)
            .Forever(0.08f));
        handle.Add(Spawn(_star).On(muzzle, Vector2.zero).Turn(0f, 120f).Size(_counterStarSize * 1.8f, _counterStarSize, 0.2f)
            .Pulse(2f, 0.1f).Forever(0.05f));
        for (int i = 0; i < 4; i++)
        {
            handle.Add(Spawn(_mote).On(muzzle, Vector2.zero).Orbit(_counterGlowSize * 0.4f, 240f, i * 90f)
                .Size(0.08f).Tint(_attackColor).Forever(0.1f));
        }

        Spawn(_sparkle).At(muzzle.position).Size(0.2f, 0.5f).Life(0.25f, 0f, 0.2f);
        return handle;
    }

    /// <summary>dash_05 : 강화된 한 발. 총구가 번쩍이고 탄환에 굵은 혜성 꼬리가 붙는다.</summary>
    public void PlayCounterShot(Transform muzzle, IReadOnlyList<Transform> bullets)
    {
        if (muzzle != null)
        {
            Spawn(_flash).At(muzzle.position).Size(0.5f, 0.8f).Tint(_attackColor).Life(0.15f, 0f, 0.12f);
            Spawn(_star).At(muzzle.position).Turn(15f, 200f).Size(0.4f, 0.8f).Life(0.25f, 0f, 0.2f);
        }

        TrailBullets(bullets, _counterTrailSize, 1f, _counterGlowSize * 1.2f);
    }

    // ───────────────────────── 보호막 ─────────────────────────

    /// <summary>shield_04 : 보호막이 깨질 때 가시 충격파가 밀려 나가고 파편이 튄다.</summary>
    public void PlayRupture(Transform player)
    {
        if (player == null)
        {
            return;
        }

        Vector3 center = player.position + (Vector3)_playerBodyOffset;
        float size = _ruptureSize;
        float flatten = _ruptureFlatten;

        Spawn(_flash).At(center).Size(0.6f, size * 0.4f).Tint(Color.white, 0.9f).Life(0.18f, 0f, 0.15f);
        Spawn(_shockwave).At(center).Turn(Random.Range(0f, 360f), 40f)
            .Size(new Vector2(size * 0.15f, size * 0.15f * flatten), new Vector2(size, size * flatten))
            .Tint(_wardColor).Life(_ruptureDuration, 0f, _ruptureDuration * 0.6f);
        Spawn(_shockwave).At(center).Turn(Random.Range(0f, 360f), -30f)
            .Size(new Vector2(size * 0.1f, size * 0.1f * flatten), new Vector2(size * 0.7f, size * 0.7f * flatten))
            .Tint(Color.white, 0.55f).Wait(0.05f).Life(_ruptureDuration * 0.85f, 0f, _ruptureDuration * 0.5f);
        Spawn(_breakRing).At(center).Size(1f, size * 0.55f).Tint(_wardColor, 0.6f)
            .Life(_ruptureDuration * 0.7f, 0f, _ruptureDuration * 0.5f);

        for (int i = 0; i < _ruptureShardCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle) * flatten);
            float life = Random.Range(0.45f, 0.7f);
            Spawn(_shard).At(center + (Vector3)(direction * 0.3f)).Move(direction * Random.Range(5f, 9f), 5f)
                .Turn(Random.Range(0f, 360f), Random.Range(-360f, 360f)).Size(Random.Range(0.1f, 0.18f))
                .Tint(_wardColor).Life(life, 0f, life * 0.5f);
        }
    }

    /// <summary>shield_02 : 보호막이 막아 낸 뒤 무적 동안 파편이 몸 둘레를 맴돈다.</summary>
    public void PlayWardLinger(Transform player, float duration)
    {
        if (player == null || duration <= 0f)
        {
            return;
        }

        Spawn(_breakRing).On(player, _playerBodyOffset).Size(_wardRingSize * 0.8f, _wardRingSize, 0.2f)
            .Tint(_wardColor, 0.5f).Pulse(3f, 0.08f).Life(duration, 0.05f, 0.25f);
        for (int i = 0; i < _wardShardCount; i++)
        {
            float speed = Random.Range(60f, 120f) * (Random.value < 0.5f ? -1f : 1f);
            Spawn(_shard).On(player, _playerBodyOffset)
                .Orbit(Random.Range(0.45f, 0.8f) * _wardRingSize / 1.5f, speed, Random.Range(0f, 360f), 0.9f, false)
                .Turn(Random.Range(0f, 360f), Random.Range(-180f, 180f)).Size(Random.Range(0.1f, 0.16f))
                .Tint(_wardColor, 0.9f).Life(duration, 0.1f, 0.3f);
        }
    }

    /// <summary>shield_05 : 보호막이 있는 동안 걸으면 금빛 발자국이 남는다. feet : 발 위치 (월드)</summary>
    public void SpawnWardStep(Vector3 feet)
    {
        Spawn(_ripple).At(feet).OnGround().Size(_stepSize * 0.7f, _stepSize, 0.2f).Tint(_wardColor)
            .Life(_stepDuration, 0f, _stepDuration * 0.75f);
        Spawn(_fly).At(feet).OnGround(1).Size(_stepSize.x * 0.55f).Tint(_wardColor, 0.9f)
            .Life(_stepDuration, 0f, _stepDuration * 0.75f);
    }

    /// <summary>플레이어 발 위치 (월드)</summary>
    public Vector3 PlayerFeet(Transform player) =>
        player != null ? player.position + (Vector3)_playerFeetOffset : Vector3.zero;

    // ───────────────────────── 정화 ─────────────────────────

    /// <summary>clease_01 : 면역 동안 정화 초승달이 옅게 몸을 돈다.</summary>
    public void PlayImmunity(Transform player, float duration)
    {
        if (player == null || duration <= 0f)
        {
            return;
        }

        float[] alphas = { 0.55f, 0.4f, 0.28f };
        for (int i = 0; i < alphas.Length; i++)
        {
            Spawn(_crescent).On(player, _playerBodyOffset).Orbit(_immuneRadius, -220f, i * 120f, 0.35f)
                .Size(_immuneCrescentSize).Tint(_cleanseColor, alphas[i]).Life(duration, 0.1f, 0.35f);
        }

        Spawn(_glow).On(player, _playerBodyOffset).Size(1.2f).Tint(_cleanseColor, 0.22f).Life(duration, 0.1f, 0.35f);
        for (int i = 0; i < 7; i++)
        {
            Vector2 offset = _playerBodyOffset + new Vector2(Random.Range(-0.35f, 0.35f), Random.Range(-0.4f, 0.2f));
            float life = Random.Range(0.8f, 1.1f);
            Spawn(_mote).On(player, offset).Move(new Vector2(0f, Random.Range(0.5f, 0.9f))).Size(Random.Range(0.08f, 0.12f))
                .Tint(_cleanseColor, 0.9f).Wait(Random.Range(0f, 0.3f)).Life(life, 0.1f, life * 0.5f);
        }
    }

    /// <summary>clease_01 : 면역이 상태이상을 막아 냈을 때 튕겨 내는 물결.</summary>
    public void PlayImmunityBlock(Transform player)
    {
        if (player == null)
        {
            return;
        }

        Vector2 offset = _playerBodyOffset + new Vector2(Random.value < 0.5f ? -0.45f : 0.45f, 0.25f);
        Spawn(_ripple).On(player, offset).Size(0.3f, 0.8f).Tint(_cleanseColor).Life(0.3f, 0f, 0.25f);
        Spawn(_sparkle).On(player, offset).Size(0.25f, 0.4f).Life(0.25f, 0f, 0.2f);
        Spawn(_flash).On(player, offset).Size(0.4f).Tint(_cleanseColor, 0.6f).Life(0.15f, 0f, 0.12f);
    }

    /// <summary>clease_02 : 이속 증가 동안 움직이면 뒤로 바람 줄기와 빛 알갱이가 흘러간다.</summary>
    public void SpawnSwiftTrail(Transform player, Vector2 moveDirection)
    {
        if (player == null || moveDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector2 direction = moveDirection.normalized;
        Vector2 behind = -direction;
        Vector2 side = new(-direction.y, direction.x);
        Vector3 body = player.position + (Vector3)_playerBodyOffset;

        // 빛줄기 그림은 왼쪽이 굵다. 굵은 쪽을 앞(플레이어 쪽)에 둔다.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180f;
        Vector3 streakPosition = body + (Vector3)(behind * 0.35f + side * Random.Range(-0.25f, 0.25f)) +
                                 new Vector3(0f, Random.Range(-0.3f, 0.2f), 0f);
        Spawn(_streak).At(streakPosition).Turn(angle)
            .Size(new Vector2(Random.Range(_swiftStreakLength.x, _swiftStreakLength.y), 0.1f))
            .Tint(_cleanseColor, _swiftAlpha).Move(behind * 1.2f, 3f).Life(0.25f, 0f, 0.2f);

        Vector3 motePosition = body + (Vector3)(behind * 0.3f + side * Random.Range(-0.3f, 0.3f)) +
                               new Vector3(0f, Random.Range(-0.4f, 0.3f), 0f);
        Spawn(_mote).At(motePosition).Move(behind * 0.6f + Vector2.up * 0.2f).Size(0.07f).Tint(_cleanseColor, 0.8f)
            .Life(0.45f, 0f, 0.3f);
    }

    /// <summary>clease_03 : 회복빛 입자가 몸에서 피어오른다.</summary>
    public void PlayHeal(Transform player)
    {
        if (player == null)
        {
            return;
        }

        Spawn(_glow).On(player, _playerBodyOffset).Size(1.1f).Tint(_healColor, 0.35f).Life(0.6f, 0.08f, 0.4f);
        Spawn(_ring).On(player, _playerFeetOffset).OnGround().Size(0.7f, 1.2f).Tint(_healColor, 0.7f).Life(0.5f, 0f, 0.35f);
        for (int i = 0; i < _healParticleCount; i++)
        {
            bool sparkle = i % 3 == 0;
            Vector2 offset = _playerBodyOffset + new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.55f, 0.25f));
            float life = Random.Range(0.6f, 1f);
            Spawn(sparkle ? _sparkle : _mote).On(player, offset).Move(new Vector2(0f, Random.Range(0.7f, 1.3f)))
                .Size(sparkle ? Random.Range(0.14f, 0.22f) : Random.Range(0.07f, 0.14f)).Tint(_healColor, 0.95f)
                .Wait(Random.Range(0f, 0.3f)).Life(life, 0.1f, life * 0.45f);
        }
    }

    /// <summary>
    /// clease_05 : 미션 클리어 정화 파동을 반경에 맞게 작게 재생하고, 파동이 닿은 적에게 빛기둥이 내려온다.
    /// hits : 맞은 적의 위치 (Transform 위치)
    /// </summary>
    public void PlayNova(Transform player, float radius, IReadOnlyList<Vector3> hits)
    {
        if (player == null)
        {
            return;
        }

        Vector3 center = player.position;
        if (_novaWave != null)
        {
            PurificationWave wave = Instantiate(_novaWave, center, Quaternion.identity);
            wave.transform.localScale = Vector3.one * (radius / _novaWaveClipRadius);
            if (wave.TryGetComponent(out Animator animator))
            {
                animator.speed = _novaWaveClipLength / _novaTravelTime;
            }
        }

        if (hits == null)
        {
            return;
        }

        float speed = radius / _novaTravelTime;
        foreach (Vector3 hit in hits)
        {
            float delay = Mathf.Min(_novaTravelTime, Vector2.Distance(center, hit) / Mathf.Max(0.01f, speed));
            Vector3 feet = hit + (Vector3)_enemyFeetOffset;
            float height = _novaPillarSize.y;
            Spawn(_pillar).At(feet + Vector3.up * (height * 0.5f))
                .Size(new Vector2(_novaPillarSize.x * 1.4f, height * 0.6f), new Vector2(_novaPillarSize.x * 0.6f, height),
                    _novaPillarDuration)
                .Tint(_cleanseColor).Wait(delay).Life(_novaPillarDuration, 0.05f, _novaPillarDuration * 0.7f);
            Spawn(_glow).At(feet).Size(0.5f, 1.1f).Tint(_cleanseColor, 0.6f).Wait(delay)
                .Life(_novaPillarDuration, 0f, _novaPillarDuration * 0.7f);
            for (int i = 0; i < 5; i++)
            {
                float life = Random.Range(0.5f, 0.8f);
                Spawn(_mote).At(feet + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0f, 0.6f), 0f))
                    .Move(new Vector2(0f, Random.Range(0.8f, 1.4f))).Size(Random.Range(0.06f, 0.1f))
                    .Tint(_cleanseColor, 0.9f).Wait(delay + Random.Range(0f, 0.15f)).Life(life, 0.05f, life * 0.5f);
            }
        }
    }

    // ───────────────────────── 내부 ─────────────────────────

    private void TrailBullets(IReadOnlyList<Transform> bullets, Vector2 size, float alpha, float glowSize)
    {
        if (bullets == null)
        {
            return;
        }

        foreach (Transform bullet in bullets)
        {
            if (bullet == null)
            {
                continue;
            }

            Spawn(_comet).On(bullet, Vector2.zero, true).Size(size).Tint(_attackColor, alpha).Forever(0.05f, 3f);
            Spawn(_glow).On(bullet, Vector2.zero).Size(glowSize).Tint(_attackColor, 0.5f).Forever(0.05f, 3f);
        }
    }

    private static float FirstAngle(IReadOnlyList<Transform> bullets, float fallback)
    {
        if (bullets != null)
        {
            foreach (Transform bullet in bullets)
            {
                if (bullet != null)
                {
                    return bullet.eulerAngles.z;
                }
            }
        }

        return fallback;
    }

    private Handle NewHandle() => new() { Owner = this };

    private Piece Spawn(SpriteRenderer template)
    {
        Piece piece = new() { Template = template, IsAlive = true };
        if (template == null)
        {
            // 견본이 비어 있으면 그리지 않는다. (체인 호출은 그대로 받는다)
            piece.IsAlive = false;
            return piece;
        }

        if (!_pools.TryGetValue(template, out Stack<SpriteRenderer> pool))
        {
            pool = new Stack<SpriteRenderer>();
            _pools[template] = pool;
        }

        SpriteRenderer renderer = pool.Count > 0 ? pool.Pop() : Instantiate(template, transform);
        renderer.gameObject.SetActive(false);
        piece.Renderer = renderer;
        piece.Color = template.color;
        if (template.sprite != null)
        {
            Vector2 size = template.sprite.bounds.size;
            piece.SpriteSize = new Vector2(Mathf.Max(0.0001f, size.x), Mathf.Max(0.0001f, size.y));
        }

        piece.SizeFrom = piece.SizeTo = new Vector2(piece.SpriteSize.x, 0f);
        _pieces.Add(piece);
        return piece;
    }

    private void Recycle(int index)
    {
        Piece piece = _pieces[index];
        _pieces.RemoveAt(index);
        piece.IsAlive = false;
        if (piece.Renderer == null)
        {
            return;
        }

        piece.Renderer.gameObject.SetActive(false);
        if (piece.Template != null && _pools.TryGetValue(piece.Template, out Stack<SpriteRenderer> pool))
        {
            pool.Push(piece.Renderer);
        }
    }

    private void LateUpdate()
    {
        float deltaTime = Time.deltaTime;
        for (int i = _pieces.Count - 1; i >= 0; i--)
        {
            Piece piece = _pieces[i];
            if (piece.Renderer == null)
            {
                _pieces.RemoveAt(i);
                piece.IsAlive = false;
                continue;
            }

            piece.Age += deltaTime;
            float time = piece.Age - piece.Delay;

            // 따라가던 대상이 사라지거나 꺼지면 그 자리에서 짧게 사라진다.
            if (piece.HasFollow)
            {
                if (piece.Follow == null || !piece.Follow.gameObject.activeInHierarchy)
                {
                    piece.HasFollow = false;
                    piece.Position = piece.LastFollowPosition + piece.Position;
                    piece.Release(_lostFade);
                    if (time <= 0f)
                    {
                        Recycle(i);
                        continue;
                    }
                }
                else
                {
                    piece.LastFollowPosition = piece.Follow.position;
                }
            }

            if (piece.Loop && piece.MaxLife > 0f && time >= piece.MaxLife)
            {
                piece.Release(_lostFade);
            }

            if (time < 0f)
            {
                continue;
            }

            if (!piece.Loop && time >= piece.Duration)
            {
                Recycle(i);
                continue;
            }

            // 움직임
            if (piece.Drag > 0f)
            {
                piece.Velocity /= 1f + piece.Drag * deltaTime;
            }

            piece.Position += (Vector3)(piece.Velocity * deltaTime);
            piece.Angle += piece.Spin * deltaTime;
            piece.OrbitAngle += piece.OrbitSpeed * deltaTime;

            Vector3 position = piece.HasFollow ? piece.LastFollowPosition + piece.Position : piece.Position;
            float angle = piece.Angle;
            if (piece.HasFollow && piece.FollowRotation)
            {
                angle += piece.Follow.eulerAngles.z;
            }

            if (piece.OrbitRadius > 0f)
            {
                float radians = piece.OrbitAngle * Mathf.Deg2Rad;
                position += new Vector3(Mathf.Cos(radians) * piece.OrbitRadius,
                    Mathf.Sin(radians) * piece.OrbitRadius * piece.OrbitFlatten, 0f);
                if (piece.OrbitAlign)
                {
                    angle += piece.OrbitAngle;
                }
            }

            // 크기
            float sizeTime = piece.SizeTime < 0f ? (piece.Loop ? 0f : piece.Duration) : piece.SizeTime;
            float k = sizeTime > 0f ? EaseOut(Mathf.Clamp01(time / sizeTime)) : 1f;
            Vector2 size = Vector2.Lerp(piece.SizeFrom, piece.SizeTo, k);
            float pulse = piece.PulseAmount > 0f ? Mathf.Sin(time * piece.PulseSpeed * Mathf.PI * 2f) : 0f;
            float width = size.x * (1f + piece.PulseAmount * pulse);
            float height = size.y > 0f
                ? size.y * (1f + piece.PulseAmount * pulse)
                : width * piece.SpriteSize.y / piece.SpriteSize.x;

            // 투명도
            float alpha = piece.FadeIn > 0f ? Mathf.Clamp01(time / piece.FadeIn) : 1f;
            if (!piece.Loop && piece.FadeOut > 0f)
            {
                alpha *= Mathf.Clamp01((piece.Duration - time) / piece.FadeOut);
            }

            alpha *= Mathf.Clamp01(1f + piece.PulseAmount * pulse);

            Transform pieceTransform = piece.Renderer.transform;
            pieceTransform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            pieceTransform.localScale = new Vector3(width / piece.SpriteSize.x, height / piece.SpriteSize.y, 1f);

            Color color = piece.Color;
            color.a *= alpha;
            piece.Renderer.color = color;
            piece.Renderer.sortingOrder = (piece.Ground ? GroundSortingOrder : TopSortingOrder) + piece.OrderOffset;

            if (!piece.Renderer.gameObject.activeSelf)
            {
                piece.Renderer.gameObject.SetActive(true);
            }
        }
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
}
