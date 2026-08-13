using Microsoft.AspNetCore.Mvc;
using MnemoToad.Knowledge.Api.TestData.Contracts;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.Repositories;

namespace MnemoToad.Knowledge.Api.TestData;

// Unauthenticated, no environment gating -- reachable on the deployed Azure app. Accepted for now;
// this is a stepping stone to its own project/server, not permanent. See Phase 3 Step 2 plan.
[ApiController]
[Route("testdata")]
public class TestDataController : ControllerBase
{
    private readonly INodeTypeRepository _nodeTypes;
    private readonly IRelationshipTypeRepository _relationshipTypes;
    private readonly IMediaAssetRepository _mediaAssets;
    private readonly IKnowledgeNodeRepository _knowledgeNodes;
    private readonly IKnowledgeRelationRepository _knowledgeRelations;

    public TestDataController(
        INodeTypeRepository nodeTypes,
        IRelationshipTypeRepository relationshipTypes,
        IMediaAssetRepository mediaAssets,
        IKnowledgeNodeRepository knowledgeNodes,
        IKnowledgeRelationRepository knowledgeRelations)
    {
        _nodeTypes = nodeTypes;
        _relationshipTypes = relationshipTypes;
        _mediaAssets = mediaAssets;
        _knowledgeNodes = knowledgeNodes;
        _knowledgeRelations = knowledgeRelations;
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed(SeedTestDataRequest request)
    {
        // Fixed dependency-safe order regardless of the request's own property/array order.
        foreach (var x in request.NodeTypes ?? new())
            await _nodeTypes.CreateAsync(new NodeType { Id = x.Id, Name = x.Name, Description = x.Description });
        foreach (var x in request.RelationshipTypes ?? new())
            await _relationshipTypes.CreateAsync(new RelationshipType { Id = x.Id, Name = x.Name, Description = x.Description });
        foreach (var x in request.MediaAssets ?? new())
            await _mediaAssets.CreateAsync(new MediaAsset { Id = x.Id, Url = x.Url });
        foreach (var x in request.KnowledgeNodes ?? new())
            await _knowledgeNodes.CreateAsync(new KnowledgeNode { Id = x.Id, NodeTypeId = x.NodeTypeId, CanonicalName = x.CanonicalName, Description = x.Description, Attributes = x.Attributes ?? new(), Media = x.Media ?? new() });
        foreach (var x in request.KnowledgeRelations ?? new())
            await _knowledgeRelations.CreateAsync(new KnowledgeRelation { Id = x.Id, SourceNodeId = x.SourceNodeId, RelationshipTypeId = x.RelationshipTypeId, TargetNodeId = x.TargetNodeId });
        return NoContent();
    }

    [HttpPost("cleanup")]
    public async Task<IActionResult> Cleanup(CleanupTestDataRequest request)
    {
        // Reverse of Seed's order -- dependents before dependencies.
        foreach (var id in request.KnowledgeRelationIds ?? new()) await _knowledgeRelations.DeleteAsync(id);
        foreach (var id in request.KnowledgeNodeIds ?? new()) await _knowledgeNodes.DeleteAsync(id);
        foreach (var id in request.NodeTypeIds ?? new()) await _nodeTypes.DeleteAsync(id);
        foreach (var id in request.RelationshipTypeIds ?? new()) await _relationshipTypes.DeleteAsync(id);
        foreach (var id in request.MediaAssetIds ?? new()) await _mediaAssets.DeleteAsync(id);
        return NoContent();
    }
}
