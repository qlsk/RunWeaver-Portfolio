using UnityEngine;

/// <summary>
/// 정화된 적에서 흩어지는 빛 입자. PurifyDeathEffect가 코드로 만든다.
/// 적이 사라져도 끝까지 떠오른 뒤 스스로 없어진다.
/// </summary>
public class PurifyMotes : MonoBehaviour
{
    private struct Mote
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Vector3 Start;
        public Vector2 Velocity;
        public float Life;
        public float Size;
    }

    private Mote[] _motes;
    private Color _color;
    private float _elapsed;
    private float _maxLife;

    public void Play(Sprite sprite, Color color, Bounds area, int sortingLayer, int sortingOrder, int count)
    {
        _color = color;
        _motes = new Mote[count];
        float spriteSize = Mathf.Max(0.0001f, sprite.bounds.size.x);

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Mote");
            go.transform.SetParent(transform, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = sortingOrder;

            Vector3 start = new(
                Random.Range(area.min.x, area.max.x),
                Random.Range(area.center.y, area.max.y),
                0f);
            float size = Random.Range(0.08f, 0.18f) / spriteSize;

            _motes[i] = new Mote
            {
                Transform = go.transform,
                Renderer = renderer,
                Start = start,
                Velocity = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.5f, 1.2f)),
                Life = Random.Range(0.5f, 1.1f),
                Size = size
            };
            _maxLife = Mathf.Max(_maxLife, _motes[i].Life);
            go.transform.position = start;
            go.transform.localScale = Vector3.one * size;
            renderer.color = new Color(color.r, color.g, color.b, 0f);
        }
    }

    private void Update()
    {
        if (_motes == null)
        {
            return;
        }

        _elapsed += Time.deltaTime;
        foreach (Mote mote in _motes)
        {
            float t = Mathf.Clamp01(_elapsed / mote.Life);
            mote.Transform.position = mote.Start + (Vector3)(mote.Velocity * _elapsed);
            Color c = _color;
            c.a = Mathf.Sin(t * Mathf.PI);
            mote.Renderer.color = c;
        }

        if (_elapsed >= _maxLife)
        {
            Destroy(gameObject);
        }
    }
}
