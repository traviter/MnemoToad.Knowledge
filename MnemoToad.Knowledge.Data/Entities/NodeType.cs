namespace MnemoToad.Knowledge.Data.Entities;

/// <summary>
/// The open vocabulary that KnowledgeNodes are classified under (e.g. "Planet", "Country").
/// </summary>
public class NodeType
{
    /// <summary>The NodeType's id.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The NodeType's display name. Unique.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Free-text description of what this NodeType represents.</summary>
    public string? Description { get; set; }
}
