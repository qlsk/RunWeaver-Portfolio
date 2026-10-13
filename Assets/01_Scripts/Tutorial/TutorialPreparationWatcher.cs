using System;
using UnityEngine;

/// <summary>
/// 출발 준비(시작 석상) 조작이 튜토리얼 단계보다 먼저 일어났는지 감시한다.
/// 이벤트가 나타내는 단계가 현재 단계보다 뒤이면 순서가 어긋난 것으로 본다.
/// TutorialConditionKind의 선언 순서가 단계 진행 순서와 같다는 전제를 쓴다.
/// </summary>
public class TutorialPreparationWatcher
{
    private ExpeditionStatue _statue;
    private MainElementSelectView _elementView;
    private RuneBookV2Presenter _runeBookPresenter;
    private Func<TutorialConditionKind> _getCurrentKind;

    public event Action OutOfOrderDetected;

    public void Begin(Func<TutorialConditionKind> getCurrentKind)
    {
        _getCurrentKind = getCurrentKind;

        _statue = UnityEngine.Object.FindAnyObjectByType<ExpeditionStatue>();
        if (_statue != null)
            _statue.Interacted += HandleStatueInteracted;
        else
            Debug.LogWarning("[TutorialPreparationWatcher] ExpeditionStatue를 찾을 수 없습니다.");

        _elementView = UnityEngine.Object.FindAnyObjectByType<MainElementSelectView>(FindObjectsInactive.Include);
        if (_elementView != null)
            _elementView.Confirmed += HandleElementConfirmed;
        else
            Debug.LogWarning("[TutorialPreparationWatcher] MainElementSelectView를 찾을 수 없습니다.");

        _runeBookPresenter = UnityEngine.Object.FindAnyObjectByType<RuneBookV2Presenter>(FindObjectsInactive.Include);
        if (_runeBookPresenter != null)
            _runeBookPresenter.Confirmed += HandleRunesConfirmed;
        else
            Debug.LogWarning("[TutorialPreparationWatcher] RuneBookV2Presenter를 찾을 수 없습니다.");
    }

    public void End()
    {
        if (_statue != null)
            _statue.Interacted -= HandleStatueInteracted;

        if (_elementView != null)
            _elementView.Confirmed -= HandleElementConfirmed;

        if (_runeBookPresenter != null)
            _runeBookPresenter.Confirmed -= HandleRunesConfirmed;

        _statue = null;
        _elementView = null;
        _runeBookPresenter = null;
        _getCurrentKind = null;
    }

    private void HandleStatueInteracted()
    {
        Check(TutorialConditionKind.StatueInteract);
    }

    private void HandleElementConfirmed(ReactionElement element)
    {
        Check(TutorialConditionKind.ElementConfirm);
    }

    private void HandleRunesConfirmed()
    {
        Check(TutorialConditionKind.RuneConfirm);
    }

    private void Check(TutorialConditionKind eventKind)
    {
        if (_getCurrentKind == null)
            return;

        if (eventKind > _getCurrentKind())
            OutOfOrderDetected?.Invoke();
    }
}
