using System;
using UnityEngine;

public class MoveCondition : ITutorialCondition
{
    private Transform _player;
    private Vector3 _lastPosition;
    private float _movedDistance;
    private float _targetDistance;
    private bool _isActive;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        _movedDistance = 0f;

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject == null)
        {
            Debug.LogError("Player object not found.");
            return;
        }

        _player = playerObject.transform;
        _lastPosition = _player.position;

        _targetDistance = step.ConditionValue;
        _isActive = true;
    }

    public void Tick()
    {
        if (!_isActive)
            return;

        Vector3 currentPosition = _player.position;
        float deltaDistance = Vector2.Distance(currentPosition, _lastPosition);
        _movedDistance += deltaDistance;
        _lastPosition = currentPosition;

        if (_movedDistance < _targetDistance)
            return;

        _isActive = false;
        Completed?.Invoke();
    }

    public void End()
    {
        _isActive = false;
        _player = null;
        _movedDistance = 0f;
    }
}