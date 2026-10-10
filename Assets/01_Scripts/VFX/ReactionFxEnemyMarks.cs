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
        public SpriteRenderer Mark;
        public float Phase;
        public float Lift;
        public bool HasLiftBase;
    }

    private sealed class Ember
    {
        public SpriteRenderer Renderer;
        public Vector3 From;
        public Target Target;
        public float Time;
        public bool HasHit;
    }

    // 여러 장판이 같은 적을 동시에 띄워도 원래 위치를 한 번만 기억한다.
    private static readonly Dictionary<Transform, (Vector3 basePosition, int count)> s_liftBases = new();

    [SerializeField] private MarkMode _mode;
    [Tooltip("ReactionAreaVisuals의 기준 반지름. 이펙트 크기에 곱해 실제 반지름을 구한다.")]
    [SerializeField, Min(0.01f)] private float _referenceRadius = 0.9f;
    [SerializeField] private Sprite _markSprite;
    [Tooltip("불덩이가 닿을 때 터지는 모양 (화염폭풍)")]
    [SerializeField] private Sprite _impactSprite;
    [SerializeField, Min(0.02f)] private float _refreshInterval = 0.1f;

    [Header("띄우기 (회오리)")]
    [SerializeField, Min(0f)] private float _liftHeight = 0.45f;
    [SerializeField, Min(0.01f)] private float _liftSpeed = 2f;

    [Header("불덩이 (화염폭풍)")]
    [Tooltip("불덩이가 떠나는 높이 (반지름 대비)")]
    [SerializeField, Min(0f)] private float _emberLaunchHeight = 0.9f;
    [SerializeField, Min(0.05f)] private float _emberFlightTime = 0.35f;
    [SerializeField, Min(0f)] private float _emberArcHeight = 0.8f;

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

        UpdateEmbers(deltaTime);
    }

    private void LateUpdate()
    {
        foreach (Target target in _targets.Values)
        {
            Apply(target);
        }
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
                target.Mark = CreateRenderer("Mark", _markSprite);
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
            Destroy(target.Mark.gameObject);
            target.Mark = null;
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
        float bob = Mathf.Sin(_time * 4f + target.Phase) * 0.04f;

        switch (_mode)
        {
            case MarkMode.Lift:
                ApplyLift(target);
                break;
            case MarkMode.Question:
                if (target.Mark != null)
                {
                    PlaceMark(target, new Vector3(bounds.center.x, bounds.max.y + 0.22f + bob, 0f), 6);
                    // 깜빡이며 두리번거리는 느낌
                    target.Mark.enabled = Mathf.Repeat(_time * 1.6f + target.Phase, 1f) < 0.75f;
                }

                break;
            case MarkMode.BrokenShield:
                if (target.Mark != null)
                {
                    PlaceMark(target, new Vector3(bounds.center.x, bounds.max.y + 0.24f + bob, 0f), 6);
                }

                break;
            case MarkMode.Mud:
                if (target.Mark != null)
                {
                    bool isRooted = target.Status != null && target.Status.IsRooted;
                    PlaceMark(target, new Vector3(bounds.center.x, bounds.min.y + 0.06f, 0f), 1);
                    float pulse = 1f + Mathf.Sin(_time * 3f + target.Phase) * 0.04f;
                    target.Mark.transform.localScale = Vector3.one * ((isRooted ? 1.3f : 1f) * pulse);
                    target.Mark.color = isRooted ? new Color(0.8f, 0.8f, 0.8f, 1f) : Color.white;
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

    private void PlaceMark(Target target, Vector3 position, int orderOffset)
    {
        target.Mark.transform.position = position;
        target.Mark.sortingLayerID = target.Body.sortingLayerID;
        target.Mark.sortingOrder = target.Body.sortingOrder + orderOffset;
    }

    // ───────────────────────── 불덩이 ─────────────────────────

    private void LaunchEmber(Target target)
    {
        if (_markSprite == null)
        {
            return;
        }

        Ember ember = new()
        {
            Renderer = CreateRenderer("Ember", _markSprite),
            From = transform.position + Vector3.up * (_radius * _emberLaunchHeight),
            Target = target
        };
        _embers.Add(ember);
        UpdateEmber(ember, 0f);
    }

    private void UpdateEmbers(float deltaTime)
    {
        for (int i = _embers.Count - 1; i >= 0; i--)
        {
            if (!UpdateEmber(_embers[i], deltaTime))
            {
                if (_embers[i].Renderer != null)
                {
                    Destroy(_embers[i].Renderer.gameObject);
                }

                _embers.RemoveAt(i);
            }
        }
    }

    /// <summary>불덩이를 움직인다. 끝났으면 false</summary>
    private bool UpdateEmber(Ember ember, float deltaTime)
    {
        if (ember.Renderer == null)
        {
            return false;
        }

        ember.Time += deltaTime;
        SpriteRenderer body = ember.Target.Body;
        if (body == null)
        {
            return false;
        }

        if (!ember.HasHit)
        {
            float t = Mathf.Clamp01(ember.Time / _emberFlightTime);
            Vector3 to = body.bounds.center;
            Vector3 position = Vector3.Lerp(ember.From, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * _emberArcHeight);
            ember.Renderer.transform.position = position;
            ember.Renderer.sortingLayerID = body.sortingLayerID;
            ember.Renderer.sortingOrder = body.sortingOrder + 8;
            if (t >= 1f)
            {
                ember.HasHit = true;
                ember.Time = 0f;
                if (_impactSprite != null)
                {
                    ember.Renderer.sprite = _impactSprite;
                }
            }

            return true;
        }

        // 터진 모양이 커지며 사라진다.
        float k = ember.Time / 0.2f;
        ember.Renderer.transform.position = body.bounds.center;
        ember.Renderer.transform.localScale = Vector3.one * (1f + k);
        ember.Renderer.color = new Color(1f, 1f, 1f, 1f - k);
        return k < 1f;
    }

    private SpriteRenderer CreateRenderer(string objectName, Sprite sprite)
    {
        if (_holder == null)
        {
            _holder = new GameObject(name + "_EnemyMarks");
        }

        GameObject go = new(objectName);
        go.transform.SetParent(_holder.transform, false);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        return renderer;
    }
}
