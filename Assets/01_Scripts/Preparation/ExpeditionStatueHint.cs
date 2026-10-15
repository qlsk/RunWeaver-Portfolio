using UnityEngine;

/// <summary>
/// 전투 씬이 열리면 보이는 "룬 비석과 상호작용하세요" 안내 문구.
/// 플레이어가 석상(룬 비석)과 처음 상호작용하면 사라지고, 다시 나타나지 않는다.
/// 이 컴포넌트는 문구 오브젝트에 붙인다. (숨겨지면 구독도 함께 해제된다)
/// </summary>
[DisallowMultipleComponent]
public class ExpeditionStatueHint : MonoBehaviour
{
    [SerializeField] private ExpeditionStatue _statue;

    private void OnEnable()
    {
        if (_statue != null)
        {
            _statue.Interacted += Hide;
        }
    }

    private void OnDisable()
    {
        if (_statue != null)
        {
            _statue.Interacted -= Hide;
        }
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
