using Microsoft.AspNetCore.Mvc;
using Moq;
using MnemoToad.Knowledge.Api.Contracts;
using MnemoToad.Knowledge.Api.Controllers;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.Repositories;
using NUnit.Framework;
using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Knowledge.Tests.Controllers;

[TestFixture]
public class RelationshipTypesControllerTests
{
    private Mock<IRelationshipTypeRepository> _repository = null!;
    private RelationshipTypesController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IRelationshipTypeRepository>();
        _controller = new RelationshipTypesController(_repository.Object);
    }

    [Test]
    public async Task GetAll_ReturnsOkWithRelationshipTypes()
    {
        var relationshipTypes = new List<RelationshipType> { new() { Id = Guid.NewGuid(), Name = "parentOf" } };
        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(relationshipTypes);

        var result = await _controller.GetAll();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(relationshipTypes));
    }

    [Test]
    public async Task GetById_WhenExists_ReturnsOkWithRelationshipType()
    {
        var relationshipType = new RelationshipType { Id = Guid.NewGuid(), Name = "parentOf" };
        _repository.Setup(r => r.GetByIdAsync(relationshipType.Id)).ReturnsAsync(relationshipType);

        var result = await _controller.GetById(relationshipType.Id);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(relationshipType));
    }

    [Test]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((RelationshipType?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Create_WithValidRequest_ReturnsCreatedWithRelationshipType()
    {
        var created = new RelationshipType { Id = Guid.NewGuid(), Name = "parentOf", InverseName = "childOf" };
        _repository.Setup(r => r.CreateAsync(It.Is<RelationshipType>(r => r.Name == "parentOf" && r.InverseName == "childOf")))
            .ReturnsAsync(created);

        var result = await _controller.Create(new RelationshipTypeRequest("parentOf", "childOf", null));

        var createdResult = result as CreatedResult;
        Assert.That(createdResult, Is.Not.Null);
        Assert.That(createdResult!.Location, Is.EqualTo($"/relationshipTypes/{created.Id}"));
        Assert.That(createdResult.Value, Is.SameAs(created));
    }

    [Test]
    public void Create_WhenRepositoryThrowsValidationException_PropagatesException()
    {
        _repository.Setup(r => r.CreateAsync(It.IsAny<RelationshipType>()))
            .ThrowsAsync(new ValidationException("A RelationshipType with that name already exists."));

        Assert.ThrowsAsync<ValidationException>(() => _controller.Create(new RelationshipTypeRequest("parentOf", null, null)));
    }

    [Test]
    public async Task Update_WhenExists_ReturnsOkWithUpdatedRelationshipType()
    {
        var id = Guid.NewGuid();
        var updated = new RelationshipType { Id = id, Name = "parentOf", Description = "New description" };
        _repository.Setup(r => r.UpdateAsync(It.Is<RelationshipType>(r => r.Id == id))).ReturnsAsync(updated);

        var result = await _controller.Update(id, new RelationshipTypeRequest("parentOf", null, "New description"));

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(updated));
    }

    [Test]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        _repository.Setup(r => r.UpdateAsync(It.IsAny<RelationshipType>())).ReturnsAsync((RelationshipType?)null);

        var result = await _controller.Update(Guid.NewGuid(), new RelationshipTypeRequest("parentOf", null, null));

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public void Update_WhenRepositoryThrowsValidationException_PropagatesException()
    {
        _repository.Setup(r => r.UpdateAsync(It.IsAny<RelationshipType>()))
            .ThrowsAsync(new ValidationException("A RelationshipType with that name already exists."));

        Assert.ThrowsAsync<ValidationException>(() => _controller.Update(Guid.NewGuid(), new RelationshipTypeRequest("parentOf", null, null)));
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
            .ThrowsAsync(new ValidationException("The RelationshipType cannot be deleted because it is referenced by one or more KnowledgeRelations."));

        Assert.ThrowsAsync<ValidationException>(() => _controller.Delete(Guid.NewGuid()));
    }
}
