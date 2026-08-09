namespace MnemoToad.Knowledge.Data.Entities;

/// <summary>Links two KnowledgeNodes (source and target) through a RelationshipType.</summary>
public class KnowledgeRelation
{
    /// <summary>The KnowledgeRelation's id.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The relation's source KnowledgeNode.</summary>
    public Guid SourceNodeId { get; set; }

    /// <summary>The RelationshipType classifying this relation.</summary>
    public Guid RelationshipTypeId { get; set; }

    /// <summary>The relation's target KnowledgeNode.</summary>
    public Guid TargetNodeId { get; set; }
}
