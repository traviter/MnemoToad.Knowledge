using MnemoToad.Knowledge.Data.Entities;

namespace MnemoToad.Knowledge.Data.Repositories;

public interface IKnowledgeNodeRepository
{
    Task<List<KnowledgeNode>> GetAllAsync(string nodeTypeName);
    Task<KnowledgeNode?> GetByIdAsync(Guid id);
    Task<KnowledgeNode> CreateAsync(KnowledgeNode knowledgeNode);
    Task<KnowledgeNode?> UpdateAsync(KnowledgeNode knowledgeNode);
    Task<bool> DeleteAsync(Guid id);
}
