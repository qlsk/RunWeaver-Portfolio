using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>상황 안내 카드(새 기록)를 한 장씩 차례로 띄운다. 일정 시간이 지나면 저절로 사라진다.</summary>
public class TutorialRecordCardView : MonoBehaviour
{
    private const string HeaderPrefix = "새 기록 / ";
    private const string HeaderLabelName = "HeaderLabel";
    private const string TitleName = "Title";
    private const string BodyName = "Body";
    private const string FooterName = "Footer";
    private const string TimerFillName = "Fill";

    [Tooltip("기록 카드를 놓는 전체 화면 RectTransform")]
    [SerializeField] private RectTransform _root;
    [Tooltip("화면 오른쪽 위 모서리 기준 카드 위치")]
    [SerializeField] private Vector2 _cardOffset = new(-24f, -110f);

    private readonly Queue<RecordCardSO> _queue = new();
    private Coroutine _playRoutine;
    private GameObject _current;

    public void Enqueue(RecordCardSO card)
    {
        _queue.Enqueue(card);

        if (_playRoutine == null)
            _playRoutine = StartCoroutine(PlayQueue());
    }

    private void OnDisable()
    {
        // 컴포넌트를 끄는 것만으로는 코루틴이 멈추지 않으므로 직접 멈춘다.
        if (_playRoutine != null)
            StopCoroutine(_playRoutine);

        _queue.Clear();
        _playRoutine = null;

        if (_current != null)
            Destroy(_current);

        _current = null;
    }

    private IEnumerator PlayQueue()
    {
        while (_queue.Count > 0)
            yield return ShowCard(_queue.Dequeue());

        _playRoutine = null;
    }

    private IEnumerator ShowCard(RecordCardSO card)
    {
        if (card.CardPrefab == null)
        {
            Debug.LogError($"[TutorialRecordCardView] 기록 카드 '{card.Id}'의 프리팹이 지정되지 않았습니다.");
            yield break;
        }

        _current = Instantiate(card.CardPrefab, _root);
        PlaceCard((RectTransform)_current.transform);

        SetText(HeaderLabelName, HeaderPrefix + card.Category);
        SetText(TitleName, card.Title);
        SetText(BodyName, card.Body);
        SetText(FooterName, card.Footer);

        Transform fill = FindDeep(_current.transform, TimerFillName);
        float elapsed = 0f;
        while (elapsed < card.DisplaySeconds)
        {
            // 기록 카드는 룬북 등으로 게임이 멈춰 있어도 시간이 흘러야 하므로 실제 시간을 쓴다.
            elapsed += Time.unscaledDeltaTime;
            if (fill != null)
                fill.localScale = new Vector3(1f - Mathf.Clamp01(elapsed / card.DisplaySeconds), 1f, 1f);

            yield return null;
        }

        Destroy(_current);
        _current = null;
    }

    private void PlaceCard(RectTransform card)
    {
        card.anchorMin = card.anchorMax = Vector2.one;
        card.pivot = Vector2.one;
        card.anchoredPosition = _cardOffset;
    }

    private void SetText(string childName, string text)
    {
        Transform child = FindDeep(_current.transform, childName);
        if (child == null)
            return;

        // 문구가 없는 칸(예: 꼬리말)은 숨긴다.
        child.gameObject.SetActive(!string.IsNullOrEmpty(text));
        if (child.TryGetComponent(out TMP_Text label))
            label.text = text;
    }

    private static Transform FindDeep(Transform parent, string childName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }

        return null;
    }
}
