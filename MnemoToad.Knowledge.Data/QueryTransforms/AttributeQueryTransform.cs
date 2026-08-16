using MnemoToad.Knowledge.Data.DbUtil;
using MnemoToad.Knowledge.Data.Entities;

namespace MnemoToad.Knowledge.Data.QueryTransforms;

public class AttributeQueryTransform : IQueryTransform<KnowledgeNode, KnowledgeNodeAttribute>
{
    private readonly IAppDbContext _db;

    public AttributeQueryTransform(IAppDbContext db) => _db = db;

    public IQueryable<KnowledgeNodeAttribute> Transform(IQueryable<KnowledgeNode> source, string name) =>
        from n in source
        join a in _db.KnowledgeNodeAttribute on n.Id equals a.KnowledgeNodeId
        where a.Key == name
        select a;
}
