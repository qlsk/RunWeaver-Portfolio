using System;
using UnityEngine;

/// <summary>
/// 정화 파동 VFX. 시작 위치에서 빛 고리가 빠르게 퍼져 나간다.
/// - 고리(_ring) : 반지름에 맞춰 커지고, _fadeStartRadius부터 최대 반지름까지 서서히 사라진다.
///   고리 이미지는 일부러 흐리게 만든 부드러운 빛 고리라서, 커질수록 은은하게 번지며 퍼진다.
/// - 안쪽 빛(_fill) : 고리가 지나간 바닥에 남는 은은한 빛. 시간이 지나며 옅어진다.
/// - 시작 빛(_flash) : 시작 위치에서 잠깐 번쩍인다.
/// 판정은 CurrentRadius / IsInside로 확인한다.
/// PurifyEnemies가 켜져 있으면 고리가 지나간 곳의 적은 정화 사망 연출(PurifyDeathEffect)과 함께 사라지고,
///   적 탄환은 바로 사라진다. (드롭과 처치 수에는 들어가지 않는다) StageManager가 미션 클리어 시 켠다.
/// _playOnMissionClear를 켜면 미션을 클리어한 순간(StageManager가 정화 단계로 넘어갈 때) 재생한다.
///   미션 오브젝트에 자식으로 붙여 두면 오브젝트 중심에서 퍼진다.
///   미션 오브젝트는 클리어 직후 삭제되므로, 재생할 때 부모에서 떨어져 나와 크기를 1로 되돌린 뒤 혼자 퍼진다.
/// 게임 시간(Time.deltaTime)으로 돌아가므로 일시정지 중에는 멈춘다.
/// </summary>
[DisallowMultipleComponent]
public class PurificationWave : MonoBehaviour
{
    [Header("구성")]
    [SerializeField] private SpriteRenderer _ring;
    [SerializeField] private SpriteRenderer _fill;
    [SerializeField] private SpriteRenderer _flash;
    [Tooltip("고리 이미지에서 빛이 가장 밝은 곳의 반지름 비율 (이미지 반지름 = 1). 판정 반지름과 보이는 고리를 맞춘다.")]
    [SerializeField, Range(0.1f, 1f)] private float _ringVisualRatio = 0.47f;

    [Header("퍼짐")]
    [Tooltip("최대 반지름(유닛). 여기까지 퍼지면 끝난다. 화면 밖 적까지 닿도록 넉넉히 둔다.")]
    [SerializeField, Min(0.1f)] private float _maxRadius = 18f;
    [Tooltip("퍼지는 속도(초당 유닛)")]
    [SerializeField, Min(0.1f)] private float _speed = 9f;
    [Tooltip("시작 직후 순간적으로 더 퍼지는 거리(유닛). 처음에 터지듯 퍼지는 느낌을 준다.")]
    [SerializeField, Min(0f)] private float _burstRadius = 1.6f;
    [SerializeField, Min(0.1f)] private float _burstSharpness = 8f;

    [Header("고리")]
    [SerializeField] private Color _ringColor = new(1f, 0.86f, 0.55f, 0.6f);
    [Tooltip("이 반지름부터 최대 반지름까지 고리가 서서히 사라진다")]
    [SerializeField, Min(0f)] private float _fadeStartRadius = 10f;

    [Header("안쪽 빛")]
    [SerializeField] private Color _fillColor = new(1f, 0.86f, 0.55f, 0.18f);
    [Tooltip("안쪽 빛이 완전히 사라지는 시간(초)")]
    [SerializeField, Min(0.01f)] private float _fillDuration = 1.6f;

    [Header("시작 빛")]
    [SerializeField] private Color _flashColor = new(1f, 0.95f, 0.8f, 0.9f);
    [SerializeField, Min(0f)] private float _flashRadius = 2.5f;
    [SerializeField, Min(0.01f)] private float _flashDuration = 0.45f;

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
    [Tooltip("정화된 적에서 흩어지는 빛 입자 이미지. 비워두면 시작 빛(_flash)의 이미지를 쓴다.")]
    [SerializeField] private Sprite _moteSprite;

