using Microsoft.AspNetCore.Mvc;
using Moq;
using MnemoToad.Knowledge.Api.Contracts;
using MnemoToad.Knowledge.Api.Controllers;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.PathResolution;
using MnemoToad.Knowledge.Data.Repositories;
using NUnit.Framework;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.Controllers;

[TestFixture]
public class KnowledgeNodesControllerTests
{
    private Mock<IKnowledgeNodeRepository> _repository = null!;
    private Mock<IPathResolutionRepository> _pathResolutionRepository = null!;
    private KnowledgeNodesController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IKnowledgeNodeRepository>();
        _pathResolutionRepository = new Mock<IPathResolutionRepository>();
        _controller = new KnowledgeNodesController(_repository.Object, _pathResolutionRepository.Object);
    }

    [Test]
    public async Task GetAll_WithNodeTypeIdFilter_PassesFilterToRepository()
    {
        var nodeTypeId = Guid.NewGuid();
        var nodes = new List<KnowledgeNode>();
        _repository.Setup(r => r.GetAllAsync(nodeTypeId)).ReturnsAsync(nodes);

        var result = await _controller.GetAll(nodeTypeId);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(nodes));
        _repository.Verify(r => r.GetAllAsync(nodeTypeId), Times.Once);
    }

    [Test]
    public async Task GetById_WhenExists_ReturnsOkWithNode()
    {
        var node = new KnowledgeNode { Id = Guid.NewGuid(), CanonicalName = "Mercury" };
        _repository.Setup(r => r.GetByIdAsync(node.Id)).ReturnsAsync(node);

        var result = await _controller.GetById(node.Id);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(node));
    }

    [Test]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((KnowledgeNode?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Create_WithValidRequest_ReturnsCreatedWithNode()
    {
        var nodeTypeId = Guid.NewGuid();
        var created = new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = nodeTypeId, CanonicalName = "Mercury" };
        _repository.Setup(r => r.CreateAsync(It.Is<KnowledgeNode>(n => n.NodeTypeId == nodeTypeId && n.CanonicalName == "Mercury")))
            .ReturnsAsync(created);

        var result = await _controller.Create(new KnowledgeNodeRequest(nodeTypeId, "Mercury", null));

        var createdResult = result as CreatedResult;
        Assert.That(createdResult, Is.Not.Null);
        Assert.That(createdResult!.Location, Is.EqualTo($"/nodes/{created.Id}"));
        Assert.That(createdResult.Value, Is.SameAs(created));
    }

    [Test]
    public async Task Create_WithAttributes_PassesAttributesToRepository()
    {
        var nodeTypeId = Guid.NewGuid();
        var attributes = new Dictionary<string, JsonValue?> { ["isoCode"] = JsonValue.Create("FR") };
        var created = new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = nodeTypeId, CanonicalName = "France", Attributes = attributes };
        _repository.Setup(r => r.CreateAsync(It.Is<KnowledgeNode>(n => n.Attributes == attributes)))
            .ReturnsAsync(created);

        var result = await _controller.Create(new KnowledgeNodeRequest(nodeTypeId, "France", null, attributes));

        var createdResult = result as CreatedResult;
        Assert.That(createdResult, Is.Not.Null);
        Assert.That(createdResult!.Value, Is.SameAs(created));
    }

    [Test]
    public async Task Create_WithNullAttributes_PassesEmptyDictionaryToRepository()
    {
        var nodeTypeId = Guid.NewGuid();
        _repository.Setup(r => r.CreateAsync(It.Is<KnowledgeNode>(n => n.Attributes!.Count == 0)))
            .ReturnsAsync(new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = nodeTypeId, CanonicalName = "Mercury" });

        var result = await _controller.Create(new KnowledgeNodeRequest(nodeTypeId, "Mercury", null, null));

        Assert.That(result, Is.InstanceOf<CreatedResult>());
        _repository.Verify(r => r.CreateAsync(It.Is<KnowledgeNode>(n => n.Attributes!.Count == 0)), Times.Once);
    }

    [Test]
    public async Task Create_WithMedia_PassesMediaDictionaryThroughUnchanged()
    {
        var nodeTypeId = Guid.NewGuid();
        var media = new Dictionary<string, JsonObject?> { ["flag"] = new JsonObject { ["id"] = Guid.NewGuid().ToString(), ["alt_text"] = "Flag of France" } };
        var created = new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = nodeTypeId, CanonicalName = "France" };
        _repository.Setup(r => r.CreateAsync(It.Is<KnowledgeNode>(n => n.Media == media)))
            .ReturnsAsync(created);

        var result = await _controller.Create(new KnowledgeNodeRequest(nodeTypeId, "France", null, null, media));

        var createdResult = result as CreatedResult;
        Assert.That(createdResult, Is.Not.Null);
        Assert.That(createdResult!.Value, Is.SameAs(created));
    }

    [Test]
    public async Task Create_WithNullMedia_PassesEmptyDictionaryToRepository()
    {
        var nodeTypeId = Guid.NewGuid();
        _repository.Setup(r => r.CreateAsync(It.Is<KnowledgeNode>(n => n.Media!.Count == 0)))
            .ReturnsAsync(new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = nodeTypeId, CanonicalName = "Mercury" });

        var result = await _controller.Create(new KnowledgeNodeRequest(nodeTypeId, "Mercury", null));

        Assert.That(result, Is.InstanceOf<CreatedResult>());
        _repository.Verify(r => r.CreateAsync(It.Is<KnowledgeNode>(n => n.Media!.Count == 0)), Times.Once);
    }

    [Test]
    public void Create_WhenRepositoryThrowsValidationException_PropagatesException()
    {
        _repository.Setup(r => r.CreateAsync(It.IsAny<KnowledgeNode>()))
            .ThrowsAsync(new ValidationException("A KnowledgeNode with the same NodeType and CanonicalName already exists."));

        Assert.ThrowsAsync<ValidationException>(() => _controller.Create(new KnowledgeNodeRequest(Guid.NewGuid(), "Mercury", null)));
    }

    [Test]
    public void Create_WhenRepositoryThrowsValidationExceptionForAlreadyLinkedMediaAsset_PropagatesException()
    {
        _repository.Setup(r => r.CreateAsync(It.IsAny<KnowledgeNode>()))
            .ThrowsAsync(new ValidationException("The specified MediaAsset is already linked to another KnowledgeNode."));

        Assert.ThrowsAsync<ValidationException>(() => _controller.Create(new KnowledgeNodeRequest(Guid.NewGuid(), "Mercury", null, null,
            new Dictionary<string, JsonObject?> { ["flag"] = new JsonObject { ["id"] = Guid.NewGuid().ToString(), ["alt_text"] = "x" } })));
    }

    [Test]
    public async Task Update_WhenExists_ReturnsOkWithUpdatedNode()
    {
        var id = Guid.NewGuid();
        var updated = new KnowledgeNode { Id = id, CanonicalName = "Mercury", Description = "New description" };
        _repository.Setup(r => r.UpdateAsync(It.Is<KnowledgeNode>(n => n.Id == id))).ReturnsAsync(updated);

        var result = await _controller.Update(id, new KnowledgeNodeRequest(Guid.NewGuid(), "Mercury", "New description"));

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(updated));
    }

    [Test]
    public async Task Update_WithAttributes_PassesAttributesToRepository()
    {
        var id = Guid.NewGuid();
        var attributes = new Dictionary<string, JsonValue?> { ["isoCode"] = JsonValue.Create("FR") };
        var updated = new KnowledgeNode { Id = id, CanonicalName = "France", Attributes = attributes };
        _repository.Setup(r => r.UpdateAsync(It.Is<KnowledgeNode>(n => n.Id == id && n.Attributes == attributes)))
            .ReturnsAsync(updated);

        var result = await _controller.Update(id, new KnowledgeNodeRequest(Guid.NewGuid(), "France", null, attributes));

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(updated));
    }

    [Test]
    public async Task Update_WithMedia_PassesMediaDictionaryThroughUnchanged()
    {
        var id = Guid.NewGuid();
        var media = new Dictionary<string, JsonObject?> { ["flag"] = new JsonObject { ["id"] = Guid.NewGuid().ToString(), ["alt_text"] = "Flag of France" } };
        var updated = new KnowledgeNode { Id = id, CanonicalName = "France" };
        _repository.Setup(r => r.UpdateAsync(It.Is<KnowledgeNode>(n => n.Id == id && n.Media == media)))
            .ReturnsAsync(updated);

        var result = await _controller.Update(id, new KnowledgeNodeRequest(Guid.NewGuid(), "France", null, null, media));

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(updated));
    }

    [Test]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        _repository.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeNode>())).ReturnsAsync((KnowledgeNode?)null);

        var result = await _controller.Update(Guid.NewGuid(), new KnowledgeNodeRequest(Guid.NewGuid(), "Mercury", null));

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public void Update_WhenRepositoryThrowsValidationException_PropagatesException()
    {
        _repository.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeNode>()))
            .ThrowsAsync(new ValidationException("The specified NodeType does not exist."));

        Assert.ThrowsAsync<ValidationException>(() => _controller.Update(Guid.NewGuid(), new KnowledgeNodeRequest(Guid.NewGuid(), "Mercury", null)));
    }

    [Test]
    public async Task Delete_WhenExists_ReturnsNoContent()
    {
        _repository.Setup(r => r.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        var result = await _controller.Delete(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        _repository.Setup(r => r.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _controller.Delete(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public void Delete_WhenRepositoryThrowsValidationException_PropagatesException()
    {
        _repository.Setup(r => r.DeleteAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new ValidationException("The KnowledgeNode cannot be deleted because it is referenced by one or more KnowledgeRelations."));

        Assert.ThrowsAsync<ValidationException>(() => _controller.Delete(Guid.NewGuid()));
    }

    [Test]
    public async Task Resolve_SingleEntryWithMultiplePaths_ReturnsAllInProperties()
    {
        var nodeId = Guid.NewGuid();
        _pathResolutionRepository.Setup(r => r.ResolveAsync(It.IsAny<IReadOnlyList<PathResolutionQuery>>()))
            .ReturnsAsync((IReadOnlyList<PathResolutionQuery> queries) => queries
                .Select(q => new ResolvedPath(q.NodeId, q.Path, JsonValue.Create($"value-for-{q.Path}"), null))
                .ToList());

        var result = await _controller.Resolve([new ResolvePathsRequestItem(nodeId, ["_canonicalName", ".population"])]);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var results = ok!.Value as List<ResolvedNodePaths>;
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results![0].NodeId, Is.EqualTo(nodeId));
        Assert.That(results[0].Properties["_canonicalName"]!.GetValue<string>(), Is.EqualTo("value-for-_canonicalName"));
        Assert.That(results[0].Properties[".population"]!.GetValue<string>(), Is.EqualTo("value-for-.population"));
        Assert.That(results[0].Errors, Is.Null);
    }

    [Test]
    public async Task Resolve_PathWithError_GoesToErrorsNotProperties()
    {
        var nodeId = Guid.NewGuid();
        _pathResolutionRepository.Setup(r => r.ResolveAsync(It.IsAny<IReadOnlyList<PathResolutionQuery>>()))
            .ReturnsAsync((IReadOnlyList<PathResolutionQuery> queries) => queries
                .Select(q => new ResolvedPath(q.NodeId, q.Path, null, "No attribute named 'gdp' on this node."))
                .ToList());

        var result = await _controller.Resolve([new ResolvePathsRequestItem(nodeId, [".gdp"])]);

        var results = (result as OkObjectResult)!.Value as List<ResolvedNodePaths>;
        Assert.That(results![0].Properties, Is.Empty);
        Assert.That(results[0].Errors, Is.Not.Null);
        Assert.That(results[0].Errors![".gdp"], Is.EqualTo("No attribute named 'gdp' on this node."));
    }

    [Test]
    public async Task Resolve_SameNodeIdInTwoEntries_ReturnsSeparateUnmergedResults()
    {
        var nodeId = Guid.NewGuid();
        _pathResolutionRepository.Setup(r => r.ResolveAsync(It.IsAny<IReadOnlyList<PathResolutionQuery>>()))
            .ReturnsAsync((IReadOnlyList<PathResolutionQuery> queries) => queries
                .Select(q => new ResolvedPath(q.NodeId, q.Path, JsonValue.Create($"value-for-{q.Path}"), null))
                .ToList());

        var result = await _controller.Resolve(
        [
            new ResolvePathsRequestItem(nodeId, ["_canonicalName"]),
            new ResolvePathsRequestItem(nodeId, [".population"])
        ]);

        var results = (result as OkObjectResult)!.Value as List<ResolvedNodePaths>;
        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results![0].Properties.Keys, Is.EquivalentTo(new[] { "_canonicalName" }));
        Assert.That(results[1].Properties.Keys, Is.EquivalentTo(new[] { ".population" }));
    }
}
