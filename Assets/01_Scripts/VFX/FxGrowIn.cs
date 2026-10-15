using UnityEngine;

/// <summary>
/// 켜질 때 작은 크기에서 원래 크기로 커지며 나타난다. (반응 장판이 생기는 순간)
/// 게임 시간으로 돌아간다.
/// </summary>
[DisallowMultipleComponent]
public class FxGrowIn : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float _startScale = 0.25f;
    [SerializeField, Min(0.01f)] private float _duration = 0.35f;

    private Vector3 _baseScale = Vector3.one;
    private float _time;
    private bool _hasBase;

    private void Awake()
    {
        _baseScale = transform.localScale;
        _hasBase = true;
    }

    private void OnEnable()
    {
        if (!_hasBase)
        {
            _baseScale = transform.localScale;
            _hasBase = true;
        }

        _time = 0f;
        Apply();
    }

    private void Update()
    {
        if (_time >= _duration)
        {
            return;
        }

        _time += Time.deltaTime;
        Apply();
    }

    private void Apply()
    {
        float t = Mathf.Clamp01(_time / _duration);
        float eased = 1f - (1f - t) * (1f - t) * (1f - t);
        transform.localScale = _baseScale * Mathf.Lerp(_startScale, 1f, eased);
    }
}
