using System.Collections;
using UnityEngine;

public class TutorialCompletionBannerView : MonoBehaviour
{
    private const string AnnotationLabelName = "Label";

    [SerializeField] private TutorialController _controller;
    [SerializeField] private GameObject _bannerPrefab;
    [SerializeField] private RectTransform _root;
    [SerializeField][Min(0.5f)] private float _displaySeconds = 3f;

    private Coroutine _showRoutine;
    private GameObject _current;

    private void OnEnable()
    {
        if (_controller != null)
            _controller.Finished += HandleFinished;
    }

    private void OnDisable()
    {
        if (_controller != null)
            _controller.Finished -= HandleFinished;

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = null;

        if (_current != null)
            Destroy(_current);

        _current = null;
    }

    private void HandleFinished()
    {
        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        if (_current != null)
            Destroy(_current);

        _current = Instantiate(_bannerPrefab, _root);

        Transform annotation = _current.transform.Find(AnnotationLabelName);
        if (annotation != null)
            annotation.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(_displaySeconds);

        Destroy(_current);
        _current = null;
        _showRoutine = null;
    }
}