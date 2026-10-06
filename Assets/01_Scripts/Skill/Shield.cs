using UnityEngine;

public class Shield : MonoBehaviour
{
    [Header("쉴드 쿨타임")]
    [SerializeField] private float _shieldCoolTime = 5f;
    private float _shieldCoolTimer = 0f;
    [Header("최대 Shield stack")]
    [SerializeField] private int _maxShield = 4;
    [Header("현재 Shield stack")]
    [SerializeField] private int _currentShield = 0;
    public int CurrentShield => _currentShield;

    private void Update()
    {
        _shieldCoolTimer += Time.deltaTime;
        if (_shieldCoolTimer >= _shieldCoolTime && _maxShield >= _currentShield)
        {
            ShieldUp();
            _shieldCoolTimer = 0f;
        }
    }

    // 쉴드 스택 하나 증가 (스킬 쿨타임 지나면 자동 사용)
    public void ShieldUp()
    {
        Debug.Log("ShieldUp");
        _currentShield++;
    }

    // 쉴드 하나 파괴 (피격 때 사용)
    public void DestroyShield()
    {
        if (_currentShield > 0)
        {
            _currentShield--;
        }
    }

    // 쉴드 최대 스택 하나 증가 (업그레이드 때 사용)
    public void ShieldMaxStackUp()
    {
        _maxShield++;
    }
}