using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.Repositories;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;
using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Knowledge.Tests.Repositories;

[TestFixture]
public class RelationshipTypeRepositoryTests
{
    private MockableAppDbContext _db = null!;
    private RelationshipTypeRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _repository = new RelationshipTypeRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task GetAllAsync_ReturnsRelationshipTypesOrderedByName()
    {
        await _db.RelationshipType.AddRangeAsync(
            new RelationshipType { Id = Guid.NewGuid(), Name = "parentOf" },
            new RelationshipType { Id = Guid.NewGuid(), Name = "hasCapital" });
        await _db.SaveChangesAsync();

        var all = await _repository.GetAllAsync();

        Assert.That(all.Select(r => r.Name), Is.EqualTo(new[] { "hasCapital", "parentOf" }));
    }

    [Test]
    public async Task GetByIdAsync_WhenExists_ReturnsRelationshipType()
    {
        var relationshipType = new RelationshipType { Id = Guid.NewGuid(), Name = "parentOf" };
        await _db.RelationshipType.AddAsync(relationshipType);
        await _db.SaveChangesAsync();

        var found = await _repository.GetByIdAsync(relationshipType.Id);

        Assert.That(found?.Name, Is.EqualTo("parentOf"));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var found = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.That(found, Is.Null);
    }

    [Test]
    public async Task CreateAsync_PersistsAndReturnsRelationshipType()
    {
        var relationshipType = new RelationshipType { Id = Guid.NewGuid(), Name = "parentOf" };

        var created = await _repository.CreateAsync(relationshipType);

        Assert.That(created, Is.SameAs(relationshipType));
        Assert.That(await _db.RelationshipType.FindAsync(relationshipType.Id), Is.Not.Null);
    }

    [Test]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        var updated = await _repository.UpdateAsync(new RelationshipType { Id = Guid.NewGuid(), Name = "parentOf" });

        Assert.That(updated, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_WithValidData_UpdatesAndReturnsRelationshipType()
    {
        var relationshipType = new RelationshipType { Id = Guid.NewGuid(), Name = "parentOf", Description = "Old" };
        await _db.RelationshipType.AddAsync(relationshipType);
        await _db.SaveChangesAsync();

        var updated = await _repository.UpdateAsync(new RelationshipType
        {
            Id = relationshipType.Id,
            Name = "parentOf",
            Description = "New description"
        });

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Description, Is.EqualTo("New description"));
    }

    [Test]
    public async Task DeleteAsync_WhenExists_RemovesRelationshipTypeAndReturnsTrue()
    {
        var relationshipType = new RelationshipType { Id = Guid.NewGuid(), Name = "parentOf" };
        await _db.RelationshipType.AddAsync(relationshipType);
        await _db.SaveChangesAsync();

        var result = await _repository.DeleteAsync(relationshipType.Id);

        Assert.That(result, Is.True);
        Assert.That(await _db.RelationshipType.AsNoTracking().FirstOrDefaultAsync(r => r.Id == relationshipType.Id), Is.Null);
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
            () => _repository.CreateAsync(new RelationshipType { Id = Guid.NewGuid(), Name = "parentOf" }));

        Assert.That(ex!.Message, Is.EqualTo("A RelationshipType with that name already exists."));
    }

    [Test]
    public async Task DeleteAsync_OnForeignKeyViolation_ThrowsValidationExceptionWithReferencedMessage()
    {
        var relationshipType = new RelationshipType { Id = Guid.NewGuid(), Name = "parentOf" };
        await _db.RelationshipType.AddAsync(relationshipType);
        await _db.SaveChangesAsync();
        _db.ThrowOnExecuteDelete<RelationshipType>(PostgresExceptionFactory.ForeignKeyViolation(tableName: "knowledge_relation"));

        var ex = Assert.ThrowsAsync<ValidationException>(() => _repository.DeleteAsync(relationshipType.Id));

        Assert.That(ex!.Message, Is.EqualTo("The RelationshipType cannot be deleted because it is referenced by one or more KnowledgeRelations."));
    }

    [Test]
    public void CreateAsync_OnNameCheckViolation_ThrowsValidationExceptionAboutLettersOnly()
    {
        _db.ThrowOnSaveChanges(PostgresExceptionFactory.CheckViolation(constraintName: "ck_relationship_type_name"));

        var ex = Assert.ThrowsAsync<ValidationException>(
            () => _repository.CreateAsync(new RelationshipType { Id = Guid.NewGuid(), Name = "hasCapital1" }));

        Assert.That(ex!.Message, Is.EqualTo("The RelationshipType Name must contain only letters."));
    }
}
