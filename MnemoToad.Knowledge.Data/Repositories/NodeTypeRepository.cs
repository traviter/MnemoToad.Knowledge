using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Entities;
using Npgsql;
using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Knowledge.Data.Repositories;

public class NodeTypeRepository : INodeTypeRepository
{
    private readonly IAppDbContext _db;

    public NodeTypeRepository(IAppDbContext db)
    {
        _db = db;
    }

    public Task<List<NodeType>> GetAllAsync() =>
        _db.NodeType.OrderBy(n => n.Name).ToListAsync();

    public async Task<NodeType?> GetByIdAsync(Guid id) =>
        await _db.NodeType.FindAsync(id);

    public async Task<NodeType> CreateAsync(NodeType nodeType)
    {
        _db.NodeType.Add(nodeType);
        await SaveChangesAsync();
        return nodeType;
    }

    public async Task<NodeType?> UpdateAsync(NodeType nodeType)
    {
        var existing = await GetByIdAsync(nodeType.Id);
        if (existing is null) return null;

        existing.Name = nodeType.Name;
        existing.Description = nodeType.Description;
        await SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            return await _db.ExecuteDeleteAsync(_db.NodeType.Where(n => n.Id == id)) > 0;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation
        })
        {
            throw new ValidationException("The NodeType cannot be deleted because it is referenced by one or more KnowledgeNodes.");
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
            throw new ValidationException("A NodeType with that name already exists.");
        }
    }
}
