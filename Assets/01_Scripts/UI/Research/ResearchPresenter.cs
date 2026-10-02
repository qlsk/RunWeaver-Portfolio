using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 연구 화면을 ResearchDataSource와 연결한다.
/// - 노드를 클릭하거나 방향키(위아래: 줄, 좌우: 단계)로 선택하면 정보 판넬에 표시한다.
/// - Research 버튼 또는 Enter로 선택한 연구를 진행한다.
/// - 처음에는 연구 가능한 첫 노드를 선택한다.
/// - Esc는 화면 닫기 요청(BackRequested)만 알린다. (로비 흐름에서 연결)
/// 데이터 소스를 비워 두면 씬에서 찾는다.
/// </summary>
[DisallowMultipleComponent]
public class ResearchPresenter : MonoBehaviour
{
    [SerializeField] private ResearchDataSource _dataSource;
    [SerializeField] private ResearchTrackView[] _tracks;
    [SerializeField] private ResearchInfoView _infoView;
    [SerializeField] private ResearchResourceView[] _resourceViews;
    [SerializeField] private SkillSelectionCursor _cursor;

    [Tooltip("누르면 Esc와 같이 화면 닫기 요청(BackRequested)을 보낸다.")]
    [SerializeField] private Button _closeButton;

    [Header("키")]
    [SerializeField] private KeyCode _researchKey = KeyCode.Return;
    [SerializeField] private KeyCode _backKey = KeyCode.Escape;

    private int _selectedTrack = -1;
    private int _selectedStep = -1;

    /// <summary>Esc로 연구 화면을 나가려 할 때</summary>
    public event Action BackRequested;

    private void Awake()
    {
        if (_dataSource == null)
        {
            _dataSource = FindAnyObjectByType<ResearchDataSource>();
        }

        if (_dataSource == null)
        {
            Debug.LogError("[ResearchPresenter] ResearchDataSource를 찾을 수 없습니다.", this);
            enabled = false;
            return;
        }

        foreach (ResearchTrackView track in _tracks)
        {
            if (track == null)
            {
                continue;
            }

            foreach (ResearchNodeView node in track.Nodes)
            {
                if (node != null)
                {
                    node.Clicked += HandleNodeClicked;
                }
            }
        }

        if (_infoView != null)
        {
            _infoView.ResearchClicked += ResearchSelected;
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(RequestBack);
        }

        _dataSource.Changed += Refresh;
    }

    private void Start()
    {
        Refresh();
        SelectFirstAvailable(true);
    }

    private void OnDestroy()
    {
        if (_dataSource != null)
        {
            _dataSource.Changed -= Refresh;
        }

        if (_infoView != null)
        {
            _infoView.ResearchClicked -= ResearchSelected;
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(RequestBack);
        }

        foreach (ResearchTrackView track in _tracks)
        {
            if (track == null)
            {
                continue;
            }

            foreach (ResearchNodeView node in track.Nodes)
            {
                if (node != null)
                {
                    node.Clicked -= HandleNodeClicked;
                }
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveSelection(-1, 0);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveSelection(1, 0);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveSelection(0, -1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveSelection(0, 1);
        }
        else if (Input.GetKeyDown(_researchKey) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ResearchSelected();
        }
        else if (Input.GetKeyDown(_backKey))
        {
            RequestBack();
        }
    }

    private void RequestBack()
    {
        Debug.Log("[ResearchPresenter] Back requested", this);
        BackRequested?.Invoke();
    }

    public void ResearchSelected()
    {
        ResearchNodeView node = GetSelectedNode();
        if (node != null)
        {
            _dataSource.TryResearch(node.ResearchId);
        }
    }

    private void Refresh()
    {
        IReadOnlyList<ResearchTrackData> tracks = _dataSource.GetTracks();
        foreach (ResearchTrackView trackView in _tracks)
        {
            if (trackView == null)
            {
                continue;
            }

            ResearchTrackData track = FindTrack(tracks, trackView.RootId);
            if (track != null)
            {
                trackView.Show(track, _dataSource);
            }
        }

        if (_resourceViews != null)
        {
            foreach (ResearchResourceView resourceView in _resourceViews)
            {
                if (resourceView != null)
                {
                    resourceView.Show(_dataSource);
                }
            }
        }

        ShowSelected(false);
    }

    private void SelectFirstAvailable(bool instant)
    {
        for (int t = 0; t < _tracks.Length; t++)
        {
            ResearchNodeView[] nodes = _tracks[t] != null ? _tracks[t].Nodes : null;
            if (nodes == null)
            {
                continue;
            }

            for (int s = 0; s < nodes.Length; s++)
            {
                ResearchNodeData data = nodes[s] != null ? _dataSource.GetNode(nodes[s].ResearchId) : null;
                if (data != null && data.State == ResearchNodeState.Available)
                {
                    Select(t, s, instant);
                    return;
                }
            }
        }

        Select(0, 0, instant);
    }

    private void MoveSelection(int trackDelta, int stepDelta)
    {
        if (_tracks.Length == 0)
        {
            return;
        }

        int track = Mathf.Clamp(_selectedTrack + trackDelta, 0, _tracks.Length - 1);
        int stepCount = _tracks[track] != null ? _tracks[track].Nodes.Length : 0;
        int step = Mathf.Clamp(_selectedStep + stepDelta, 0, Mathf.Max(0, stepCount - 1));
        Select(track, step, false);
    }

    private void Select(int track, int step, bool instant)
    {
        _selectedTrack = track;
        _selectedStep = step;
        ShowSelected(instant);
    }

    private void ShowSelected(bool instantCursor)
    {
        ResearchNodeView node = GetSelectedNode();
        if (_infoView != null)
        {
            _infoView.Show(node != null ? _dataSource.GetNode(node.ResearchId) : null);
        }

        if (_cursor != null && node != null)
        {
            _cursor.MoveTo(node.RectTransform, instantCursor);
        }
    }

    private ResearchNodeView GetSelectedNode()
    {
        if (_selectedTrack < 0 || _selectedTrack >= _tracks.Length || _tracks[_selectedTrack] == null)
        {
            return null;
        }

        ResearchNodeView[] nodes = _tracks[_selectedTrack].Nodes;
        return _selectedStep >= 0 && _selectedStep < nodes.Length ? nodes[_selectedStep] : null;
    }

    private void HandleNodeClicked(ResearchNodeView node)
    {
        for (int t = 0; t < _tracks.Length; t++)
        {
            int step = _tracks[t] != null ? Array.IndexOf(_tracks[t].Nodes, node) : -1;
            if (step >= 0)
            {
                Select(t, step, false);
                return;
            }
        }
    }

    private static ResearchTrackData FindTrack(IReadOnlyList<ResearchTrackData> tracks, string rootId)
    {
        if (tracks == null)
        {
            return null;
        }

        foreach (ResearchTrackData track in tracks)
        {
            if (track.Id == rootId)
            {
                return track;
            }
        }

        return null;
    }
}
