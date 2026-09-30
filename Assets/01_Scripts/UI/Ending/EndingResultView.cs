using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 결과창(EndingUI) 표시.
/// 버튼은 이벤트만 발생시키고, 실제 씬 이동은 이벤트를 구독한 쪽(추후 SceneManager)이 담당한다.
/// </summary>
[DisallowMultipleComponent]
public class EndingResultView : MonoBehaviour
{
    private const int SecondsPerMinute = 60;

    [Header("Play Result")]
    [SerializeField] private TMP_Text _survivingTimeText;
    [SerializeField] private TMP_Text _maxDepthText;
    [SerializeField] private TMP_Text _totalDamageText;
    [SerializeField] private TMP_Text _killCountText;
    [SerializeField] private string _survivingTimeFormat = "Surviving Time: {0}";
    [SerializeField] private string _maxDepthFormat = "Max Depth: Floor {0}";
    [SerializeField] private string _totalDamageFormat = "Total Damage: {0}";
    [SerializeField] private string _killCountFormat = "Kill Count: {0}";

    [Header("종료 사유 문구")]
    [SerializeField] private TMP_Text _endReasonText;
    [SerializeField, TextArea] private string _diedText =
        "You danced with the Spirit of Flame and were reduced to a handful of ashes!";
    [SerializeField, TextArea] private string _returnedText = "You chose to return safely.";
    [SerializeField, TextArea] private string _escapedText = "You made a hasty escape.";
    [SerializeField, TextArea] private string _timeOverText =
        "Time ran out, and the depths swallowed you whole!";

    [Header("칸의 부모")]
    [SerializeField] private Transform _activatedRuneContainer;
    [SerializeField] private Transform _gatheredRuneContainer;
    [SerializeField] private Transform _researchResourceContainer;

    [Header("버튼")]
    [SerializeField] private Button _lobbyButton;
    [SerializeField] private Button _restartButton;

    private IconSlotView[] _activatedRuneSlots = Array.Empty<IconSlotView>();
    private CountSlotView[] _gatheredRuneSlots = Array.Empty<CountSlotView>();
    private CountSlotView[] _researchResourceSlots = Array.Empty<CountSlotView>();
    private bool _isInitialized;

    /// <summary>To Lobby 버튼을 눌렀을 때 발생한다.</summary>
    public event Action LobbyRequested;

    /// <summary>ReStart 버튼을 눌렀을 때 발생한다.</summary>
    public event Action RestartRequested;

    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (_lobbyButton != null)
        {
            _lobbyButton.onClick.RemoveListener(HandleLobbyClicked);
        }

        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveListener(HandleRestartClicked);
        }
    }

    public void ShowResult(RunResultData result)
    {
        Initialize();

        if (result == null)
        {
            return;
        }

        SetText(_survivingTimeText, _survivingTimeFormat, FormatTime(result.SurvivalSeconds));
        SetText(_maxDepthText, _maxDepthFormat, result.MaxDepth.ToString());
        SetText(_totalDamageText, _totalDamageFormat, result.TotalDamage.ToString());
        SetText(_killCountText, _killCountFormat, result.KillCount.ToString());

        if (_endReasonText != null)
        {
            _endReasonText.text = GetEndReasonText(result.EndReason);
        }

        for (int i = 0; i < _researchResourceSlots.Length; i++)
        {
            if (i < result.ResearchResources.Count)
            {
                ResourceAmountData resource = result.ResearchResources[i];
                _researchResourceSlots[i].Show(resource.Icon, resource.Amount);
            }
            else
            {
                _researchResourceSlots[i].Hide();
            }
        }
    }

    /// <summary>
    /// 장착했던 룬을 표시한다. 빈 칸은 null로 전달한다.
    /// </summary>
    public void ShowActivatedRunes(IReadOnlyList<RuneBookEntryData> equippedEntries)
    {
        Initialize();

        for (int i = 0; i < _activatedRuneSlots.Length; i++)
        {
            RuneBookEntryData entry = equippedEntries != null && i < equippedEntries.Count ? equippedEntries[i] : null;
            if (entry == null)
            {
                _activatedRuneSlots[i].ShowEmpty();
            }
            else
            {
                _activatedRuneSlots[i].Show(entry.Icon);
            }
        }
    }

    /// <summary>
    /// 남은 룬석을 앞 칸부터 채우고, 남는 칸은 숨긴다.
    /// </summary>
    public void ShowGatheredRunes(IReadOnlyList<RuneStoneStackData> runeStones)
    {
        Initialize();

        int count = runeStones?.Count ?? 0;
        if (count > _gatheredRuneSlots.Length)
        {
            Debug.LogWarning($"[EndingResultView] 룬석은 {count}종인데 칸은 {_gatheredRuneSlots.Length}개입니다.", this);
        }

        for (int i = 0; i < _gatheredRuneSlots.Length; i++)
        {
            if (i < count)
            {
                _gatheredRuneSlots[i].Show(runeStones[i].Icon, runeStones[i].Quantity);
            }
            else
            {
                _gatheredRuneSlots[i].Hide();
            }
        }
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        _activatedRuneSlots = CollectSlots<IconSlotView>(_activatedRuneContainer);
        _gatheredRuneSlots = CollectSlots<CountSlotView>(_gatheredRuneContainer);
        _researchResourceSlots = CollectSlots<CountSlotView>(_researchResourceContainer);

        if (_lobbyButton != null)
        {
            _lobbyButton.onClick.AddListener(HandleLobbyClicked);
        }

        if (_restartButton != null)
        {
            _restartButton.onClick.AddListener(HandleRestartClicked);
        }
    }

    private static T[] CollectSlots<T>(Transform container) where T : Component
    {
        if (container == null)
        {
            return Array.Empty<T>();
        }

        List<T> slots = new();
        foreach (Transform child in container)
        {
            if (child.TryGetComponent(out T slot))
            {
                slots.Add(slot);
            }
        }

        return slots.ToArray();
    }

    private static void SetText(TMP_Text text, string format, string value)
    {
        if (text != null)
        {
            text.text = string.Format(format, value);
        }
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{totalSeconds / SecondsPerMinute:00}:{totalSeconds % SecondsPerMinute:00}";
    }

    private string GetEndReasonText(RunEndReason reason)
    {
        return reason switch
        {
            RunEndReason.Returned => _returnedText,
            RunEndReason.Escaped => _escapedText,
            RunEndReason.TimeOver => _timeOverText,
            _ => _diedText
        };
    }

    private void HandleLobbyClicked()
    {
        Debug.Log("[EndingResultView] 로비 이동 요청");
        LobbyRequested?.Invoke();
    }

    private void HandleRestartClicked()
    {
        Debug.Log("[EndingResultView] 재시작 요청");
        RestartRequested?.Invoke();
    }
}
