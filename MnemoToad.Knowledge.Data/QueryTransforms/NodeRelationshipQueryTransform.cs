using MnemoToad.Knowledge.Data.DbUtil;
using MnemoToad.Knowledge.Data.Entities;

namespace MnemoToad.Knowledge.Data.QueryTransforms;

public class NodeRelationshipQueryTransform : IQueryTransform<KnowledgeNode, KnowledgeNode>
{
    private readonly IAppDbContext _db;

    public NodeRelationshipQueryTransform(IAppDbContext db) => _db = db;

    public IQueryable<KnowledgeNode> Transform(IQueryable<KnowledgeNode> source, string name) =>
        from n in source
        join r in _db.KnowledgeRelation on n.Id equals r.SourceNodeId
        join rt in _db.RelationshipType on r.RelationshipTypeId equals rt.Id
        where rt.Name == name
        join target in _db.KnowledgeNode on r.TargetNodeId equals target.Id
        select target;
}
