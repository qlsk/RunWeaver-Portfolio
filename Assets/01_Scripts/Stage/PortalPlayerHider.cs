using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 포탈로 빨려 들어가는 동안 진짜 플레이어를 숨기고 조작을 막는다.
/// - 숨길 때 플레이어 그림(SpriteRenderer)과 같은 모양의 복사본(CreateGhosts)을 만들어 포탈이 움직이게 한다.
/// - 층 전환 화면(ScreenFader)이 덮였다가 걷히기 시작하면 플레이어를 되돌리고 스스로 사라진다.
///   전환이 시작되지 않으면(포탈 사용 실패 등) 잠시 뒤 되돌린다.
/// 포탈은 층 이동 때 파괴되므로 이 컴포넌트는 따로 만든 오브젝트에서 돈다.
/// </summary>
public class PortalPlayerHider : MonoBehaviour
{
    private const float FallbackRestoreDelay = 4f;

    private readonly List<SpriteRenderer> _renderers = new();
    private readonly List<Behaviour> _behaviours = new();
    private Rigidbody2D _rigidbody;
    private bool _wasSimulated;
    private bool _sawCovering;
    private float _elapsed;
    private bool _isRestored;

    public static PortalPlayerHider Hide(GameObject player)
    {
        if (player == null)
        {
            return null;
        }

        var hider = new GameObject("PortalPlayerHider").AddComponent<PortalPlayerHider>();
        hider.Initialize(player);
        return hider;
    }

    private void Initialize(GameObject player)
    {
        foreach (SpriteRenderer renderer in player.GetComponentsInChildren<SpriteRenderer>())
        {
            if (renderer.enabled)
            {
                _renderers.Add(renderer);
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

    /// <summary>숨긴 그림과 같은 모양의 복사본을 만들고 진짜 그림은 숨긴다.</summary>
    public List<SpriteRenderer> CreateGhosts()
    {
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
            ghost.sortingOrder = source.sortingOrder + 20;
            ghosts.Add(ghost);
            source.enabled = false;
        }

        return ghosts;
    }

    private void Update()
    {
        _elapsed += Time.unscaledDeltaTime;
        if (ScreenFader.IsCovering)
        {
            _sawCovering = true;
            return;
        }

        if (_sawCovering || _elapsed >= FallbackRestoreDelay)
        {
            Restore(true);
        }
    }

    private void OnDestroy()
    {
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

        foreach (Behaviour behaviour in _behaviours)
        {
            if (behaviour != null)
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
