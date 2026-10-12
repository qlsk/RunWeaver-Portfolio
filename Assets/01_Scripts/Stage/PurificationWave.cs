using System;
using UnityEngine;

/// <summary>
/// 정화 파동. 시작 위치에서 빛 고리가 빠르게 퍼져 나간다.
/// 모습(고리·안쪽 빛·시작 빛의 크기와 투명도)과 반지름은 애니메이션 클립(PurificationWave_Wave)이 정한다.
/// 이 스크립트는 클립을 재생하고, 클립이 움직이는 반지름(_radius)으로 판정만 한다.
/// - 판정은 CurrentRadius / IsInside로 확인한다.
/// - PurifyEnemies가 켜져 있으면 고리가 지나간 곳의 적은 정화 사망 연출(PurifyDeathEffect)과 함께 사라지고,
///   적 탄환은 바로 사라진다. (드롭과 처치 수에는 들어가지 않는다) StageManager가 미션 클리어 시 켠다.
/// - _playOnMissionClear를 켜면 미션을 클리어한 순간(StageManager가 정화 단계로 넘어갈 때) 재생한다.
///   미션 오브젝트는 클리어 직후 삭제되므로, 재생할 때 부모에서 떨어져 나와 크기를 1로 되돌린 뒤 혼자 퍼진다.
/// 게임 시간으로 돌아가므로 일시정지 중에는 멈춘다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class PurificationWave : MonoBehaviour
{
    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int WaveHash = Animator.StringToHash("Wave");

    [Header("구성")]
    [SerializeField] private Animator _animator;
    [Tooltip("현재 반지름(유닛). 애니메이션 클립이 움직인다.")]
    [SerializeField] private float _radius;

    [Header("정화")]
    [Tooltip("정화된 적이 물드는 색")]
    [SerializeField] private Color _purifyTint = new(0.72f, 0.88f, 1f, 1f);

    [Header("테스트")]
    [Tooltip("켜면 시작할 때 바로 재생한다")]
    [SerializeField] private bool _playOnStart;
    [Tooltip("켜면 재생이 끝난 뒤 오브젝트를 없앤다")]
    [SerializeField] private bool _destroyOnFinish;

    [Header("미션 클리어")]
    [Tooltip("켜면 미션을 클리어한 순간 재생한다. (부모에서 떨어져 나와 재생하고, 끝나면 사라진다)")]
    [SerializeField] private bool _playOnMissionClear;

    [Header("정화")]
    [Tooltip("켜면 고리가 지나간 곳의 적과 적 탄환을 정화한다. 미션 클리어로 재생될 때는 StageManager가 켠다.")]
    [SerializeField] private bool _purifyEnemies;
    [Tooltip("정화된 적에서 흩어지는 빛 입자 프리팹 (PurifyMotes)")]
    [SerializeField] private GameObject _motesPrefab;

    private StageManager _stageManager;

    /// <summary>현재 고리 반지름(유닛)</summary>
    public float CurrentRadius => IsPlaying ? _radius : 0f;

    public bool IsPlaying { get; private set; }

    /// <summary>매 프레임 반지름이 바뀔 때 (반지름)</summary>
    public event Action<float> RadiusChanged;

    /// <summary>최대 반지름까지 퍼져 끝났을 때</summary>
    public event Action Finished;

    /// <summary>고리가 지나간 곳의 적과 적 탄환을 정화할지</summary>
    public bool PurifyEnemies
    {
        get => _purifyEnemies;
        set => _purifyEnemies = value;
    }

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
    }

    private void Start()
    {
        if (_playOnMissionClear)
        {
            // Instance는 없으면 새로 만들기 때문에 씬에 있는 것만 찾는다.
            _stageManager = FindAnyObjectByType<StageManager>();
            if (_stageManager != null)
            {
                _stageManager.PhaseChanged += HandlePhaseChanged;
            }
        }

        // 코드로 만든 직후 이미 재생했으면 다시 시작하지 않는다.
        if (_playOnStart && !IsPlaying)
        {
            Play();
        }
    }

    private void OnDestroy()
    {
        if (_stageManager != null)
        {
            _stageManager.PhaseChanged -= HandlePhaseChanged;
        }
    }

    private void HandlePhaseChanged(StagePhase phase)
    {
        // 꺼져 있으면 재생하지 않는다. (StageManager가 따로 만든 파동을 쓸 때 붙은 파동을 끈다)
        if (phase != StagePhase.Purification || IsPlaying || !isActiveAndEnabled)
        {
            return;
        }

        PlayDetached();
    }

    /// <summary>
    /// 부모에서 떨어져 나와 재생하고, 끝나면 사라진다. (미션 오브젝트는 클리어 직후 삭제되므로)
    /// 부모 크기의 영향도 없앤다.
    /// </summary>
    public void PlayDetached()
    {
        transform.SetParent(null, true);
        transform.localScale = Vector3.one;
        _destroyOnFinish = true;
        Play();
    }

    /// <summary>지금 위치에서 파동을 시작한다.</summary>
    public void Play()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        IsPlaying = true;
        _radius = 0f;
        _animator.Play(WaveHash, 0, 0f);
        _animator.Update(0f);
    }

    /// <summary>지정한 위치로 옮겨 파동을 시작한다.</summary>
    public void Play(Vector3 position)
    {
        transform.position = position;
        Play();
    }

    /// <summary>위치가 지금 고리 안쪽(이미 파동이 지나간 곳)인지 (2D 거리)</summary>
    public bool IsInside(Vector3 worldPosition)
    {
        Vector2 offset = worldPosition - transform.position;
        return IsPlaying && offset.sqrMagnitude <= _radius * _radius;
    }

    [ContextMenu("Play")]
    private void PlayFromContextMenu()
    {
        Play();
    }

    // 애니메이터가 반지름을 바꾼 뒤(Update 다음)에 판정한다.
    private void LateUpdate()
    {
        if (!IsPlaying)
        {
            return;
        }

        RadiusChanged?.Invoke(_radius);

        if (_purifyEnemies)
        {
            PurifyInside();
        }

        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        if (state.shortNameHash == WaveHash && state.normalizedTime >= 1f)
        {
            Finish();
        }
    }

    /// <summary>고리 안쪽에 들어온 적은 정화 사망 연출을 시작하고, 적 탄환은 없앤다.</summary>
    private void PurifyInside()
    {
        Vector2 center = transform.position;
        float sqrRadius = _radius * _radius;

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            if (((Vector2)enemy.transform.position - center).sqrMagnitude <= sqrRadius)
            {
                PurifyDeathEffect.Purify(enemy, transform.position, _motesPrefab, _purifyTint);
            }
        }

        EnemyBullet[] bullets = FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None);
        foreach (EnemyBullet bullet in bullets)
        {
            if (bullet != null && ((Vector2)bullet.transform.position - center).sqrMagnitude <= sqrRadius)
            {
                Destroy(bullet.gameObject);
            }
        }
    }

    private void Finish()
    {
        IsPlaying = false;
        _animator.Play(IdleHash, 0, 0f);
        Finished?.Invoke();

        if (_destroyOnFinish)
        {
            Destroy(gameObject);
        }
    }
}
