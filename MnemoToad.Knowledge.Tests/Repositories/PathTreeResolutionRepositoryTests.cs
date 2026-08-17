using Moq;
using MockQueryable;
using MockQueryable.Moq;
using MnemoToad.Knowledge.Data;
using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.DbUtil;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.PathResolution;
using MnemoToad.Knowledge.Data.Repositories;
using MnemoToad.Knowledge.Data.TerminalResolvers;
using NUnit.Framework;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.Repositories;

[TestFixture]
public class PathTreeResolutionRepositoryTests
{
    private Mock<IAppDbContext> _db = null!;
    private Mock<IPathExpressionParser> _parser = null!;
    private Mock<ITerminalResolverFactory> _terminalResolverFactory = null!;
    private Mock<IQueryTransform<KnowledgeNode, KnowledgeNode>> _forwardEdgeQueryTransform = null!;
    private Mock<IQueryTransform<KnowledgeNode, KnowledgeNode>> _backwardEdgeQueryTransform = null!;
    private Mock<ITerminalResolver> _resolver = null!;
    private PathTreeResolutionRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IAppDbContext>();
        _db.Setup(d => d.KnowledgeNode).Returns(new List<KnowledgeNode>().BuildMockDbSet().Object);
        _parser = new Mock<IPathExpressionParser>();
        _terminalResolverFactory = new Mock<ITerminalResolverFactory>();
        _forwardEdgeQueryTransform = new Mock<IQueryTransform<KnowledgeNode, KnowledgeNode>>();
        _backwardEdgeQueryTransform = new Mock<IQueryTransform<KnowledgeNode, KnowledgeNode>>();
        _resolver = new Mock<ITerminalResolver>();
        _terminalResolverFactory.Setup(f => f.GetResolver(It.IsAny<PathTerminalKind>())).Returns(_resolver.Object);
        _repository = new PathTreeResolutionRepository(_db.Object, _parser.Object, _terminalResolverFactory.Object,
            _forwardEdgeQueryTransform.Object, _backwardEdgeQueryTransform.Object);
    }

    private void SetupParse(string path, PathExpression expression) =>
        _parser.Setup(p => p.TryParse(path, out expression)).Returns(true);

    private static IQueryable<KnowledgeNode> NodesWithIds(params Guid[] ids) =>
        ids.Select(id => new KnowledgeNode { Id = id }).ToList().BuildMock();

    [Test]
    public async Task ResolveTreeAsync_EdgeTraversalMatchesNothing_YieldsZeroRowsEvenWhenSiblingTerminalWouldResolve()
    {
        var nodeId = Guid.NewGuid();
        SetupParse(".canonicalName", new PathExpression([], PathTerminalKind.Attribute, "canonicalName"));
        SetupParse("<cityInCountry.canonicalName",
            new PathExpression([new PathEdge("cityInCountry", PathEdgeDirection.Backward)], PathTerminalKind.Attribute, "canonicalName"));
        _backwardEdgeQueryTransform
            .Setup(t => t.Transform(It.IsAny<IQueryable<KnowledgeNode>>(), "cityInCountry"))
            .Returns(NodesWithIds());
        _resolver.Setup(r => r.ResolveAsync(It.IsAny<IQueryable<KnowledgeNode>>(), "canonicalName"))
            .ReturnsAsync((Result<JsonNode>)JsonValue.Create("France")!);

        var rows = await _repository.ResolveTreeAsync([nodeId], [".canonicalName", "<cityInCountry.canonicalName"]);

        Assert.That(rows, Is.Empty);
        _terminalResolverFactory.Verify(f => f.GetResolver(It.IsAny<PathTerminalKind>()), Times.Never);
    }

    [Test]
    public async Task ResolveTreeAsync_TerminalFails_ProducesErrorEntryWithoutDroppingRowOrSiblings()
    {
        var nodeId = Guid.NewGuid();
        SetupParse(".canonicalName", new PathExpression([], PathTerminalKind.Attribute, "canonicalName"));
        SetupParse(".gdp", new PathExpression([], PathTerminalKind.Attribute, "gdp"));
        _resolver.Setup(r => r.ResolveAsync(It.IsAny<IQueryable<KnowledgeNode>>(), "canonicalName"))
            .ReturnsAsync((Result<JsonNode>)JsonValue.Create("France")!);
        _resolver.Setup(r => r.ResolveAsync(It.IsAny<IQueryable<KnowledgeNode>>(), "gdp"))
            .ReturnsAsync((Result<JsonNode>)new Error("Path could not be resolved."));

        var rows = await _repository.ResolveTreeAsync([nodeId], [".canonicalName", ".gdp"]);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].Properties[".canonicalName"]!.GetValue<string>(), Is.EqualTo("France"));
        Assert.That(rows[0].Errors, Is.Not.Null);
        Assert.That(rows[0].Errors![".gdp"], Is.EqualTo("Path could not be resolved."));
    }

    [Test]
    public async Task ResolveTreeAsync_TwoIndependentEdgeChildren_CrossProductsRowCounts()
    {
        var nodeId = Guid.NewGuid();
        SetupParse(">edgeA.canonicalName",
            new PathExpression([new PathEdge("edgeA", PathEdgeDirection.Forward)], PathTerminalKind.Attribute, "canonicalName"));
        SetupParse(">edgeB.canonicalName",
            new PathExpression([new PathEdge("edgeB", PathEdgeDirection.Forward)], PathTerminalKind.Attribute, "canonicalName"));
        _forwardEdgeQueryTransform
            .Setup(t => t.Transform(It.IsAny<IQueryable<KnowledgeNode>>(), "edgeA"))
            .Returns(NodesWithIds(Guid.NewGuid(), Guid.NewGuid()));
        _forwardEdgeQueryTransform
            .Setup(t => t.Transform(It.IsAny<IQueryable<KnowledgeNode>>(), "edgeB"))
            .Returns(NodesWithIds(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        _resolver.Setup(r => r.ResolveAsync(It.IsAny<IQueryable<KnowledgeNode>>(), "canonicalName"))
            .ReturnsAsync((Result<JsonNode>)JsonValue.Create("x")!);

        var rows = await _repository.ResolveTreeAsync([nodeId], [">edgeA.canonicalName", ">edgeB.canonicalName"]);

        Assert.That(rows, Has.Count.EqualTo(6));
        Assert.That(rows, Has.All.Matches<ResolvedNodeRow>(r => r.NodeId == nodeId));
    }

    [Test]
    public async Task ResolveTreeAsync_MultipleStartingNodes_EvaluatesEachIndependently()
    {
        var nodeId1 = Guid.NewGuid();
        var nodeId2 = Guid.NewGuid();
        SetupParse(".canonicalName", new PathExpression([], PathTerminalKind.Attribute, "canonicalName"));
        _resolver.Setup(r => r.ResolveAsync(It.IsAny<IQueryable<KnowledgeNode>>(), "canonicalName"))
            .ReturnsAsync((Result<JsonNode>)JsonValue.Create("x")!);

        var rows = await _repository.ResolveTreeAsync([nodeId1, nodeId2], [".canonicalName"]);

        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows.Select(r => r.NodeId), Is.EquivalentTo(new[] { nodeId1, nodeId2 }));
    }
}
