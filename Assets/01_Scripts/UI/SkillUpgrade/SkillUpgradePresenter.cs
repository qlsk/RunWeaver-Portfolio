using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스킬 업그레이드 화면 전체를 담당한다.
/// - 성장 노드를 누르면 오른쪽 패널에 이름, 레벨, 선행 노드, 설명, 비용을 보여주고 Upgrade 버튼을 켠다.
/// - 스킬 본체(가운데 큰 노드)를 누르면 스킬 소개를 보여주고 레벨, 비용, Upgrade 버튼을 숨긴다.
/// - Upgrade 버튼: 재료 부족, 선행 노드 미충족, 준비 중이면 비활성 / 최대 레벨이면 "MAX"
/// - Close 버튼과 Esc로 닫는다. 씬 이동 등은 Closed 이벤트를 구독한 쪽이 담당한다.
/// - 선택 표시(SkillSelectionCursor)가 선택한 노드로 부드럽게 이동한다.
/// - 방향키로 그 방향에 있는 가장 가까운 노드를 선택한다. 누르고 있으면 일정 간격으로 계속 넘어간다.
/// </summary>
[DisallowMultipleComponent]
public class SkillUpgradePresenter : MonoBehaviour
{
    [SerializeField] private SkillUpgradeDataSource _dataSource;

    [Tooltip("열고 닫을 화면 루트. 비워두면 이 오브젝트를 사용한다.")]
    [SerializeField] private GameObject _screenRoot;

    [Header("노드 (자식의 SkillNodeView, SkillMainNodeView를 자동으로 모은다)")]
    [SerializeField] private Transform _nodeContainer;

    [Header("정보 패널")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _prerequisiteText;
    [SerializeField] private TMP_Text _descriptionText;

    [Header("비용 (자식의 SkillCostRowView를 위에서부터 사용한다)")]
    [SerializeField] private GameObject _costPanel;

    [Header("버튼 (버튼 오브젝트 또는 그 부모를 넣으면 하위의 Button을 찾는다)")]
    [SerializeField] private Transform _upgradeButtonRoot;
    [SerializeField] private Transform _closeButtonRoot;
    [SerializeField] private KeyCode[] _closeKeys = { KeyCode.Escape };

    [Header("선택 표시")]
    [SerializeField] private SkillSelectionCursor _selectionCursor;

    [Header("방향키 이동")]
    [Tooltip("스킬 본체(가운데 큰 노드)도 방향키 이동 대상에 포함한다.")]
    [SerializeField] private bool _includeMainNodesInNavigation = true;
    [Tooltip("누른 방향에서 이 각도 안에 있는 노드만 후보로 본다.")]
    [SerializeField, Range(10f, 89f)] private float _maxNavigationAngle = 60f;
    [Tooltip("누르고 있을 때 반복이 시작되기까지의 시간 (초)")]
    [SerializeField, Min(0f)] private float _repeatDelay = 0.35f;
    [Tooltip("반복 이동 간격 (초)")]
    [SerializeField, Min(0.02f)] private float _repeatInterval = 0.12f;

    [Header("문구")]
    [SerializeField] private string _levelFormat = "Max Lv. {0} / Current Lv. {1}";
    [SerializeField] private string _prerequisiteFormat = "Requires: {0} Lv. 1";
    [SerializeField] private string _upgradeLabel = "Upgrade";
    [SerializeField] private string _maxLevelLabel = "MAX";

    private readonly List<SkillNodeView> _nodes = new();
    private readonly List<SkillMainNodeView> _mainNodes = new();
    private SkillCostRowView[] _costRows = Array.Empty<SkillCostRowView>();
    private Button _upgradeButton;
    private TMP_Text _upgradeButtonText;
    private Button _closeButton;

    private SkillNodeView _selectedNode;
    private SkillMainNodeView _selectedMainNode;
    private SkillUpgradeDataSource _subscribedDataSource;
    private bool _isInitialized;

    private Vector2 _heldDirection;
    private float _nextRepeatTime;

    /// <summary>Close 버튼이나 Esc로 화면을 닫았을 때 발생한다.</summary>
    public event Action Closed;

    private GameObject ScreenRoot => _screenRoot != null ? _screenRoot : gameObject;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        if (_dataSource != null)
        {
            _dataSource.Changed += Refresh;
            _subscribedDataSource = _dataSource;
        }

        Refresh();
        MoveCursorToSelection(true);
    }

    private void OnDisable()
    {
        if (_subscribedDataSource != null)
        {
            _subscribedDataSource.Changed -= Refresh;
            _subscribedDataSource = null;
        }
    }

