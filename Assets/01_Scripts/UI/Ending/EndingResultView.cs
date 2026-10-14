using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 결과창(EndingUI) : 연금술 협회에 제출되는 "조사 보고서" 한 장.
/// - 왼쪽 보고서 : 조사 개체 번호, 기록 다섯 줄, 사용한 룬 구성, 소견, 결과 도장
/// - 오른쪽 : 결과 제목과 한 줄 설명, 메뉴(다음 개체 투입 / 타이틀로 돌아가기)
/// 열릴 때 보고서가 아래에서 올라오고, 잠시 뒤 도장이 쾅 찍힌다. (실제 시간)
/// 도장이 닿는 순간 종이가 살짝 떨리고, 도장 가장자리에서 먼지가 옅게 피어올라 흩어진다.
/// 도장 소리는 Ending_Appear 클립의 애니메이션 이벤트가 PlayStampSound를 불러서 낸다.
/// 등장(Appear)과 나가기(Leave) 연출은 애니메이션 클립(EndingResult 컨트롤러)이 맡는다. 먼지는 Report/StampDust 아래 이미지들이다.
/// 버튼은 이벤트만 발생시키고, 실제 씬 이동은 EndingResultPresenter가 담당한다.
///
/// 종료 사유에 따라 바뀌는 것
/// - 공통 : 도장, 소견, 오른쪽 제목과 부제, 제목 색과 뒤에 번지는 빛 색
/// - 임무 실패 : 다섯째 줄이 "오염도"로 바뀌고 아래에 "오염 한계 초과" 표시. 종이 위 오염 얼룩, 보랏빛 배경
/// - 보스 처치 : 보고서 제목이 "정화 보고서"로 바뀌고 금박 테두리와 인장, 푸른 배경. 첫 메뉴가 "다음 탐사 시작"
/// 결과별 장식은 _xxxDecorations에 넣은 오브젝트를 해당 결과일 때만 켠다.
/// </summary>
[DisallowMultipleComponent]
public class EndingResultView : MonoBehaviour
{
    private const int SecondsPerMinute = 60;
    private const int SecondsPerHour = 3600;
    private const int RestartMenuIndex = 0;
    private const int TitleMenuIndex = 1;

    [Header("보고서")]
    [SerializeField] private TMP_Text _subjectText;
    [SerializeField] private string _subjectFormat = "조사 개체 : 호문쿨루스 제 {0}호";
    [SerializeField] private EndingStatRowView _timeRow;
    [SerializeField] private EndingStatRowView _depthRow;
    [SerializeField] private EndingStatRowView _damageRow;
    [SerializeField] private EndingStatRowView _killRow;
    [SerializeField] private EndingStatRowView _bestClearRow;
    [SerializeField] private string _depthFormat = "{0}층";
    [SerializeField] private string _clearedDepthFormat = "{0}층 (최심부)";
    [Tooltip("최단 정화 기록이 없을 때")]
    [SerializeField] private string _noRecordText = "--:--";
    [SerializeField] private Color _newRecordColor = new(0.55f, 0.13f, 0.12f, 1f);
    [Tooltip("최단 기록을 갱신했을 때만 켜는 표시 (★ 최단 기록 갱신)")]
    [SerializeField] private GameObject _newRecordMark;

    [Header("사용한 룬 구성")]
    [SerializeField] private EndingRuneSlotView[] _runeSlots;

    [Header("소견")]
    [SerializeField] private TMP_Text _remarkText;
    [SerializeField, TextArea] private string[] _diedRemarks =
    {
        "사인 : 정령과의 교전 중 소실.\n잔해 회수 불가. 다음 개체를 투입한다.",
        "사인 : 오염된 정령의 집중 공격.\n잔해 회수 불가. 다음 개체를 투입한다.",
        "사인 : 과도한 손상으로 형체 붕괴.\n잔해 회수 불가. 다음 개체를 투입한다.",
    };
    [SerializeField, TextArea] private string[] _missionFailedRemarks =
    {
        "임무 시한 초과. 개체가 오염에 잠식됨.\n잔해 회수 불가. 해당 구역 접근 금지.",
    };
    [SerializeField, TextArea] private string[] _abandonedRemarks =
    {
        "조사 중단 요청에 따라 개체를 회수함.\n장착 룬은 회수 과정에서 소실되었다.",
    };
    [SerializeField, TextArea] private string[] _clearedRemarks =
    {
        "최심부의 수호자 소멸 확인.\n정령계 오염의 근원이 정화되었다.",
    };

    [Header("도장")]
    [SerializeField] private Image _stampImage;
    [SerializeField] private Sprite _diedStamp;
    [SerializeField] private Sprite _missionFailedStamp;
    [SerializeField] private Sprite _abandonedStamp;
    [SerializeField] private Sprite _clearedStamp;

