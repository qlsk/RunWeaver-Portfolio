using UnityEngine;

/// <summary>
/// [테스트 전용] 심도 패널과 타이머 패널을 확인하기 위한 가짜 스테이지 진행.
/// 시작하면 1층에 입장하고 제한 시간이 줄어든다. 룬북을 열어 게임이 멈추면 타이머도 멈춘다.
///
/// 테스트 키
/// - (Minus): 다음 심도 입장 (제한 시간 초기화)
/// = (Equals): 남은 시간을 15초로 (경고, 강조, 시간 초과 실패 확인용)
/// 남은 시간이 0이 되면 TimeExpired를 한 번 발생시킨다. (다음 심도 입장 시 초기화)
/// </summary>
public class FakeStageStatusSource : StageStatusSource
{
    [SerializeField, Min(0f)] private float _timeLimit = 120f;
    [SerializeField] private bool _hasTimeLimit = true;
    [SerializeField] private string[] _stageNames = { "Realm of Flames", "Frozen Depths", "Howling Wilds", "Stone Abyss" };

    [Header("테스트 키")]
    [SerializeField] private KeyCode _nextDepthKey = KeyCode.Minus;
    [SerializeField] private KeyCode _shortenTimeKey = KeyCode.Equals;
    [SerializeField, Min(0f)] private float _shortenedTime = 15f;

    private int _depth;
    private float _remainingSeconds;
    private bool _hasExpired;

    private void Start()
    {
        EnterNextDepth();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_nextDepthKey))
        {
            EnterNextDepth();
        }

        if (Input.GetKeyDown(_shortenTimeKey) && _hasTimeLimit)
        {
            _remainingSeconds = Mathf.Min(_remainingSeconds, _shortenedTime);
        }

        if (!_hasTimeLimit)
        {
            return;
        }

        if (_hasExpired)
        {
            return;
        }

        _remainingSeconds = Mathf.Max(0f, _remainingSeconds - Time.deltaTime);
        RaiseRemainingTimeChanged(_remainingSeconds);

        if (_remainingSeconds <= 0f)
        {
            _hasExpired = true;
            RaiseTimeExpired();
        }
    }

    [ContextMenu("Test/Enter Next Depth")]
    private void EnterNextDepth()
    {
        _depth++;
        _remainingSeconds = _timeLimit;
        _hasExpired = false;

        string stageName = _stageNames != null && _stageNames.Length > 0
            ? _stageNames[(_depth - 1) % _stageNames.Length]
            : "Stage";

        RaiseStageEntered(_depth, stageName);
        RaiseRemainingTimeChanged(_hasTimeLimit ? _remainingSeconds : NoTimeLimit);
    }
}
