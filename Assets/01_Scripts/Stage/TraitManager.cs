using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 런 한정 특성 보유 목록. 최대 레벨 없음, 한 번 선택되면 다시 뜨지 않는다.
///
/// - 획득은 RewardController 확정 시에만 (AcquireTrait 호출). 수치 조회는 핸들러/컴포저가 GetMagnitude로.
/// - 사망·런 종료 시 초기화 (씬 오브젝트라 씬 전환 시 자동 소멸 + PlayerHealth.Died 구독).
/// </summary>
[DisallowMultipleComponent]
public class TraitManager : MonoBehaviour
{
    private static TraitManager s_instance;

    public static TraitManager Instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = FindAnyObjectByType<TraitManager>();
                if (s_instance == null)
                {
                    GameObject go = new GameObject("TraitManager");
                    s_instance = go.AddComponent<TraitManager>();
                    Debug.Log("[TraitManager] 씬에 없어 자동으로 생성하였습니다.");
                }
            }
            return s_instance;
        }
    }

    private readonly HashSet<string> _acquiredIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TraitData> _acquiredData = new(StringComparer.Ordinal);

    /// <summary>특성 획득 시 (id).</summary>
    public event Action<string> OnAcquired;

    private PlayerHealth _playerHealth;

    private void Awake()
    {
        if (s_instance == null)
        {
            s_instance = this;
        }
        else if (s_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (_playerHealth != null)
        {
            _playerHealth.Died += HandlePlayerDied;
        }
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.Died -= HandlePlayerDied;
        }

        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    /// <summary>선택 확정 이후에만 호출한다. 중복 획득은 무시된다.</summary>
    public bool AcquireTrait(TraitData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.Id))
        {
            return false;
        }

        if (!_acquiredIds.Add(data.Id))
        {
            return false;
        }

        _acquiredData[data.Id] = data;
        Debug.Log($"[TraitManager] 특성 획득: {data.Id}");
        OnAcquired?.Invoke(data.Id);
        return true;
    }

    /// <summary>보유한 특성 ID (저장용)</summary>
    public IReadOnlyCollection<string> AcquiredIds => _acquiredIds;

    public bool Has(string id)
    {
        return !string.IsNullOrWhiteSpace(id) && _acquiredIds.Contains(id);
    }

    public bool TryGet(string id, out TraitData data)
    {
        data = null;
        return !string.IsNullOrWhiteSpace(id) && _acquiredData.TryGetValue(id, out data);
    }

    /// <summary>보유 특성의 수치. 없으면 fallback.</summary>
    public float GetMagnitude(string id, float fallback)
    {
        return TryGet(id, out TraitData data) ? data.Magnitude : fallback;
    }

    /// <summary>보유 특성의 지속 시간. 없으면 fallback.</summary>
    public float GetDuration(string id, float fallback)
    {
        return TryGet(id, out TraitData data) && data.Duration > 0f ? data.Duration : fallback;
    }

    public void ResetAll()
    {
        _acquiredIds.Clear();
        _acquiredData.Clear();
        Debug.Log("[TraitManager] 보유 특성 초기화");
    }

    private void HandlePlayerDied()
    {
        ResetAll();
    }
}
