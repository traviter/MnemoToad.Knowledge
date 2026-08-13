using Moq;
using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.PathResolution;
using MnemoToad.Knowledge.Data.QueryTransforms;
using MnemoToad.Knowledge.Data.Repositories;
using MnemoToad.Knowledge.Data.TerminalResolvers;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.Repositories;

[TestFixture]
public class PathResolutionRepositoryTests
{
    private MockableAppDbContext _db = null!;
    private Mock<IPathExpressionParser> _parser = null!;
    private Mock<ITerminalResolverFactory> _terminalResolverFactory = null!;
    private Mock<IQueryTransform<KnowledgeNode, KnowledgeNode>> _edgeQueryTransform = null!;
    private Mock<ITerminalResolver> _resolver = null!;
    private PathResolutionRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _parser = new Mock<IPathExpressionParser>();
        _terminalResolverFactory = new Mock<ITerminalResolverFactory>();
        _edgeQueryTransform = new Mock<IQueryTransform<KnowledgeNode, KnowledgeNode>>();
        _edgeQueryTransform
            .Setup(t => t.Transform(It.IsAny<IQueryable<KnowledgeNode>>(), It.IsAny<string>()))
            .Returns((IQueryable<KnowledgeNode> source, string _) => source);
        _resolver = new Mock<ITerminalResolver>();
        _repository = new PathResolutionRepository(_db, _parser.Object, _terminalResolverFactory.Object, _edgeQueryTransform.Object);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private void SetupParse(string path, PathExpression? expression) =>
        _parser.Setup(p => p.TryParse(path, out expression)).Returns(true);

    private void SetupParseFailure(string path)
    {
        PathExpression? expression = null;
        _parser.Setup(p => p.TryParse(path, out expression)).Returns(false);
    }

    private void SetupResolver(PathTerminalKind kind, Result<JsonNode> result)
    {
        _resolver.Setup(r => r.ResolveAsync(It.IsAny<IQueryable<KnowledgeNode>>(), It.IsAny<string>())).ReturnsAsync(result);
        _terminalResolverFactory.Setup(f => f.GetResolver(kind)).Returns(_resolver.Object);
    }

