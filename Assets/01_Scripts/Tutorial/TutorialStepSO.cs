using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Tutorial Step SO")]
public class TutorialStepSO : ScriptableObject
{
    [SerializeField] private string _title;
    [SerializeField, TextArea] private string _body;
    [SerializeField] private TutorialConditionKind _condition;
    [SerializeField] private float _conditionValue;

    [Header("카드 행동 행")]
    [SerializeField] private TutorialActionRowKind _actionRow;
    [SerializeField] private string[] _keyLabels;
    [SerializeField] private string _actionLabel;

    [Header("오버레이")]
    [SerializeField] private TutorialOverlayKind _overlay;
    [SerializeField] private GameObject _overlayPrefab;
    [SerializeField] private string _overlayText;
    [Tooltip("스포트라이트 오버레이가 비출 UI의 대상 id (TutorialSpotlightView의 대상 목록과 맞춘다)")]
    [SerializeField] private string[] _spotlightTargetIds;

    public string Title => _title;
    public string Body => _body;
    public TutorialConditionKind Condition => _condition;
    public float ConditionValue => _conditionValue;
    public TutorialActionRowKind ActionRow => _actionRow;
    public IReadOnlyList<string> KeyLabels => _keyLabels;
    public string ActionLabel => _actionLabel;
    public TutorialOverlayKind Overlay => _overlay;
    public GameObject OverlayPrefab => _overlayPrefab;
    public string OverlayText => _overlayText;
    public IReadOnlyList<string> SpotlightTargetIds => _spotlightTargetIds;
}
