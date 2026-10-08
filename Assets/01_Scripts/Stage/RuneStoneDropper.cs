using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 룬석 퍼짐 드랍 처리. 선택 즉시 플레이어 위치를 중심으로 랜덤 방향/거리로 흩뿌린다.
///
/// - 드랍된 룬석은 기존 획득(줍기) 로직을 그대로 쓴다: RunePickup 프리팹 Instantiate → Initialize()
///   (EnemyDrop과 같은 방식, Stage 외부 수정 없음)
/// - 룬 종류는 RuneStoneDatabase에서 랜덤 추첨한다 (읽기 전용).
/// - 스테이지 전환 직전 드랍이어도拾得 로직이 살아 있어 정상 처리된다.
/// </summary>
[DisallowMultipleComponent]
public class RuneStoneDropper : MonoBehaviour
{
    [Header("룬 픽업 프리팹 (EnemyDrop과 같은 것)")]
    [Tooltip("Assets/02_Prefabs/Rune/Rune.prefab")]
    [SerializeField, FormerlySerializedAs("runePickupPrefab")] private RunePickup _runePickupPrefab;

    [Header("퍼짐 연출 (인스펙터 노출)")]
    [Tooltip("중심에서 최소 흩어짐 거리")]
    [SerializeField, FormerlySerializedAs("scatterRadiusMin"), Min(0f)] private float _scatterRadiusMin = 1f;
    [Tooltip("중심에서 최대 흩어짐 거리")]
    [SerializeField, FormerlySerializedAs("scatterRadiusMax"), Min(0f)] private float _scatterRadiusMax = 2.5f;
    [Tooltip("스폰 시 팡 커지는 연출 시간 (초). 0이면 연출 없음")]
    [SerializeField, FormerlySerializedAs("scatterDuration"), Min(0f)] private float _scatterDuration = 0.4f;
    [Tooltip("픽업 1개당 룬석 수량")]
    [SerializeField, FormerlySerializedAs("quantityPerPickup"), Min(1)] private int _quantityPerPickup = 1;

    /// <summary>
    /// 오브젝트가 없으므로 플레이어 위치를 드랍 중심점으로 쓴다. 못 찾으면 원점.
    /// </summary>
    public void Scatter(int totalCount)
    {
        if (totalCount <= 0)
        {
            return;
        }

        if (_runePickupPrefab == null)
        {
            Debug.LogWarning("[RuneStoneDropper] RunePickup 프리팹이 비어 있어 드랍하지 못했습니다.", this);
            return;
        }

        if (_runePickupPrefab.GetComponent<SpriteRenderer>() == null)
        {
            Debug.LogError("[RuneStoneDropper] 연결된 프리팹에 SpriteRenderer가 없어 Initialize가 실패합니다. Rune.prefab을 연결하세요.", this);
            return;
        }

        RuneStoneDatabase database = RuneStoneDatabase.Instance;
        if (database == null || !database.DatabaseReady)
        {
            Debug.LogWarning("[RuneStoneDropper] RuneStoneDatabase가 준비되지 않아 드랍하지 못했습니다.", this);
            return;
        }

        IReadOnlyList<RuneStoneDefinition> defs = database.GetAll();
        if (defs == null || defs.Count == 0)
        {
            Debug.LogWarning("[RuneStoneDropper] 룬석 정의가 비어 있어 드랍하지 못했습니다.", this);
            return;
        }

        PlayerRuneManager runeManager = PlayerRuneManager.Instance;
        if (runeManager == null || !runeManager.IsInitialized)
        {
            Debug.LogError("[RuneStoneDropper] PlayerRuneManager 미초기화. 부트스트랩→마을→전투 흐름으로 진입해야 획득됩니다. (전투씬 직접 실행 시 획득 불가)", this);
            return;
        }

        Vector3 center = GetDropCenter();
        float minR = Mathf.Min(_scatterRadiusMin, _scatterRadiusMax);
        float maxR = Mathf.Max(_scatterRadiusMin, _scatterRadiusMax);

        Sprite prefabSprite = _runePickupPrefab.GetComponent<SpriteRenderer>()?.sprite;
        int nullIconCount = 0;

        for (int i = 0; i < totalCount; i++)
        {
            RuneStoneDefinition def = defs[Random.Range(0, defs.Count)];
            // DB 아이콘이 비어 있으면 투명 픽업이 되므로 프리팹 기본 스프라이트로 대체한다.
            Sprite icon = def.Icon != null ? def.Icon : prefabSprite;
            if (icon == null)
            {
                nullIconCount++;
            }
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(minR, maxR);
            Vector3 target = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * dist;

            RunePickup pickup = null;
            try
            {
                // 목표 지점에 바로 스폰한다. 중심에서 출발시키면 플레이어와 겹친 채로 나와
                // 즉시 자동 수집되어 연출이 보이지 않는다.
                pickup = Instantiate(_runePickupPrefab, target, Quaternion.identity);
                pickup.Initialize(def.RuneCharacter, Mathf.Max(1, _quantityPerPickup), icon);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RuneStoneDropper] 픽업 초기화 실패 ({i + 1}/{totalCount}): {e.GetType().Name}: {e.Message}", this);
                if (pickup != null)
                {
                    Destroy(pickup.gameObject);
                }
                continue;
            }

            if (_scatterDuration > 0f)
            {
                StartCoroutine(PopRoutine(pickup, _scatterDuration));
            }
        }

        Debug.Log($"[RuneStoneDropper] 룬석 {totalCount}개 퍼짐 드랍 (아이콘 없음 {nullIconCount}개는 프리팹 기본값 사용)");
    }

    private Vector3 GetDropCenter()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform.position;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// 등장 팡 연출. 위치 이동은 하지 않는다 (이동시키면 RunePickup 트리거 판정과 꼬인다).
    /// </summary>
    private IEnumerator PopRoutine(RunePickup pickup, float duration)
    {
        if (pickup == null)
        {
            yield break;
        }

        Vector3 baseScale = pickup.transform.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (pickup == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - (1f - t) * (1f - t);
            pickup.transform.localScale = baseScale * Mathf.Lerp(0.3f, 1f, eased);
            yield return null;
        }

        if (pickup != null)
        {
            pickup.transform.localScale = baseScale;
        }
    }
}
