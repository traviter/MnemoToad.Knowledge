using MnemoToad.Knowledge.Data.PathResolution;
using NUnit.Framework;

namespace MnemoToad.Knowledge.Tests.PathResolution;

[TestFixture]
public class PathTrieNodeTests
{
    private PathExpressionParser _parser = null!;

    [SetUp]
    public void SetUp() => _parser = new PathExpressionParser();

    private PathTrieNode BuildTrie(params string[] paths)
    {
        var expressions = new Dictionary<string, PathExpression>();
        foreach (var path in paths)
        {
            Assert.That(_parser.TryParse(path, out var expression), Is.True, $"'{path}' failed to parse.");
            expressions[path] = expression!;
        }
        return PathTrieNode.Build(expressions);
    }

    [Test]
    public void Build_TwoPathsSharingEdgePrefix_MergeIntoOneChild()
    {
        var root = BuildTrie("<cityInCountry.canonicalName", "<cityInCountry>cityInState.canonicalName");

        Assert.That(root.Children, Has.Count.EqualTo(1));
        var cityNode = root.Children[new PathEdge("cityInCountry", PathEdgeDirection.Backward)];
        Assert.That(cityNode.Terminals, Has.Count.EqualTo(1));
        Assert.That(cityNode.Terminals[0].OriginalPath, Is.EqualTo("<cityInCountry.canonicalName"));
        Assert.That(cityNode.Children, Has.Count.EqualTo(1));
        var stateNode = cityNode.Children[new PathEdge("cityInState", PathEdgeDirection.Forward)];
        Assert.That(stateNode.Terminals[0].OriginalPath, Is.EqualTo("<cityInCountry>cityInState.canonicalName"));
    }

    [Test]
    public void Build_TwoPathsWithNoSharedPrefix_ProduceTwoChildren()
    {
        var root = BuildTrie(">capital.canonicalName", ">continent.canonicalName");

        Assert.That(root.Children, Has.Count.EqualTo(2));
        Assert.That(root.Children.Keys, Is.EquivalentTo(new[]
        {
            new PathEdge("capital", PathEdgeDirection.Forward),
            new PathEdge("continent", PathEdgeDirection.Forward)
        }));
    }

    [Test]
    public void Build_ZeroEdgePath_AttachesTerminalDirectlyOnRoot()
    {
        var root = BuildTrie(".canonicalName");

        Assert.That(root.Children, Is.Empty);
        Assert.That(root.Terminals, Has.Count.EqualTo(1));
        Assert.That(root.Terminals[0], Is.EqualTo((PathTerminalKind.Attribute, "canonicalName", ".canonicalName")));
    }

    [Test]
    public void Build_ForwardAndBackwardEdgeSameName_ProduceTwoDistinctChildren()
    {
        var root = BuildTrie(">cityInCountry.canonicalName", "<cityInCountry.canonicalName");

        Assert.That(root.Children, Has.Count.EqualTo(2));
        Assert.That(root.Children.Keys, Is.EquivalentTo(new[]
        {
            new PathEdge("cityInCountry", PathEdgeDirection.Forward),
            new PathEdge("cityInCountry", PathEdgeDirection.Backward)
        }));
    }

    [Test]
    public void Build_MultiplePathsEndingAtSameNode_AttachAllTerminalsThere()
    {
        var root = BuildTrie(">capital_canonicalName", ">capital.population", ">capital#flag");

        var capitalNode = root.Children[new PathEdge("capital", PathEdgeDirection.Forward)];
        Assert.That(capitalNode.Terminals, Has.Count.EqualTo(3));
        Assert.That(capitalNode.Terminals.Select(t => t.Kind), Is.EquivalentTo(new[]
        {
            PathTerminalKind.Column, PathTerminalKind.Attribute, PathTerminalKind.Media
        }));
    }
}
