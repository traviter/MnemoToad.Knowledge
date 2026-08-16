using MnemoToad.Knowledge.Data.Entities;

namespace MnemoToad.Knowledge.Data.Repositories;

public interface INodeTypeRepository
{
    Task<List<NodeType>> GetAllAsync();
    Task<NodeType?> GetByIdAsync(Guid id);
    Task<NodeType> CreateAsync(NodeType nodeType);
    Task<NodeType?> UpdateAsync(NodeType nodeType);
    Task<bool> DeleteAsync(Guid id);
}