    [Header("제목")]
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _subtitleText;
    [Tooltip("제목 뒤에 번지는 빛. 결과에 따라 색이 바뀐다")]
    [SerializeField] private Graphic _titleGlow;
    [SerializeField] private string _diedTitle = "탐사 종료";
    [SerializeField] private string _missionFailedTitle = "개체 잠식";
    [SerializeField] private string _abandonedTitle = "탐사 중단";
    [SerializeField] private string _clearedTitle = "정령계 정화";
    [SerializeField] private string _diedSubtitle = "호문쿨루스 제 {0}호의 기록이 협회에 제출되었습니다.";
    [SerializeField] private string _missionFailedSubtitle = "호문쿨루스 제 {0}호의 기록이 오염된 채 회수되었습니다.";
    [SerializeField] private string _abandonedSubtitle = "호문쿨루스 제 {0}호가 협회로 회수되었습니다.";
    [SerializeField] private string _clearedSubtitle = "호문쿨루스 제 {0}호가 오염의 근원을 정화했습니다.";
    [SerializeField] private Color _diedGlowColor = new(0.6f, 0.12f, 0.12f, 0.45f);
    [SerializeField] private Color _missionFailedGlowColor = new(0.55f, 0.24f, 0.67f, 0.5f);
    [SerializeField] private Color _abandonedGlowColor = new(0.4f, 0.4f, 0.4f, 0.35f);
    [SerializeField] private Color _clearedGlowColor = new(0.43f, 0.63f, 0.9f, 0.5f);
    [Tooltip("임무 실패일 때 제목 글자색. 나머지(사망, 포기)는 프리팹에 적힌 색을 쓴다")]
    [SerializeField] private Color _missionFailedTitleColor = new(0.886f, 0.808f, 0.91f, 1f);
    [SerializeField] private Color _clearedTitleColor = new(0.925f, 0.941f, 0.98f, 1f);

    [Header("보고서 제목")]
    [SerializeField] private TMP_Text _reportTitleText;
    [SerializeField] private string _reportTitle = "조사 보고서";
    [SerializeField] private string _clearedReportTitle = "정화 보고서";

    [Header("오염도 (임무 실패일 때 다섯째 줄)")]
    [SerializeField] private string _contaminationLabel = "오염도";
    [SerializeField] private string _contaminationValue = "100%";
    [SerializeField] private Color _contaminationColor = new(0.43f, 0.16f, 0.47f, 1f);
    [Tooltip("다섯째 줄 아래 표시 (최단 기록 갱신 표시 자리를 같이 쓴다)")]
    [SerializeField] private string _contaminationNote = "▲ 오염 한계 초과";

    [Header("결과별 장식 (해당 결과일 때만 켠다)")]
    [SerializeField] private GameObject[] _diedDecorations;
    [SerializeField] private GameObject[] _missionFailedDecorations;
    [SerializeField] private GameObject[] _abandonedDecorations;
    [SerializeField] private GameObject[] _clearedDecorations;

    [Header("메뉴 글자")]
    [Tooltip("첫 메뉴(다시 시작)의 글자")]
    [SerializeField] private TMP_Text _restartLabel;
    [SerializeField] private string _restartText = "다음 개체 투입";
    [SerializeField] private string _clearedRestartText = "다음 탐사 시작";

    [Header("메뉴 (0 : 다음 개체 투입, 1 : 타이틀로 돌아가기)")]
    [SerializeField] private EndingMenuView _menu;

    [Header("연출")]
    [Tooltip("등장(Appear) / 나가기(Leave) 클립을 재생하는 애니메이터 (unscaled 시간)")]
    [SerializeField] private Animator _animator;

    private static readonly int s_appearHash = Animator.StringToHash("Appear");
    private static readonly int s_leaveHash = Animator.StringToHash("Leave");

    private TMP_Text _newRecordMarkText;
    private string _newRecordMarkDefaultText = string.Empty;
    private Color _newRecordMarkDefaultColor = Color.white;
    private Color _defaultTitleColor = Color.white;
    private bool _isInitialized;

    /// <summary>다음 개체 투입(다시 시작)을 골랐을 때</summary>
    public event Action RestartRequested;

    /// <summary>타이틀로 돌아가기를 골랐을 때</summary>
    public event Action TitleRequested;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        if (_menu != null)
        {
            _menu.Confirmed += HandleMenuConfirmed;
        }

