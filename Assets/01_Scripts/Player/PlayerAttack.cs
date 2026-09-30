using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("참조")]
    [LabelText("탄환 프리팹")][SerializeField] private Bullet _bulletPrefab;
    [LabelText("발사 위치")][SerializeField] private GameObject _firePosition;

    [Header("설정 값")]
    [LabelText("발사 쿨타임")][SerializeField] private float _bulletFireRate = 1f;
    [LabelText("탄환 공격력")][SerializeField, Min(0f)] private float _bulletDamage = 10f;
    [LabelText("탄환 속도")][SerializeField, Min(0.01f)] private float _bulletSpeed = 5f;
    [LabelText("탄환 사거리")][SerializeField, Min(0f)] private float _bulletRange = 10f;

    [SerializeField] private ElementType _mainElement;

    private PlayerInputHandler _playerInputHandler;
    private Camera _camera;
    private float _bulletFireCoolTimer;

    public ElementType MainElement => _mainElement;

    /// <summary>공격에 필요한 참조를 확인하고 첫 발사를 준비한다.</summary>
    private void Start()
    {
        _playerInputHandler = GetComponent<PlayerInputHandler>();
        _camera = Camera.main;
        if (_playerInputHandler == null || _camera == null || _bulletPrefab == null || _firePosition == null)
            throw new MissingReferenceException(
                "PlayerAttack requires input, a main camera, a bullet prefab and a fire position.");

        _bulletFireCoolTimer = _bulletFireRate;
    }

    /// <summary>발사 대기 시간을 갱신하고 공격 입력이 유지되면 발사한다.</summary>
    private void Update()
    {
        _bulletFireCoolTimer += Time.deltaTime;
        if (_playerInputHandler.IsAttackHeld)
            Fire();
    }

    /// <summary>발사 간격에 맞춰 마우스 방향으로 주력 원소를 가진 탄환을 발사한다.</summary>
    public void Fire()
    {
        if (_bulletFireCoolTimer < _bulletFireRate)
            return;

        Vector2 targetPosition = _camera.ScreenToWorldPoint(_playerInputHandler.MousePosition);
        Vector2 direction = (targetPosition - (Vector2)_firePosition.transform.position).normalized;
        if (direction == Vector2.zero)
            return;

        float rotationZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Bullet bullet = Instantiate(_bulletPrefab, _firePosition.transform.position,
            Quaternion.Euler(0f, 0f, rotationZ));
        bullet.Initialize(_bulletDamage, _bulletSpeed, _bulletRange, direction, _mainElement);

        _bulletFireCoolTimer = 0f;
    }

    /// <summary>앞으로 발사할 탄환의 주력 원소를 변경한다.</summary>
    public void SetMainElement(ElementType element)
    {
        if (!Enum.IsDefined(typeof(ElementType), element))
            throw new ArgumentOutOfRangeException(nameof(element));

        _mainElement = element;
    }
}