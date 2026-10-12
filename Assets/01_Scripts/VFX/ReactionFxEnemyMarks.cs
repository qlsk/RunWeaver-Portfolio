using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 반응 장판 안에 있는 적에게 붙는 연출. 반응 이펙트 프리팹의 루트에 붙인다.
/// 장판 스크립트를 건드리지 않고, 이펙트 크기(반지름)로 범위를 계산해 그 안의 적을 직접 찾는다.
/// - 띄우기 (회오리) : 끌려가는 적의 몸(HitVisual)을 살짝 띄우고 흔든다. 장판에서 벗어나거나 장판이 사라지면 제자리로 돌린다.
/// - 불덩이 (화염폭풍) : 새로 들어온 적에게 기둥 위에서 불덩이가 날아가 터진다.
/// - 물음표 (증기) : 머리 위에 "?"를 띄운다.
/// - 깨진 방패 (모래폭풍) : 머리 위에 방어 약화 표시를 띄운다.
/// - 진흙 고리 (진흙) : 발목에 진흙 고리를 두르고, 속박되면 더 크고 진하게 보인다.
/// 표시(물음표, 방패, 진흙 고리)와 불덩이의 모양·움직임은 프리팹과 애니메이션 클립에 들어 있고,
/// 코드는 프리팹을 적 위치에 맞춰 띄우고 상태(속박 여부)만 넘긴다. 띄우기는 적 몸을 직접 움직이므로 코드에 남아 있다.
/// - 표시 프리팹 : 반복 상태 하나 (진흙 고리는 Rooted bool로 Normal / Rooted 상태를 바꾼다)
/// - 불덩이 프리팹 : Fly(날아가는 호, 자식이 위로 솟는다) → Impact(터지며 사라짐). 루트 위치는 코드가 옮긴다.
/// 게임 시간으로 돌아간다.
/// </summary>
[DisallowMultipleComponent]
public class ReactionFxEnemyMarks : MonoBehaviour
{
    public enum MarkMode
    {
        Lift,
        Ember,
        Question,
        BrokenShield,
        Mud
    }

    private sealed class Target
    {
        public EnemyHealth Health;
        public EnemyStatusEffectController Status;
        public SpriteRenderer Body;
        public GameObject Mark;
        public Animator MarkAnimator;
        public float Phase;
        public float Lift;
        public bool HasLiftBase;
    }

    private sealed class Ember
    {
        public GameObject Root;
        public Animator Animator;
        public Vector3 From;
        public Target Target;
    }

    private static readonly int RootedHash = Animator.StringToHash("Rooted");
    private static readonly int FlyHash = Animator.StringToHash("Fly");
    private static readonly int ImpactHash = Animator.StringToHash("Impact");

    // 여러 장판이 같은 적을 동시에 띄워도 원래 위치를 한 번만 기억한다.
    private static readonly Dictionary<Transform, (Vector3 basePosition, int count)> s_liftBases = new();

    [SerializeField] private MarkMode _mode;
    [Tooltip("ReactionAreaVisuals의 기준 반지름. 이펙트 크기에 곱해 실제 반지름을 구한다.")]
    [SerializeField, Min(0.01f)] private float _referenceRadius = 0.9f;
    [Tooltip("적에게 붙는 표시 프리팹 (물음표, 깨진 방패, 진흙 고리)")]
    [SerializeField] private GameObject _markPrefab;
    [Tooltip("기둥 위에서 날아가는 불덩이 프리팹 (화염폭풍). Fly → Impact 상태")]
    [SerializeField] private GameObject _emberPrefab;
    [SerializeField, Min(0.02f)] private float _refreshInterval = 0.1f;

    [Header("띄우기 (회오리)")]
    [SerializeField, Min(0f)] private float _liftHeight = 0.45f;
    [SerializeField, Min(0.01f)] private float _liftSpeed = 2f;

    [Header("불덩이 (화염폭풍)")]
    [Tooltip("불덩이가 떠나는 높이 (반지름 대비)")]
    [SerializeField, Min(0f)] private float _emberLaunchHeight = 0.9f;

    private readonly Dictionary<EnemyHealth, Target> _targets = new();
    private readonly List<Ember> _embers = new();
    private readonly List<EnemyHealth> _leaving = new();
    private readonly HashSet<EnemyHealth> _inside = new();
    private GameObject _holder;
    private float _radius;
    private float _refreshTimer;
    private float _time;

