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
    private readonly INodeTypeRepository _nodeTypeRepository;

    public KnowledgeNodesController(
        IKnowledgeNodeRepository repository,
        IPathResolutionRepository pathResolutionRepository,
        INodeTypeRepository nodeTypeRepository)
    {
        _repository = repository;
        _pathResolutionRepository = pathResolutionRepository;
        _nodeTypeRepository = nodeTypeRepository;
    }

    /// <summary>
    /// Lists every KnowledgeNode of a given NodeType. List items omit <c>attributes</c>/<c>media</c>
    /// entirely — use <c>GET /nodes/{id}</c> for a node's full attributes and media.
    /// </summary>
    /// <param name="nodeTypeName">The NodeType's name to filter by. Required — there is no unfiltered "list every node" call.</param>
    /// <response code="200">The matching KnowledgeNodes, in no particular order. Empty array if the NodeType has no KnowledgeNodes.</response>
    /// <response code="400"><c>nodeTypeName</c> was missing or empty.</response>
    /// <response code="404">No NodeType exists with that name.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<KnowledgeNode>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll([FromQuery, Required] string? nodeTypeName)
    {
        var nodes = await _repository.GetAllAsync(nodeTypeName!);
        if (nodes.Count == 0 && !await NodeTypeExistsAsync(nodeTypeName!)) return NotFound();
        return Ok(nodes);
    }

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
    public async Task<IActionResult> Delete(Guid id) =>
        await _repository.DeleteAsync(id) ? NoContent() : NotFound();

    /// <summary>
    /// Resolves a batch of Property Path DSL expressions against KnowledgeNodes in one call. One
    /// response entry per request entry, in request order — not deduped by nodeId, since the same
    /// node can appear in more than one entry.
    /// </summary>
    /// <param name="items">The nodes and, for each, the paths to resolve against it.</param>
    /// <response code="200">
    /// One result per requested entry, in request order. <c>properties</c> holds the paths that
    /// resolved — a scalar for a column/attribute path, the stored media stanza (<c>id</c>,
    /// <c>alt_text</c>, and any other client-supplied fields, all flat) for a media path.
    /// <c>errors</c> (omitted when empty) holds the paths that didn't — node not found, an edge in
    /// the path doesn't match any relation, or the terminal's attribute/media key is missing — each
    /// reported as <c>"Path could not be resolved."</c>, without specifying which stage failed. A
    /// path appears in exactly one of the two. Partial failures never fail the batch.
    /// </response>
    /// <response code="400">
    /// The request array was missing/empty, an entry's <c>nodeId</c> was missing, <c>paths</c> was
    /// missing/empty, or a path isn't valid Path DSL syntax.
    /// </response>
    [HttpPost("resolve")]
    [ProducesResponseType(typeof(IEnumerable<ResolvedNodePaths>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Resolve([Required, MinLength(1)] List<ResolvePathsRequestItem>? items) =>
        Ok(await ResolveItemsAsync(items!));

    /// <summary>
    /// Finds every KnowledgeNode of the named NodeType and resolves the same set of Property Path
    /// DSL expressions against each — equivalent to calling <c>GET /nodes?nodeTypeName=...</c> then
    /// feeding every returned node id, paired with the same paths, into <c>POST /nodes/resolve</c>.
    /// </summary>
    /// <param name="request">The NodeType name and the paths to resolve against each of its nodes.</param>
    /// <response code="200">
    /// One result per matching KnowledgeNode, same shape as <c>POST /nodes/resolve</c>. Empty array
    /// if the NodeType exists but has no KnowledgeNodes.
    /// </response>
    /// <response code="400">
    /// <c>nodeTypeName</c> or <c>paths</c> was missing/empty, or a path isn't valid Path DSL syntax.
    /// </response>
    /// <response code="404">No NodeType exists with that name.</response>
    [HttpPost("resolve/type")]
    [ProducesResponseType(typeof(IEnumerable<ResolvedNodePaths>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveByType(ResolveByNodeTypeRequest request)
    {
        var nodes = await _repository.GetAllAsync(request.NodeTypeName!);
        if (nodes.Count == 0 && !await NodeTypeExistsAsync(request.NodeTypeName!)) return NotFound();

        var items = nodes.Select(n => new ResolvePathsRequestItem(n.Id, request.Paths)).ToList();
        return Ok(await ResolveItemsAsync(items));
    }

    private async Task<bool> NodeTypeExistsAsync(string nodeTypeName) =>
        (await _nodeTypeRepository.GetAllAsync()).Any(nt => nt.Name == nodeTypeName);

    private async Task<List<ResolvedNodePaths>> ResolveItemsAsync(List<ResolvePathsRequestItem> items)
    {
        var queries = items
            .SelectMany(item => item.Paths!.Select(path => new PathResolutionQuery(item.NodeId!.Value, path)))
            .ToList();
        var resolved = await _pathResolutionRepository.ResolveAsync(queries);

        var results = new List<ResolvedNodePaths>();
        var cursor = 0;
        foreach (var item in items)
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
        return results;
    }
}
