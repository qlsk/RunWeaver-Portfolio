using System.Collections;
using UnityEngine;

/// <summary>튜토리얼을 끝까지 마치면(건너뛰기 제외) 다음 층 도착 시 완료 배너를 잠깐 보여준다.</summary>
public class TutorialCompletionBannerView : MonoBehaviour
{
    private const string AnnotationLabelName = "Label";

    [SerializeField] private TutorialController _controller;
    [Tooltip("완료 배너 프리팹(TutorialCompletionBanner)")]
    [SerializeField] private GameObject _bannerPrefab;
    [Tooltip("배너를 놓는 전체 화면 RectTransform")]
    [SerializeField] private RectTransform _root;
    [SerializeField, Min(0.5f)] private float _displaySeconds = 3f;

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

        // 컴포넌트를 끄는 것만으로는 코루틴이 멈추지 않으므로 직접 멈춘다.
        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = null;

        if (_current != null)
            Destroy(_current);

        _current = null;
    }

    private void HandleFinished()
    {
        if (_bannerPrefab == null)
        {
            Debug.LogError("[TutorialCompletionBannerView] 완료 배너 프리팹이 지정되지 않았습니다.");
            return;
        }

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        if (_current != null)
            Destroy(_current);

        _current = Instantiate(_bannerPrefab, _root);

        // 프리팹의 '다음 층 도착 시, 3초 표시' 라벨은 시안 설명이라 화면에 보이지 않게 한다.
        Transform annotation = _current.transform.Find(AnnotationLabelName);
        if (annotation != null)
            annotation.gameObject.SetActive(false);

        // 배너는 층 전환 직후에 뜨므로 게임이 멈춰 있어도 흘러야 해 실제 시간을 쓴다.
        yield return new WaitForSecondsRealtime(_displaySeconds);

        Destroy(_current);
        _current = null;
        _showRoutine = null;
    }
}
