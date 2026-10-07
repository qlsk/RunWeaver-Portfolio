using UnityEngine;

/// <summary>
/// 사냥의 낙인 표식. 적 오브젝트에 런타임으로 붙는다. 시간이 끝나면 스스로 떨어진다.
/// </summary>
[DisallowMultipleComponent]
public class MarkedPrey : MonoBehaviour
{
    private float _remaining;

    public bool IsActive => _remaining > 0f;

    public void Apply(float duration)
    {
        _remaining = Mathf.Max(_remaining, Mathf.Max(0f, duration));
    }

    private void Update()
    {
        if (_remaining > 0f)
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                Destroy(this);
            }
        }
    }
}
