using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 포탈로 빨려 들어가는 동안 진짜 플레이어를 숨기고 조작을 막는다.
/// - 숨길 때 플레이어 그림(SpriteRenderer)과 같은 모양의 복사본(CreateGhosts)을 만들어 포탈이 움직이게 한다.
/// - 층 전환 화면(ScreenFader)이 덮였다가 걷히기 시작하면, 새 위치에서 도착 연출(PortalArrival)을 시작한다.
///   도착 연출이 플레이어를 뱉어낼 때 RestoreNow로 되돌린다.
/// - 직접 숨긴 경우(watchTransition = false)에는 RestoreNow를 부를 때까지 숨겨 둔다. (런 시작 도착 연출)
/// - keepVisible이면 그림은 그대로 보이고 조작만 막는다. (페이드 방식 층 이동)
/// - 전환이 시작되지 않으면(포탈 사용 실패 등) 잠시 뒤 되돌린다.
/// - 연출 도중 런이 끝나면(사망, 조사 포기) 실패 연출이 ReleaseAll을 불러 플레이어 그림을 바로 되돌린다.
///   그래야 실패 연출이 진짜 플레이어를 분해할 수 있다. 이미 죽었으면 조작은 되돌리지 않는다.
/// 포탈은 층 이동 때 파괴되므로 이 컴포넌트는 따로 만든 오브젝트에서 돈다.
/// </summary>
public class PortalPlayerHider : MonoBehaviour
{
    private const float FallbackRestoreDelay = 4f;

    private static readonly List<PortalPlayerHider> s_active = new();

    private readonly List<SpriteRenderer> _renderers = new();
    private readonly List<Behaviour> _behaviours = new();
    private readonly List<SpriteRenderer> _ghosts = new();
    private Rigidbody2D _rigidbody;
    private bool _wasSimulated;
    private bool _sawCovering;
    private float _elapsed;
    private bool _isRestored;
    private bool _watchTransition;
    private bool _arrivalStarted;
    private Transform _player;
    private SortingGroup _sortingGroup;

    /// <summary>플레이어 발 위치 (지금)</summary>
    public Vector3 PlayerPosition => _player != null ? _player.position : transform.position;

    /// <summary>도착 연출 등으로 플레이어를 숨기거나 조작을 막고 있는 hider가 있는지</summary>
    public static bool IsAnyHiding => s_active.Exists(hider => hider != null && !hider._isRestored);

    /// <param name="watchTransition">true면 층 전환이 걷힐 때 도착 연출을 시작한다. false면 RestoreNow까지 숨겨 둔다.</param>
    /// <param name="keepVisible">true면 그림을 숨기지 않고 조작만 막는다. (페이드 방식)</param>
    public static PortalPlayerHider Hide(GameObject player, bool watchTransition = true, bool keepVisible = false)
    {
        if (player == null)
        {
            return null;
        }

        var hider = new GameObject("PortalPlayerHider").AddComponent<PortalPlayerHider>();
        hider._watchTransition = watchTransition;
        hider.Initialize(player, keepVisible);
        return hider;
    }

    /// <summary>
    /// 진행 중인 포탈 연출을 모두 멈추고 플레이어 그림을 되돌린다. (실패 연출이 시작될 때)
    /// 복사본과 도착 균열은 지운다. 플레이어가 죽었으면 조작은 막힌 채로 둔다.
    /// </summary>
    public static void ReleaseAll()
    {
        foreach (PortalArrival arrival in FindObjectsByType<PortalArrival>(FindObjectsSortMode.None))
        {
            Destroy(arrival.gameObject);
        }

        foreach (PortalPlayerHider hider in s_active.ToArray())
        {
            if (hider != null)
            {
                hider.Restore(true);
            }
        }
    }

    private void Awake()
    {
        s_active.Add(this);
    }

    /// <summary>숨긴 플레이어를 바로 되돌린다.</summary>
    public void RestoreNow()
    {
        Restore(true);
    }

