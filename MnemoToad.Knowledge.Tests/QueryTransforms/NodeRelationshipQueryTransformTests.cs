using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.QueryTransforms;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;

namespace MnemoToad.Knowledge.Tests.QueryTransforms;

[TestFixture]
public class NodeRelationshipQueryTransformTests
{
    private MockableAppDbContext _db = null!;
    private NodeRelationshipQueryTransform _transform = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _transform = new NodeRelationshipQueryTransform(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private IQueryable<KnowledgeNode> Source(Guid id) => _db.KnowledgeNode.Where(n => n.Id == id);

    [Test]
    public async Task Transform_MatchingRelationshipName_FollowsToTargetNode()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var source = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var target = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Paris");
        var relationshipType = await _db.CreateRelationshipTypeAsync(name: "capital");
        await _db.CreateKnowledgeRelationAsync(source.Id, relationshipType.Id, target.Id);

        var result = await _transform.Transform(Source(source.Id), "capital").ToListAsync();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].CanonicalName, Is.EqualTo("Paris"));
    }

    [Test]
    public async Task Transform_NoMatchingRelationshipName_ReturnsEmpty()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var source = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var target = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var relationshipType = await _db.CreateRelationshipTypeAsync(name: "capital");
        await _db.CreateKnowledgeRelationAsync(source.Id, relationshipType.Id, target.Id);

        var result = await _transform.Transform(Source(source.Id), "doesNotExist").ToListAsync();

        Assert.That(result, Is.Empty);
    }
}
