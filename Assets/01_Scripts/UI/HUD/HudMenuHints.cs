using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 오른쪽 위의 메뉴 안내 (클릭은 되지 않는 표시). [룬북 아이콘][TAB]  [일시정지 아이콘][ESC]
/// - 그림과 배치는 Resources/HudHints/HudMenuHints 프리팹, 움직임은 그 Animator의 클립이 한다.
///   이 컴포넌트는 "Dim"(옅게 보일지)과 "Notify"(알림이 켜졌는지) 값만 정해 넘긴다.
/// - 초반 층에서는 또렷하고, _fadeFromDepth 층부터는 옅게 보인다.
/// - 층 사이 상점이 열려 있는 동안(StagePhase.Shop)은 통째로 숨긴다. 상점에서는 Tab(룬북)이 막혀 있고
///   Esc는 일시정지가 아니라 상점 나가기라 안내가 맞지 않고, 상점의 자원 표시와 자리가 겹친다.
/// - 새로 장착할 수 있게 된 룬 워드가 생기면(룬 문자가 모이거나 완성품을 얻어서) 룬북 아이콘에 하늘색 점과 푸른빛이 켜진다.
///   룬북을 열면 꺼지고, 그 뒤 또 새로 장착 가능한 룬 워드가 생기면 다시 켜진다.
/// - PulseRuneBook : 룬북 아이콘 뒤의 금빛(프리팹의 RuneBookPulse 이미지)이 한 번 부풀었다 사라진다. (룬석 보상 카드를 골랐을 때)
///   게임이 멈춘 동안에도 움직인다. (실제 시간)
/// RuneBookV2Toggle이 만든다. (룬북을 쓸 수 있는 전투 씬에서만 보인다)
/// </summary>
[DisallowMultipleComponent]
public class HudMenuHints : MonoBehaviour
{
    private const string PrefabPath = "HudHints/HudMenuHints";
    private const float CheckInterval = 0.5f;
    private const string PulseName = "RuneBookPulse";
    private static readonly int s_dimHash = Animator.StringToHash("Dim");
    private static readonly int s_notifyHash = Animator.StringToHash("Notify");

    // 룬북을 잠그는 동안 숨길 힌트 (프리팹의 자식 이름). 애니메이터는 색과 크기만 바꾸므로 끄고 켜는 것과 겹치지 않는다.
    private static readonly string[] s_runeBookHintNames = { "KeyTab", "RuneBook", "Glow", "Dot", PulseName };

    [SerializeField] private int _fadeFromDepth = 3;

    [Header("룬북 강조 (PulseRuneBook)")]
    [SerializeField, Min(0.01f)] private float _pulseDuration = 1.1f;
    [SerializeField, Range(0f, 1f)] private float _pulseMaxAlpha = 0.9f;
    [Tooltip("크기 (시작, 끝)")]
    [SerializeField] private Vector2 _pulseScale = new(0.8f, 1.2f);

    private RuneBookV2Toggle _toggle;
    private GameObject _hints;
    private Animator _animator;
    private readonly HashSet<string> _seen = new();
    private readonly HashSet<string> _affordable = new();
    private readonly Dictionary<char, int> _cost = new();
    private float _nextCheck;
    private bool _primed;
    private GameObject[] _runeBookHintObjects;
    private bool _isRuneBookHintHidden;
    private Image _pulse;
    private Coroutine _pulseRoutine;

    public static HudMenuHints Create(RuneBookV2Toggle toggle)
    {
        HudMenuHints hints = toggle.gameObject.GetComponent<HudMenuHints>();
        if (hints == null)
        {
            hints = toggle.gameObject.AddComponent<HudMenuHints>();
        }

        hints._toggle = toggle;
        hints.Build();
        return hints;
    }

    private void Build()
    {
        if (_hints != null)
        {
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[HudMenuHints] Resources/{PrefabPath} 프리팹이 없습니다.", this);
            return;
        }

        _hints = Instantiate(prefab);
        _hints.name = prefab.name;
        _animator = _hints.GetComponent<Animator>();

        var runeBookHints = new List<GameObject>();
        foreach (string hintName in s_runeBookHintNames)
        {
            Transform hint = _hints.transform.Find(hintName);
            if (hint != null)
            {
                runeBookHints.Add(hint.gameObject);
            }
        }

        _runeBookHintObjects = runeBookHints.ToArray();

        Transform pulse = _hints.transform.Find(PulseName);
        _pulse = pulse != null ? pulse.GetComponent<Image>() : null;
        SetPulse(0f, _pulseScale.x);
    }

