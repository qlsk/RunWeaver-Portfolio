using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 스킬 성장 노드 한 칸. 노드 오브젝트(예: AttackNode_01)에 붙인다.
/// 노드 이미지의 색으로 레벨을 표시한다. 레벨 0은 시작 색, 최대 레벨은 스킬 본체(MainNode)의 색이며
/// 그 사이는 레벨 비율만큼 섞는다. 선행 노드를 찍지 않았거나 준비 중인 노드는 잠금 색으로 표시한다.
/// 자식 이미지(슬롯, 아이콘)를 눌러도 이 노드가 선택된다.
/// </summary>
[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class SkillNodeView : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("DB의 성장 노드 id (예: attack_01)")]
    [SerializeField] private string _nodeId;

    [Tooltip("최대 레벨 색을 가져올 스킬 본체 이미지. 비워두면 부모의 SkillMainNodeView 이미지를 사용한다.")]
    [SerializeField] private Image _mainNodeImage;

    [Header("색")]
    [SerializeField] private Color _zeroLevelColor = Color.white;
    [SerializeField] private Color _lockedColor = new(0.35f, 0.35f, 0.38f, 1f);

    [Header("선택 표시")]
    [SerializeField, Min(1f)] private float _selectedScale = 1.15f;

    private Image _levelImage;
    private Vector3 _normalScale = Vector3.one;
    private bool _isInitialized;

    public event Action<SkillNodeView> Clicked;

    public string NodeId => _nodeId;

    private void Awake()
    {
        Initialize();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Clicked?.Invoke(this);
        }
    }

    public void Show(SkillNodeData node)
    {
        Initialize();
        if (node == null)
        {
            return;
        }

        if (node.IsComingSoon || !node.IsPrerequisiteMet)
        {
            _levelImage.color = _lockedColor;
            return;
        }

        Color maxLevelColor = _mainNodeImage != null ? _mainNodeImage.color : _zeroLevelColor;
        _levelImage.color = Color.Lerp(_zeroLevelColor, maxLevelColor, node.LevelRatio);
    }

    public void SetSelected(bool isSelected)
    {
        Initialize();
        transform.localScale = isSelected ? _normalScale * _selectedScale : _normalScale;
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        _levelImage = GetComponent<Image>();
        _normalScale = transform.localScale;

        if (_mainNodeImage == null)
        {
            SkillMainNodeView mainNode = GetComponentInParent<SkillMainNodeView>(true);
            if (mainNode != null)
            {
                _mainNodeImage = mainNode.GetComponent<Image>();
            }
        }

        if (string.IsNullOrWhiteSpace(_nodeId))
        {
            Debug.LogWarning($"[SkillNodeView] {name}의 노드 id가 비어 있습니다.", this);
        }
    }
}
