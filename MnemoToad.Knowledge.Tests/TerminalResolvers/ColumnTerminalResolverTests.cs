using MockQueryable;
using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.TerminalResolvers;
using NUnit.Framework;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.TerminalResolvers;

[TestFixture]
public class ColumnTerminalResolverTests
{
    private ColumnTerminalResolver _resolver = null!;

    [SetUp]
    public void SetUp() => _resolver = new ColumnTerminalResolver();

    private static IQueryable<KnowledgeNode> TargetNode(KnowledgeNode node) => new[] { node }.BuildMock();

    [Test]
    public async Task ResolveAsync_UnknownColumnName_ReturnsFailure()
    {
        var result = await _resolver.ResolveAsync(Array.Empty<KnowledgeNode>().BuildMock(), "bogus");

        var failure = result as Result<JsonNode>.Failure;
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Message, Is.EqualTo("No column named 'bogus' on KnowledgeNode."));
    }

    [Test]
    public async Task ResolveAsync_NoMatchingNode_ReturnsFailure()
    {
        var result = await _resolver.ResolveAsync(Array.Empty<KnowledgeNode>().BuildMock(), "canonicalName");

        var failure = result as Result<JsonNode>.Failure;
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Message, Is.EqualTo("Path could not be resolved."));
    }

    [Test]
    public async Task ResolveAsync_Id_ReturnsNodeIdAsString()
    {
        var node = new KnowledgeNode { CanonicalName = "France" };

        var result = await _resolver.ResolveAsync(TargetNode(node), "id");

        var success = result as Result<JsonNode>.Success;
        Assert.That(success, Is.Not.Null);
        Assert.That(success!.Value.GetValue<string>(), Is.EqualTo(node.Id.ToString()));
    }

    [Test]
    public async Task ResolveAsync_CanonicalName_ReturnsStoredName()
    {
        var node = new KnowledgeNode { CanonicalName = "France" };

        var result = await _resolver.ResolveAsync(TargetNode(node), "canonicalName");

        var success = result as Result<JsonNode>.Success;
        Assert.That(success, Is.Not.Null);
        Assert.That(success!.Value.GetValue<string>(), Is.EqualTo("France"));
    }

    [Test]
    public async Task ResolveAsync_DescriptionWhenSet_ReturnsStoredDescription()
    {
        var node = new KnowledgeNode { CanonicalName = "France", Description = "A country in Western Europe." };

        var result = await _resolver.ResolveAsync(TargetNode(node), "description");

        var success = result as Result<JsonNode>.Success;
        Assert.That(success, Is.Not.Null);
        Assert.That(success!.Value.GetValue<string>(), Is.EqualTo("A country in Western Europe."));
    }

    [Test]
    public async Task ResolveAsync_DescriptionWhenNull_ReturnsFailure()
    {
        var node = new KnowledgeNode { CanonicalName = "France", Description = null };

        var result = await _resolver.ResolveAsync(TargetNode(node), "description");

        var failure = result as Result<JsonNode>.Failure;
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Message, Is.EqualTo("Path could not be resolved."));
    }
}
