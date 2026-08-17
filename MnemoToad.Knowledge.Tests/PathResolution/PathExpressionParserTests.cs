using MnemoToad.Knowledge.Data.PathResolution;
using NUnit.Framework;

namespace MnemoToad.Knowledge.Tests.PathResolution;

[TestFixture]
public class PathExpressionParserTests
{
    private PathExpressionParser _parser = null!;

    [SetUp]
    public void SetUp() => _parser = new PathExpressionParser();

    [Test]
    public void TryParse_Column_Succeeds()
    {
        var result = _parser.TryParse("_canonicalName", out var expression);

        Assert.That(result, Is.True);
        Assert.That(expression!.Edges, Is.Empty);
        Assert.That(expression.TerminalKind, Is.EqualTo(PathTerminalKind.Column));
        Assert.That(expression.TerminalName, Is.EqualTo("canonicalName"));
    }

    [Test]
    public void TryParse_Attribute_Succeeds()
    {
        var result = _parser.TryParse(".population", out var expression);

        Assert.That(result, Is.True);
        Assert.That(expression!.TerminalKind, Is.EqualTo(PathTerminalKind.Attribute));
        Assert.That(expression.TerminalName, Is.EqualTo("population"));
    }

    [Test]
    public void TryParse_Media_Succeeds()
    {
        var result = _parser.TryParse("#flag", out var expression);

        Assert.That(result, Is.True);
        Assert.That(expression!.TerminalKind, Is.EqualTo(PathTerminalKind.Media));
        Assert.That(expression.TerminalName, Is.EqualTo("flag"));
    }

    [Test]
    public void TryParse_OneEdgeThenColumn_Succeeds()
    {
        var result = _parser.TryParse(">capital_canonicalName", out var expression);

        Assert.That(result, Is.True);
        Assert.That(expression!.Edges, Is.EqualTo(new[] { new PathEdge("capital", PathEdgeDirection.Forward) }));
        Assert.That(expression.TerminalKind, Is.EqualTo(PathTerminalKind.Column));
        Assert.That(expression.TerminalName, Is.EqualTo("canonicalName"));
    }

    [Test]
    public void TryParse_TwoEdgesThenColumn_Succeeds()
    {
        var result = _parser.TryParse(">partOf>partOf_canonicalName", out var expression);

        Assert.That(result, Is.True);
        Assert.That(expression!.Edges, Is.EqualTo(new[]
        {
            new PathEdge("partOf", PathEdgeDirection.Forward),
            new PathEdge("partOf", PathEdgeDirection.Forward)
        }));
        Assert.That(expression.TerminalName, Is.EqualTo("canonicalName"));
    }

    [Test]
    public void TryParse_OneBackwardEdgeThenColumn_Succeeds()
    {
        var result = _parser.TryParse("<capital_canonicalName", out var expression);

        Assert.That(result, Is.True);
        Assert.That(expression!.Edges, Is.EqualTo(new[] { new PathEdge("capital", PathEdgeDirection.Backward) }));
        Assert.That(expression.TerminalKind, Is.EqualTo(PathTerminalKind.Column));
        Assert.That(expression.TerminalName, Is.EqualTo("canonicalName"));
    }

    [Test]
    public void TryParse_MixedForwardAndBackwardEdges_Succeeds()
    {
        var result = _parser.TryParse(">partOf<capital_canonicalName", out var expression);

        Assert.That(result, Is.True);
        Assert.That(expression!.Edges, Is.EqualTo(new[]
        {
            new PathEdge("partOf", PathEdgeDirection.Forward),
            new PathEdge("capital", PathEdgeDirection.Backward)
        }));
        Assert.That(expression.TerminalName, Is.EqualTo("canonicalName"));
    }

    [Test]
    public void TryParse_BackwardEdgeWithNoTerminal_Fails()
    {
        var result = _parser.TryParse("<capital", out _);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParse_BackwardEdgeNameContainingReservedCharacter_Fails()
    {
        var result = _parser.TryParse("<a_b.value", out _);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParse_EmptyString_Fails()
    {
        var result = _parser.TryParse("", out var expression);

        Assert.That(result, Is.False);
        Assert.That(expression, Is.Null);
    }

    [Test]
    public void TryParse_NoTerminalMarker_Fails()
    {
        var result = _parser.TryParse("not-a-valid-path", out _);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParse_EdgeWithNoTerminal_Fails()
    {
        var result = _parser.TryParse(">capital", out _);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParse_MarkerWithEmptyName_Fails()
    {
        var result = _parser.TryParse("_", out _);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParse_EdgeNameContainingUnderscore_Fails()
    {
        // Documents a real, discovered grammar constraint: '_'/'.'/'#' are reserved terminal
        // markers, so an edge name containing one makes the string ambiguous to parse.
        var result = _parser.TryParse(">a_b.value", out _);

        Assert.That(result, Is.False);
    }
}
