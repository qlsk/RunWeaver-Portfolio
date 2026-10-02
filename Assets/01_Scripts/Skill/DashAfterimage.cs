using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class DashAfterimage : MonoBehaviour
{
    [Header("Dash 잔상 (Feel)")]
    [SerializeField] private bool _afterimagesEnabled = true;
    [SerializeField, Min(0.01f)] private float _afterimageInterval = 0.035f;
    [SerializeField, Min(0.01f)] private float _afterimageLifetime = 0.2f;
    [SerializeField] private Color _afterimageColor = new Color(0.4f, 0.8f, 1f, 0.45f);
    [Tooltip("비워두면 플레이어와 자식의 SpriteRenderer 사용")]
    [SerializeField] private SpriteRenderer[] _afterimageSources;
    private readonly List<Afterimage> _afterimages = new List<Afterimage>();
    private float _nextAfterimageTime;
    private bool _isEmitting;

    private void Awake()
    {
        if (_afterimageSources == null || _afterimageSources.Length == 0)
        {
            _afterimageSources = GetComponentsInChildren<SpriteRenderer>(true);
        }
    }

    public void BeginEmission()
    {
        if (!isActiveAndEnabled) return;
        _isEmitting = true;
        _nextAfterimageTime = Time.time;
    }

    public void StopEmission()
    {
        // Existing snapshots finish fading after the dash ends.
        _isEmitting = false;
    }

    public void Clear()
    {
        StopEmission();
        foreach (Afterimage afterimage in _afterimages)
        {
            if (afterimage.Root == null) continue;
            afterimage.Feedbacks.StopFeedbacks();
            afterimage.Root.SetActive(false);
        }
    }

    private void OnDisable()
    {
        Clear();
    }

    private void LateUpdate()
    {
        foreach (Afterimage afterimage in _afterimages)
        {
            if (afterimage.Root != null && afterimage.Root.activeSelf && Time.time >= afterimage.ExpiresAt)
            {
                afterimage.Feedbacks.StopFeedbacks();
                afterimage.Root.SetActive(false);
            }
        }

        if (!_isEmitting || !_afterimagesEnabled || Time.time < _nextAfterimageTime) return;
        _nextAfterimageTime = Time.time + Mathf.Max(0.01f, _afterimageInterval);

        Afterimage available = _afterimages.Find(item => item.Root != null && !item.Root.activeSelf);
        if (available == null)
        {
            available = CreateAfterimage();
            _afterimages.Add(available);
        }

        available.Root.SetActive(true);
        for (int i = 0; i < _afterimageSources.Length; i++)
        {
            SpriteRenderer source = _afterimageSources[i];
            SpriteRenderer copy = available.Sprites[i];
            bool visible = source != null && source.enabled && source.gameObject.activeInHierarchy && source.sprite != null;
            available.Fades[i].Active = visible;
            copy.gameObject.SetActive(visible);
            if (!visible) continue;

            copy.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            copy.transform.localScale = source.transform.lossyScale;
            copy.gameObject.layer = source.gameObject.layer;
            copy.sprite = source.sprite;
            copy.sharedMaterial = source.sharedMaterial;
            copy.flipX = source.flipX;
            copy.flipY = source.flipY;
            copy.drawMode = source.drawMode;
            copy.size = source.size;
            copy.spriteSortPoint = source.spriteSortPoint;
            copy.sortingLayerID = source.sortingLayerID;
            copy.sortingOrder = source.sortingOrder - 10;
            copy.color = source.color * _afterimageColor;
            available.Fades[i].Duration = Mathf.Max(0.01f, _afterimageLifetime);
        }

        available.ExpiresAt = Time.time + Mathf.Max(0.01f, _afterimageLifetime);
        available.Feedbacks.PlayFeedbacks();
    }

    private Afterimage CreateAfterimage()
    {
        GameObject root = new GameObject(name + " Dash Afterimage");
        root.SetActive(false);
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        MMF_Player feedbacks = root.AddComponent<MMF_Player>();
        feedbacks.InitializationMode = MMFeedbacks.InitializationModes.Script;
        var afterimage = new Afterimage
        {
            Root = root,
            Feedbacks = feedbacks,
            Sprites = new SpriteRenderer[_afterimageSources.Length],
            Fades = new MMF_SpriteRendererAlpha[_afterimageSources.Length]
        };

        for (int i = 0; i < _afterimageSources.Length; i++)
        {
            GameObject spriteObject = new GameObject("Sprite " + i);
            spriteObject.transform.SetParent(root.transform, false);
            SpriteRenderer sprite = spriteObject.AddComponent<SpriteRenderer>();
            var fade = new MMF_SpriteRendererAlpha
            {
                BoundSpriteRenderer = sprite,
                Mode = MMF_SpriteRendererAlpha.Modes.ToDestinationAlpha,
                InitialAlphaMode = MMF_SpriteRendererAlpha.InitialAlphaModes.InitialAlphaOnPlay,
                ToDestinationAlpha = 0f,
                Duration = Mathf.Max(0.01f, _afterimageLifetime)
            };
            afterimage.Sprites[i] = sprite;
            afterimage.Fades[i] = fade;
            feedbacks.AddFeedback(fade);
        }

        root.SetActive(true);
        feedbacks.Initialization();
        return afterimage;
    }

    private void OnDestroy()
    {
        foreach (Afterimage afterimage in _afterimages)
        {
            if (afterimage.Root != null) Destroy(afterimage.Root);
        }
    }

    private sealed class Afterimage
    {
        public GameObject Root;
        public MMF_Player Feedbacks;
        public SpriteRenderer[] Sprites;
        public MMF_SpriteRendererAlpha[] Fades;
        public float ExpiresAt;
    }
}
