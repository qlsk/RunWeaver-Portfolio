using UnityEngine;
using UnityEngine.Rendering;

/// 고정 크기의 암석을 지면 마스크 안으로 이동시키며 드러낸다.
public class RockEruptionVisual : MonoBehaviour
{
    private SpriteRenderer _rock;
    private SpriteRenderer[] _dust;
    private SpriteRenderer[] _chips;
    private Vector3[] _dustOrigins;
    private Vector3 _center;
    private float _height;
    private Color _rockColor;
    private Sprite _maskSprite, _dustSprite;
    private Texture2D _dustTexture;

    public void Initialize(SpriteRenderer rock, Vector3 center, float height)
    {
        _rock = rock;
        _center = center;
        _height = Mathf.Max(0.01f, height);
        _rockColor = rock.color;
        var group = gameObject.AddComponent<SortingGroup>();
        group.sortingLayerName = rock.sortingLayerName;
        group.sortingOrder = rock.sortingOrder;
        rock.transform.SetParent(transform, true);
        rock.sortingOrder = 0;
        rock.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        // 초기 크기와 비율을 고정하고 이후에는 위치와 투명도만 변경한다.
        float scale = _height / Mathf.Max(0.01f, rock.sprite.bounds.size.y);
        rock.transform.localScale = Vector3.one * scale;
        float width = rock.sprite.bounds.size.x * scale;
        var mask = new GameObject("GroundMask").AddComponent<SpriteMask>();
        mask.transform.SetParent(transform, false);
        _maskSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);
        mask.sprite = _maskSprite;
        mask.transform.position = center + Vector3.up * (_height * 0.5f);
        mask.transform.localScale = new Vector3(width + 0.2f, _height * 2f, 1f);
        mask.alphaCutoff = 0.5f;
        CreateDust(width);
        Animate(0f, 0.15f, 0.4f, 0.25f);
    }

    private void CreateDust(float width)
    {
        _dustTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        var pixels = new Color[256];
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f)) / 7.5f;
                pixels[y * 16 + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - distance) * 2f));
            }
        _dustTexture.SetPixels(pixels);
        _dustTexture.Apply();
        _dustSprite = Sprite.Create(_dustTexture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        _dust = new SpriteRenderer[6];
        _dustOrigins = new Vector3[6];
        _chips = new SpriteRenderer[4];
        for (int i = 0; i < _chips.Length; i++)
        {
            var chip = new GameObject("StoneChip").AddComponent<SpriteRenderer>();
            chip.transform.SetParent(transform, false);
            chip.sprite = _maskSprite;
            chip.sortingLayerName = _rock.sortingLayerName;
            chip.sortingOrder = 2;
            chip.transform.localScale = Vector3.one * (0.06f + i * 0.02f);
            _chips[i] = chip;
        }
        for (int i = 0; i < _dust.Length; i++)
        {
            var puff = new GameObject("GroundDust").AddComponent<SpriteRenderer>();
            puff.transform.SetParent(transform, false);
            puff.sprite = _dustSprite;
            puff.sortingLayerName = _rock.sortingLayerName;
            puff.sortingOrder = 1;
            _dustOrigins[i] = _center + new Vector3((i / 5f - 0.5f) * width, -_height * 0.5f, 0f);
            _dust[i] = puff;
        }
    }

    public void Animate(float elapsed, float rise, float hold, float fade)
    {
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, rise));
        float eased = 1f - (1f - t) * (1f - t);
        Vector3 center = _center - Vector3.up * (_height * (1f - eased));
        _rock.transform.position = center - _rock.transform.TransformVector(_rock.sprite.bounds.center);
        _rock.enabled = true;
        float disappear = Mathf.Clamp01((elapsed - rise - hold) / Mathf.Max(0.01f, fade));
        Color color = _rockColor;
        color.a *= 1f - disappear;
        _rock.color = color;
        float chipTime = Mathf.Clamp01(elapsed / 0.4f);
        for (int i = 0; i < _chips.Length; i++)
        {
            _chips[i].enabled = elapsed < 0.4f;
            float direction = (i - 1.5f) * 0.6f;
            _chips[i].transform.position = _center + new Vector3(direction * chipTime, -_height * 0.5f + 1.6f * chipTime * (1f - chipTime), 0f);
            _chips[i].transform.rotation = Quaternion.Euler(0f, 0f, i * 40f + chipTime * 160f);
            _chips[i].color = new Color(0.3f, 0.28f, 0.24f, 1f - chipTime);
        }
        for (int i = 0; i < _dust.Length; i++)
        {
            float puffTime = elapsed < rise + hold ? elapsed / 0.35f : disappear;
            float p = Mathf.Clamp01(puffTime);
            _dust[i].enabled = puffTime <= 1f;
            float side = i < 3 ? -1f : 1f;
            _dust[i].transform.position = _dustOrigins[i] + new Vector3(side * p * 0.35f, p * 0.15f, 0);
            _dust[i].transform.localScale = new Vector3(0.3f + p * 0.5f, 0.18f + p * 0.3f, 1);
            _dust[i].color = new Color(0.4f, 0.36f, 0.28f, (1f - p) * 0.65f);
        }
    }

    private void OnDestroy()
    {
        if (_maskSprite != null) Destroy(_maskSprite);
        if (_dustSprite != null) Destroy(_dustSprite);
        if (_dustTexture != null) Destroy(_dustTexture);
    }
}

