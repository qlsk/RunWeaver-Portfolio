using UnityEngine;

/// <summary>
/// 스프라이트 프레임을 차례로 바꿔 그리는 간단한 재생기. (애니메이터 없이 쓰는 반응 이펙트용)
/// - 반복 재생 : 켜질 때마다 처음(또는 무작위 프레임)부터 다시 돈다.
/// - 한 번 재생 : 마지막 프레임까지 재생한 뒤 숨긴다. 다시 켜면 처음부터 재생한다.
/// 게임 시간으로 돌아가므로 일시정지 중에는 멈춘다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class SpriteFramePlayer : MonoBehaviour
{
    [SerializeField] private Sprite[] _frames;
    [SerializeField, Min(1f)] private float _framesPerSecond = 12f;
    [SerializeField] private bool _loop = true;
    [Tooltip("반복 재생일 때 시작 프레임을 무작위로 정한다. (여러 개가 겹쳐도 똑같이 움직이지 않게)")]
    [SerializeField] private bool _randomStart = true;

    private SpriteRenderer _renderer;
    private float _time;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (_renderer == null)
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        int count = _frames != null ? _frames.Length : 0;
        _time = _loop && _randomStart && count > 0 ? Random.Range(0, count) / _framesPerSecond : 0f;
        _renderer.enabled = true;
        Apply();
    }

    private void Update()
    {
        _time += Time.deltaTime;
        Apply();
    }

    private void Apply()
    {
        if (_frames == null || _frames.Length == 0)
        {
            return;
        }

        int index = Mathf.FloorToInt(_time * _framesPerSecond);
        if (_loop)
        {
            index %= _frames.Length;
        }
        else if (index >= _frames.Length)
        {
            _renderer.enabled = false;
            return;
        }

        _renderer.sprite = _frames[index];
    }
}
