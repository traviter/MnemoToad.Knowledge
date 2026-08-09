using Microsoft.AspNetCore.Mvc;
using MnemoToad.Knowledge.Api.Contracts;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.Repositories;

namespace MnemoToad.Knowledge.Api.Controllers;

/// <summary>
/// A MediaAsset is a catalogued URL — no blob storage, just a link — attached to a KnowledgeNode
/// via its <c>id</c> in that node's <c>media</c> dictionary. No list-all: assets are looked up by
/// id, not browsed.
/// </summary>
[ApiController]
[Route("mediaAssets")]
public class MediaAssetsController : ControllerBase
{
    private readonly IMediaAssetRepository _repository;

    public MediaAssetsController(IMediaAssetRepository repository)
    {
        _repository = repository;
    }

    /// <summary>Gets a single MediaAsset by id.</summary>
    /// <param name="id">The MediaAsset's id.</param>
    /// <response code="200">The matching MediaAsset.</response>
    /// <response code="404">No MediaAsset exists with that id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MediaAsset), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id) =>
        await _repository.GetByIdAsync(id) is { } mediaAsset ? Ok(mediaAsset) : NotFound();

    /// <summary>Creates a new MediaAsset.</summary>
    /// <param name="request">The asset's URL.</param>
    /// <response code="201">The created MediaAsset.</response>
    /// <response code="400"><c>url</c> was missing.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MediaAsset), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(MediaAssetRequest request)
    {
        var created = await _repository.CreateAsync(new MediaAsset { Url = request.Url });
        return Created($"/mediaAssets/{created.Id}", created);
    }

    /// <summary>Replaces an existing MediaAsset's URL.</summary>
    /// <param name="id">The MediaAsset's id.</param>
    /// <param name="request">The asset's new URL.</param>
    /// <response code="200">The updated MediaAsset.</response>
    /// <response code="400"><c>url</c> was missing.</response>
    /// <response code="404">No MediaAsset exists with that id.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(MediaAsset), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, MediaAssetRequest request)
    {
        var updated = await _repository.UpdateAsync(new MediaAsset { Id = id, Url = request.Url });
        return updated is not null ? Ok(updated) : NotFound();
    }

    /// <summary>
    /// Deletes a MediaAsset. Nothing references <c>media_asset</c> by foreign key, so this never
    /// fails on a constraint — a KnowledgeNode's media entry pointing at a now-deleted asset simply
    /// becomes a dangling <c>id</c>.
    /// </summary>
    /// <param name="id">The MediaAsset's id.</param>
    /// <response code="204">The MediaAsset was deleted.</response>
    /// <response code="404">No MediaAsset exists with that id.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id) =>
        await _repository.DeleteAsync(id) ? NoContent() : NotFound();
}
