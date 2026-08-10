using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Api.TestData.Contracts;

public record SeedTestDataRequest(
    List<TestNodeType>? NodeTypes,
    List<TestRelationshipType>? RelationshipTypes,
    List<TestMediaAsset>? MediaAssets,
    List<TestKnowledgeNode>? KnowledgeNodes,
    List<TestKnowledgeRelation>? KnowledgeRelations);

public record TestNodeType(Guid Id, string Name, string? Description = null);

public record TestRelationshipType(Guid Id, string Name, string? InverseName = null, string? Description = null);

public record TestMediaAsset(Guid Id, string Url);

public record TestKnowledgeNode(
    Guid Id,
    Guid NodeTypeId,
    string CanonicalName,
    string? Description = null,
    Dictionary<string, JsonValue?>? Attributes = null,
    Dictionary<string, JsonObject?>? Media = null);

public record TestKnowledgeRelation(Guid Id, Guid SourceNodeId, Guid RelationshipTypeId, Guid TargetNodeId);
