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
        Assert.That(expression!.Hops, Is.Empty);
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
    public void TryParse_OneHopThenColumn_Succeeds()
    {
        var result = _parser.TryParse(">capital_canonicalName", out var expression);

        Assert.That(result, Is.True);
        Assert.That(expression!.Hops, Is.EqualTo(new[] { "capital" }));
        Assert.That(expression.TerminalKind, Is.EqualTo(PathTerminalKind.Column));
        Assert.That(expression.TerminalName, Is.EqualTo("canonicalName"));
    }

    [Test]
    public void TryParse_TwoHopsThenColumn_Succeeds()
    {
        var result = _parser.TryParse(">partOf>partOf_canonicalName", out var expression);

        Assert.That(result, Is.True);
        Assert.That(expression!.Hops, Is.EqualTo(new[] { "partOf", "partOf" }));
        Assert.That(expression.TerminalName, Is.EqualTo("canonicalName"));
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
    public void TryParse_HopWithNoTerminal_Fails()
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
    public void TryParse_HopNameContainingUnderscore_Fails()
    {
        // Documents a real, discovered grammar constraint: '_'/'.'/'#' are reserved terminal
        // markers, so a hop name containing one makes the string ambiguous to parse.
        var result = _parser.TryParse(">a_b.value", out _);

        Assert.That(result, Is.False);
    }
}
