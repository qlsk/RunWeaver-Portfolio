using System.Collections.Generic;
using UnityEngine;

// Visual geometry only; EnemyShockwaveAttack owns the circular damage band.
public sealed class ShockwaveRingVisual : MonoBehaviour
{
    private const int Segments = 192;
    private readonly List<Vector3> _vertices = new List<Vector3>(3000);
    private readonly List<Color> _colors = new List<Color>(3000);
    private readonly List<int> _triangles = new List<int>(10000);
    private Mesh _mesh;
    private Material _material;

    private void Awake()
    {
        _mesh = new Mesh { name = "Ground shockwave" };
        _mesh.MarkDynamic();
        gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
        var renderer = gameObject.AddComponent<MeshRenderer>();
        _material = new Material(Shader.Find("Sprites/Default"));
        renderer.sharedMaterial = _material;
        renderer.sortingOrder = 12;
    }

    public void Draw(float radius, float width, Color energy, float fade)
    {
        _vertices.Clear();
        _colors.Clear();
        _triangles.Clear();
        Color earth = new Color(0.42f, 0.46f, 0.32f, energy.a);
        // Radial alpha falloff gives the crest a trailing wake instead of a solid outline.
        Band(radius, -width * 2.4f, -width * 0.35f, energy, 0f, 0.22f, fade);
        Band(radius, -width * 0.35f, 0f, energy, 0.22f, 0.85f, fade);
        Band(radius, 0f, width * 0.35f, energy, 0.85f, 0f, fade);
        Band(radius, width * 0.25f, width * 1.3f, earth, 0.3f, 0f, fade);

        // Stable angular seeds keep fragments coherent as the wave expands.
        for (int i = 0; i < 96; i++)
        {
            float seed = Noise(i * 7 + 3);
            float angle = (i + seed) * Mathf.PI * 2f / 96;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 tangent = new Vector2(-direction.y, direction.x);
            float pulse = Mathf.Sin(Mathf.Clamp01(Mathf.Repeat(radius * 0.35f + seed, 1f)) * Mathf.PI);
            Vector2 center = direction * (radius + width * (seed * 2f - 0.5f));
            float size = Mathf.Min(radius * 0.12f, (0.025f + seed * 0.065f) * pulse);
            Color dust = Color.Lerp(earth, energy, seed * 0.3f);
            dust.a *= fade * pulse * 0.65f;
            int start = _vertices.Count;
            Vertex(center + direction * size, dust);
            Vertex(center + tangent * size * 0.65f, dust);
            Vertex(center - direction * size, dust);
            Vertex(center - tangent * size * 0.65f, dust);
            Quad(start, start + 1, start + 2, start + 3);
        }
        _mesh.Clear();
        _mesh.SetVertices(_vertices);
        _mesh.SetColors(_colors);
        _mesh.SetTriangles(_triangles, 0);
        _mesh.RecalculateBounds();
    }

    private void Band(float radius, float inner, float outer, Color color, float innerAlpha, float outerAlpha, float fade)
    {
        int start = _vertices.Count;
        for (int i = 0; i <= Segments; i++)
        {
            int sample = i % Segments;
            float angle = sample * Mathf.PI * 2f / Segments;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float noise = Noise(sample) - 0.5f;
            float irregularity = noise * Mathf.Min(radius * 0.08f, 0.1f);
            Color tint = color;
            float opacity = fade * (0.65f + Noise(sample + 37) * 0.35f);
            tint.a = color.a * innerAlpha * opacity;
            Vertex(direction * Mathf.Max(0f, radius + inner + irregularity), tint);
            tint.a = color.a * outerAlpha * opacity;
            Vertex(direction * Mathf.Max(0f, radius + outer + irregularity), tint);
            if (i > 0) Quad(start + (i - 1) * 2, start + (i - 1) * 2 + 1, start + i * 2 + 1, start + i * 2);
        }
    }

    private static float Noise(int seed) => Mathf.Repeat(Mathf.Sin(seed * 127.1f + 19.7f) * 43758.5453f, 1f);
    private void Vertex(Vector2 point, Color color) { _vertices.Add(point); _colors.Add(color); }
    private void Quad(int a, int b, int c, int d)
    {
        _triangles.Add(a); _triangles.Add(b); _triangles.Add(c);
        _triangles.Add(a); _triangles.Add(c); _triangles.Add(d);
    }
    private void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
        if (_material != null) Destroy(_material);
    }
}