        PlayState(s_appearHash);
    }

    private void OnDisable()
    {
        if (_menu != null)
        {
            _menu.Confirmed -= HandleMenuConfirmed;
        }
    }

    /// <param name="homunculusNumber">보고서의 "제 N호"</param>
    /// <param name="bestClearSeconds">최단 정화 기록(초). 없으면 음수</param>
    /// <param name="isNewRecord">이번 판으로 최단 기록을 갱신했는지</param>
    public void ShowResult(RunResultData result, int homunculusNumber, float bestClearSeconds, bool isNewRecord)
    {
        Initialize();
        if (result == null)
        {
            return;
        }

        RunEndReason reason = result.EndReason;
        string number = homunculusNumber.ToString(CultureInfo.InvariantCulture);
        SetText(_subjectText, string.Format(_subjectFormat, number));

        if (_timeRow != null) _timeRow.SetValue(FormatTime(result.SurvivalSeconds));
        if (_depthRow != null)
        {
            string format = reason == RunEndReason.Cleared ? _clearedDepthFormat : _depthFormat;
            _depthRow.SetValue(string.Format(format, result.MaxDepth));
        }

        if (_damageRow != null) _damageRow.SetValue(result.TotalDamage.ToString("#,0", CultureInfo.InvariantCulture));
        if (_killRow != null) _killRow.SetValue(result.KillCount.ToString("#,0", CultureInfo.InvariantCulture));
        bool isContaminated = reason == RunEndReason.MissionFailed;
        if (_bestClearRow != null)
        {
            if (isContaminated)
            {
                _bestClearRow.SetLabel(_contaminationLabel);
                _bestClearRow.SetValue(_contaminationValue, _contaminationColor);
            }
            else
            {
                _bestClearRow.SetLabel(_bestClearRow.DefaultLabel);
                string best = bestClearSeconds >= 0f ? FormatTime(bestClearSeconds) : _noRecordText;
                if (isNewRecord)
                {
                    _bestClearRow.SetValue(best, _newRecordColor);
                }
                else
                {
                    _bestClearRow.SetValue(best);
                }
            }
        }

        ShowRowNote(isContaminated, isNewRecord);
        SetText(_reportTitleText, reason == RunEndReason.Cleared ? _clearedReportTitle : _reportTitle);
        SetText(_restartLabel, reason == RunEndReason.Cleared ? _clearedRestartText : _restartText);
        ShowDecorations(reason);

        SetText(_remarkText, Pick(GetRemarks(reason)));

        if (_stampImage != null)
        {
            _stampImage.sprite = GetStamp(reason);
        }

        SetText(_titleText, GetTitle(reason));
        if (_titleText != null)
        {
            _titleText.color = GetTitleColor(reason);
        }

        SetText(_subtitleText, string.Format(GetSubtitle(reason), number));
        if (_titleGlow != null)
        {
            _titleGlow.color = GetGlowColor(reason);
        }
    }

    /// <summary>
    /// 사용한 룬을 표시한다. 앞에서부터 채우고, 열린 칸 중 남은 칸은 빈 칸, 열리지 않은 칸은 숨긴다.
    /// </summary>
    public void ShowRunes(IReadOnlyList<EndingRuneData> runes, int activeSlotCount)
    {
        Initialize();
        if (_runeSlots == null)
        {
            return;
        }

        int count = runes?.Count ?? 0;
        for (int i = 0; i < _runeSlots.Length; i++)
        {
            EndingRuneSlotView slot = _runeSlots[i];
            if (slot == null)
            {
                continue;
            }

            if (i < count)
            {
                slot.Show(runes[i]);
            }
            else if (i < activeSlotCount)
            {
                slot.ShowEmpty();
            }
            else
            {
                slot.Hide();
            }
        }
    }

    /// <summary>
    /// Ending_Appear 클립의 애니메이션 이벤트에서 부른다. 도장이 종이에 닿는 순간(0.98초)에 맞춰
    /// 소리 파일 앞부분의 여유(약 0.1초)만큼 일찍 부른다.
    /// </summary>
    public void PlayStampSound()
    {
        Sfx.Play(Sfx.EndingStamp);
    }

    private void PlayState(int stateHash)
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_animator != null)
        {
            _animator.Play(stateHash, 0, 0f);
        }
    }

    private void HandleMenuConfirmed(int index)
    {
        if (index == RestartMenuIndex || index == TitleMenuIndex)
        {
            PlayLeave();
        }

        if (index == RestartMenuIndex)
        {
            Debug.Log("[EndingResultView] 다시 시작 요청");
            RestartRequested?.Invoke();
        }
        else if (index == TitleMenuIndex)
        {
            Debug.Log("[EndingResultView] 타이틀 이동 요청");
            TitleRequested?.Invoke();
        }
    }

    /// <summary>
    /// 나갈 때 : 버튼이 반짝이는 동안 잠깐 기다렸다가 보고서가 아래로 가라앉으며 흐려지고, 오른쪽도 함께 흐려진다. (Leave 클립)
    /// 화면을 검게 덮는 것은 SceneTransition(RunFlow)이 같은 시간에 맞춰 한다.
    /// </summary>
    private void PlayLeave()
    {
        PlayState(s_leaveHash);
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        if (_newRecordMark != null)
        {
            _newRecordMarkText = _newRecordMark.GetComponent<TMP_Text>();
            if (_newRecordMarkText != null)
            {
                _newRecordMarkDefaultText = _newRecordMarkText.text;
                _newRecordMarkDefaultColor = _newRecordMarkText.color;
            }
        }

        if (_titleText != null)
        {
            _defaultTitleColor = _titleText.color;
        }
    }

    /// <summary>다섯째 줄 아래 표시 : 임무 실패면 "오염 한계 초과", 최단 기록을 갱신했으면 "최단 기록 갱신"</summary>
    private void ShowRowNote(bool isContaminated, bool isNewRecord)
    {
        if (_newRecordMark == null)
        {
            return;
        }

        _newRecordMark.SetActive(isContaminated || isNewRecord);
        if (_newRecordMarkText == null)
        {
            return;
        }

        _newRecordMarkText.text = isContaminated ? _contaminationNote : _newRecordMarkDefaultText;
        _newRecordMarkText.color = isContaminated ? _contaminationColor : _newRecordMarkDefaultColor;
    }

    /// <summary>결과별 장식을 모두 끈 뒤 이번 결과의 장식만 켠다.</summary>
    private void ShowDecorations(RunEndReason reason)
    {
        SetActive(_diedDecorations, false);
        SetActive(_missionFailedDecorations, false);
        SetActive(_abandonedDecorations, false);
        SetActive(_clearedDecorations, false);

        GameObject[] decorations = reason switch
        {
            RunEndReason.MissionFailed => _missionFailedDecorations,
            RunEndReason.Abandoned => _abandonedDecorations,
            RunEndReason.Cleared => _clearedDecorations,
            _ => _diedDecorations
        };
        SetActive(decorations, true);
    }

    private static void SetActive(GameObject[] objects, bool isActive)
    {
        if (objects == null)
        {
            return;
        }

        foreach (GameObject target in objects)
        {
            if (target != null)
            {
                target.SetActive(isActive);
            }
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private static string Pick(string[] pool)
    {
        return pool == null || pool.Length == 0 ? string.Empty : pool[UnityEngine.Random.Range(0, pool.Length)];
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        if (total >= SecondsPerHour)
        {
            return $"{total / SecondsPerHour}:{total % SecondsPerHour / SecondsPerMinute:00}:{total % SecondsPerMinute:00}";
        }

        return $"{total / SecondsPerMinute:00}:{total % SecondsPerMinute:00}";
    }

    private string[] GetRemarks(RunEndReason reason) => reason switch
    {
        RunEndReason.MissionFailed => _missionFailedRemarks,
        RunEndReason.Abandoned => _abandonedRemarks,
        RunEndReason.Cleared => _clearedRemarks,
        _ => _diedRemarks
    };

    private Sprite GetStamp(RunEndReason reason) => reason switch
    {
        RunEndReason.MissionFailed => _missionFailedStamp,
        RunEndReason.Abandoned => _abandonedStamp,
        RunEndReason.Cleared => _clearedStamp,
        _ => _diedStamp
    };

    private string GetTitle(RunEndReason reason) => reason switch
    {
        RunEndReason.MissionFailed => _missionFailedTitle,
        RunEndReason.Abandoned => _abandonedTitle,
        RunEndReason.Cleared => _clearedTitle,
        _ => _diedTitle
    };

    private string GetSubtitle(RunEndReason reason) => reason switch
    {
        RunEndReason.MissionFailed => _missionFailedSubtitle,
        RunEndReason.Abandoned => _abandonedSubtitle,
        RunEndReason.Cleared => _clearedSubtitle,
        _ => _diedSubtitle
    };

    private Color GetTitleColor(RunEndReason reason) => reason switch
    {
        RunEndReason.MissionFailed => _missionFailedTitleColor,
        RunEndReason.Cleared => _clearedTitleColor,
        _ => _defaultTitleColor
    };

    private Color GetGlowColor(RunEndReason reason) => reason switch
    {
        RunEndReason.MissionFailed => _missionFailedGlowColor,
        RunEndReason.Abandoned => _abandonedGlowColor,
        RunEndReason.Cleared => _clearedGlowColor,
        _ => _diedGlowColor
    };
}
