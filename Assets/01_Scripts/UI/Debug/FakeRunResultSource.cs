using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [테스트 전용] 결과창을 확인하기 위한 가짜 결과 데이터. 인스펙터에서 값을 바꿔 확인할 수 있다.
/// 스테이지 제한 시간이 끝나면(TimeExpired) 종료 사유를 TimeOver로 바꿔 전달한다.
/// (다음 스테이지 입장 시 인스펙터의 종료 사유로 되돌아간다)
/// </summary>
public class FakeRunResultSource : RunResultSource
{
    [Header("Play Result")]
    [SerializeField, Min(0f)] private float _survivalSeconds = 1314f;
    [SerializeField, Min(0)] private int _maxDepth = 4;
    [SerializeField, Min(0)] private long _totalDamage = 125423;
    [SerializeField, Min(0)] private int _killCount = 484;
    [SerializeField] private RunEndReason _endReason = RunEndReason.Died;

    [Header("연구 자원 (원초 물질, 아조트, 수은, 에테르 결정)")]
    [SerializeField] private int _primaMateria = 120;
    [SerializeField] private int _azoth = 2;
    [SerializeField] private int _mercury = 35;
    [SerializeField] private int _aetherCrystal = 8;

    [Header("시간 초과 감지 (비워두면 사용 안 함)")]
    [SerializeField] private StageStatusSource _stageSource;

    private bool _isTimeOver;

    private void OnEnable()
    {
        if (_stageSource != null)
        {
            _stageSource.TimeExpired += HandleTimeExpired;
            _stageSource.StageEntered += HandleStageEntered;
        }
    }

    private void OnDisable()
    {
        if (_stageSource != null)
        {
            _stageSource.TimeExpired -= HandleTimeExpired;
            _stageSource.StageEntered -= HandleStageEntered;
        }
    }

    public override RunResultData GetResult()
    {
        List<ResourceAmountData> researchResources = new()
        {
            new ResourceAmountData("prima_materia", null, _primaMateria),
            new ResourceAmountData("azoth", null, _azoth),
            new ResourceAmountData("mercury", null, _mercury),
            new ResourceAmountData("aether_crystal", null, _aetherCrystal)
        };

        RunEndReason endReason = _isTimeOver ? RunEndReason.TimeOver : _endReason;
        return new RunResultData(_survivalSeconds, _maxDepth, _totalDamage, _killCount, endReason, researchResources);
    }

    private void HandleTimeExpired()
    {
        _isTimeOver = true;
    }

    private void HandleStageEntered(int depth, string stageName)
    {
        _isTimeOver = false;
    }
}
