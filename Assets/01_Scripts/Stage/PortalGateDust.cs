using UnityEngine;

/// <summary>바위 문 바닥에서 옅게 일어나 퍼지며 사라지는 흙먼지.</summary>
public class PortalGateDust : MonoBehaviour
{
    private const int PuffCount = 14;
    private const float HalfWidth = 1.8f;
    private const float StartSize = 0.25f;
    private const float EndSize = 0.65f;
    private const float MaxAlpha = 0.32f;
    private static readonly Color DustColor = new(0.47f, 0.49f, 0.43f, 1f);
    private static Sprite s_puffSprite;

    private SpriteRenderer[] _puffs;
    private Vector3[] _velocities;
    private float[] _delays;
    private float _life;
    private float _elapsed;

    public static void Spawn(Vector3 ground, int sortingLayerId, int sortingOrder, float life)
    {
        var go = new GameObject("PortalGateDust");
        go.transform.position = ground;
        go.AddComponent<PortalGateDust>().Build(sortingLayerId, sortingOrder, life);
    }

    private void Build(int sortingLayerId, int sortingOrder, float life)
    {
        _life = life;
        _puffs = new SpriteRenderer[PuffCount];
        _velocities = new Vector3[PuffCount];
        _delays = new float[PuffCount];
        Sprite sprite = GetPuffSprite();
        for (int i = 0; i < PuffCount; i++)
        {
            var puff = new GameObject("Puff").AddComponent<SpriteRenderer>();
            puff.transform.SetParent(transform, false);
            float side = Random.Range(-1f, 1f);
            puff.transform.localPosition = new Vector3(side * HalfWidth, Random.Range(-0.05f, 0.2f), 0f);
            puff.sprite = sprite;
            puff.sortingLayerID = sortingLayerId;
            puff.sortingOrder = sortingOrder;
            puff.color = Color.clear;
            _puffs[i] = puff;
            _velocities[i] = new Vector3(side * Random.Range(0.3f, 0.7f), Random.Range(0.15f, 0.45f), 0f);
            _delays[i] = Random.Range(0f, 0.15f);
        }
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        bool alive = false;
        for (int i = 0; i < _puffs.Length; i++)
        {
            float t = (_elapsed - _delays[i]) / _life;
            if (t < 0f)
            {
                alive = true;
                continue;
            }

            if (t >= 1f)
            {
                _puffs[i].color = Color.clear;
                continue;
            }

            alive = true;
            Transform puff = _puffs[i].transform;
            puff.localPosition += _velocities[i] * (Time.deltaTime * (1f - t));
            puff.localScale = Vector3.one * Mathf.Lerp(StartSize, EndSize, t);
            Color color = DustColor;
            color.a = MaxAlpha * Mathf.Sin(t * Mathf.PI);
            _puffs[i].color = color;
        }

        if (!alive)
        {
            Destroy(gameObject);
        }
    }

    private static Sprite GetPuffSprite()
    {
        if (s_puffSprite != null)
        {
            return s_puffSprite;
        }

        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }

        texture.Apply();
        s_puffSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return s_puffSprite;
    }
}