    [Test]
    public async Task ResolveAsync_UnparseablePath_ReturnsErrorWithoutTouchingOtherCollaborators()
    {
        SetupParseFailure("not-a-valid-path");

        var result = await _repository.ResolveAsync(new PathResolutionQuery(Guid.NewGuid(), "not-a-valid-path"));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo("Invalid Path DSL syntax."));
        _terminalResolverFactory.Verify(f => f.GetResolver(It.IsAny<PathTerminalKind>()), Times.Never);
        _edgeQueryTransform.Verify(t => t.Transform(It.IsAny<IQueryable<KnowledgeNode>>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ResolveAsync_ZeroEdgePath_DispatchesToColumnResolverWithoutCallingEdgeTransform()
    {
        var nodeId = Guid.NewGuid();
        SetupParse("_canonicalName", new PathExpression([], PathTerminalKind.Column, "canonicalName"));
        SetupResolver(PathTerminalKind.Column, JsonValue.Create("France")!);

        var result = await _repository.ResolveAsync(new PathResolutionQuery(nodeId, "_canonicalName"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value!.GetValue<string>(), Is.EqualTo("France"));
        _terminalResolverFactory.Verify(f => f.GetResolver(PathTerminalKind.Column), Times.Once);
        _edgeQueryTransform.Verify(t => t.Transform(It.IsAny<IQueryable<KnowledgeNode>>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ResolveAsync_TerminalResolverReturnsFailure_ReturnsErrorMessage()
    {
        SetupParse(".gdp", new PathExpression([], PathTerminalKind.Attribute, "gdp"));
        SetupResolver(PathTerminalKind.Attribute, new Error("Path could not be resolved."));

        var result = await _repository.ResolveAsync(new PathResolutionQuery(Guid.NewGuid(), ".gdp"));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo("Path could not be resolved."));
    }

    [Test]
    public async Task ResolveAsync_AttributeTerminal_DispatchesToAttributeResolver()
    {
        SetupParse(".population", new PathExpression([], PathTerminalKind.Attribute, "population"));
        SetupResolver(PathTerminalKind.Attribute, JsonValue.Create(68000000)!);

        var result = await _repository.ResolveAsync(new PathResolutionQuery(Guid.NewGuid(), ".population"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value!.GetValue<int>(), Is.EqualTo(68000000));
        _terminalResolverFactory.Verify(f => f.GetResolver(PathTerminalKind.Attribute), Times.Once);
    }

    [Test]
    public async Task ResolveAsync_MediaTerminal_DispatchesToMediaResolver()
    {
        SetupParse("#flag", new PathExpression([], PathTerminalKind.Media, "flag"));
        SetupResolver(PathTerminalKind.Media, new JsonObject { ["id"] = "abc" });

        var result = await _repository.ResolveAsync(new PathResolutionQuery(Guid.NewGuid(), "#flag"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value!["id"]!.GetValue<string>(), Is.EqualTo("abc"));
        _terminalResolverFactory.Verify(f => f.GetResolver(PathTerminalKind.Media), Times.Once);
    }

    [Test]
    public async Task ResolveAsync_WithEdges_CallsEdgeTransformOncePerEdgeInOrder()
    {
        SetupParse(">continent>country_canonicalName", new PathExpression(["continent", "country"], PathTerminalKind.Column, "canonicalName"));
        SetupResolver(PathTerminalKind.Column, JsonValue.Create("France")!);

        var result = await _repository.ResolveAsync(new PathResolutionQuery(Guid.NewGuid(), ">continent>country_canonicalName"));

        Assert.That(result.Error, Is.Null);
        _edgeQueryTransform.Verify(t => t.Transform(It.IsAny<IQueryable<KnowledgeNode>>(), "continent"), Times.Once);
        _edgeQueryTransform.Verify(t => t.Transform(It.IsAny<IQueryable<KnowledgeNode>>(), "country"), Times.Once);
    }

    [Test]
    public async Task ResolveAsync_Batch_ResolvesEachQueryIndependentlyInOrder()
    {
        var nodeId = Guid.NewGuid();
        SetupParse("_canonicalName", new PathExpression([], PathTerminalKind.Column, "canonicalName"));
        SetupParse(".population", new PathExpression([], PathTerminalKind.Attribute, "population"));

        var columnResolver = new Mock<ITerminalResolver>();
        columnResolver.Setup(r => r.ResolveAsync(It.IsAny<IQueryable<KnowledgeNode>>(), "canonicalName"))
            .ReturnsAsync((Result<JsonNode>)JsonValue.Create("France")!);
        var attributeResolver = new Mock<ITerminalResolver>();
        attributeResolver.Setup(r => r.ResolveAsync(It.IsAny<IQueryable<KnowledgeNode>>(), "population"))
            .ReturnsAsync((Result<JsonNode>)new Error("Path could not be resolved."));

        _terminalResolverFactory.Setup(f => f.GetResolver(PathTerminalKind.Column)).Returns(columnResolver.Object);
        _terminalResolverFactory.Setup(f => f.GetResolver(PathTerminalKind.Attribute)).Returns(attributeResolver.Object);

        var results = await _repository.ResolveAsync(
        [
            new PathResolutionQuery(nodeId, "_canonicalName"),
            new PathResolutionQuery(nodeId, ".population")
        ]);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results[0].Value!.GetValue<string>(), Is.EqualTo("France"));
        Assert.That(results[0].Error, Is.Null);
        Assert.That(results[1].Value, Is.Null);
        Assert.That(results[1].Error, Is.EqualTo("Path could not be resolved."));
    }
}
