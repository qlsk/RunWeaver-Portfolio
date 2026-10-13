using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 층을 옮기는 동안 플레이어의 조작을 막는다. (필요하면 그림도 숨긴다)
/// - 층 전환 화면(ScreenFader)이 덮였다가 걷히기 시작하면, 새 위치에서 도착 연출(PortalArrival)을 시작한다.
///   도착 연출이 화면이 다 밝아졌을 때 RestoreNow로 되돌린다.
/// - 직접 숨긴 경우(watchTransition = false)에는 RestoreNow를 부를 때까지 숨겨 둔다. (런 시작 도착 연출)
/// - keepVisible이면 그림은 그대로 보이고 조작만 막는다. (페이드 방식 층 이동)
/// - 전환이 시작되지 않으면(포탈 사용 실패 등) 잠시 뒤 되돌린다.
/// - 연출 도중 런이 끝나면(사망, 조사 포기) 실패 연출이 ReleaseAll을 불러 플레이어 그림을 바로 되돌린다.
///   이미 죽었으면 조작은 되돌리지 않는다.
/// 포탈은 층 이동 때 파괴되므로 이 컴포넌트는 따로 만든 오브젝트에서 돈다.
/// </summary>
public class PortalPlayerHider : MonoBehaviour
{
    private const float FallbackRestoreDelay = 4f;

    private static readonly List<PortalPlayerHider> s_active = new();

    private readonly List<SpriteRenderer> _renderers = new();
    private readonly List<Behaviour> _behaviours = new();
    private Rigidbody2D _rigidbody;
    private bool _wasSimulated;
    private bool _sawCovering;
    private float _elapsed;
    private bool _isRestored;
    private bool _watchTransition;
    private bool _arrivalStarted;
    private Transform _player;

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
    /// 도착 균열은 지운다. 플레이어가 죽었으면 조작은 막힌 채로 둔다.
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
            // 새 층 화면이 걷히기 시작했다 : 도착 연출(열린 균열 앞에 선 플레이어)을 시작한다.
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
