using System;
using UnityEngine;

public class EnemyTargetDetector : MonoBehaviour
{
    private Transform _target;
    public Transform Target => _target;

    private void Awake()
    {
        if (_target != null)
        {
            return;
        }

        var player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            _target = player.transform;
        }
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }
}