    private void Start()
    {
        // ReactionAreaVisuals가 반지름에 맞춰 키운 뒤의 크기로 범위를 정한다.
        _radius = Mathf.Abs(transform.lossyScale.x) * _referenceRadius;
        _holder = new GameObject(name + "_EnemyMarks");
        Refresh();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        _time += deltaTime;
        _refreshTimer += deltaTime;
        if (_refreshTimer >= _refreshInterval)
        {
            _refreshTimer = 0f;
            Refresh();
        }
    }

    private void LateUpdate()
    {
        foreach (Target target in _targets.Values)
        {
            Apply(target);
        }

        UpdateEmbers();
    }

    private void OnDestroy()
    {
        foreach (Target target in _targets.Values)
        {
            Release(target);
        }

        _targets.Clear();
        if (_holder != null)
        {
            Destroy(_holder);
        }
    }

    // ───────────────────────── 범위 ─────────────────────────

    private void Refresh()
    {
        if (_radius <= 0f)
        {
            return;
        }

        _inside.Clear();
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, _radius))
        {
            if (!collider.TryGetComponent(out EnemyHealth enemy) || enemy.IsDead || !enemy.isActiveAndEnabled)
            {
                continue;
            }

            _inside.Add(enemy);
            if (!_targets.ContainsKey(enemy))
            {
                Enter(enemy);
            }
        }

        _leaving.Clear();
        foreach (KeyValuePair<EnemyHealth, Target> pair in _targets)
        {
            if (pair.Key == null || pair.Key.IsDead || !_inside.Contains(pair.Key))
            {
                _leaving.Add(pair.Key);
            }
        }

        foreach (EnemyHealth enemy in _leaving)
        {
            Release(_targets[enemy]);
            _targets.Remove(enemy);
        }
    }

    private void Enter(EnemyHealth enemy)
    {
        Target target = new()
        {
            Health = enemy,
            Status = enemy.GetComponent<EnemyStatusEffectController>(),
            Body = FindBody(enemy),
            Phase = Random.Range(0f, Mathf.PI * 2f)
        };
        _targets.Add(enemy, target);

        switch (_mode)
        {
            case MarkMode.Question:
            case MarkMode.BrokenShield:
            case MarkMode.Mud:
                SpawnMark(target);
                break;
            case MarkMode.Ember:
                LaunchEmber(target);
                break;
        }
    }

    private void Release(Target target)
    {
        if (target.HasLiftBase && target.Body != null)
        {
            Transform body = target.Body.transform;
            if (s_liftBases.TryGetValue(body, out (Vector3 basePosition, int count) entry))
            {
                if (entry.count <= 1)
                {
                    body.localPosition = entry.basePosition;
                    s_liftBases.Remove(body);
                }
                else
                {
                    s_liftBases[body] = (entry.basePosition, entry.count - 1);
                }
            }
        }

        target.HasLiftBase = false;
        if (target.Mark != null)
        {
            Destroy(target.Mark);
            target.Mark = null;
            target.MarkAnimator = null;
        }
    }

    private static SpriteRenderer FindBody(EnemyHealth enemy)
    {
        Transform hitVisual = enemy.transform.Find("HitVisual");
        if (hitVisual != null && hitVisual.TryGetComponent(out SpriteRenderer renderer))
        {
            return renderer;
        }

        return enemy.GetComponentInChildren<SpriteRenderer>();
    }

    // ───────────────────────── 표시 ─────────────────────────

    private void Apply(Target target)
    {
        if (target.Body == null)
        {
            return;
        }

        Bounds bounds = target.Body.bounds;

        switch (_mode)
        {
            case MarkMode.Lift:
                ApplyLift(target);
                break;
            case MarkMode.Question:
                PlaceMark(target, new Vector3(bounds.center.x, bounds.max.y + 0.22f, 0f), 6);
                break;
            case MarkMode.BrokenShield:
                PlaceMark(target, new Vector3(bounds.center.x, bounds.max.y + 0.24f, 0f), 6);
                break;
            case MarkMode.Mud:
                PlaceMark(target, new Vector3(bounds.center.x, bounds.min.y + 0.06f, 0f), 1);
                if (target.MarkAnimator != null)
                {
                    target.MarkAnimator.SetBool(RootedHash, target.Status != null && target.Status.IsRooted);
                }

                break;
        }
    }

    private void ApplyLift(Target target)
    {
        Transform body = target.Body.transform;
        bool isPulled = target.Status != null && target.Status.IsPulled;
        target.Lift = Mathf.MoveTowards(target.Lift, isPulled ? 1f : 0f, _liftSpeed * Time.deltaTime);

        if (!target.HasLiftBase)
        {
            if (target.Lift <= 0f)
            {
                return;
            }

            if (s_liftBases.TryGetValue(body, out (Vector3 basePosition, int count) entry))
            {
                s_liftBases[body] = (entry.basePosition, entry.count + 1);
            }
            else
            {
                s_liftBases[body] = (body.localPosition, 1);
            }

            target.HasLiftBase = true;
        }

        Vector3 basePosition = s_liftBases[body].basePosition;
        float eased = target.Lift * target.Lift * (3f - 2f * target.Lift);
        float bob = Mathf.Sin(_time * 6f + target.Phase) * 0.05f * eased;
        float sway = Mathf.Sin(_time * 9f + target.Phase) * 0.05f * eased;
        body.localPosition = basePosition + new Vector3(sway, _liftHeight * eased + bob, 0f);
    }

    private void SpawnMark(Target target)
    {
        if (_markPrefab == null)
        {
            return;
        }

        target.Mark = Spawn(_markPrefab);
        target.MarkAnimator = target.Mark.GetComponent<Animator>();
        if (target.MarkAnimator != null)
        {
            // 여러 적의 표시가 똑같이 움직이지 않게 시작 지점을 흩어 놓는다.
            target.MarkAnimator.Update(0f);
            AnimatorStateInfo state = target.MarkAnimator.GetCurrentAnimatorStateInfo(0);
            target.MarkAnimator.Play(state.fullPathHash, 0, Random.value);
        }
    }

    private static void PlaceMark(Target target, Vector3 position, int orderOffset)
    {
        if (target.Mark == null)
        {
            return;
        }

        target.Mark.transform.position = position;
        SetSorting(target.Mark, target.Body.sortingLayerID, target.Body.sortingOrder + orderOffset);
    }

    private static void SetSorting(GameObject root, int sortingLayerId, int sortingOrder)
    {
        foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder;
        }
    }

    // ───────────────────────── 불덩이 ─────────────────────────

    private void LaunchEmber(Target target)
    {
        if (_emberPrefab == null)
        {
            return;
        }

        GameObject root = Spawn(_emberPrefab);
        Ember ember = new()
        {
            Root = root,
            Animator = root.GetComponent<Animator>(),
            From = transform.position + Vector3.up * (_radius * _emberLaunchHeight),
            Target = target
        };
        root.transform.position = ember.From;
        _embers.Add(ember);
    }

    private void UpdateEmbers()
    {
        for (int i = _embers.Count - 1; i >= 0; i--)
        {
            if (!UpdateEmber(_embers[i]))
            {
                if (_embers[i].Root != null)
                {
                    Destroy(_embers[i].Root);
                }

                _embers.RemoveAt(i);
            }
        }
    }

    /// <summary>불덩이 루트를 대상 쪽으로 옮긴다. (호를 그리며 솟는 것과 터지는 모양은 클립) 끝났으면 false</summary>
    private static bool UpdateEmber(Ember ember)
    {
        SpriteRenderer body = ember.Target.Body;
        if (ember.Root == null || ember.Animator == null || body == null)
        {
            return false;
        }

        AnimatorStateInfo state = ember.Animator.GetCurrentAnimatorStateInfo(0);
        Vector3 to = body.bounds.center;
        if (state.shortNameHash == FlyHash)
        {
            ember.Root.transform.position = Vector3.Lerp(ember.From, to, Mathf.Clamp01(state.normalizedTime));
        }
        else if (state.shortNameHash == ImpactHash)
        {
            ember.Root.transform.position = to;
        }

        SetSorting(ember.Root, body.sortingLayerID, body.sortingOrder + 8);
        return !(state.shortNameHash == ImpactHash && state.normalizedTime >= 1f);
    }

    private GameObject Spawn(GameObject prefab)
    {
        if (_holder == null)
        {
            _holder = new GameObject(name + "_EnemyMarks");
        }

        return Instantiate(prefab, _holder.transform);
    }
}
