using Microsoft.AspNetCore.Mvc;
using MnemoToad.Knowledge.Api.Contracts;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.Repositories;
using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Knowledge.Api.Controllers;

/// <summary>
/// A KnowledgeRelation links two KnowledgeNodes (source and target) through a RelationshipType —
/// e.g. "France" --capitalOf--&gt; "Paris". Link-only: no list-all, get-by-id, or update.
/// </summary>
[ApiController]
[Route("relationships")]
public class KnowledgeRelationsController : ControllerBase
{
    private readonly IKnowledgeRelationRepository _repository;

    public KnowledgeRelationsController(IKnowledgeRelationRepository repository)
    {
        _repository = repository;
    }

    /// <summary>Lists every KnowledgeRelation where the given node is either the source or the target.</summary>
    /// <param name="nodeId">The KnowledgeNode's id.</param>
    /// <response code="200">The matching KnowledgeRelations, in no particular order.</response>
    [HttpGet("/nodes/{nodeId:guid}/relationships")]
    [ProducesResponseType(typeof(IEnumerable<KnowledgeRelation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByNodeId(Guid nodeId) =>
        Ok(await _repository.GetByNodeIdAsync(nodeId));

    /// <summary>Creates a new KnowledgeRelation between two existing KnowledgeNodes.</summary>
    /// <param name="request">The source node, RelationshipType, and target node.</param>
    /// <response code="201">The created KnowledgeRelation.</response>
    /// <response code="400">
    /// A required field was missing, <c>sourceNodeId</c>/<c>targetNodeId</c>/<c>relationshipTypeId</c>
    /// doesn't reference an existing row, or the exact same source/type/target triple already exists.
    /// </response>
    [HttpPost]
    [ProducesResponseType(typeof(KnowledgeRelation), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(KnowledgeRelationRequest request)
    {
        try
        {
            var created = await _repository.CreateAsync(new KnowledgeRelation
            {
                SourceNodeId = request.SourceNodeId!.Value,
                RelationshipTypeId = request.RelationshipTypeId!.Value,
                TargetNodeId = request.TargetNodeId!.Value
            });
            return Created($"/relationships/{created.Id}", created);
        }
        catch (ValidationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>Deletes a KnowledgeRelation. Nothing else references a relation, so this never fails on a constraint.</summary>
    /// <param name="id">The KnowledgeRelation's id.</param>
    /// <response code="204">The KnowledgeRelation was deleted.</response>
    /// <response code="404">No KnowledgeRelation exists with that id.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id) =>
        await _repository.DeleteAsync(id) ? NoContent() : NotFound();
}
