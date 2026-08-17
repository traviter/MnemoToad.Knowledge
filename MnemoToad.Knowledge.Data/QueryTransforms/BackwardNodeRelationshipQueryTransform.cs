using MnemoToad.Knowledge.Data.DbUtil;
using MnemoToad.Knowledge.Data.Entities;

namespace MnemoToad.Knowledge.Data.QueryTransforms;

public class BackwardNodeRelationshipQueryTransform : IQueryTransform<KnowledgeNode, KnowledgeNode>
{
    private readonly IAppDbContext _db;

    public BackwardNodeRelationshipQueryTransform(IAppDbContext db) => _db = db;

    public IQueryable<KnowledgeNode> Transform(IQueryable<KnowledgeNode> source, string name) =>
        from n in source
        join r in _db.KnowledgeRelation on n.Id equals r.TargetNodeId
        join rt in _db.RelationshipType on r.RelationshipTypeId equals rt.Id
        where rt.Name == name
        join target in _db.KnowledgeNode on r.SourceNodeId equals target.Id
        select target;
}
