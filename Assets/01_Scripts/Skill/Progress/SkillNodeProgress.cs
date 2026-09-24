using UnityEngine;

public class SkillNodeProgress
{
    private string _nodeId;
    private int _level;

    public string NodeId => _nodeId;
    public int Level => _level;
    public bool IsActivated => _level > 0;

    public SkillNodeProgress(string nodeId)
    {
        _nodeId = nodeId;
        _level = 0;
    }
}
