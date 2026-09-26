using UnityEngine;

[System.Serializable]
public class SkillNodeParameterData
{
    [SerializeField] private string _nodeId;
    [SerializeField] private int _level;
    [SerializeField] private ParameterType _parameterType;
    [SerializeField] private float _value;

    public string NodeId => _nodeId;
    public int Level => _level;
    public ParameterType ParameterType => _parameterType;
    public float Value => _value;

    public SkillNodeParameterData(string nodeId, int level, ParameterType parameterType, float value)
    {
        _nodeId = nodeId;
        _level = level;
        _parameterType = parameterType;
        _value = value;
    }
}
