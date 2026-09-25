using System;
using UnityEngine;

/// <summary>
/// 원정 단위로 플레이어의 골드를 관리하는 클래스이다. 원정 시작 시 생성 후 초기화한다.
/// </summary>
public class Wallet
{
    private GoldData _goldData;
    private int _balance;

    public int Balance => _balance;
    public event Action<int> BalanceChanged;

    public Wallet(GoldData goldData)
    {
        if (goldData == null)
        {
            throw new ArgumentNullException(nameof(goldData), "Gold data is required.");
        }
        _goldData = goldData;
        _balance = 1000;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be positive.");
        }
        _balance += amount;
        BalanceChanged?.Invoke(_balance);
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be positive.");
        }
        if (_balance < amount) return false;
        _balance -= amount;
        BalanceChanged?.Invoke(_balance);
        return true;
    }
}
