using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.QueryTransforms;
using MnemoToad.Knowledge.Data.TerminalResolvers;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.Components;

[TestFixture]
public class AttributeTerminalResolverTests
{
    private MockableAppDbContext _db = null!;
    private AttributeTerminalResolver _resolver = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _resolver = new AttributeTerminalResolver(new AttributeQueryTransform(_db));
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private IQueryable<KnowledgeNode> TargetNode(Guid id) => _db.KnowledgeNode.Where(n => n.Id == id);

    [Test]
    public async Task ResolveAsync_AttributeExists_ReturnsStoredValue()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        await _db.CreateKnowledgeNodeAttributeAsync(node.Id, "population", JsonValue.Create(68000000));

        var result = await _resolver.ResolveAsync(TargetNode(node.Id), "population");

        var success = result as Result<JsonNode>.Success;
        Assert.That(success, Is.Not.Null);
        Assert.That(success!.Value.GetValue<int>(), Is.EqualTo(68000000));
    }

    [Test]
    public async Task ResolveAsync_AttributeMissing_ReturnsFailure()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);

        var result = await _resolver.ResolveAsync(TargetNode(node.Id), "gdp");

        var failure = result as Result<JsonNode>.Failure;
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Message, Is.EqualTo("Path could not be resolved."));
    }

    [Test]
    public async Task ResolveAsync_NoMatchingNode_ReturnsFailure()
    {
        var result = await _resolver.ResolveAsync(TargetNode(Guid.NewGuid()), "population");

        var failure = result as Result<JsonNode>.Failure;
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Message, Is.EqualTo("Path could not be resolved."));
    }
}