    private void Initialize(GameObject player, bool keepVisible)
    {
        _player = player.transform;
        _sortingGroup = player.GetComponent<SortingGroup>();
        foreach (SpriteRenderer renderer in keepVisible
                     ? System.Array.Empty<SpriteRenderer>()
                     : player.GetComponentsInChildren<SpriteRenderer>())
        {
            if (renderer.enabled)
            {
                _renderers.Add(renderer);
                renderer.enabled = false;   // 숨긴 순간부터 보이지 않게 한다
            }
        }

        // 이동, 공격, 대시, 상호작용을 막는다.
        AddBehaviour(player.GetComponent<PlayerMove>());
        AddBehaviour(player.GetComponent<PlayerAttack>());
        AddBehaviour(player.GetComponentInChildren<Dash>());
        AddBehaviour(player.GetComponent<PlayerInteraction>());

        _rigidbody = player.GetComponent<Rigidbody2D>();
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _wasSimulated = _rigidbody.simulated;
            _rigidbody.simulated = false;
        }
    }

    private void AddBehaviour(Behaviour behaviour)
    {
        if (behaviour != null && behaviour.enabled)
        {
            behaviour.enabled = false;
            _behaviours.Add(behaviour);
        }
    }

    /// <summary>숨긴 그림과 같은 모양의 복사본을 만든다. (진짜 그림은 숨긴 때부터 꺼져 있다)</summary>
    /// <param name="minOrder">복사본이 이 순서보다 앞에 그려지게 한다 (균열 앞)</param>
    public List<SpriteRenderer> CreateGhosts(int minOrder = int.MinValue)
    {
        // 플레이어는 SortingGroup(YSort)으로 정렬되므로, 복사본은 그룹 순서를 기준으로 원래 앞뒤를 지킨다.
        int baseOrder = _sortingGroup != null ? _sortingGroup.sortingOrder : 0;
        baseOrder = Mathf.Max(baseOrder, minOrder) + 10;
        List<SpriteRenderer> ghosts = new();
        foreach (SpriteRenderer source in _renderers)
        {
            if (source == null)
            {
                continue;
            }

            var go = new GameObject("PortalGhost_" + source.name);
            go.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            go.transform.localScale = source.transform.lossyScale;
            var ghost = go.AddComponent<SpriteRenderer>();
            ghost.sprite = source.sprite;
            ghost.flipX = source.flipX;
            ghost.flipY = source.flipY;
            ghost.color = source.color;
            ghost.sharedMaterial = source.sharedMaterial;
            ghost.sortingLayerID = source.sortingLayerID;
            ghost.sortingOrder = Mathf.Clamp(baseOrder + source.sortingOrder, -32768, 32767);
            ghosts.Add(ghost);
            _ghosts.Add(ghost);
            source.enabled = false;
        }

        return ghosts;
    }

    private void Update()
    {
        if (!_watchTransition || _arrivalStarted)
        {
            return;
        }

        _elapsed += Time.unscaledDeltaTime;
        if (ScreenFader.IsCovering)
        {
            _sawCovering = true;
            return;
        }

        if (_sawCovering)
        {
            // 새 층 화면이 걷히기 시작했다 : 도착 연출이 플레이어를 뱉어낸다.
            _arrivalStarted = true;
            PortalArrival.Play(this);
        }
        else if (_elapsed >= FallbackRestoreDelay)
        {
            Restore(true);
        }
    }

    private void OnDestroy()
    {
        s_active.Remove(this);
        Restore(false);
    }

    private void Restore(bool destroySelf)
    {
        if (_isRestored)
        {
            return;
        }

        _isRestored = true;
        foreach (SpriteRenderer ghost in _ghosts)
        {
            if (ghost != null)
            {
                Destroy(ghost.gameObject);
            }
        }

        _ghosts.Clear();
        foreach (SpriteRenderer renderer in _renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        // 연출 도중 죽었으면 PlayerLifecycle이 끈 조작을 다시 켜지 않는다.
        bool isDead = _player != null && _player.TryGetComponent(out PlayerHealth health) && health.IsDead;
        foreach (Behaviour behaviour in _behaviours)
        {
            if (behaviour != null && !isDead)
            {
                behaviour.enabled = true;
            }
        }

        if (_rigidbody != null)
        {
            _rigidbody.simulated = _wasSimulated;
        }

        if (destroySelf)
        {
            Destroy(gameObject);
        }
    }
}
