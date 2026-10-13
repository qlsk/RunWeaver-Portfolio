using UnityEngine;

/// <summary>
/// 적이 죽을 때의 기본 연출 설정. 정화 사망 연출(PurifyDeathEffect)을 기본 사망 연출로 쓴다.
/// - 프리팹(Resources/EnemyDeath/EnemyDeathFx)에 붙여 두고 값만 고친다. 씬에 둘 필요는 없다.
/// - EnemyHealth가 죽는 순간 TryPlay를 부른다. 사망 이벤트(OnDeath)는 그 전에 나가므로 드롭과 처치 수는 그대로다.
/// - 연출이 시작되는 순간 이미 죽은 것으로 본다. (IsDead가 참이고, 처치 미션·스포너 집계도 이때 끝난다)
/// - 보스는 런 클리어 연출(RunClearSequence)이 따로 있으므로 예전처럼 바로 사라진다.
/// - 끄거나 프리팹이 없으면 예전처럼 바로 사라진다.
/// - 번쩍인 뒤 플레이어 반대쪽으로 살짝 밀려나며 위로 늘어나 사라지고, 빛 입자가 흩어진다.
/// </summary>
[DisallowMultipleComponent]
public class EnemyDeathFx : MonoBehaviour
{
    private const string ResourcePath = "EnemyDeath/EnemyDeathFx";

    private static EnemyDeathFx s_settings;
    private static bool s_loaded;
    private static Transform s_player;
    private static StageManager s_stage;

    [Tooltip("끄면 적이 죽자마자 사라진다")]
    [SerializeField] private bool _usePurifyDeath = true;
    [Tooltip("몸에서 흩어지는 빛 입자 (08_VFX/PurifyMotes)")]
    [SerializeField] private GameObject _motesPrefab;
    [Tooltip("죽을 때 물드는 색")]
    [SerializeField] private Color _tint = new(0.72f, 0.88f, 1f, 1f);

    /// <summary>적에게 사망 연출을 건다. 연출이 적을 없애면 true, 직접 없애야 하면 false.</summary>
    public static bool TryPlay(EnemyHealth enemy)
    {
        if (enemy == null)
        {
            return false;
        }

        if (!s_loaded)
        {
            s_loaded = true;
            s_settings = Resources.Load<EnemyDeathFx>(ResourcePath);
        }

        if (s_settings == null || !s_settings._usePurifyDeath)
        {
            return false;
        }

        // 보스는 런 클리어 연출이 처치된 보스 그림을 복제해 따로 흩어지게 한다.
        // (StageManager.Instance는 없으면 새로 만들므로 씬에 있는 것만 찾는다)
        if (s_stage == null)
        {
            s_stage = FindAnyObjectByType<StageManager>();
        }

        if (s_stage != null && s_stage.BossHealth == enemy)
        {
            return false;
        }

        if (s_player == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            s_player = player != null ? player.transform : null;
        }

        // 플레이어 반대쪽으로 밀려난다. 플레이어가 없으면 위로 떠오르기만 한다.
        Vector3 origin = s_player != null ? s_player.position : enemy.transform.position + Vector3.down;
        PurifyDeathEffect.Purify(enemy, origin, s_settings._motesPrefab, s_settings._tint);
        return true;
    }
}
