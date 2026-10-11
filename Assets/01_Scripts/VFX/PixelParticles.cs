using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미션 오브젝트 연출용 아주 단순한 픽셀 알갱이. (빛 알갱이, 파편, 불티)
/// 알갱이마다 SpriteRenderer 하나를 쓰고, 다 쓴 것은 다시 쓴다. 월드 좌표로 움직인다.
/// - Emit : 위치, 속도, 중력, 수명, 색, 크기(월드 단위)를 받아 하나 띄운다. 끝으로 갈수록 흐려진다.
/// - floorY가 있으면 그 높이에 닿을 때 멈춘다. (파편이 바닥에 떨어진 듯)
/// - DestroyWhenEmpty : 남은 알갱이가 없으면 스스로 사라진다. (주인이 파괴된 뒤 남기는 파편)
/// 게임 시간으로 움직인다.
/// </summary>
public class PixelParticles : MonoBehaviour
{
    private sealed class Particle
    {
        public SpriteRenderer Renderer;
        public Vector3 Velocity;
        public float Gravity;
        public float Age;
        public float Life;
        public Color Color;
        public float FloorY;
        public bool HasFloor;
        public bool FadeIn;
        public bool Alive;
    }

    private readonly List<Particle> _particles = new();
    private int _sortingLayerId;
    private int _sortingOrder;

    public bool DestroyWhenEmpty { get; set; }

    /// <summary>독립 오브젝트로 만든다. (주인 오브젝트가 사라져도 남는다)</summary>
    public static PixelParticles CreateDetached(string objectName, int sortingLayerId, int sortingOrder)
    {
        var particles = new GameObject(objectName).AddComponent<PixelParticles>();
        particles.SetSorting(sortingLayerId, sortingOrder);
        particles.DestroyWhenEmpty = true;
        return particles;
    }

    public void SetSorting(int sortingLayerId, int sortingOrder)
    {
        _sortingLayerId = sortingLayerId;
        _sortingOrder = sortingOrder;
    }

    public void Emit(Vector3 position, Vector3 velocity, float gravity, float life, Color color, float size,
        float? floorY = null, bool fadeIn = false)
    {
        Particle particle = GetFree();
        particle.Renderer.transform.position = position;
        particle.Renderer.transform.localScale = Vector3.one * size;
        particle.Velocity = velocity;
        particle.Gravity = gravity;
        particle.Age = 0f;
        particle.Life = Mathf.Max(0.05f, life);
        particle.Color = color;
        particle.HasFloor = floorY.HasValue;
        particle.FloorY = floorY ?? 0f;
        particle.FadeIn = fadeIn;
        particle.Alive = true;
        particle.Renderer.enabled = true;
        Apply(particle, 0f);
    }

    private Particle GetFree()
    {
        foreach (Particle particle in _particles)
        {
            if (!particle.Alive)
            {
                return particle;
            }
        }

        var go = new GameObject("Pixel");
        go.transform.SetParent(transform, true);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = MissionFxSprites.Pixel;
        Material material = MissionFxSprites.UnlitMaterial;
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }

        var created = new Particle { Renderer = renderer };
        _particles.Add(created);
        return created;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        bool anyAlive = false;
        foreach (Particle particle in _particles)
        {
            if (!particle.Alive)
            {
                continue;
            }

            particle.Age += dt;
            if (particle.Age >= particle.Life)
            {
                particle.Alive = false;
                particle.Renderer.enabled = false;
                continue;
            }

            anyAlive = true;
            Transform target = particle.Renderer.transform;
            particle.Velocity.y -= particle.Gravity * dt;
            Vector3 position = target.position + particle.Velocity * dt;
            if (particle.HasFloor && position.y <= particle.FloorY && particle.Velocity.y < 0f)
            {
                // 바닥에 닿으면 튀지 않고 멈춘다.
                position.y = particle.FloorY;
                particle.Velocity = Vector3.zero;
                particle.Gravity = 0f;
            }

            target.position = position;
            Apply(particle, particle.Age / particle.Life);
        }

        if (!anyAlive && DestroyWhenEmpty)
        {
            Destroy(gameObject);
        }
    }

    private void Apply(Particle particle, float t)
    {
        Color color = particle.Color;
        float fade = particle.FadeIn ? Mathf.Sin(t * Mathf.PI) : 1f - t * t;
        color.a *= fade;
        particle.Renderer.color = color;
        particle.Renderer.sortingLayerID = _sortingLayerId;
        particle.Renderer.sortingOrder = _sortingOrder;
    }
}
