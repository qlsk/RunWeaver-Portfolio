using TMPro;
using UnityEngine;

/// <summary>
/// 연구 한 줄 (줄 이름, 설명, 단계 노드들, 연결선, 누적 효과).
/// 표시할 줄은 루트 연구 id(_rootId)로 지정한다.
/// </summary>
[DisallowMultipleComponent]
public class ResearchTrackView : MonoBehaviour
{
    [Tooltip("DB의 분류용 루트 연구 id (예: research_rune_01)")]
    [SerializeField] private string _rootId;

    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _subtitleText;
    [SerializeField] private TMP_Text _totalText;
    [SerializeField] private ResearchNodeView[] _nodes;

    [Tooltip("노드 사이 연결선. i번째 선은 i번째 노드와 i+1번째 노드 사이")]
    [SerializeField] private ResearchLineView[] _lines;

    public string RootId => _rootId;

    public ResearchNodeView[] Nodes => _nodes;

    public void Show(ResearchTrackData track, ResearchDataSource source)
    {
        if (track == null)
        {
            return;
        }

        SetText(_nameText, track.DisplayName);
        SetText(_subtitleText, track.Subtitle);
        SetText(_totalText, track.TotalText);

        for (int i = 0; i < _nodes.Length; i++)
        {
            ResearchNodeData node = i < track.NodeIds.Count ? source.GetNode(track.NodeIds[i]) : null;
            if (_nodes[i] != null)
            {
                _nodes[i].Show(node);
            }

            if (_lines != null && i < _lines.Length && _lines[i] != null)
            {
                _lines[i].SetOn(node != null && node.State == ResearchNodeState.Researched);
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
}
