using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Entities;
using Npgsql;
using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Knowledge.Data.Repositories;

public class RelationshipTypeRepository : IRelationshipTypeRepository
{
    private readonly IAppDbContext _db;

    public RelationshipTypeRepository(IAppDbContext db)
    {
        _db = db;
    }

    public Task<List<RelationshipType>> GetAllAsync() =>
        _db.RelationshipType.OrderBy(r => r.Name).ToListAsync();

    public async Task<RelationshipType?> GetByIdAsync(Guid id) =>
        await _db.RelationshipType.FindAsync(id);

    public async Task<RelationshipType> CreateAsync(RelationshipType relationshipType)
    {
        _db.RelationshipType.Add(relationshipType);
        await SaveChangesAsync();
        return relationshipType;
    }

    public async Task<RelationshipType?> UpdateAsync(RelationshipType relationshipType)
    {
        var existing = await GetByIdAsync(relationshipType.Id);
        if (existing is null) return null;

        existing.Name = relationshipType.Name;
        existing.InverseName = relationshipType.InverseName;
        existing.Description = relationshipType.Description;
        await SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            return await _db.ExecuteDeleteAsync(_db.RelationshipType.Where(r => r.Id == id)) > 0;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
            TableName: "knowledge_relation"
        })
        {
            throw new ValidationException("The RelationshipType cannot be deleted because it is referenced by one or more KnowledgeRelations.");
        }
    }

    private async Task SaveChangesAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        })
        {
            throw new ValidationException("A RelationshipType with that name already exists.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.CheckViolation,
            ConstraintName: "ck_relationship_type_name"
        })
        {
            throw new ValidationException("The RelationshipType Name must contain only letters.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.CheckViolation,
            ConstraintName: "ck_relationship_type_inverse_name"
        })
        {
            throw new ValidationException("The RelationshipType InverseName must contain only letters.");
        }
    }
}
