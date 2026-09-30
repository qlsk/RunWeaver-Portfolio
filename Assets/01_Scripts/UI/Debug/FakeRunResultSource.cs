using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [테스트 전용] 결과창을 확인하기 위한 가짜 결과 데이터. 인스펙터에서 값을 바꿔 확인할 수 있다.
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

    public override RunResultData GetResult()
    {
        List<ResourceAmountData> researchResources = new()
        {
            new ResourceAmountData("prima_materia", null, _primaMateria),
            new ResourceAmountData("azoth", null, _azoth),
            new ResourceAmountData("mercury", null, _mercury),
            new ResourceAmountData("aether_crystal", null, _aetherCrystal)
        };

        return new RunResultData(_survivalSeconds, _maxDepth, _totalDamage, _killCount, _endReason, researchResources);
    }
}
