using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EllipseAttackArea : System.IDisposable
{
    private const int Segments = 64;
    private readonly PolygonCollider2D _query;
    private readonly Vector2[] _points = new Vector2[Segments];

    public EllipseAttackArea(GameObject owner)
    {
        var root = new GameObject("Ellipse Attack Query");
        root.SetActive(false);
        SceneManager.MoveGameObjectToScene(root, owner.scene);
        _query = root.AddComponent<PolygonCollider2D>();
        _query.isTrigger = true;
        _query.enabled = false;
        root.SetActive(true);
    }

    public void Overlap(Vector2 center, float radius, float heightRatio, List<Collider2D> results)
    {
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            _points[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * heightRatio);
        }
        _query.SetPath(0, _points);
        _query.transform.position = center;
        results.Clear();
        // Enable only during this synchronous query, never during a physics simulation step.
        _query.enabled = true;
        try
        {
            Physics2D.SyncTransforms();
            _query.Overlap(new ContactFilter2D { useTriggers = true }, results);
        }
        finally
        {
            _query.enabled = false;
        }
    }

    public static void DrawGizmo(Vector3 center, float radius, float heightRatio)
    {
        Vector3 previous = center + Vector3.right * radius;
        for (int i = 1; i <= Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * heightRatio, 0f);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }

    public void Dispose()
    {
        if (_query != null) Object.Destroy(_query.gameObject);
    }
}
