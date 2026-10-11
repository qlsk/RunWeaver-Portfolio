using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미션 클리어 후 다음 층으로 내려가는 포탈 1개를 등장시킨다. (StageManager가 위치를 정한다. 기본은 맵 중앙)
///
/// - StageManager가 미션 클리어 즉시(정화 대기 시작과 함께) SpawnPortal을 부른다.
/// - 포탈은 FadeIn으로 등장하며, 정화 대기 중에도 사용할 수 있다.
/// - 포탈을 쓰거나 런이 끝나면 StageManager가 DespawnPortals로 정리한다.
///
/// StageManager가 같은 오브젝트에 자동으로 붙이고 포탈 프리팹과 중앙 위치를 넘겨준다.
/// </summary>
[DisallowMultipleComponent]
public class PortalSpawner : MonoBehaviour
{
    [Header("포탈")]
    [SerializeField] private Portal _portalPrefab;
    [SerializeField] private Vector3 _center = Vector3.zero;

    private readonly List<Portal> _spawned = new();

    /// <summary>
    /// StageManager의 포탈 설정을 넘겨받는다. 인스펙터에 직접 넣은 값이 있으면 프리팹은 그것을 쓴다.
    /// </summary>
    public void Configure(GameObject portalPrefab, Vector3 center)
    {
        if (_portalPrefab == null && portalPrefab != null)
        {
            _portalPrefab = portalPrefab.GetComponent<Portal>();
        }

        _center = center;
    }

    private void Start()
    {
        if (_portalPrefab == null)
        {
            Debug.LogWarning("[PortalSpawner] Portal 프리팹이 없어 포탈을 만들 수 없습니다.", this);
        }
    }

    /// <summary>
    /// 맵 중앙에 다음 층으로 내려가는 포탈을 만든다. 남아 있던 포탈은 정리한다.
    /// </summary>
    /// <param name="label">포탈 위에 띄울 문구 (예: Descend, Face the Boss)</param>
    /// <param name="isBoss">보스 층으로 내려가는 포탈이면 true (보스 균열 그림을 쓴다)</param>
    public void SpawnPortal(string label, bool isBoss = false)
    {
        SpawnPortal(label, _center, isBoss);
    }

    /// <summary>
    /// 지정한 위치에 다음 층으로 내려가는 포탈을 만든다. 남아 있던 포탈은 정리한다.
    /// </summary>
    public void SpawnPortal(string label, Vector3 position, bool isBoss = false)
    {
        DespawnPortals();

        if (_portalPrefab == null)
        {
            return;
        }

        Portal portal = Instantiate(_portalPrefab, position, Quaternion.identity);
        portal.Setup(label, isBoss);
        portal.PlayFadeIn();
        _spawned.Add(portal);
    }

    /// <summary>
    /// 남아 있는 포탈을 모두 없앤다.
    /// </summary>
    public void DespawnPortals()
    {
        foreach (Portal portal in _spawned)
        {
            if (portal != null)
            {
                Destroy(portal.gameObject);
            }
        }

        _spawned.Clear();
    }
}
