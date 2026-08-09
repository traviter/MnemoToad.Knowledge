using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Knowledge.Api.Contracts;

/// <summary>The body for creating a KnowledgeRelation.</summary>
/// <param name="SourceNodeId">The relation's source KnowledgeNode. Must reference an existing node. Required.</param>
/// <param name="RelationshipTypeId">The RelationshipType classifying this relation. Must reference an existing RelationshipType. Required.</param>
/// <param name="TargetNodeId">The relation's target KnowledgeNode. Must reference an existing node. Required.</param>
public record KnowledgeRelationRequest([Required] Guid? SourceNodeId, [Required] Guid? RelationshipTypeId, [Required] Guid? TargetNodeId);
