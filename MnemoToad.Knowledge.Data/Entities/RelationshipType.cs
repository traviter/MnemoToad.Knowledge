namespace MnemoToad.Knowledge.Data.Entities;

/// <summary>
/// The open vocabulary that KnowledgeRelations are classified under (e.g. "capitalOf").
/// </summary>
public class RelationshipType
{
    /// <summary>The RelationshipType's id.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The relationship's name. Unique.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Free-text description of what this relationship represents.</summary>
    public string? Description { get; set; }
}