    /// <summary>룬북 아이콘 뒤의 금빛이 한 번 부풀었다 사라진다.</summary>
    public void PulseRuneBook()
    {
        if (_pulse == null || !isActiveAndEnabled)
        {
            return;
        }

        if (_pulseRoutine != null)
        {
            StopCoroutine(_pulseRoutine);
        }

        _pulseRoutine = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        float elapsed = 0f;
        while (elapsed < _pulseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _pulseDuration);
            SetPulse(Mathf.Sin(t * Mathf.PI) * _pulseMaxAlpha, Mathf.Lerp(_pulseScale.x, _pulseScale.y, 1f - (1f - t) * (1f - t)));
            yield return null;
        }

        SetPulse(0f, _pulseScale.x);
        _pulseRoutine = null;
    }

    private void SetPulse(float alpha, float scale)
    {
        if (_pulse == null)
        {
            return;
        }

        Color color = _pulse.color;
        color.a = alpha;
        _pulse.color = color;
        _pulse.enabled = alpha > 0f;
        _pulse.rectTransform.localScale = Vector3.one * scale;
    }

    private void Update()
    {
        if (_animator == null)
        {
            return;
        }

        // 상점이 열려 있는 동안은 숨긴다. (숨긴 동안에는 애니메이터 값을 넘기지 않는다)
        StageManager stage = StageManager.Instance;
        bool isInShop = stage != null && stage.Phase == StagePhase.Shop;
        if (_hints.activeSelf == isInShop)
        {
            _hints.SetActive(!isInShop);
        }

        if (isInShop)
        {
            return;
        }

        RefreshRuneBookHintVisibility();

        int depth = stage != null ? stage.CurrentDepth : 1;
        _animator.SetFloat(s_dimHash, depth >= _fadeFromDepth ? 1f : 0f);

        if (Time.unscaledTime >= _nextCheck)
        {
            _nextCheck = Time.unscaledTime + CheckInterval;
            RefreshAffordable();
        }

        // 룬북을 열면 지금 장착 가능한 것들을 확인한 것으로 보고 알림을 끈다.
        if (_toggle != null && _toggle.IsOpen)
        {
            _seen.UnionWith(_affordable);
        }

        bool notify = false;
        foreach (string id in _affordable)
        {
            if (!_seen.Contains(id))
            {
                notify = true;
                break;
            }
        }

        _animator.SetBool(s_notifyHash, notify);
    }

    /// <summary>룬북이 잠겨 있으면 [룬북][TAB] 힌트를 숨긴다. 잠금이 바뀐 프레임에만 켜고 끈다.</summary>
    private void RefreshRuneBookHintVisibility()
    {
        bool isLocked = _toggle != null && _toggle.IsLocked;
        if (isLocked == _isRuneBookHintHidden || _runeBookHintObjects == null)
        {
            return;
        }

        _isRuneBookHintHidden = isLocked;
        foreach (GameObject hint in _runeBookHintObjects)
        {
            if (hint != null)
            {
                hint.SetActive(!isLocked);
            }
        }
    }

    /// <summary>지금 새로 장착할 수 있는 룬 워드 (룬 문자가 충분하거나 완성품이 있고, 아직 장착하지 않았거나 겹쳐 넣을 수 있음)</summary>
    private void RefreshAffordable()
    {
        _affordable.Clear();
        PlayerRuneManager manager = PlayerRuneManager.Instance;
        RuneEffectDatabase database = RuneEffectDatabase.Instance;
        if (manager == null || !manager.IsInitialized || database == null || !database.DatabaseReady)
        {
            return;
        }

        RuneEffectLoadout loadout = manager.RuneEffectLoadout;
        foreach (RuneEffectDefinition effect in database.GetAll())
        {
            if (effect == null || string.IsNullOrEmpty(effect.RuneWord))
            {
                continue;
            }

            if (loadout != null && loadout.GetEquippedCount(effect.Id) > 0 && !loadout.CanOverlap(effect.Id))
            {
                continue;
            }

            bool affordable = OwnedRuneWords.GetCount(effect.Id) > 0;
            if (!affordable)
            {
                _cost.Clear();
                foreach (char rune in effect.RuneWord)
                {
                    if (char.IsWhiteSpace(rune)) continue;
                    _cost[rune] = _cost.TryGetValue(rune, out int count) ? count + 1 : 1;
                }

                affordable = _cost.Count > 0 && manager.RuneStoneInventory.HasEnoughRuneStones(_cost);
            }

            if (affordable)
            {
                _affordable.Add(effect.Id);
            }
        }

        // 처음 확인한 것(런 시작 때 이미 가능한 것)은 알림 대상이 아니다.
        if (!_primed)
        {
            _primed = true;
            _seen.UnionWith(_affordable);
        }
    }

    private void OnDestroy()
    {
        if (_hints != null)
        {
            Destroy(_hints);
        }
    }
}
