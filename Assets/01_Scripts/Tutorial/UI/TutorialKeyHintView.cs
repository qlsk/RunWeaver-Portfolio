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

    public void Hide()
    {
        if (_current == null)
            return;
        _current.SetActive(false);
        Destroy(_current);
        _current = null;
    }
}