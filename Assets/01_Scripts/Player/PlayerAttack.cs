using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;

[DefaultExecutionOrder(300)]
public class PlayerAttack : MonoBehaviour
{
    [FoldoutGroup("References")] [SerializeField] private Bullet _bulletPrefab;
    [FoldoutGroup("References")] [SerializeField] private GameObject _firePosition;
    [FoldoutGroup("References")] [SerializeField] private MMF_Player _shootFeedbacks;
    [FoldoutGroup("References")] [SerializeField] private MMF_Player _hitFeedbacks;
    [FoldoutGroup("References")] [SerializeField] private MMF_Player _killFeedbacks;

    [FoldoutGroup("Element VFX")] [SerializeField] private BulletVisualEffects _fireVfx = new();
    [FoldoutGroup("Element VFX")] [SerializeField] private BulletVisualEffects _waterVfx = new();
    [FoldoutGroup("Element VFX")] [SerializeField] private BulletVisualEffects _earthVfx = new();
    [FoldoutGroup("Element VFX")] [SerializeField] private BulletVisualEffects _windVfx = new();

    [FoldoutGroup("Bullet Settings")] [SerializeField] private float _bulletFireRate = 1f;
    [FoldoutGroup("Bullet Settings")] [SerializeField, Min(0f)] private float _bulletDamage = 10f;
    [FoldoutGroup("Bullet Settings")] [SerializeField, Min(0.01f)] private float _bulletSpeed = 5f;
    [FoldoutGroup("Bullet Settings")] [SerializeField, Min(0f)] private float _bulletRange = 10f;
    [FoldoutGroup("Bullet Settings")] [SerializeField] private ElementType _mainElement;

    [FoldoutGroup("Firing Pattern")] [SerializeField] private float _spreadAngle = 15f;
    [FoldoutGroup("Firing Pattern")] [SerializeField] private float _multipleInterval = 0.1f;

    [FoldoutGroup("Magazine and Reload")] [SerializeField] private int _magazineSize = 10;
    [FoldoutGroup("Magazine and Reload")] [SerializeField] private float _reloadTime = 2f;

    // References
    private PlayerInputHandler _playerInputHandler;
    private PlayerStatusEffectController _statusEffects;
    private Camera _camera;
    private RuneEffectDatabase _runeEffectDatabase;
    private PlayerRuneManager _playerRuneManager;
    private RuneEffectLoadout _runeEffectLoadout;

    // Loadout
    private RuneEffectConfiguration _runeEffectConfiguration;

