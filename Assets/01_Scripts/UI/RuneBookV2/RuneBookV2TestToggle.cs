using UnityEngine;

/// <summary>
/// [테스트 전용] 새 룬북 테스트 씬에서 키로 룬북을 열고 닫는다. (확정하지 않고 닫으면 바꾼 내용이 사라지는지 확인용)
/// 실제 게임에서는 RuneBookToggle 같은 입력 처리로 교체한다. 항상 켜져 있는 오브젝트에 붙인다.
/// </summary>
[DisallowMultipleComponent]
public class RuneBookV2TestToggle : MonoBehaviour
{
    [SerializeField] private GameObject _runeBook;
    [SerializeField] private KeyCode _toggleKey = KeyCode.Tab;

    private void Update()
    {
        if (_runeBook != null && Input.GetKeyDown(_toggleKey))
        {
            _runeBook.SetActive(!_runeBook.activeSelf);
        }
    }
}
