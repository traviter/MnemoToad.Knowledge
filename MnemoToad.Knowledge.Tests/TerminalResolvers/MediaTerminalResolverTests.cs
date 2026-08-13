using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.QueryTransforms;
using MnemoToad.Knowledge.Data.TerminalResolvers;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.TerminalResolvers;

[TestFixture]
public class MediaTerminalResolverTests
{
    private MockableAppDbContext _db = null!;
    private MediaTerminalResolver _resolver = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _resolver = new MediaTerminalResolver(new MediaQueryTransform(_db));
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private IQueryable<KnowledgeNode> TargetNode(Guid id) => _db.KnowledgeNode.Where(n => n.Id == id);

    [Test]
    public async Task ResolveAsync_MediaWithNoExtraFields_ReturnsIdAndAltTextOnly()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var mediaAsset = await _db.CreateMediaAssetAsync();
        await _db.CreateKnowledgeNodeMediaAsync(node.Id, "flag", mediaAsset.Id, "A flag");

        var result = await _resolver.ResolveAsync(TargetNode(node.Id), "flag");

        var success = result as Result<JsonNode>.Success;
        Assert.That(success, Is.Not.Null);
        var value = success!.Value.AsObject();
        Assert.That(value["id"]!.GetValue<string>(), Is.EqualTo(mediaAsset.Id.ToString()));
        Assert.That(value["alt_text"]!.GetValue<string>(), Is.EqualTo("A flag"));
    }

    [Test]
    public async Task ResolveAsync_MediaWithExtraFields_ReturnsAllFieldsFlat()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var mediaAsset = await _db.CreateMediaAssetAsync();
        await _db.CreateKnowledgeNodeMediaAsync(node.Id, "coatOfArms", mediaAsset.Id, "A coat of arms",
            new JsonObject { ["id"] = mediaAsset.Id.ToString(), ["alt_text"] = "A coat of arms", ["credit"] = "Wikimedia Commons" });

        var result = await _resolver.ResolveAsync(TargetNode(node.Id), "coatOfArms");

        var success = result as Result<JsonNode>.Success;
        Assert.That(success, Is.Not.Null);
        var value = success!.Value.AsObject();
        Assert.That(value["credit"]!.GetValue<string>(), Is.EqualTo("Wikimedia Commons"));
        Assert.That(value.ContainsKey("metadata"), Is.False);
    }

    [Test]
    public async Task ResolveAsync_MediaMissing_ReturnsFailure()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);

        var result = await _resolver.ResolveAsync(TargetNode(node.Id), "flag");

        var failure = result as Result<JsonNode>.Failure;
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Message, Is.EqualTo("Path could not be resolved."));
    }
}
