using Microsoft.AspNetCore.Mvc;
using MnemoToad.Knowledge.Api.Contracts;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.PathResolution;
using MnemoToad.Knowledge.Data.Repositories;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Api.Controllers;

/// <summary>
/// A KnowledgeNode is a single "thing" in the graph (e.g. "France", "Mercury"), classified by a
/// NodeType, with scalar Attributes embedded directly and Media links keyed by name.
/// </summary>
[ApiController]
[Route("nodes")]
public class KnowledgeNodesController : ControllerBase
{
    private readonly IKnowledgeNodeRepository _repository;
    private readonly IPathResolutionRepository _pathResolutionRepository;

    public KnowledgeNodesController(IKnowledgeNodeRepository repository, IPathResolutionRepository pathResolutionRepository)
    {
        _repository = repository;
        _pathResolutionRepository = pathResolutionRepository;
    }

    /// <summary>
    /// Lists every KnowledgeNode of a given NodeType. List items omit <c>attributes</c>/<c>media</c>
    /// entirely — use <c>GET /nodes/{id}</c> for a node's full attributes and media.
    /// </summary>
    /// <param name="nodeTypeId">The NodeType to filter by. Required — there is no unfiltered "list every node" call.</param>
    /// <response code="200">The matching KnowledgeNodes, in no particular order.</response>
    /// <response code="400"><c>nodeTypeId</c> was missing or not a valid GUID.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<KnowledgeNode>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery, Required] Guid? nodeTypeId) =>
        Ok(await _repository.GetAllAsync(nodeTypeId!.Value));

    /// <summary>Gets a single KnowledgeNode by id, including its full attributes and media.</summary>
    /// <param name="id">The KnowledgeNode's id.</param>
    /// <response code="200">The matching KnowledgeNode.</response>
    /// <response code="404">No KnowledgeNode exists with that id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(KnowledgeNode), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id) =>
        await _repository.GetByIdAsync(id) is { } knowledgeNode ? Ok(knowledgeNode) : NotFound();

    /// <summary>Creates a new KnowledgeNode, optionally with attributes and media in the same call.</summary>
    /// <param name="request">The new KnowledgeNode's NodeType, name, description, attributes, and media.</param>
    /// <response code="201">The created KnowledgeNode.</response>
    /// <response code="400">
    /// A required field was missing, <c>nodeTypeId</c> doesn't reference an existing NodeType, a
    /// KnowledgeNode with the same NodeType and CanonicalName already exists, or a media entry was
    /// missing a valid <c>id</c>/<c>alt_text</c>.
    /// </response>
    [HttpPost]
    [ProducesResponseType(typeof(KnowledgeNode), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(KnowledgeNodeRequest request)
    {
        try
        {
            var created = await _repository.CreateAsync(new KnowledgeNode
            {
                NodeTypeId = request.NodeTypeId!.Value,
                CanonicalName = request.CanonicalName,
                Description = request.Description,
                Attributes = request.Attributes ?? new(),
                Media = request.Media ?? new()
            });
            return Created($"/nodes/{created.Id}", created);
        }
        catch (ValidationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// Replaces an existing KnowledgeNode's name, description, attributes, and media.
    /// <c>attributes</c>/<c>media</c> are a full replace, not a merge — a key present on the node
    /// but absent from the request is removed.
    /// </summary>
    /// <param name="id">The KnowledgeNode's id.</param>
    /// <param name="request">The KnowledgeNode's new NodeType, name, description, attributes, and media.</param>
    /// <response code="200">The updated KnowledgeNode.</response>
    /// <response code="400">
    /// A required field was missing, <c>nodeTypeId</c> doesn't reference an existing NodeType,
    /// another KnowledgeNode already has the same NodeType and CanonicalName, or a media entry was
    /// missing a valid <c>id</c>/<c>alt_text</c>.
    /// </response>
    /// <response code="404">No KnowledgeNode exists with that id.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(KnowledgeNode), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, KnowledgeNodeRequest request)
    {
        try
        {
            var updated = await _repository.UpdateAsync(new KnowledgeNode
            {
                Id = id,
                NodeTypeId = request.NodeTypeId!.Value,
                CanonicalName = request.CanonicalName,
                Description = request.Description,
                Attributes = request.Attributes ?? new(),
                Media = request.Media ?? new()
            });
            return updated is not null ? Ok(updated) : NotFound();
        }
        catch (ValidationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// Deletes a KnowledgeNode. Its own attributes and media are deleted along with it (cascade);
    /// KnowledgeRelations still pointing at it block the delete instead.
    /// </summary>
    /// <param name="id">The KnowledgeNode's id.</param>
    /// <response code="204">The KnowledgeNode was deleted.</response>
    /// <response code="400">
    /// The KnowledgeNode is still referenced by one or more KnowledgeRelations and can't be deleted.
    /// </response>
    /// <response code="404">No KnowledgeNode exists with that id.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            return await _repository.DeleteAsync(id) ? NoContent() : NotFound();
        }
        catch (ValidationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// Resolves a batch of Property Path DSL expressions against KnowledgeNodes in one call. One
    /// response entry per request entry, in request order — not deduped by nodeId, since the same
    /// node can appear in more than one entry.
    /// </summary>
    /// <param name="items">The nodes and, for each, the paths to resolve against it.</param>
    /// <response code="200">
    /// One result per requested entry, in request order. <c>properties</c> holds the paths that
    /// resolved — a scalar for a column/attribute path, an <c>{ id, alt_text, metadata }</c> object
    /// for a media path. <c>errors</c> (omitted when empty) holds the paths that didn't — node not
    /// found, a hop's relation doesn't exist, or the terminal's attribute/media key is missing —
    /// with a message per path. A path appears in exactly one of the two. Partial failures never
    /// fail the batch.
    /// </response>
    /// <response code="400">
    /// The request array was missing/empty, an entry's <c>nodeId</c> was missing, <c>paths</c> was
    /// missing/empty, or a path isn't valid Path DSL syntax.
    /// </response>
    [HttpPost("resolve")]
    [ProducesResponseType(typeof(IEnumerable<ResolvedNodePaths>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Resolve([Required, MinLength(1)] List<ResolvePathsRequestItem>? items)
    {
        var queries = items!
            .SelectMany(item => item.Paths!.Select(path => new PathResolutionQuery(item.NodeId!.Value, path)))
            .ToList();
        var resolved = await _pathResolutionRepository.ResolveAsync(queries);

        var results = new List<ResolvedNodePaths>();
        var cursor = 0;
        foreach (var item in items!)
        {
            var properties = new Dictionary<string, JsonNode?>();
            Dictionary<string, string>? errors = null;
            foreach (var _ in item.Paths!)
            {
                var r = resolved[cursor++];
                if (r.Error is not null)
                    (errors ??= new())[r.Path] = r.Error;
                else
                    properties[r.Path] = r.Value;
            }
            results.Add(new ResolvedNodePaths(item.NodeId!.Value, properties, errors));
        }
        return Ok(results);
    }
}
