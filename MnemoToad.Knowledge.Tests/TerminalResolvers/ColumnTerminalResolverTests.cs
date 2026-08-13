using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.TerminalResolvers;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.TerminalResolvers;

[TestFixture]
public class ColumnTerminalResolverTests
{
    private MockableAppDbContext _db = null!;
    private ColumnTerminalResolver _resolver = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _resolver = new ColumnTerminalResolver();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private IQueryable<KnowledgeNode> TargetNode(Guid id) => _db.KnowledgeNode.Where(n => n.Id == id);

    [Test]
    public async Task ResolveAsync_UnknownColumnName_ReturnsFailure()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);

        var result = await _resolver.ResolveAsync(TargetNode(node.Id), "bogus");

        var failure = result as Result<JsonNode>.Failure;
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Message, Is.EqualTo("No column named 'bogus' on KnowledgeNode."));
    }

    [Test]
    public async Task ResolveAsync_NoMatchingNode_ReturnsFailure()
    {
        var result = await _resolver.ResolveAsync(TargetNode(Guid.NewGuid()), "canonicalName");

        var failure = result as Result<JsonNode>.Failure;
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Message, Is.EqualTo("Path could not be resolved."));
    }

    [Test]
    public async Task ResolveAsync_Id_ReturnsNodeIdAsString()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);

        var result = await _resolver.ResolveAsync(TargetNode(node.Id), "id");

        var success = result as Result<JsonNode>.Success;
        Assert.That(success, Is.Not.Null);
        Assert.That(success!.Value.GetValue<string>(), Is.EqualTo(node.Id.ToString()));
    }

    [Test]
    public async Task ResolveAsync_CanonicalName_ReturnsStoredName()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "France");

        var result = await _resolver.ResolveAsync(TargetNode(node.Id), "canonicalName");

        var success = result as Result<JsonNode>.Success;
        Assert.That(success, Is.Not.Null);
        Assert.That(success!.Value.GetValue<string>(), Is.EqualTo("France"));
    }

    [Test]
    public async Task ResolveAsync_DescriptionWhenSet_ReturnsStoredDescription()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id, description: "A country in Western Europe.");

        var result = await _resolver.ResolveAsync(TargetNode(node.Id), "description");

        var success = result as Result<JsonNode>.Success;
        Assert.That(success, Is.Not.Null);
        Assert.That(success!.Value.GetValue<string>(), Is.EqualTo("A country in Western Europe."));
    }

    [Test]
    public async Task ResolveAsync_DescriptionWhenNull_ReturnsFailure()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id, description: null);

        var result = await _resolver.ResolveAsync(TargetNode(node.Id), "description");

        var failure = result as Result<JsonNode>.Failure;
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Message, Is.EqualTo("Path could not be resolved."));
    }
}
