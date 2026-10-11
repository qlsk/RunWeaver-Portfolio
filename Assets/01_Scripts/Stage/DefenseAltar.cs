using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DefenseAltar : MonoBehaviour
{
    public static event Action OnAltarDestroyed;
    private static readonly HashSet<DefenseAltar> ActiveAltars = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTargetRegistry() => ActiveAltars.Clear();

    private void OnEnable() => ActiveAltars.Add(this);
    private void OnDisable() => ActiveAltars.Remove(this);

    public static bool TryGetEnemyTarget(Transform enemy, out Transform target)
    {
        target = null;
        if (enemy == null) return false;

        float nearestDistance = float.MaxValue;
        foreach (DefenseAltar altar in ActiveAltars)
        {
            if (altar == null || !altar.isActiveAndEnabled || altar.IsDestroyed ||
                altar.gameObject.scene != enemy.gameObject.scene) continue;

            float distance = ((Vector2)altar.transform.position - (Vector2)enemy.position).sqrMagnitude;
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            target = altar.transform;
        }

        return target != null;
    }

    [Header("제단 체력 설정")]
    [SerializeField, FormerlySerializedAs("maxHp")] private float _maxHp = 100f;
    private float _currentHp = 100f;
    private bool _isInitialized = false;

    [Header("테스트/디버그 입력 (기본 비활성화)")]
    [Tooltip("체크 시에만 마우스 우클릭 및 단축키 피격 테스트가 동작합니다.")]
    [SerializeField, FormerlySerializedAs("allowTestInputs")] private bool _allowTestInputs = false;

    public bool IsDestroyed => _isInitialized && _currentHp <= 0f;
    public float CurrentHp => _currentHp;
    public float MaxHp => _maxHp;

    private void Awake()
    {
        _currentHp = _maxHp;
        _isInitialized = true;
    }

    private void Start()
    {
        if (!_isInitialized || _currentHp <= 0f)
        {
            _currentHp = _maxHp;
            _isInitialized = true;
        }
    }

    public void TakeDamage(float damage)
    {
        if (IsDestroyed) return;

        _currentHp -= damage;
        Debug.Log($"[DefenseAltar] 제단 피격! 남은 HP: {_currentHp}/{_maxHp}");

        if (_currentHp <= 0f)
        {
            _currentHp = 0f;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("[DefenseAltar] 제단 파괴!");
        OnAltarDestroyed?.Invoke();
        Destroy(gameObject);
    }

    // 마우스 우클릭 테스트 (실사용 중 실수 클릭 방지를 위해 allowTestInputs 가 true 일 때만 동작)
    private void OnMouseOver()
    {
        if (_allowTestInputs && Input.GetMouseButtonDown(1))
        {
            TakeDamage(20f);
        }
    }

    // K키 즉시 파괴 테스트 (실사용 중 실수 입력 방지를 위해 allowTestInputs 가 true 일 때만 동작)
    private void Update()
    {
        if (_allowTestInputs && Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(_maxHp);
        }
    }
}
