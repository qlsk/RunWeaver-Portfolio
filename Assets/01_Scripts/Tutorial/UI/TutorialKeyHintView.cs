using TMPro;
using UnityEngine;

public class TutorialKeyHintView : MonoBehaviour
{
    private const string LabelName = "Label";

    [SerializeField] private RectTransform _hintRoot;

    private GameObject _current;

    public void Show(TutorialStepSO step)
    {
        Hide();
        _current = Instantiate(step.OverlayPrefab, _hintRoot);
        Transform label = _current.transform.Find(LabelName);
        if (label.TryGetComponent(out TMP_Text labelText))
            labelText.text = step.OverlayText;
    }

    private void Update()
    {
        // 일시정지 메뉴나 룬북이 열려 있으면 그 위에 겹치지 않게 숨긴다.
        if (_current == null)
            return;

        bool isVisible = !TutorialOverlayBlocker.IsBlocked;
        if (_current.activeSelf != isVisible)
            _current.SetActive(isVisible);
    }

    public void Hide()
    {
        if (_current == null)
            return;
        _current.SetActive(false);
        Destroy(_current);
        _current = null;
    }
}