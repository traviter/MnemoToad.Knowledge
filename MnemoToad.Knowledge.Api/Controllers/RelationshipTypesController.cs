using Microsoft.AspNetCore.Mvc;
using MnemoToad.Knowledge.Api.Contracts;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.Repositories;

namespace MnemoToad.Knowledge.Api.Controllers;

/// <summary>
/// A RelationshipType is the open vocabulary that KnowledgeRelations are classified under
/// (e.g. "capitalOf", inverse "hasCapital").
/// </summary>
[ApiController]
[Route("relationshipTypes")]
public class RelationshipTypesController : ControllerBase
{
    private readonly IRelationshipTypeRepository _repository;

    public RelationshipTypesController(IRelationshipTypeRepository repository)
    {
        _repository = repository;
    }

    /// <summary>Lists every RelationshipType.</summary>
    /// <response code="200">All RelationshipTypes, in no particular order.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RelationshipType>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() =>
        Ok(await _repository.GetAllAsync());

    /// <summary>Gets a single RelationshipType by id.</summary>
    /// <param name="id">The RelationshipType's id.</param>
    /// <response code="200">The matching RelationshipType.</response>
    /// <response code="404">No RelationshipType exists with that id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RelationshipType), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id) =>
        await _repository.GetByIdAsync(id) is { } relationshipType ? Ok(relationshipType) : NotFound();

    /// <summary>Creates a new RelationshipType.</summary>
    /// <param name="request">The name, optional inverse name, and optional description for the new RelationshipType.</param>
    /// <response code="201">The created RelationshipType.</response>
    /// <response code="400">
    /// <c>Name</c> was missing, or a RelationshipType with that name already exists.
    /// </response>
    [HttpPost]
    [ProducesResponseType(typeof(RelationshipType), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(RelationshipTypeRequest request)
    {
        var created = await _repository.CreateAsync(new RelationshipType
        {
            Name = request.Name,
            InverseName = request.InverseName,
            Description = request.Description
        });
        return Created($"/relationshipTypes/{created.Id}", created);
    }

    /// <summary>Replaces an existing RelationshipType's name, inverse name, and description.</summary>
    /// <param name="id">The RelationshipType's id.</param>
    /// <param name="request">The RelationshipType's new name, optional inverse name, and optional description.</param>
    /// <response code="200">The updated RelationshipType.</response>
    /// <response code="400">
    /// <c>Name</c> was missing, or another RelationshipType already has that name.
    /// </response>
    /// <response code="404">No RelationshipType exists with that id.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RelationshipType), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, RelationshipTypeRequest request)
    {
        var updated = await _repository.UpdateAsync(new RelationshipType
        {
            Id = id,
            Name = request.Name,
            InverseName = request.InverseName,
            Description = request.Description
        });
        return updated is not null ? Ok(updated) : NotFound();
    }

    /// <summary>Deletes a RelationshipType.</summary>
    /// <param name="id">The RelationshipType's id.</param>
    /// <response code="204">The RelationshipType was deleted.</response>
    /// <response code="400">
    /// The RelationshipType is still referenced by one or more KnowledgeRelations and can't be deleted.
    /// </response>
    /// <response code="404">No RelationshipType exists with that id.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id) =>
        await _repository.DeleteAsync(id) ? NoContent() : NotFound();
}
