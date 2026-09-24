public class SkillNodeParameterData
{
    private string _nodeId;
    private int _level;
    private ParameterType _parameterType;
    private float _value;

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
