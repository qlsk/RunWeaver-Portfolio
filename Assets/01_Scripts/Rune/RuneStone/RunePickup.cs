using System;
using UnityEngine;

public class RunePickup : MonoBehaviour
{
    [SerializeField] private char _runeCharacter;
    [SerializeField, Min(1)] private int _quantity = 1;

    private GameObject _player;
    [SerializeField] private float _moveSpeed = 5f;
    private SpriteRenderer _spriteRenderer;

    private bool _isFollowing;
    private Vector3 _followStart;
    private float _followDuration;
    private float _followElapsed;

    private bool _isBeingPickedUp;
    private bool _isCollected;

    public char RuneCharacter => _runeCharacter;
    public int Quantity => _quantity;

    [SerializeField] private float _pickupTime = 3f;
    [SerializeField, Min(0f)] private float _attractRadius = 2f;
    private float _pickupCoolTimer = 0;

    public void Initialize(char runeCharacter, int quantity, Sprite runeIcon)
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (runeCharacter == '\0' || char.IsWhiteSpace(runeCharacter))
        {
            throw new ArgumentException("룬 문자가 비어 있습니다.", nameof(runeCharacter));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "획득 수량은 양수여야 합니다.");
        }

        if (_isBeingPickedUp || _isCollected)
        {
            throw new InvalidOperationException("획득 중이거나 획득한 룬은 초기화할 수 없습니다.");
        }

        _spriteRenderer.sprite = runeIcon;
        _runeCharacter = runeCharacter;
        _quantity = quantity;
    }

    // 인벤토리에 추가하기 전에 호출해 중복 획득을 막습니다.
    public bool TryBeginPickup()
    {
        if (!isActiveAndEnabled || _isBeingPickedUp || _isCollected) return false;
        if (_runeCharacter == '\0' || char.IsWhiteSpace(_runeCharacter) || _quantity <= 0) return false;

        _isBeingPickedUp = true;
        return true;
    }

    // TryAddRuneStone에 성공한 경우에만 호출합니다.
    public void CompletePickup()
    {
        if (!_isBeingPickedUp || _isCollected) return;

        _isCollected = true;
        _isBeingPickedUp = false;
        Sfx.Play(Sfx.RunePickedUp);
        Destroy(gameObject);
    }

    // 인벤토리 추가에 실패하면 다시 획득할 수 있도록 잠금을 해제합니다.
    public void CancelPickup()
    {
        if (_isCollected) return;

        _isBeingPickedUp = false;
    }

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        _pickupCoolTimer += Time.deltaTime;
        bool isPlayerNearby = _player != null
            && ((Vector2)(_player.transform.position - transform.position)).sqrMagnitude
            <= _attractRadius * _attractRadius;

        if (_isFollowing || isPlayerNearby || _pickupCoolTimer >= _pickupTime)
        {
            FollowPlayer();
        }
    }

    private void FollowPlayer()
    {
        if (_player == null || _moveSpeed <= 0f) return;

        Vector3 target = _player.transform.position;
        target.z = transform.position.z;

        if (!_isFollowing)
        {
            _isFollowing = true;
            _followStart = transform.position;
            float distance = Vector3.Distance(_followStart, target);
            _followDuration = Mathf.Max(0.2f, distance / _moveSpeed);
            _followElapsed = 0f;
        }

        _followElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_followElapsed / _followDuration);
        float easedProgress = progress >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * progress);
        transform.position = Vector3.Lerp(_followStart, target, easedProgress);
    }
}
