using Microsoft.AspNetCore.Mvc;
using MnemoToad.Knowledge.Api.Contracts;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.Repositories;

namespace MnemoToad.Knowledge.Api.Controllers;

/// <summary>
/// A NodeType is the open vocabulary that KnowledgeNodes are classified under (e.g. "Planet",
/// "Country") — it has no fields of its own beyond a name and description.
/// </summary>
[ApiController]
[Route("nodeTypes")]
public class NodeTypesController : ControllerBase
{
    private readonly INodeTypeRepository _repository;

    public NodeTypesController(INodeTypeRepository repository)
    {
        _repository = repository;
    }

    /// <summary>Lists every NodeType.</summary>
    /// <response code="200">All NodeTypes, in no particular order.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NodeType>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() =>
        Ok(await _repository.GetAllAsync());

    /// <summary>Gets a single NodeType by id.</summary>
    /// <param name="id">The NodeType's id.</param>
    /// <response code="200">The matching NodeType.</response>
    /// <response code="404">No NodeType exists with that id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NodeType), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id) =>
        await _repository.GetByIdAsync(id) is { } nodeType ? Ok(nodeType) : NotFound();

    /// <summary>Creates a new NodeType.</summary>
    /// <param name="request">The name and optional description for the new NodeType.</param>
    /// <response code="201">The created NodeType.</response>
    /// <response code="400">
    /// <c>Name</c> was missing, or a NodeType with that name already exists.
    /// </response>
    [HttpPost]
    [ProducesResponseType(typeof(NodeType), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(NodeTypeRequest request)
    {
        var created = await _repository.CreateAsync(new NodeType { Name = request.Name, Description = request.Description });
        return Created($"/nodeTypes/{created.Id}", created);
    }

    /// <summary>Replaces an existing NodeType's name and description.</summary>
    /// <param name="id">The NodeType's id.</param>
    /// <param name="request">The NodeType's new name and optional description.</param>
    /// <response code="200">The updated NodeType.</response>
    /// <response code="400">
    /// <c>Name</c> was missing, or another NodeType already has that name.
    /// </response>
    /// <response code="404">No NodeType exists with that id.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(NodeType), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, NodeTypeRequest request)
    {
        var updated = await _repository.UpdateAsync(new NodeType { Id = id, Name = request.Name, Description = request.Description });
        return updated is not null ? Ok(updated) : NotFound();
    }

    /// <summary>Deletes a NodeType.</summary>
    /// <param name="id">The NodeType's id.</param>
    /// <response code="204">The NodeType was deleted.</response>
    /// <response code="400">
    /// The NodeType is still referenced by one or more KnowledgeNodes and can't be deleted.
    /// </response>
    /// <response code="404">No NodeType exists with that id.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id) =>
        await _repository.DeleteAsync(id) ? NoContent() : NotFound();
}
