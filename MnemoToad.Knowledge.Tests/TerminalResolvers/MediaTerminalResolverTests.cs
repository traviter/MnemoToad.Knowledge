using Moq;
using MockQueryable;
using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.QueryTransforms;
using MnemoToad.Knowledge.Data.TerminalResolvers;
using NUnit.Framework;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.TerminalResolvers;

[TestFixture]
public class MediaTerminalResolverTests
{
    private Mock<IQueryTransform<KnowledgeNode, KnowledgeNodeMedia>> _queryTransform = null!;
    private MediaTerminalResolver _resolver = null!;

    [SetUp]
    public void SetUp()
    {
        _queryTransform = new Mock<IQueryTransform<KnowledgeNode, KnowledgeNodeMedia>>();
        _resolver = new MediaTerminalResolver(_queryTransform.Object);
    }

    [Test]
    public async Task ResolveAsync_TransformYieldsRow_ReturnsItsJson()
    {
        var mediaAssetId = Guid.NewGuid();
        var row = new KnowledgeNodeMedia { KnowledgeNodeId = Guid.NewGuid(), Key = "flag", MediaAssetId = mediaAssetId, AltText = "A flag" };
        _queryTransform
            .Setup(t => t.Transform(It.IsAny<IQueryable<KnowledgeNode>>(), "flag"))
            .Returns(new[] { row }.BuildMock());

        var result = await _resolver.ResolveAsync(Enumerable.Empty<KnowledgeNode>().AsQueryable(), "flag");

        var success = result as Result<JsonNode>.Success;
        Assert.That(success, Is.Not.Null);
        var value = success!.Value.AsObject();
        Assert.That(value["id"]!.GetValue<string>(), Is.EqualTo(mediaAssetId.ToString()));
        Assert.That(value["alt_text"]!.GetValue<string>(), Is.EqualTo("A flag"));
    }

    [Test]
    public async Task ResolveAsync_TransformYieldsNoRows_ReturnsFailure()
    {
        _queryTransform
            .Setup(t => t.Transform(It.IsAny<IQueryable<KnowledgeNode>>(), "flag"))
            .Returns(Array.Empty<KnowledgeNodeMedia>().BuildMock());

        var result = await _resolver.ResolveAsync(Enumerable.Empty<KnowledgeNode>().AsQueryable(), "flag");

        var failure = result as Result<JsonNode>.Failure;
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Message, Is.EqualTo("Path could not be resolved."));
    }

    [Test]
    public async Task ResolveAsync_PassesSourceAndTerminalNameToTransform()
    {
        var source = Enumerable.Empty<KnowledgeNode>().AsQueryable();
        _queryTransform
            .Setup(t => t.Transform(It.IsAny<IQueryable<KnowledgeNode>>(), "flag"))
            .Returns(Array.Empty<KnowledgeNodeMedia>().BuildMock());

        await _resolver.ResolveAsync(source, "flag");

        _queryTransform.Verify(t => t.Transform(source, "flag"), Times.Once);
    }
}
