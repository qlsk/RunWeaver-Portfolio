using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 결과창이 켜질 때 이번 판의 결과와 장착 룬을 읽어 EndingResultView(조사 보고서)에 표시한다.
/// - 결과 : RunResultSource (조사 시간, 도달 심도, 총 피해량, 처치 수, 종료 사유)
/// - 호문쿨루스 번호와 최단 정화 기록 : EndingRecordStore (기기에 저장)
/// - 장착 룬 : PlayerRuneManager의 룬 효과 로드아웃 (현재 룬북이 쓰는 장착 상태)
/// 다음 개체 투입 : RunFlow.Restart / 타이틀로 돌아가기 : RunFlow.GoToTitle
/// </summary>
[DisallowMultipleComponent]
public class EndingResultPresenter : MonoBehaviour
{
    [SerializeField] private EndingResultView _view;
    [SerializeField] private RunResultSource _resultSource;
    [Tooltip("룬 효과 아이콘 목록. 룬 효과 데이터에 아이콘이 없을 때 쓴다")]
    [SerializeField] private RuneEffectIconCatalog _iconCatalog;

    [Header("주력 원소 칸 색")]
    [SerializeField] private Color _fireColor = new(0.77f, 0.32f, 0.19f, 1f);
    [SerializeField] private Color _waterColor = new(0.24f, 0.43f, 0.67f, 1f);
    [SerializeField] private Color _earthColor = new(0.61f, 0.44f, 0.23f, 1f);
    [SerializeField] private Color _windColor = new(0.24f, 0.53f, 0.43f, 1f);
    [SerializeField] private Color _otherColor = new(0.45f, 0.38f, 0.55f, 1f);

    private bool _isLeaving;
    private bool _hasShown;

    private void OnEnable()
    {
        if (_view == null)
        {
            Debug.LogError("[EndingResultPresenter] EndingResultView가 할당되지 않았습니다.", this);
            return;
        }

        _isLeaving = false;
        _view.RestartRequested += HandleRestartRequested;
        _view.TitleRequested += HandleTitleRequested;

        // 한 판에 한 번만 번호를 받고 기록을 남긴다. (창이 다시 켜져도 늘어나지 않게)
        if (!_hasShown)
        {
            _hasShown = true;
            ShowReport();
        }
    }

    private void OnDisable()
    {
        if (_view != null)
        {
            _view.RestartRequested -= HandleRestartRequested;
            _view.TitleRequested -= HandleTitleRequested;
        }
    }

    private void ShowReport()
    {
        RunResultData result = _resultSource != null ? _resultSource.GetResult() : null;
        if (result == null)
        {
            Debug.LogWarning("[EndingResultPresenter] 결과 데이터가 할당되지 않았습니다.", this);
            result = new RunResultData(0f, 1, 0, 0, RunFlow.EndReason ?? RunEndReason.Died);
        }

        int number = EndingRecordStore.TakeHomunculusNumber();
        bool isNewRecord = result.EndReason == RunEndReason.Cleared &&
                           EndingRecordStore.TryRecordClear(result.SurvivalSeconds);
        _view.ShowResult(result, number, EndingRecordStore.BestClearSeconds, isNewRecord);

        List<EndingRuneData> runes = CollectEquippedRunes(out int activeSlotCount);
        _view.ShowRunes(runes, activeSlotCount);
    }

    private List<EndingRuneData> CollectEquippedRunes(out int activeSlotCount)
    {
        List<EndingRuneData> runes = new();
        activeSlotCount = 0;

        PlayerRuneManager runeManager = PlayerRuneManager.Instance;
        RuneEffectDatabase database = RuneEffectDatabase.Instance;
        if (runeManager == null || !runeManager.IsInitialized || database == null)
        {
            Debug.LogWarning("[EndingResultPresenter] 룬 데이터가 없어 사용한 룬을 표시하지 않습니다.", this);
            return runes;
        }

        RuneEffectLoadout loadout = runeManager.RuneEffectLoadout;
        activeSlotCount = loadout.ActiveSlotCount;

        IReadOnlyList<string> ids = loadout.EquippedEffectIds;
        for (int i = 0; i < ids.Count; i++)
        {
            string id = ids[i];
            if (string.IsNullOrEmpty(id) || !database.TryGetById(id, out RuneEffectDefinition effect))
            {
                continue;
            }

            bool isMain = i == RuneEffectLoadout.PrimaryElementSlotIndex &&
                          effect.Category == RuneEffectCategory.Element;
            runes.Add(new EndingRuneData(GetName(effect), FindIcon(effect), isMain, GetElementColor(id)));
        }

        activeSlotCount = Mathf.Max(activeSlotCount, runes.Count);
        return runes;
    }

    private static string GetName(RuneEffectDefinition effect)
    {
        if (!string.IsNullOrEmpty(effect.DisplayName)) return effect.DisplayName;
        if (!string.IsNullOrEmpty(effect.EnglishName)) return effect.EnglishName;
        return effect.RuneWord;
    }

    private Sprite FindIcon(RuneEffectDefinition effect)
    {
        if (effect.Icon != null || _iconCatalog == null)
        {
            return effect.Icon;
        }

        try
        {
            return _iconCatalog.GetIcon(effect.Id).Icon;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private Color GetElementColor(string effectId)
    {
        return effectId switch
        {
            RuneEffectIds.Fire => _fireColor,
            RuneEffectIds.Water => _waterColor,
            RuneEffectIds.Earth => _earthColor,
            RuneEffectIds.Wind => _windColor,
            _ => _otherColor
        };
    }

    private void HandleRestartRequested()
    {
        if (_isLeaving)
        {
            return;
        }

        _isLeaving = true;
        RunFlow.Restart();
    }

    private void HandleTitleRequested()
    {
        if (_isLeaving)
        {
            return;
        }

        _isLeaving = true;
        RunFlow.GoToTitle();
    }
}
