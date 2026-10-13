using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 특성 테스트 씬(00_Scenes/Seong/TraitTestScene) 전용 도우미. 실제 게임 흐름에는 쓰지 않는다.
/// - 기본 몹을 플레이어 근처에 띄우고, 죽으면 잠시 뒤 새로 띄운다. (동시에 살아 있는 수를 바꿀 수 있다)
/// - 화면 왼쪽 패널에서 특성을 바로 받는다. 보상 선택 없이 TraitManager에 넣는다. (shield_01은 최대 보호막도 올린다)
/// - 연출 확인용 버튼 : 보호막 채우기, 피격 받기, 상태이상 걸기, 체력 조절.
/// - 받은 특성을 지우려면 씬을 다시 실행한다.
/// F1로 패널을 숨기거나 다시 연다.
/// </summary>
[DisallowMultipleComponent]
public class TraitTestBench : MonoBehaviour
{
    [Header("적")]
    [Tooltip("띄울 몹 프리팹 (EnemyHealth가 있어야 한다)")]
    [SerializeField] private GameObject _enemyPrefab;
    [Tooltip("동시에 살아 있을 몹 수")]
    [SerializeField, Range(0, 10)] private int _aliveCount = 1;
    [Tooltip("몹이 죽은 뒤 새로 나올 때까지의 시간 (초)")]
    [SerializeField, Min(0f)] private float _respawnDelay = 1f;
    [Tooltip("플레이어에게서 떨어진 거리 (최소, 최대)")]
    [SerializeField] private Vector2 _spawnDistance = new(3.5f, 5f);
    [Tooltip("비워 두면 플레이어 둘레에 띄운다. 넣으면 이 위치들 중 하나에 띄운다")]
    [SerializeField] private Transform[] _spawnPoints;

    [Header("특성")]
    [Tooltip("패널에 보여 줄 특성 (보상 풀과 같은 목록)")]
    [SerializeField] private TraitData[] _traits;

    [Header("플레이어 버튼")]
    [Tooltip("피격 받기 버튼의 피해량")]
    [SerializeField, Min(0f)] private float _testHitDamage = 10f;
    [Tooltip("상태이상 걸기 버튼에 쓰는 피해량 (화상 피해 계산용)")]
    [SerializeField, Min(0f)] private float _testElementDamage = 10f;

    [Header("패널")]
    [SerializeField] private bool _showPanel = true;
    [Tooltip("1080p 기준 패널 너비 (픽셀)")]
    [SerializeField, Min(160f)] private float _panelWidth = 300f;

    private readonly List<EnemyHealth> _enemies = new();
    private readonly List<float> _respawnTimes = new();
    private Transform _player;
    private PlayerHealth _health;
    private Shield _shield;
    private PlayerStatusEffectController _statusEffects;
    private Vector2 _scroll;
    private GUIStyle _titleStyle;
    private GUIStyle _ownedStyle;

    private void OnEnable()
    {
        EnemyHealth.OnDeath += HandleEnemyDeath;
    }

    private void OnDisable()
    {
        EnemyHealth.OnDeath -= HandleEnemyDeath;
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            _showPanel = !_showPanel;
        }

        FindPlayer();
        if (_player == null || _enemyPrefab == null)
        {
            return;
        }

        _enemies.RemoveAll(enemy => enemy == null || enemy.IsDead);

        // 모자란 만큼 기다렸다가 띄운다. (죽은 수만큼 대기 시간이 쌓여 있다)
        int missing = _aliveCount - _enemies.Count;
        while (_respawnTimes.Count < missing)
        {
            _respawnTimes.Add(Time.time);
        }

        while (_respawnTimes.Count > Mathf.Max(0, missing))
        {
            _respawnTimes.RemoveAt(_respawnTimes.Count - 1);
        }

