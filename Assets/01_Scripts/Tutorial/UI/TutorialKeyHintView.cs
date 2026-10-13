using TMPro;
using UnityEngine;

/// <summary>키 힌트 알약 오버레이를 만들어 문구를 채우고, 끝나면 파괴한다.</summary>
public class TutorialKeyHintView : MonoBehaviour
{
    private const string LabelName = "Label";

    [SerializeField] private RectTransform _hintRoot;

    private GameObject _current;

    public void Show(TutorialStepSO step)
    {
        Hide();

        if (step.OverlayPrefab == null)
        {
            Debug.LogError("[TutorialKeyHintView] 키 힌트 프리팹이 지정되지 않았습니다.");
            return;
        }

        _current = Instantiate(step.OverlayPrefab, _hintRoot);

        Transform label = _current.transform.Find(LabelName);
        if (label != null && label.TryGetComponent(out TMP_Text labelText))
            labelText.text = step.OverlayText;
    }

    public void Hide()
    {
        if (_current == null)
            return;

        // Destroy는 프레임 끝에 실행되므로, 같은 프레임에 새 알약이 생겨도 겹치지 않게 먼저 끈다.
        _current.SetActive(false);
        Destroy(_current);
        _current = null;
    }
}
