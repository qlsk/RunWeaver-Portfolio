using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미션 클리어 후 포탈 3개(귀환 / 1층 하강 / 2~3층 하강)를 등장시킨다.
///
/// - 미션이 클리어되면 대기를 시작하고, 성공 패널(MissionResultPresenter)이 사라지면 포탈을 FadeIn으로 등장시킨다.
/// - 성공 패널이 없거나 연출이 끝나지 않으면 대기 시간이 지난 뒤 등장시킨다.
/// - 위험 포탈의 층수(2~3)는 생성할 때 정해서 텍스트로 보여준다.
/// - 새 미션이 시작되면 남아 있는 포탈을 정리한다.
///
/// StageManager가 같은 오브젝트에 자동으로 붙이고 포탈 프리팹과 중앙 위치를 넘겨준다.
/// </summary>
[DisallowMultipleComponent]
public class PortalSpawner : MonoBehaviour
{
    [Header("포탈")]
    [SerializeField] private Portal _portalPrefab;
    [SerializeField] private Vector3 _center = Vector3.zero;
    [Tooltip("포탈 사이 가로 간격 (왼쪽: 귀환 / 가운데: 1층 / 오른쪽: 위험)")]
    [SerializeField, Min(0f)] private float _spacing = 5f;
    [Tooltip("위험 포탈이 내려가는 층수 범위 (최소, 최대 포함)")]
    [SerializeField] private Vector2Int _deepFloorRange = new(2, 3);

    [Header("등장 시점")]
    [Tooltip("비워두면 씬에서 찾는다")]
    [SerializeField] private MissionResultPresenter _resultPresenter;
    [Tooltip("성공 패널이 없을 때 클리어 후 포탈이 나오기까지 걸리는 시간")]
    [SerializeField, Min(0f)] private float _delayWithoutPanel = 1.5f;
    [Tooltip("성공 패널이 있어도 이 시간이 지나면 포탈을 등장시킨다 (연출이 끊겼을 때 대비)")]
    [SerializeField, Min(0f)] private float _maxWaitWithPanel = 8f;

    private readonly List<Portal> _spawned = new();
    private MissionManager _missionManager;
    private Coroutine _waitRoutine;
    private bool _isWaiting;

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
        _missionManager = MissionManager.Instance;
        if (_missionManager != null)
        {
            _missionManager.OnMissionCompleted += HandleMissionCompleted;
            _missionManager.OnMissionInitialized += HandleMissionInitialized;
        }

        if (_resultPresenter == null)
        {
            _resultPresenter = FindAnyObjectByType<MissionResultPresenter>(FindObjectsInactive.Include);
        }

        if (_resultPresenter != null)
        {
            _resultPresenter.SuccessFinished += HandleSuccessFinished;
        }

        if (_portalPrefab == null)
        {
            Debug.LogWarning("[PortalSpawner] Portal 프리팹이 없어 포탈을 만들 수 없습니다.", this);
        }
    }

    private void OnDestroy()
    {
        if (_missionManager != null)
        {
            _missionManager.OnMissionCompleted -= HandleMissionCompleted;
            _missionManager.OnMissionInitialized -= HandleMissionInitialized;
        }

        if (_resultPresenter != null)
        {
            _resultPresenter.SuccessFinished -= HandleSuccessFinished;
        }
    }

    private void HandleMissionCompleted(MissionBase mission)
    {
        StopWaiting();
        _isWaiting = true;

        float delay = _resultPresenter != null ? _maxWaitWithPanel : _delayWithoutPanel;
        _waitRoutine = StartCoroutine(WaitAndSpawn(delay));
    }

    private void HandleSuccessFinished()
    {
        if (!_isWaiting)
        {
            return;
        }

        StopWaiting();
        SpawnPortals();
    }

    private void HandleMissionInitialized(MissionBase mission)
    {
        StopWaiting();
        DespawnPortals();
    }

    private IEnumerator WaitAndSpawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        _waitRoutine = null;

        if (_isWaiting)
        {
            _isWaiting = false;
            SpawnPortals();
        }
    }

    private void StopWaiting()
    {
        _isWaiting = false;
        if (_waitRoutine != null)
        {
            StopCoroutine(_waitRoutine);
            _waitRoutine = null;
        }
    }

    private void SpawnPortals()
    {
        DespawnPortals();

        if (_portalPrefab == null)
        {
            return;
        }

        int minFloors = Mathf.Max(1, Mathf.Min(_deepFloorRange.x, _deepFloorRange.y));
        int maxFloors = Mathf.Max(minFloors, Mathf.Max(_deepFloorRange.x, _deepFloorRange.y));
        int deepFloors = Random.Range(minFloors, maxFloors + 1);

        Spawn(PortalType.Return, 0, -_spacing);
        Spawn(PortalType.Descend, 1, 0f);
        Spawn(PortalType.DeepDescend, deepFloors, _spacing);
    }

    private void Spawn(PortalType type, int floors, float offsetX)
    {
        Portal portal = Instantiate(_portalPrefab, _center + new Vector3(offsetX, 0f, 0f), Quaternion.identity);
        portal.Setup(type, floors);
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