        for (int i = _respawnTimes.Count - 1; i >= 0; i--)
        {
            if (Time.time >= _respawnTimes[i])
            {
                _respawnTimes.RemoveAt(i);
                SpawnEnemy();
            }
        }
    }

    private void HandleEnemyDeath(EnemyHealth enemy)
    {
        if (!_enemies.Remove(enemy))
        {
            return;
        }

        _respawnTimes.Add(Time.time + _respawnDelay);
    }

    private void FindPlayer()
    {
        if (_player != null)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        _player = player.transform;
        _health = player.GetComponentInChildren<PlayerHealth>();
        _shield = player.GetComponentInChildren<Shield>();
        _statusEffects = player.GetComponentInChildren<PlayerStatusEffectController>();
        if (_health == null)
        {
            _health = FindAnyObjectByType<PlayerHealth>();
        }

        if (_shield == null)
        {
            _shield = FindAnyObjectByType<Shield>();
        }

        if (_statusEffects == null)
        {
            _statusEffects = FindAnyObjectByType<PlayerStatusEffectController>();
        }
    }

    private void SpawnEnemy()
    {
        Vector3 position;
        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            Transform point = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
            position = point != null ? point.position : _player.position;
        }
        else
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(_spawnDistance.x, Mathf.Max(_spawnDistance.x, _spawnDistance.y));
            position = _player.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distance;
        }

        GameObject enemy = Instantiate(_enemyPrefab, position, Quaternion.identity);
        enemy.name = _enemyPrefab.name;
        EnemyHealth health = enemy.GetComponentInChildren<EnemyHealth>();
        if (health != null)
        {
            _enemies.Add(health);
        }
        else
        {
            Debug.LogWarning("[TraitTestBench] 몹 프리팹에 EnemyHealth가 없습니다.", this);
        }
    }

    private void Grant(TraitData data)
    {
        TraitManager traits = TraitManager.Instance;
        if (data == null || traits == null || !traits.AcquireTrait(data))
        {
            return;
        }

        // RewardController.ApplyReward와 같게 : shield_01은 1회성 최대치 증가라 받을 때 바로 올린다.
        if (data.Id == "shield_01" && _shield != null)
        {
            _shield.ShieldMaxStackUp();
        }
    }

    private void FillShield()
    {
        if (_shield == null)
        {
            return;
        }

        while (_shield.CurrentShield < _shield.MaxShield)
        {
            _shield.ShieldUp();
        }
    }

    private void OnGUI()
    {
        if (!_showPanel)
        {
            GUI.Label(new Rect(8f, 8f, 300f, 24f), "F1 : 특성 테스트 패널");
            return;
        }

        float scale = Mathf.Max(0.5f, Screen.height / 1080f);
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        _titleStyle ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 15 };
        _ownedStyle ??= new GUIStyle(GUI.skin.button) { normal = { textColor = new Color(1f, 0.85f, 0.45f) } };

        float height = Screen.height / scale - 16f;
        GUILayout.BeginArea(new Rect(8f, 8f, _panelWidth, height), GUI.skin.box);
        _scroll = GUILayout.BeginScrollView(_scroll);

        GUILayout.Label("특성 테스트  (F1 숨기기)", _titleStyle);
        GUILayout.Label("받은 특성은 씬을 다시 실행하면 지워진다.");

        DrawTraitButtons();

        GUILayout.Space(8f);
        GUILayout.Label("플레이어", _titleStyle);
        string shieldText = _shield != null ? $"{_shield.CurrentShield} / {_shield.MaxShield}" : "-";
        if (GUILayout.Button($"보호막 가득 채우기  ({shieldText})"))
        {
            FillShield();
        }

        if (GUILayout.Button($"피격 받기 ({_testHitDamage:0})  - 보호막 먼저 깨짐") && _health != null)
        {
            _health.TakeDamage(_testHitDamage);
        }

        if (GUILayout.Button("상태이상 걸기 (화상 + 감속)") && _statusEffects != null)
        {
            _statusEffects.ApplyElementEffect(ReactionElement.Fire, _testElementDamage);
            _statusEffects.ApplyElementEffect(ReactionElement.Water, _testElementDamage);
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("체력 50%") && _health != null && _health.CurrentHealth > _health.MaxHealth * 0.5f)
        {
            _health.TakeDamageOverTime(_health.CurrentHealth - _health.MaxHealth * 0.5f);
        }

        if (GUILayout.Button("체력 가득") && _health != null)
        {
            _health.Heal(_health.MaxHealth);
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(8f);
        GUILayout.Label("몹", _titleStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"동시에 {_aliveCount}마리 (살아 있음 {_enemies.Count})");
        if (GUILayout.Button("-", GUILayout.Width(28f)))
        {
            _aliveCount = Mathf.Max(0, _aliveCount - 1);
        }

        if (GUILayout.Button("+", GUILayout.Width(28f)))
        {
            _aliveCount = Mathf.Min(10, _aliveCount + 1);
        }

        GUILayout.EndHorizontal();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
        GUI.matrix = previous;
    }

    private void DrawTraitButtons()
    {
        if (_traits == null || _traits.Length == 0)
        {
            GUILayout.Label("특성 목록(_traits)이 비어 있다.");
            return;
        }

        TraitManager traits = TraitManager.Instance;
        if (GUILayout.Button("모든 특성 받기"))
        {
            foreach (TraitData data in _traits)
            {
                Grant(data);
            }
        }

        TraitCategory? lastCategory = null;
        foreach (TraitData data in _traits)
        {
            if (data == null)
            {
                continue;
            }

            if (lastCategory != data.Category)
            {
                lastCategory = data.Category;
                GUILayout.Label(data.Category.ToString(), _titleStyle);
            }

            bool owned = traits != null && traits.Has(data.Id);
            string label = owned ? $"[보유] {data.DisplayName}  ({data.Id})" : $"{data.DisplayName}  ({data.Id})";
            GUI.enabled = !owned;
            if (GUILayout.Button(label, owned ? _ownedStyle : GUI.skin.button))
            {
                Grant(data);
            }

            GUI.enabled = true;
        }
    }
}
