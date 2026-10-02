using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyDrop : MonoBehaviour
{
    private EnemyHealth _enemyHealth;

    [SerializeField] private int _minQuantity = 0;
    [SerializeField] private int _maxQuantity = 1;
    [SerializeField] private RunePickup runePrefab;
    private List<RuneStoneDefinition> _runeStones = new List<RuneStoneDefinition>();
    [SerializeField] private int _amountOfGold = 10;

    private void Start()
    {
        _enemyHealth = GetComponent<EnemyHealth>();
        EnemyHealth.OnDeath += Drop;
    }

    private void OnDestroy()
    {
        // 정적 이벤트이므로 EnemyHealth가 먼저 파괴되었더라도 항상 구독을 해제한다.
        EnemyHealth.OnDeath -= Drop;
    }

    private void Drop(EnemyHealth health)
    {
        // OnDeath는 모든 적이 공유하는 이벤트이므로, 자기 자신이 죽었을 때만 드롭한다.
        if (health != _enemyHealth) return;
        RuneDrop();
        GoldDrop();
    }

    public void RuneDrop()
    {
        if (runePrefab == null) return;

        var database = RuneStoneDatabase.Instance;
        if (database == null) return;

        // DB 초기화가 완료된 목록을 드롭 시점에 가져옵니다.
        var runeStones = database.GetAll();
        if (runeStones == null || runeStones.Count == 0) return;

        _runeStones = new List<RuneStoneDefinition>(runeStones);
        int index = UnityEngine.Random.Range(0, _runeStones.Count);
        char runeCharacter = _runeStones[index].RuneCharacter;

        // int Random.Range는 최댓값을 포함하지 않으므로 +1 해서 _maxQuantity까지 나오게 한다.
        int quantity = UnityEngine.Random.Range(_minQuantity, _maxQuantity + 1);
        if (quantity == 0) return;

        Sprite runeIcon = _runeStones[index].Icon;

        RunePickup pickup = Instantiate(runePrefab, transform.position, Quaternion.identity);
        pickup.Initialize(runeCharacter, quantity, runeIcon);
    }

    public void GoldDrop()
    {
        var session = GameSession.Instance;
        if (session == null || !session.IsReady) return;
        
        var holdings = session.Current?.Holdings;
        if (holdings == null || !holdings.IsOnExpedition) return;
        if (_amountOfGold <= 0) return;
        
        holdings.ExpeditionWallet?.AddGold(_amountOfGold);
    }
}