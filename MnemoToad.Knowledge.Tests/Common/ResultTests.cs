using MnemoToad.Knowledge.Data.Common;
using NUnit.Framework;

namespace MnemoToad.Knowledge.Tests.Common;

[TestFixture]
public class ResultTests
{
    [Test]
    public void ImplicitConversion_FromValue_ProducesSuccess()
    {
        Result<string> result = "France";

        var success = result as Result<string>.Success;
        Assert.That(success, Is.Not.Null);
        Assert.That(success!.Value, Is.EqualTo("France"));
    }

    [Test]
    public void ImplicitConversion_FromError_ProducesFailure()
    {
        Result<string> result = new Error("Path could not be resolved.");

        var failure = result as Result<string>.Failure;
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Message, Is.EqualTo("Path could not be resolved."));
    }

    [Test]
    public void PatternMatching_DistinguishesSuccessFromFailure()
    {
        Result<int> success = 42;
        Result<int> failure = new Error("boom");

        Assert.That(success is Result<int>.Success, Is.True);
        Assert.That(success is Result<int>.Failure, Is.False);
        Assert.That(failure is Result<int>.Failure, Is.True);
        Assert.That(failure is Result<int>.Success, Is.False);
    }
}
