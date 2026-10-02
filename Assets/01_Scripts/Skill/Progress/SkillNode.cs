using System;
using UnityEngine;

public class SkillNode
{
    private string _nodeId;
    private int _maxLevel;
    private int _level;

    public string NodeId => _nodeId;
    public int Level => _level;
    public bool IsActivated => _level > 0;
    public bool IsCompleted => _level == _maxLevel;

    public SkillNode(string nodeId, int maxLevel)
    {
        _nodeId = nodeId;
        _maxLevel = maxLevel;
        _level = 0;
    }

    public bool TryIncreaseLevel()
    {
        if (_level >= _maxLevel) return false;
        _level++;
        return true;
    }
}
