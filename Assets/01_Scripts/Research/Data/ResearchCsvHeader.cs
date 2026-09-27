public class ResearchNodeCsvHeader
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public string NodeType { get; set; }
    public string RootId { get; set; }
}

public class ResearchPrerequisiteCsvHeader
{
    public string ResearchId { get; set; }
    public string PrerequisiteResearchId { get; set; }
}

public class ResearchCostCsvHeader
{
    public string ResearchId { get; set; }
    public string ResourceId { get; set; }
    public int Amount { get; set; }
}

public class ResearchEffectCsvHeader
{
    public string ResearchId { get; set; }
    public string EffectType { get; set; }
    public string TargetId { get; set; }
    public float? Value { get; set; }
}
