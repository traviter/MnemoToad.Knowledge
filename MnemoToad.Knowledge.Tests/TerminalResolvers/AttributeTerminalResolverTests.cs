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
public class AttributeTerminalResolverTests
{
    private Mock<IQueryTransform<KnowledgeNode, KnowledgeNodeAttribute>> _queryTransform = null!;
    private AttributeTerminalResolver _resolver = null!;

    [SetUp]
    public void SetUp()
    {
        _queryTransform = new Mock<IQueryTransform<KnowledgeNode, KnowledgeNodeAttribute>>();
        _resolver = new AttributeTerminalResolver(_queryTransform.Object);
    }

    [Test]
    public async Task ResolveAsync_TransformYieldsRow_ReturnsItsValue()
    {
        var row = new KnowledgeNodeAttribute { KnowledgeNodeId = Guid.NewGuid(), Key = "population", Value = JsonValue.Create(68000000)! };
        _queryTransform
            .Setup(t => t.Transform(It.IsAny<IQueryable<KnowledgeNode>>(), "population"))
            .Returns(new[] { row }.BuildMock());

        var result = await _resolver.ResolveAsync(Enumerable.Empty<KnowledgeNode>().AsQueryable(), "population");

        var success = result as Result<JsonNode>.Success;
        Assert.That(success, Is.Not.Null);
        Assert.That(success!.Value.GetValue<int>(), Is.EqualTo(68000000));
    }

    [Test]
    public async Task ResolveAsync_TransformYieldsNoRows_ReturnsFailure()
    {
        _queryTransform
            .Setup(t => t.Transform(It.IsAny<IQueryable<KnowledgeNode>>(), "gdp"))
            .Returns(Array.Empty<KnowledgeNodeAttribute>().BuildMock());

        var result = await _resolver.ResolveAsync(Enumerable.Empty<KnowledgeNode>().AsQueryable(), "gdp");

        var failure = result as Result<JsonNode>.Failure;
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Message, Is.EqualTo("Path could not be resolved."));
    }

    [Test]
    public async Task ResolveAsync_PassesSourceAndTerminalNameToTransform()
    {
        var source = Enumerable.Empty<KnowledgeNode>().AsQueryable();
        _queryTransform
            .Setup(t => t.Transform(It.IsAny<IQueryable<KnowledgeNode>>(), "population"))
            .Returns(Array.Empty<KnowledgeNodeAttribute>().BuildMock());

        await _resolver.ResolveAsync(source, "population");

        _queryTransform.Verify(t => t.Transform(source, "population"), Times.Once);
    }
}
