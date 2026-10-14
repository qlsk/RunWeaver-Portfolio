using System.Collections.Generic;
using UnityEngine;

/// <summary>필수 단계 하나의 문구, 완료 조건, 카드 행동 행, 오버레이 데이터.</summary>
[CreateAssetMenu(menuName = "Scriptable Objects/Tutorial Step SO")]
public class TutorialStepSO : ScriptableObject
{
    [SerializeField] private string _title;
    [SerializeField, TextArea] private string _body;
    [SerializeField] private TutorialConditionKind _condition;
    [SerializeField] private float _conditionValue;
    [Tooltip("조건이 가리키는 대상 id. 룬 효과 장착 조건이 장착을 확인할 룬 효과 id")]
    [SerializeField] private string _conditionTargetId;

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
    [Tooltip("월드 표식 오버레이가 따라갈 대상")]
    [SerializeField] private TutorialMarkerTarget _markerTarget;

    public string Title => _title;
    public string Body => _body;
    public TutorialConditionKind Condition => _condition;
    public float ConditionValue => _conditionValue;
    public string ConditionTargetId => _conditionTargetId;
    public TutorialActionRowKind ActionRow => _actionRow;
    public IReadOnlyList<string> KeyLabels => _keyLabels;
    public string ActionLabel => _actionLabel;
    public TutorialOverlayKind Overlay => _overlay;
    public GameObject OverlayPrefab => _overlayPrefab;
    public string OverlayText => _overlayText;
    public IReadOnlyList<string> SpotlightTargetIds => _spotlightTargetIds;
    public TutorialMarkerTarget MarkerTarget => _markerTarget;
}