    private void OnDestroy()
    {
        foreach (SkillNodeView node in _nodes)
        {
            node.Clicked -= HandleNodeClicked;
        }

        foreach (SkillMainNodeView mainNode in _mainNodes)
        {
            mainNode.Clicked -= HandleMainNodeClicked;
        }

        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.RemoveListener(HandleUpgradeClicked);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(Close);
        }
    }

    private void Update()
    {
        if (!ScreenRoot.activeInHierarchy)
        {
            return;
        }

        foreach (KeyCode key in _closeKeys)
        {
            if (Input.GetKeyDown(key))
            {
                Close();
                return;
            }
        }

        UpdateKeyboardNavigation();
    }

    [ContextMenu("Test/Open")]
    public void Open()
    {
        ScreenRoot.SetActive(true);
        Refresh();
        MoveCursorToSelection(true);
    }

    public void Close()
    {
        if (!ScreenRoot.activeSelf)
        {
            return;
        }

        ScreenRoot.SetActive(false);
        SafeEventNotifier.Notify(Closed);
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        if (_dataSource == null)
        {
            Debug.LogError("[SkillUpgradePresenter] 데이터 소스가 할당되지 않았습니다.", this);
        }

        if (_nodeContainer != null)
        {
            _nodes.AddRange(_nodeContainer.GetComponentsInChildren<SkillNodeView>(true));
            _mainNodes.AddRange(_nodeContainer.GetComponentsInChildren<SkillMainNodeView>(true));
        }
        else
        {
            Debug.LogError("[SkillUpgradePresenter] 노드 컨테이너가 할당되지 않았습니다.", this);
        }

        foreach (SkillNodeView node in _nodes)
        {
            node.Clicked += HandleNodeClicked;
        }

        foreach (SkillMainNodeView mainNode in _mainNodes)
        {
            mainNode.Clicked += HandleMainNodeClicked;
        }

        if (_costPanel != null)
        {
            _costRows = _costPanel.GetComponentsInChildren<SkillCostRowView>(true);
        }

        if (_upgradeButtonRoot != null)
        {
            _upgradeButton = _upgradeButtonRoot.GetComponentInChildren<Button>(true);
            _upgradeButtonText = _upgradeButtonRoot.GetComponentInChildren<TMP_Text>(true);
        }

        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.AddListener(HandleUpgradeClicked);
        }
        else
        {
            Debug.LogWarning("[SkillUpgradePresenter] Upgrade 버튼을 찾지 못했습니다.", this);
        }

        if (_closeButtonRoot != null)
        {
            _closeButton = _closeButtonRoot.GetComponentInChildren<Button>(true);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Close);
        }

        // 처음에는 첫 번째 스킬 본체의 소개를 보여준다.
        _selectedMainNode = _mainNodes.Count > 0 ? _mainNodes[0] : null;
    }

    private void HandleNodeClicked(SkillNodeView node)
    {
        Select(node, null);
    }

    private void HandleMainNodeClicked(SkillMainNodeView mainNode)
    {
        Select(null, mainNode);
    }

    private void Select(SkillNodeView node, SkillMainNodeView mainNode)
    {
        _selectedNode = node;
        _selectedMainNode = node == null ? mainNode : null;
        Refresh();
        MoveCursorToSelection(false);
    }

    private RectTransform GetSelectedTransform()
    {
        if (_selectedNode != null)
        {
            return (RectTransform)_selectedNode.transform;
        }

        return _selectedMainNode != null ? (RectTransform)_selectedMainNode.transform : null;
    }

    private void MoveCursorToSelection(bool instant)
    {
        if (_selectionCursor != null)
        {
            _selectionCursor.MoveTo(GetSelectedTransform(), instant);
        }
    }

    private void UpdateKeyboardNavigation()
    {
        Vector2 direction = ReadArrowDirection();
        if (direction == Vector2.zero)
        {
            _heldDirection = Vector2.zero;
            return;
        }

        bool isNewPress = direction != _heldDirection;
        if (!isNewPress && Time.unscaledTime < _nextRepeatTime)
        {
            return;
        }

        _nextRepeatTime = Time.unscaledTime + (isNewPress ? _repeatDelay : _repeatInterval);
        _heldDirection = direction;
        Navigate(direction);
    }

    /// <summary>
    /// 방향키 입력. 여러 키를 함께 누르면 가장 먼저 확인된 방향 하나만 사용한다.
    /// </summary>
    private static Vector2 ReadArrowDirection()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            return Vector2.up;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            return Vector2.down;
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            return Vector2.left;
        }

        return Input.GetKey(KeyCode.RightArrow) ? Vector2.right : Vector2.zero;
    }

    /// <summary>
    /// 현재 선택에서 누른 방향으로 일정 각도 안에 있는 노드 중, 가깝고 방향이 정확한 노드를 선택한다.
    /// </summary>
    private void Navigate(Vector2 direction)
    {
        RectTransform current = GetSelectedTransform();
        if (current == null)
        {
            if (_nodes.Count > 0)
            {
                Select(_nodes[0], null);
            }

            return;
        }

        Vector2 origin = current.position;
        float minCos = Mathf.Cos(_maxNavigationAngle * Mathf.Deg2Rad);
        float bestScore = float.MaxValue;
        SkillNodeView bestNode = null;
        SkillMainNodeView bestMainNode = null;

        foreach (SkillNodeView node in _nodes)
        {
            if (node != _selectedNode && node.isActiveAndEnabled
                && TryScore(origin, node.transform.position, direction, minCos, out float score) && score < bestScore)
            {
                bestScore = score;
                bestNode = node;
                bestMainNode = null;
            }
        }

        if (_includeMainNodesInNavigation)
        {
            foreach (SkillMainNodeView mainNode in _mainNodes)
            {
                if (mainNode != _selectedMainNode && mainNode.isActiveAndEnabled
                    && TryScore(origin, mainNode.transform.position, direction, minCos, out float score)
                    && score < bestScore)
                {
                    bestScore = score;
                    bestNode = null;
                    bestMainNode = mainNode;
                }
            }
        }

        if (bestNode != null || bestMainNode != null)
        {
            Select(bestNode, bestMainNode);
        }
    }

    /// <summary>
    /// 거리가 가까울수록, 누른 방향과 일치할수록 점수가 낮다(좋다).
    /// </summary>
    private static bool TryScore(Vector2 origin, Vector2 position, Vector2 direction, float minCos, out float score)
    {
        Vector2 offset = position - origin;
        float distance = offset.magnitude;
        score = float.MaxValue;
        if (distance < 0.01f)
        {
            return false;
        }

        float cos = Vector2.Dot(offset / distance, direction);
        if (cos < minCos)
        {
            return false;
        }

        // 방향이 어긋날수록 거리에 벌점을 준다. (정면이면 1배, 허용 각도 끝이면 약 3배)
        score = distance * (1f + 2f * (1f - cos) / (1f - minCos));
        return true;
    }

    private void HandleUpgradeClicked()
    {
        if (_dataSource == null || _selectedNode == null)
        {
            return;
        }

        if (!_dataSource.TryUpgrade(_selectedNode.NodeId))
        {
            Debug.Log($"[SkillUpgradePresenter] {_selectedNode.NodeId}을(를) 업그레이드할 수 없습니다.");
        }

        // 데이터 소스의 Changed 이벤트로 화면이 갱신되지만, 이벤트가 없는 경우를 대비해 한 번 더 갱신한다.
        Refresh();
    }

    private void Refresh()
    {
        if (_dataSource == null)
        {
            return;
        }

        foreach (SkillNodeView node in _nodes)
        {
            node.Show(_dataSource.GetNode(node.NodeId));
            node.SetSelected(node == _selectedNode);
        }

        foreach (SkillMainNodeView mainNode in _mainNodes)
        {
            mainNode.SetSelected(mainNode == _selectedMainNode);
        }

        if (_selectedNode != null)
        {
            ShowNode(_dataSource.GetNode(_selectedNode.NodeId));
        }
        else if (_selectedMainNode != null)
        {
            ShowSkillIntro(_dataSource.GetSkillIntro(_selectedMainNode.SkillId));
        }
    }

    private void ShowNode(SkillNodeData node)
    {
        SetText(_nameText, node.DisplayName);
        SetText(_descriptionText, node.Description);

        bool hasLevel = !node.IsComingSoon;
        SetActive(_levelText, hasLevel);
        if (hasLevel)
        {
            SetText(_levelText, string.Format(_levelFormat, node.MaxLevel, node.Level));
        }

        bool showPrerequisite = !node.IsComingSoon && !node.IsPrerequisiteMet
                                                   && !string.IsNullOrEmpty(node.PrerequisiteName);
        SetActive(_prerequisiteText, showPrerequisite);
        if (showPrerequisite)
        {
            SetText(_prerequisiteText, string.Format(_prerequisiteFormat, node.PrerequisiteName));
        }

        ShowCosts(node.Costs);

        SetUpgradeButtonVisible(true);
        if (_upgradeButton != null)
        {
            _upgradeButton.interactable = !node.IsMaxLevel && _dataSource.CanUpgrade(node.Id);
        }

        SetText(_upgradeButtonText, node.IsMaxLevel ? _maxLevelLabel : _upgradeLabel);
    }

    private void ShowSkillIntro(SkillIntroData skill)
    {
        SetText(_nameText, skill.DisplayName);
        SetText(_descriptionText, skill.Description);
        SetActive(_levelText, false);
        SetActive(_prerequisiteText, false);
        ShowCosts(Array.Empty<SkillCostData>());
        SetUpgradeButtonVisible(false);
    }

    private void ShowCosts(IReadOnlyList<SkillCostData> costs)
    {
        if (_costPanel != null)
        {
            _costPanel.SetActive(costs.Count > 0);
        }

        for (int i = 0; i < _costRows.Length; i++)
        {
            if (i < costs.Count)
            {
                _costRows[i].Show(costs[i]);
            }
            else
            {
                _costRows[i].Hide();
            }
        }
    }

    private void SetUpgradeButtonVisible(bool isVisible)
    {
        if (_upgradeButtonRoot != null)
        {
            _upgradeButtonRoot.gameObject.SetActive(isVisible);
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }

    private static void SetActive(Component component, bool isActive)
    {
        if (component != null)
        {
            component.gameObject.SetActive(isActive);
        }
    }
}
