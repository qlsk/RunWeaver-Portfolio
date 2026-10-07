using System;
using UnityEngine;
using UnityEngine.Serialization;

public class DummyEnemy : MonoBehaviour
{
    public static event Action<DummyEnemy> OnEnemyDied;

    [Header("몬스터 속성")]
    [SerializeField, FormerlySerializedAs("isElite")] private bool _isElite = false;
    [SerializeField, FormerlySerializedAs("maxHp")] private float _maxHp = 10f; // 기본 HP
    private float _currentHp;

    [SerializeField, FormerlySerializedAs("moveSpeed")] private float _moveSpeed = 2.5f;
    [SerializeField, FormerlySerializedAs("stoppingDistance")] private float _stoppingDistance = 0.5f;

    [Header("타겟 설정 (비워둘 경우 Player 태그 자동 탐색)")]
    [SerializeField, FormerlySerializedAs("targetTransform")] private Transform _targetTransform;

    public bool IsElite
    {
        get
        {
            // StageEnemyInfo 어댑터가 부착되어 있다면 엘리트 여부 가져오기
            if (TryGetComponent(out StageEnemyInfo info))
            {
                return info.IsElite;
            }
            return _isElite;
        }
    }

    private void Start()
    {
        _currentHp = _maxHp;
        FindTarget();
    }

    private void Update()
    {
        if (_targetTransform == null)
        {
            FindTarget();
        }

        if (_targetTransform != null)
        {
            float distance = Vector2.Distance(transform.position, _targetTransform.position);
            if (distance > _stoppingDistance)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    _targetTransform.position,
                    _moveSpeed * Time.deltaTime
                );
            }
        }
    }

    private void FindTarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _targetTransform = playerObj.transform;
            return;
        }

        DefenseAltar altar = FindAnyObjectByType<DefenseAltar>();
        if (altar != null)
        {
            _targetTransform = altar.transform;
        }
    }

    /// <summary>
    /// 외부 총알 및 피격 판정용 함수 (데미지 수신)
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (_currentHp <= 0f) return;

        _currentHp -= damage;
        Debug.Log($"[DummyEnemy] {gameObject.name} 피격! 남은 HP: {_currentHp}/{_maxHp}");

        if (_currentHp <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// 2D Trigger 충돌 감지 (총알 피격)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleBulletCollision(collision.gameObject);
    }

    /// <summary>
    /// 2D Collision 충돌 감지 (총알 피격)
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleBulletCollision(collision.gameObject);
    }

    private void HandleBulletCollision(GameObject obj)
    {
        // "Bullet" 태그이거나 이름에 Bullet이 포함된 프리팹, 또는 Bullet 컴포넌트 충돌 처리
        if (obj.CompareTag("Bullet") || obj.name.Contains("Bullet") || obj.name.Contains("bullet") || obj.GetComponent<Bullet>() != null)
        {
            float damage = 10f; // 더미 몬스터 피격 기본 데미지

            TakeDamage(damage);

            // 현재 Bullet의 적중 처리와 동일하게 탄환을 제거한다.
            Destroy(obj);
        }
    }

    // 마우스 좌클릭 시 수동 사망 테스트 (기존 유지)
    private void OnMouseDown()
    {
        Die();
    }

    public void Die()
    {
        Debug.Log($"[DummyEnemy] 더미 몬스터 사망: {gameObject.name} (IsElite: {IsElite})");
        OnEnemyDied?.Invoke(this);
        Destroy(gameObject);
    }
}
