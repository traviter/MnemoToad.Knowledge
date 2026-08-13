using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.QueryTransforms;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.QueryTransforms;

[TestFixture]
public class AttributeQueryTransformTests
{
    private MockableAppDbContext _db = null!;
    private AttributeQueryTransform _transform = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _transform = new AttributeQueryTransform(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private IQueryable<KnowledgeNode> Source(Guid id) => _db.KnowledgeNode.Where(n => n.Id == id);

    [Test]
    public async Task Transform_MatchingKey_ReturnsAttributeRow()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        await _db.CreateKnowledgeNodeAttributeAsync(node.Id, "population", JsonValue.Create(68000000));

        var result = await _transform.Transform(Source(node.Id), "population").ToListAsync();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Value.GetValue<int>(), Is.EqualTo(68000000));
    }

    [Test]
    public async Task Transform_NonMatchingKey_ReturnsEmpty()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        await _db.CreateKnowledgeNodeAttributeAsync(node.Id, "population", JsonValue.Create(68000000));

        var result = await _transform.Transform(Source(node.Id), "gdp").ToListAsync();

        Assert.That(result, Is.Empty);
    }
}
