using MnemoToad.Knowledge.Api.Contracts;
using NUnit.Framework;

namespace MnemoToad.Knowledge.Tests.Contracts;

[TestFixture]
public class ValidPathExpressionAttributeTests
{
    private ValidPathExpressionAttribute _attribute = null!;

    [SetUp]
    public void SetUp() => _attribute = new ValidPathExpressionAttribute();

    [Test]
    public void IsValid_AllPathsValid_ReturnsTrue()
    {
        var result = _attribute.IsValid(new List<string> { "_canonicalName", ".population", "#flag" });

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsValid_OneInvalidEntry_ReturnsFalse()
    {
        var result = _attribute.IsValid(new List<string> { "_canonicalName", "not-a-valid-path" });

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValid_EmptyList_ReturnsTrue()
    {
        // MinLength owns rejecting an empty list; this attribute only cares about syntax.
        var result = _attribute.IsValid(new List<string>());

        Assert.That(result, Is.True);
    }
}
