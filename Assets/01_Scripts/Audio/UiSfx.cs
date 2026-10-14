using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 전역 UI 클릭·호버음 자동 장착. 남의 파일을 건드리지 않는다.
/// 씬 로드 + 주기 스캔으로 클릭 처리체가 있는 오브젝트에 소리 컴포넌트를 붙인다.
/// 자기가 직접 소리를 가진 곳(보상UI 전체, 주력원소 구슬 클릭)은 제외한다.
/// </summary>
public sealed class UiSfx : MonoBehaviour
{
    private const float RescanInterval = 1.5f;
    private const float ClickVolume = 0.5f;
    private const float HoverVolume = 0.25f;
    private const float HoverDebounce = 0.08f;

    private static float _lastHoverTime = float.NegativeInfinity;
    private float _rescanTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        GameObject go = new GameObject("UiSfx");
        DontDestroyOnLoad(go);
        go.AddComponent<UiSfx>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Rescan();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Rescan();
    }

    private void Update()
    {
        _rescanTimer += Time.unscaledDeltaTime;
        if (_rescanTimer >= RescanInterval)
        {
            _rescanTimer = 0f;
            Rescan();
        }
    }

    private void Rescan()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (Transform transform in transforms)
        {
            if (transform == null || !transform.gameObject.activeInHierarchy)
            {
                continue;
            }

            GameObject go = transform.gameObject;
            if (go.GetComponentInParent<RewardSelectUI>() != null)
            {
                continue;
            }

            if (go.GetComponent<IPointerClickHandler>() != null && go.GetComponent<UiClickSfx>() == null)
            {
                if (go.GetComponent<MainElementOrbView>() == null)
                {
                    go.AddComponent<UiClickSfx>();
                }
            }

            if (go.GetComponent<IPointerEnterHandler>() != null && go.GetComponent<UiHoverSfx>() == null)
            {
                go.AddComponent<UiHoverSfx>();
            }
        }
    }

    private sealed class UiClickSfx : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            Sfx.Play(Sfx.UiClick, ClickVolume);
        }
    }

    private sealed class UiHoverSfx : MonoBehaviour, IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Time.unscaledTime - _lastHoverTime < HoverDebounce)
            {
                return;
            }

            _lastHoverTime = Time.unscaledTime;
            Sfx.Play(Sfx.UiHover, HoverVolume);
        }
    }
}
