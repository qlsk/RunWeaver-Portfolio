using UnityEngine;

/// <summary>
/// PlayerHealth의 체력을 HUD 체력바에 표시한다.
/// PlayerHealth에 체력 변경 이벤트가 없어서 매 프레임 값을 비교하고, 바뀌었을 때만 갱신한다.
/// PlayerHealth에 변경 이벤트가 추가되면 이벤트 구독 방식으로 교체한다.
/// </summary>
[DisallowMultipleComponent]
public class PlayerHealthHudBinder : MonoBehaviour
{
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private HealthBarView _view;

    private float _shownCurrentHealth = float.NaN;
    private float _shownMaxHealth = float.NaN;

    private void Awake()
    {
        // 씬에서 연결하지 않았으면 플레이어를 찾아서 쓴다. (마을 HUD 등)
        if (_playerHealth == null)
        {
            _playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        if (_playerHealth == null)
        {
            Debug.LogError("[PlayerHealthHudBinder] PlayerHealth가 할당되지 않았습니다.", this);
        }

        if (_view == null)
        {
            Debug.LogError("[PlayerHealthHudBinder] HealthBarView가 할당되지 않았습니다.", this);
        }

        // 체력 위험 연출 (화면 가장자리 붉은 기운 + 혈관, 체력바 밝아짐)
        if (_playerHealth != null)
        {
            LowHealthOverlay overlay = GetComponent<LowHealthOverlay>();
            if (overlay == null)
            {
                overlay = gameObject.AddComponent<LowHealthOverlay>();
            }

            overlay.Initialize(_playerHealth, _view);
        }
    }

    private void LateUpdate()
    {
        if (_playerHealth == null || _view == null)
        {
            return;
        }

        float currentHealth = _playerHealth.CurrentHealth;
        float maxHealth = _playerHealth.MaxHealth;
        if (Mathf.Approximately(currentHealth, _shownCurrentHealth) && Mathf.Approximately(maxHealth, _shownMaxHealth))
        {
            return;
        }

        _shownCurrentHealth = currentHealth;
        _shownMaxHealth = maxHealth;
        _view.SetHealth(currentHealth, maxHealth);
    }
}
