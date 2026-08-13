using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.QueryTransforms;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;

namespace MnemoToad.Knowledge.Tests.QueryTransforms;

[TestFixture]
public class MediaQueryTransformTests
{
    private MockableAppDbContext _db = null!;
    private MediaQueryTransform _transform = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _transform = new MediaQueryTransform(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private IQueryable<KnowledgeNode> Source(Guid id) => _db.KnowledgeNode.Where(n => n.Id == id);

    [Test]
    public async Task Transform_MatchingKey_ReturnsMediaRow()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var mediaAsset = await _db.CreateMediaAssetAsync();
        await _db.CreateKnowledgeNodeMediaAsync(node.Id, "flag", mediaAsset.Id, "A flag");

        var result = await _transform.Transform(Source(node.Id), "flag").ToListAsync();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].AltText, Is.EqualTo("A flag"));
    }

    [Test]
    public async Task Transform_NonMatchingKey_ReturnsEmpty()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var mediaAsset = await _db.CreateMediaAssetAsync();
        await _db.CreateKnowledgeNodeMediaAsync(node.Id, "flag", mediaAsset.Id, "A flag");

        var result = await _transform.Transform(Source(node.Id), "photo").ToListAsync();

        Assert.That(result, Is.Empty);
    }
}
