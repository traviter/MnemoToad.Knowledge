namespace MnemoToad.Knowledge.Api.TestData.Contracts;

public record CleanupTestDataRequest(
    List<Guid>? KnowledgeRelationIds,
    List<Guid>? KnowledgeNodeIds,
    List<Guid>? NodeTypeIds,
    List<Guid>? RelationshipTypeIds,
    List<Guid>? MediaAssetIds);
