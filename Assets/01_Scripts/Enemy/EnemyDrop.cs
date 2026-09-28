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

    private void Start()
    {
        _enemyHealth = GetComponent<EnemyHealth>();
        _enemyHealth.OnDeath += Drop;
    }

    private void OnDestroy()
    {
        if (_enemyHealth != null) _enemyHealth.OnDeath -= Drop;
    }

    private void Drop()
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

        int quantity = UnityEngine.Random.Range(_minQuantity, _maxQuantity);
        if (quantity == 0) return;

        Sprite runeIcon = _runeStones[index].Icon;

        RunePickup pickup = Instantiate(runePrefab, transform.position, Quaternion.identity);
        pickup.Initialize(runeCharacter, quantity, runeIcon);
    }
}
