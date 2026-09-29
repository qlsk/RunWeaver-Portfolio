// Return/Fix/Orbit은 상호 배타이므로 하나의 값으로 선택한다.
public enum BulletEndEffect
{
    None,
    Return,
    Fix,
    Orbit
}

// 발사 시 확정하는 룬 설정. 생성 후 변경할 수 없으므로 여러 탄환이 공유할 수 있다.
// 적중 횟수나 사용 상태는 여기가 아닌 BulletRuntime에 저장한다.
public sealed class BulletEffectConfig
{
    public BulletEndEffect EndEffect { get; }

    public bool Pierce { get; }
    public bool Reverse { get; }
    public bool Seek { get; }
    public bool Grow { get; }

    public float SeekRadius { get; } // 적 탐색 반경. 탄환 사거리와 별개다.
    public float OrbitDuration { get; } // 회전 지속시간(초). 원소 효과 Duration과 별개다.
    public float TrapDuration { get; } // Fix가 생성한 독립 오브젝트의 수명(초).
    public float GrowDistanceToMax { get; } // 최대 성장(피해 2배)에 필요한 이동거리.
    public float ReverseSteeringDelay { get; } // 반전 후 Seek/Return의 방향 보정을 멈추는 시간(초).

    // bool 기본값은 모두 false다. 수치 기본값은 해당 효과를 켰을 때 사용한다.
    public BulletEffectConfig(BulletEndEffect endEffect = BulletEndEffect.None,
        bool pierce = false, bool reverse = false, bool seek = false, bool grow = false,
        float seekRadius = 5f, float orbitDuration = 5f, float trapDuration = 10f, float growDistanceToMax = 5f,
        float reverseSteeringDelay = 0.1f
    )
    {
        EndEffect = endEffect;
        Pierce = pierce;
        Reverse = reverse;
        Seek = seek;
        Grow = grow;

        SeekRadius = UnityEngine.Mathf.Max(0f, seekRadius);
        OrbitDuration = UnityEngine.Mathf.Max(0f, orbitDuration);
        TrapDuration = UnityEngine.Mathf.Max(0f, trapDuration);
        GrowDistanceToMax = UnityEngine.Mathf.Max(0f, growDistanceToMax);
        ReverseSteeringDelay = UnityEngine.Mathf.Max(0f, reverseSteeringDelay);
    }
}
