using MnemoToad.Knowledge.Data.Entities;

namespace MnemoToad.Knowledge.Data.QueryTransforms;

public class MediaQueryTransform : IQueryTransform<KnowledgeNode, KnowledgeNodeMedia>
{
    private readonly IAppDbContext _db;

    public MediaQueryTransform(IAppDbContext db) => _db = db;

    public IQueryable<KnowledgeNodeMedia> Transform(IQueryable<KnowledgeNode> source, string name) =>
        from n in source
        join m in _db.KnowledgeNodeMedia on n.Id equals m.KnowledgeNodeId
        where m.Key == name
        select m;
}
