using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.Repositories;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;
using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Knowledge.Tests.Repositories;

[TestFixture]
public class NodeTypeRepositoryTests
{
    private MockableAppDbContext _db = null!;
    private NodeTypeRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _repository = new NodeTypeRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task GetAllAsync_ReturnsNodeTypesOrderedByName()
    {
        await _db.NodeType.AddRangeAsync(
            new NodeType { Id = Guid.NewGuid(), Name = "Place" },
            new NodeType { Id = Guid.NewGuid(), Name = "Animal" });
        await _db.SaveChangesAsync();

        var all = await _repository.GetAllAsync();

        Assert.That(all.Select(n => n.Name), Is.EqualTo(new[] { "Animal", "Place" }));
    }

    [Test]
    public async Task GetByIdAsync_WhenExists_ReturnsNodeType()
    {
        var nodeType = new NodeType { Id = Guid.NewGuid(), Name = "Person" };
        await _db.NodeType.AddAsync(nodeType);
        await _db.SaveChangesAsync();

        var found = await _repository.GetByIdAsync(nodeType.Id);

        Assert.That(found?.Name, Is.EqualTo("Person"));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var found = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.That(found, Is.Null);
    }

    [Test]
    public async Task CreateAsync_PersistsAndReturnsNodeType()
    {
        var nodeType = new NodeType { Id = Guid.NewGuid(), Name = "Person" };

        var created = await _repository.CreateAsync(nodeType);

        Assert.That(created, Is.SameAs(nodeType));
        Assert.That(await _db.NodeType.FindAsync(nodeType.Id), Is.Not.Null);
    }

    [Test]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        var updated = await _repository.UpdateAsync(new NodeType { Id = Guid.NewGuid(), Name = "Person" });

        Assert.That(updated, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_WithValidData_UpdatesAndReturnsNodeType()
    {
        var nodeType = new NodeType { Id = Guid.NewGuid(), Name = "Person", Description = "Old" };
        await _db.NodeType.AddAsync(nodeType);
        await _db.SaveChangesAsync();

        var updated = await _repository.UpdateAsync(new NodeType { Id = nodeType.Id, Name = "Person", Description = "New description" });

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Description, Is.EqualTo("New description"));
    }

    [Test]
    public async Task UpdateAsync_OnUniqueViolation_ThrowsValidationExceptionWithDuplicateNameMessage()
    {
        var nodeType = new NodeType { Id = Guid.NewGuid(), Name = "Place" };
        await _db.NodeType.AddAsync(nodeType);
        await _db.SaveChangesAsync();
        _db.ThrowOnSaveChanges(PostgresExceptionFactory.UniqueViolation());

        var ex = Assert.ThrowsAsync<ValidationException>(
            () => _repository.UpdateAsync(new NodeType { Id = nodeType.Id, Name = "Person" }));

        Assert.That(ex!.Message, Is.EqualTo("A NodeType with that name already exists."));
    }

    [Test]
    public async Task DeleteAsync_WhenExists_RemovesNodeTypeAndReturnsTrue()
    {
        var nodeType = new NodeType { Id = Guid.NewGuid(), Name = "Person" };
        await _db.NodeType.AddAsync(nodeType);
        await _db.SaveChangesAsync();

        var result = await _repository.DeleteAsync(nodeType.Id);

        Assert.That(result, Is.True);
        Assert.That(await _db.NodeType.AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeType.Id), Is.Null);
    }

    [Test]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        var result = await _repository.DeleteAsync(Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    [Test]
    public void CreateAsync_OnUniqueViolation_ThrowsValidationExceptionWithDuplicateNameMessage()
    {
        _db.ThrowOnSaveChanges(PostgresExceptionFactory.UniqueViolation());

        var ex = Assert.ThrowsAsync<ValidationException>(
            () => _repository.CreateAsync(new NodeType { Id = Guid.NewGuid(), Name = "Person" }));

        Assert.That(ex!.Message, Is.EqualTo("A NodeType with that name already exists."));
    }

    [Test]
    public async Task DeleteAsync_OnForeignKeyViolation_ThrowsValidationExceptionWithReferencedMessage()
    {
        var nodeType = new NodeType { Id = Guid.NewGuid(), Name = "Person" };
        await _db.NodeType.AddAsync(nodeType);
        await _db.SaveChangesAsync();
        _db.ThrowOnExecuteDelete<NodeType>(PostgresExceptionFactory.ForeignKeyViolation());

        var ex = Assert.ThrowsAsync<ValidationException>(() => _repository.DeleteAsync(nodeType.Id));

        Assert.That(ex!.Message, Is.EqualTo("The NodeType cannot be deleted because it is referenced by one or more KnowledgeNodes."));
    }
}