    // State Flags
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] private bool _isInitialized;

    // Ammo and reload
    private int _currentAmmo;
    private bool _isReloading;
    private float _reloadTimer;

    /// <summary>보상 특성용: 탄환 발사 통지 (연발 중 추가탄 제외, 1회 공격당 1회).</summary>
    public event Action Fired;

    /// <summary>보상 특성용 배율 (기본 1). 피해·발사간격에 곱해진다.</summary>
    public float DamageMultiplier { get; set; } = 1f;
    public float FireIntervalMultiplier { get; set; } = 1f;

    private int _magazineBonus;
    /// <summary>보상 특성용: 탄창 보너스 탄수 누적.</summary>
    public void AddMagazineBonus(int bonus)
    {
        _magazineBonus = Mathf.Max(0, _magazineBonus + bonus);
    }

    // Attack timing
    private float _bulletFireCoolTimer;

    // Burst state
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] private int _remainingBurstShots;
    private float _multipleTimer;
    private PlayerAttackData _attackData;

    public int MagazineSize => _magazineSize + _magazineBonus;
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] public int CurrentAmmo => _currentAmmo;

    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] public bool IsReloading => _isReloading;
    public float ReloadTime => _reloadTime;
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] public float ReloadTimer => _reloadTimer;

    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] public float BulletFireCoolTimer => _bulletFireCoolTimer;
    public ElementType MainElement => _mainElement;

    private void OnEnable()
    {
        if (!_isInitialized)
            return;

        _playerRuneManager.LoadoutReplaced += OnLoadoutReplaced;
        OnLoadoutReplaced();
    }

    /// <summary>공격에 필요한 참조를 확인하고 첫 발사를 준비한다.</summary>
    private void Start()
    {
        _playerInputHandler = GetComponent<PlayerInputHandler>();
        _statusEffects = GetComponent<PlayerStatusEffectController>();
        _camera = Camera.main;

        if (_playerInputHandler == null || _camera == null || _bulletPrefab == null || _firePosition == null)
            throw new MissingReferenceException(
                "PlayerAttack requires input, a main camera, a bullet prefab and a fire position.");

        _currentAmmo = MagazineSize;
        _bulletFireCoolTimer = _bulletFireRate;

        InitializeAsync().Forget();
    }

    /// <summary>발사 대기 시간을 갱신하고 공격 입력이 유지되면 발사한다.</summary>
    private void Update()
    {
        if (!_isInitialized)
            return;

        float intervalMultiplier = _statusEffects != null ? _statusEffects.AttackIntervalMultiplier : 1f;
        _bulletFireCoolTimer += Time.deltaTime / intervalMultiplier;

        if (_remainingBurstShots > 0)
        {
            UpdateBurst();
            return;
        }

        if (_isReloading)
        {
            UpdateReload();

            if (_isReloading)
                return;
        }

        if (_playerInputHandler.IsReloadPressed || _currentAmmo == 0)
            Reload();

        if (_isReloading)
            return;

        if (_playerInputHandler.IsAttackHeld)
            Fire();
    }

    private void OnDisable()
    {
        _remainingBurstShots = 0;
        if (_shootFeedbacks != null)
        {
            _shootFeedbacks.StopFeedbacks();
            _shootFeedbacks.RestoreInitialValues();
        }

        if (_playerRuneManager == null)
            return;

        _playerRuneManager.LoadoutReplaced -= OnLoadoutReplaced;

        if (_runeEffectLoadout != null)
            _runeEffectLoadout.LoadoutChanged -= OnLoadoutChanged;

        _runeEffectLoadout = null;
    }

    private async UniTask InitializeAsync()
    {
        await UniTask.WaitUntil(() => RuneEffectDatabase.Instance != null && RuneEffectDatabase.Instance.DatabaseReady,
            cancellationToken: this.GetCancellationTokenOnDestroy());

        await UniTask.WaitUntil(
            () => PlayerRuneManager.Instance != null && PlayerRuneManager.Instance.IsInitialized,
            cancellationToken: this.GetCancellationTokenOnDestroy());

        _runeEffectDatabase = RuneEffectDatabase.Instance;
        _playerRuneManager = PlayerRuneManager.Instance;
        _isInitialized = true;

        if (isActiveAndEnabled)
        {
            _playerRuneManager.LoadoutReplaced += OnLoadoutReplaced;
            OnLoadoutReplaced();
        }
    }

    private void OnLoadoutReplaced()
    {
        if (_runeEffectLoadout != null)
            _runeEffectLoadout.LoadoutChanged -= OnLoadoutChanged;

        _runeEffectLoadout = _playerRuneManager.RuneEffectLoadout;
        _runeEffectLoadout.LoadoutChanged += OnLoadoutChanged;

        OnLoadoutChanged(_runeEffectLoadout.EquippedEffectIds);
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

        _runeEffectConfiguration = new RuneEffectConfiguration(effects);
    }

    /// <summary>발사 간격에 맞춰 마우스 방향으로 주력 원소를 가진 탄환을 발사한다.</summary>
    private void Fire()
    {
        float fireInterval = _bulletFireRate * FireIntervalMultiplier;
        if (_currentAmmo <= 0 || _bulletFireCoolTimer < fireInterval)
            return;

        Vector2 targetPosition = _playerInputHandler.MousePosition;
        Vector2 direction = (targetPosition - (Vector2)_firePosition.transform.position).normalized;

        if (direction == Vector2.zero)
            return;

        float damage = _bulletDamage * DamageMultiplier * _runeEffectConfiguration.SpreadDamageMultiplier *
                       _runeEffectConfiguration.MultipleDamageMultiplier;
        _attackData = new PlayerAttackData(direction, damage, _bulletSpeed, _bulletRange, _runeEffectConfiguration,
            transform);

        _currentAmmo--;
        _bulletFireCoolTimer = 0f;

        SpawnBullets(_attackData);
        Fired?.Invoke();

        _remainingBurstShots = _attackData.Configuration.ShotsPerAttack - 1;
        _multipleTimer = _multipleInterval;
    }

    private void UpdateBurst()
    {
        float intervalMultiplier = _statusEffects != null ? _statusEffects.AttackIntervalMultiplier : 1f;
        _multipleTimer -= Time.deltaTime / intervalMultiplier;
        if (_multipleTimer > 0f)
            return;

        SpawnBullets(_attackData);
        _remainingBurstShots--;
        _multipleTimer = _multipleInterval;
    }

    private void SpawnBullets(PlayerAttackData attackData)
    {
        BulletVisualEffects visualEffects = GetVisualEffects(attackData.Configuration.PrimaryElement);
        for (var i = 0; i < attackData.Configuration.BulletsPerShot; i++)
        {
            float angle = attackData.Configuration.IsSpread ? (i - 1) * _spreadAngle : 0f;
            Vector2 bulletDirection = Quaternion.Euler(0f, 0f, angle) * attackData.Direction;
            float rotationZ = Mathf.Atan2(bulletDirection.y, bulletDirection.x) * Mathf.Rad2Deg;

            Quaternion bulletRotation = Quaternion.Euler(0f, 0f, rotationZ);
            Bullet bullet = Instantiate(_bulletPrefab, _firePosition.transform.position, bulletRotation);

            PlayerAttackData bulletData = attackData.WithDirection(bulletDirection);
            bullet.Initialize(bulletData, visualEffects);
        }

        // 산탄 수와 관계없이 한 발사당 한 번 재생한다. 연발은 각 발사마다 이 메서드를 호출한다.
        if (visualEffects != null)
            BulletCastEffect.Play(_firePosition.transform, visualEffects.CastPrefab,
                visualEffects.CastScale, visualEffects.CastLifetime);
        _shootFeedbacks?.PlayFeedbacks(_firePosition.transform.position);
    }

    /// 탄환 적중음과 처치 연출을 몬스터 수명과 독립적으로 재생한다.
    public void PlayHitFeedback(Vector2 position, bool killed)
    {
        _hitFeedbacks?.PlayFeedbacks(position);
        if (killed)
            _killFeedbacks?.PlayFeedbacks(position);
    }

    /// 피해 및 상태 효과와 동일하게 첫 번째 원소 룬을 기준으로 연출을 선택한다.
    private BulletVisualEffects GetVisualEffects(ReactionElement? element) => element switch
    {
        ReactionElement.Fire => _fireVfx,
        ReactionElement.Water => _waterVfx,
        ReactionElement.Earth => _earthVfx,
        ReactionElement.Wind => _windVfx,
        _ => null
    };

    public void Reload()
    {
        if (_remainingBurstShots > 0 || _isReloading || _currentAmmo >= MagazineSize) return;

        _isReloading = true;
        _reloadTimer = 0f;
    }

    private void UpdateReload()
    {
        _reloadTimer += Time.deltaTime;
        if (_reloadTimer < _reloadTime)
            return;

        _currentAmmo = MagazineSize;
        _isReloading = false;
    }

    /// <summary>앞으로 발사할 탄환의 주력 원소를 변경한다.</summary>
    public void SetMainElement(ElementType element)
    {
        if (!Enum.IsDefined(typeof(ElementType), element))
            throw new ArgumentOutOfRangeException(nameof(element));

        _mainElement = element;
    }
}
