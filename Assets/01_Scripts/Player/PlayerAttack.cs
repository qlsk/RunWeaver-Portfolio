using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

[DefaultExecutionOrder(300)]
public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Bullet _bulletPrefab;
    [SerializeField] private GameObject _firePosition;

    [Header("Settings")]
    [SerializeField] private float _bulletFireRate = 1f;
    [SerializeField, Min(0f)] private float _bulletDamage = 10f;
    [SerializeField, Min(0.01f)] private float _bulletSpeed = 5f;
    [SerializeField, Min(0f)] private float _bulletRange = 10f;

    [SerializeField] private ElementType _mainElement;

    [Header("탄창 및 재장전")]
    [SerializeField] private int _magazineSize = 10;
    [SerializeField] private float _reloadTime = 2f;

    // References
    private RuneEffectDatabase _runeEffectDatabase;
    private RuneEffectLoadoutManager _runeEffectLoadoutManager;

    // Loadout
    private IReadOnlyList<RuneEffectDefinition> _activatedRuneEffects;

    // State Flags
    private bool _isInitialized;

    // Attack states
    private int _currentAmmo;
    private bool _isReloading;
    private float _reloadTimer;

    public int MagazineSize => _magazineSize;
    public int CurrentAmmo => _currentAmmo;
    public bool IsReloading => _isReloading;
    public float ReloadTime => _reloadTime;
    public float ReloadTimer => _reloadTimer;
    public float BulletFireCoolTimer => _bulletFireCoolTimer;

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

        _currentAmmo = _magazineSize;
        _bulletFireCoolTimer = _bulletFireRate;

        InitializeAsync().Forget();
    }

    /// <summary>발사 대기 시간을 갱신하고 공격 입력이 유지되면 발사한다.</summary>
    private void Update()
    {
        if (!_isInitialized)
            return;

        _bulletFireCoolTimer += Time.deltaTime;
        if (_isReloading)
        {
            _reloadTimer += Time.deltaTime;
            if (_reloadTimer < _reloadTime) return;

            _currentAmmo = _magazineSize;
            _isReloading = false;
        }

        if (_playerInputHandler.IsReloadPressed || _currentAmmo == 0)
            Reload();

        if (_isReloading) return;

        if (_playerInputHandler.IsAttackHeld)
            Fire();
    }

    private void OnEnable()
    {
        if (_runeEffectLoadoutManager == null)
            return;
        _runeEffectLoadoutManager.LoadoutReplaced += OnLoadoutReplaced;
        _runeEffectLoadoutManager.RuneEffectLoadout.LoadoutChanged += OnLoadoutChanged;
    }

    private void OnDisable()
    {
        if (_runeEffectLoadoutManager == null)
            return;
        _runeEffectLoadoutManager.LoadoutReplaced -= OnLoadoutReplaced;
        _runeEffectLoadoutManager.RuneEffectLoadout.LoadoutChanged -= OnLoadoutChanged;
    }

    private async UniTask InitializeAsync()
    {
        await UniTask.WaitUntil(() => RuneEffectDatabase.Instance != null && RuneEffectDatabase.Instance.DatabaseReady,
            cancellationToken: this.GetCancellationTokenOnDestroy());
        await UniTask.WaitUntil(
            () => RuneEffectLoadoutManager.Instance != null && RuneEffectLoadoutManager.Instance.IsInitialized,
            cancellationToken: this.GetCancellationTokenOnDestroy());

        _runeEffectDatabase = RuneEffectDatabase.Instance;
        _runeEffectLoadoutManager = RuneEffectLoadoutManager.Instance;
        _runeEffectLoadoutManager.LoadoutReplaced += OnLoadoutReplaced;
        _runeEffectLoadoutManager.RuneEffectLoadout.LoadoutChanged += OnLoadoutChanged;
        OnLoadoutChanged(_runeEffectLoadoutManager.RuneEffectLoadout.EquippedEffectIds);

        _isInitialized = true;
    }

    private void OnLoadoutReplaced()
    {
        // TODO
    }

    private void OnLoadoutChanged(IReadOnlyList<string> activatedRuneEffectIds)
    {
        List<RuneEffectDefinition> effects = new(activatedRuneEffectIds.Count);
        foreach (string id in activatedRuneEffectIds)
        {
            if (!_runeEffectDatabase.TryGetById(id, out RuneEffectDefinition effect))
                throw new InvalidOperationException($"Unknown rune effect ID: {id}");

            effects.Add(effect);
        }

        _activatedRuneEffects = effects;
    }

    /// <summary>발사 간격에 맞춰 마우스 방향으로 주력 원소를 가진 탄환을 발사한다.</summary>
    public void Fire()
    {
        if (_isReloading || _currentAmmo <= 0 || _bulletFireCoolTimer < _bulletFireRate)
            return;

        Vector2 targetPosition = _playerInputHandler.MousePosition;
        Vector2 direction = (targetPosition - (Vector2)_firePosition.transform.position).normalized;
        if (direction == Vector2.zero)
            return;
        // 마우스 Screen 좌표 -> World 좌표

        float rotationZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Bullet bullet = Instantiate(_bulletPrefab, _firePosition.transform.position,
            Quaternion.Euler(0f, 45f, rotationZ));
        bullet.Initialize(_bulletDamage, _bulletSpeed, _bulletRange, direction, _activatedRuneEffects);

        bullet.PlayCastEffect(_firePosition.transform);

        _currentAmmo--;

        // 발사 간격을 마지막 발사 시점부터 측정
        _bulletFireCoolTimer = 0f;
    }

    public void Reload()
    {
        if (_isReloading || _currentAmmo >= _magazineSize) return;

        _isReloading = true;
        _reloadTimer = 0f;
    }

    /// <summary>앞으로 발사할 탄환의 주력 원소를 변경한다.</summary>
    public void SetMainElement(ElementType element)
    {
        if (!Enum.IsDefined(typeof(ElementType), element))
            throw new ArgumentOutOfRangeException(nameof(element));

        _mainElement = element;
    }
}