    private StageManager _stageManager;

    private float _elapsed;

    /// <summary>현재 고리 반지름(유닛)</summary>
    public float CurrentRadius { get; private set; }

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
        SetVisible(false);
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
        _elapsed = 0f;
        CurrentRadius = 0f;
        IsPlaying = true;
        SetVisible(true);
        Apply();
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
        return IsPlaying && offset.sqrMagnitude <= CurrentRadius * CurrentRadius;
    }

    [ContextMenu("Play")]
    private void PlayFromContextMenu()
    {
        Play();
    }

    private void Update()
    {
        if (!IsPlaying)
        {
            return;
        }

        _elapsed += Time.deltaTime;
        CurrentRadius = Mathf.Min(_maxRadius,
            _speed * _elapsed + _burstRadius * (1f - Mathf.Exp(-_elapsed * _burstSharpness)));
        Apply();
        RadiusChanged?.Invoke(CurrentRadius);

        if (_purifyEnemies)
        {
            PurifyInside();
        }

        if (CurrentRadius >= _maxRadius)
        {
            Finish();
        }
    }

    /// <summary>고리 안쪽에 들어온 적은 정화 사망 연출을 시작하고, 적 탄환은 없앤다.</summary>
    private void PurifyInside()
    {
        Vector2 center = transform.position;
        float sqrRadius = CurrentRadius * CurrentRadius;
        Sprite mote = _moteSprite != null ? _moteSprite : (_flash != null ? _flash.sprite : null);

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            if (((Vector2)enemy.transform.position - center).sqrMagnitude <= sqrRadius)
            {
                PurifyDeathEffect.Purify(enemy, transform.position, mote, _ringColor);
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
        SetVisible(false);
        Finished?.Invoke();

        if (_destroyOnFinish)
        {
            Destroy(gameObject);
        }
    }

    private void Apply()
    {
        float radius = CurrentRadius;

        // 고리 : 이미지에서 밝은 부분이 반지름에 오도록 크기를 맞춘다.
        if (_ring != null)
        {
            SetDiameter(_ring, radius * 2f / _ringVisualRatio);
            float fade = 1f - Mathf.InverseLerp(_fadeStartRadius, _maxRadius, radius);
            _ring.color = WithAlpha(_ringColor, _ringColor.a * fade);
        }

        // 안쪽 빛 : 고리 크기를 따라가며 시간이 지나면 옅어진다.
        if (_fill != null)
        {
            SetDiameter(_fill, radius * 2f);
            float fade = 1f - Mathf.Clamp01(_elapsed / _fillDuration);
            _fill.color = WithAlpha(_fillColor, _fillColor.a * fade * fade);
        }

        // 시작 빛 : 시작 위치에서 잠깐 번쩍이며 살짝 커진다.
        if (_flash != null)
        {
            float t = Mathf.Clamp01(_elapsed / _flashDuration);
            SetDiameter(_flash, _flashRadius * 2f * (1f + 0.5f * t));
            _flash.color = WithAlpha(_flashColor, _flashColor.a * (1f - t) * (1f - t));
        }
    }

    private void SetVisible(bool isVisible)
    {
        if (_ring != null) _ring.enabled = isVisible;
        if (_fill != null) _fill.enabled = isVisible;
        if (_flash != null) _flash.enabled = isVisible;
    }

    /// <summary>스프라이트의 원래 크기를 기준으로 원하는 지름(유닛)이 되도록 크기를 바꾼다.</summary>
    private static void SetDiameter(SpriteRenderer renderer, float diameter)
    {
        if (renderer.sprite == null)
        {
            return;
        }

        float spriteSize = Mathf.Max(0.0001f, renderer.sprite.bounds.size.x);
        float scale = diameter / spriteSize;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
