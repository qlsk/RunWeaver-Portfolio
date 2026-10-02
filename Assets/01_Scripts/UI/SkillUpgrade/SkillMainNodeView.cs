using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 스킬 본체(가운데 큰 노드, 예: AttackMainNode). 누르면 스킬 소개를 보여준다.
/// 성장 노드가 이 오브젝트의 자식이어도, 성장 노드를 누르면 성장 노드가 먼저 클릭을 받는다.
/// </summary>
[DisallowMultipleComponent]
public class SkillMainNodeView : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("DB의 스킬 id (attack, dash, shield, cleanse)")]
    [SerializeField] private string _skillId;

    [Header("선택 표시")]
    [SerializeField, Min(1f)] private float _selectedScale = 1.08f;

    private Transform _scaleTarget;
    private Vector3 _normalScale = Vector3.one;
    private bool _isInitialized;

    public event Action<SkillMainNodeView> Clicked;

    public string SkillId => _skillId;

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

    public void SetSelected(bool isSelected)
    {
        Initialize();
        if (_scaleTarget != null)
        {
            _scaleTarget.localScale = isSelected ? _normalScale * _selectedScale : _normalScale;
        }
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        // 성장 노드들이 자식이므로 본체 전체를 키우지 않고, 본체 문양(첫 번째 자식)만 키운다.
        _scaleTarget = transform.childCount > 0 ? transform.GetChild(0) : null;
        if (_scaleTarget != null)
        {
            _normalScale = _scaleTarget.localScale;
        }
    }
}
