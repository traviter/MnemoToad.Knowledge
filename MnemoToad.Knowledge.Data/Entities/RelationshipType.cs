namespace MnemoToad.Knowledge.Data.Entities;

/// <summary>
/// The open vocabulary that KnowledgeRelations are classified under (e.g. "capitalOf", inverse
/// "hasCapital").
/// </summary>
public class RelationshipType
{
    /// <summary>The RelationshipType's id.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The relationship's forward name. Unique.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The relationship's reverse name, for traversing the Path DSL backward.</summary>
    public string? InverseName { get; set; }

    /// <summary>Free-text description of what this relationship represents.</summary>
    public string? Description { get; set; }
}
