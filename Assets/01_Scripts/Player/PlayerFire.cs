using UnityEngine;

public class PlayerFire : MonoBehaviour
{
    [Header("마탄 쿨타임")]
    [SerializeField] private float _bulletFireRate = 1f;
    [Header("마탄 프리팹")]
    [SerializeField] private Bullet _bulletPrefab;
    [Header("총구 위치")]
    [SerializeField] private GameObject _firePosition;
    // 마탄 쿨타이머
    private float _bulletFireCoolTimer = 0f;

    private void Update()
    {
        // 마탄 쿨타임이 다 지났다면
        if (_bulletFireCoolTimer >= _bulletFireRate)
        {
            // 총구에서 마탄 프리팹 소환
            Instantiate(_bulletPrefab, _firePosition.transform.position, transform.rotation);
            // 마탄 쿨타이머 초기화
            _bulletFireCoolTimer = 0f;
        }

        _bulletFireCoolTimer += Time.deltaTime;
    }
}
