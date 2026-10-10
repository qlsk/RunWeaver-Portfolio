using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 실패 연출에서 흩어지는 작은 입자. RunFailureSequence가 실행 중에 만든다.
/// 게임 시간이 멈춰도 움직이도록 실제 시간을 쓴다.
/// </summary>
[DisallowMultipleComponent]
public class FailureMotes : MonoBehaviour
{
    private sealed class Mote
    {
        public SpriteRenderer Renderer;
        public Vector2 Velocity;
        public Color Color;
        public float Age;
        public float Life;
        public float Size;
        public float Gravity;
        public bool IsAlive;
    }

    private const float Drag = 1.1f;
    private const float FadeInTime = 0.08f;

    private readonly List<Mote> _motes = new();
    private Sprite _sprite;
    private Material _alphaMaterial;
    private Material _additiveMaterial;
    private int _sortingLayerId;
    private int _sortingOrder;
    private float _spriteWidth = 1f;

    public void Setup(Sprite sprite, Material alphaMaterial, Material additiveMaterial, int sortingLayerId, int sortingOrder)
    {
        _sprite = sprite;
        _alphaMaterial = alphaMaterial;
        _additiveMaterial = additiveMaterial;
        _sortingLayerId = sortingLayerId;
        _sortingOrder = sortingOrder;
        _spriteWidth = sprite != null ? Mathf.Max(0.0001f, sprite.bounds.size.x) : 1f;
    }

    /// <summary>입자 하나를 뿌린다. gravity가 양수면 떠오르고 음수면 떨어진다. size는 월드 크기</summary>
    public void Emit(Vector2 position, Vector2 velocity, Color color, float size, float life, float gravity, bool isAdditive)
    {
        if (_sprite == null)
        {
            return;
        }

        Mote mote = GetFreeMote();
        mote.Velocity = velocity;
        mote.Color = color;
        mote.Age = 0f;
        mote.Life = Mathf.Max(0.05f, life);
        mote.Size = size;
        mote.Gravity = gravity;
        mote.IsAlive = true;

        SpriteRenderer renderer = mote.Renderer;
        renderer.sharedMaterial = isAdditive ? _additiveMaterial : _alphaMaterial;
        renderer.transform.position = new Vector3(position.x, position.y, 0f);
        renderer.gameObject.SetActive(true);
        Apply(mote);
    }

    private Mote GetFreeMote()
    {
        foreach (Mote mote in _motes)
        {
            if (!mote.IsAlive)
            {
                return mote;
            }
        }

        GameObject go = new("Mote");
        go.transform.SetParent(transform, false);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = _sprite;
        renderer.sortingLayerID = _sortingLayerId;
        renderer.sortingOrder = _sortingOrder;

        Mote created = new() { Renderer = renderer };
        _motes.Add(created);
        return created;
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        foreach (Mote mote in _motes)
        {
            if (!mote.IsAlive)
            {
                continue;
            }

            mote.Age += deltaTime;
            if (mote.Age >= mote.Life)
            {
                mote.IsAlive = false;
                mote.Renderer.gameObject.SetActive(false);
                continue;
            }

            mote.Velocity.y += mote.Gravity * deltaTime;
            mote.Velocity *= Mathf.Max(0f, 1f - Drag * deltaTime);
            mote.Renderer.transform.position += (Vector3)(mote.Velocity * deltaTime);
            Apply(mote);
        }
    }

    private void Apply(Mote mote)
    {
        float t = mote.Age / mote.Life;
        float alpha = Mathf.Clamp01(mote.Age / FadeInTime) * Mathf.Pow(1f - t, 1.2f);
        Color color = mote.Color;
        color.a *= alpha;
        mote.Renderer.color = color;
        mote.Renderer.transform.localScale = Vector3.one * (mote.Size * (1f - 0.4f * t) / _spriteWidth);
    }
}
