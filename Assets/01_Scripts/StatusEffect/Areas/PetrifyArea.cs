using Sirenix.OdinInspector;
using UnityEngine;

/// 석화 범위에 땅 스파이크 연출을 표시하고 지속 시간이 끝나면 제거한다.
public class PetrifyArea : MonoBehaviour
{
    [FoldoutGroup("References"), SerializeField] private ReactionAreaVisuals _areaVisuals;

    private float _remainingDuration;
    private bool _isInitialized;

    /// 석화 반경에 맞춰 연출 크기를 정하고 지속 시간 동안 유지한다.
    public void Initialize(float radius, float duration)
    {
        _remainingDuration = duration;
        _isInitialized = true;
        if (_areaVisuals != null)
            _areaVisuals.Initialize(radius);
    }

    private void Update()
    {
        if (!_isInitialized)
            return;

        _remainingDuration -= Time.deltaTime;
        if (_remainingDuration <= 0f)
            Destroy(gameObject);
    }
}
