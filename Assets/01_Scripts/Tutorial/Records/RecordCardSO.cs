using UnityEngine;

/// <summary>상황 안내 카드(새 기록) 한 장의 문구와 발생 조건 데이터. 같은 id의 카드는 한 번만 보여준다.</summary>
[CreateAssetMenu(menuName = "Scriptable Objects/Tutorial Record Card SO")]
public class RecordCardSO : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private RecordTriggerKind _trigger;
    [Tooltip("발생 조건을 더 좁히는 키. 원소 반응은 '불+물'처럼 알파벳 순 영문 쌍(예: Fire+Water), 임무 시작은 임무 종류 이름. 비우면 해당 조건의 모든 발생에 반응한다")]
    [SerializeField] private string _triggerKey;

    [Header("문구")]
    [Tooltip("머리말의 '새 기록 / ' 뒤에 붙는 분류")]
    [SerializeField] private string _category;
    [SerializeField] private string _title;
    [SerializeField, TextArea] private string _body;
    [SerializeField] private string _footer;

    [Header("표시")]
    [Tooltip("일반, 원소 반응, 경고 중 알맞은 카드 프리팹")]
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField, Min(1f)] private float _displaySeconds = 6f;

    public string Id => _id;
    public RecordTriggerKind Trigger => _trigger;
    public string TriggerKey => _triggerKey;
    public string Category => _category;
    public string Title => _title;
    public string Body => _body;
    public string Footer => _footer;
    public GameObject CardPrefab => _cardPrefab;
    public float DisplaySeconds => _displaySeconds;
}
