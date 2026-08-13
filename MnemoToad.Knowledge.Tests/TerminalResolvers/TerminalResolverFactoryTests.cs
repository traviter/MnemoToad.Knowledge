using MnemoToad.Knowledge.Data.PathResolution;
using MnemoToad.Knowledge.Data.TerminalResolvers;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;

namespace MnemoToad.Knowledge.Tests.TerminalResolvers;

[TestFixture]
public class TerminalResolverFactoryTests
{
    private MockableAppDbContext _db = null!;
    private TerminalResolverFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _factory = new TerminalResolverFactory(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public void GetResolver_Column_ReturnsColumnTerminalResolver() =>
        Assert.That(_factory.GetResolver(PathTerminalKind.Column), Is.InstanceOf<ColumnTerminalResolver>());

    [Test]
    public void GetResolver_Attribute_ReturnsAttributeTerminalResolver() =>
        Assert.That(_factory.GetResolver(PathTerminalKind.Attribute), Is.InstanceOf<AttributeTerminalResolver>());

    [Test]
    public void GetResolver_Media_ReturnsMediaTerminalResolver() =>
        Assert.That(_factory.GetResolver(PathTerminalKind.Media), Is.InstanceOf<MediaTerminalResolver>());

    [Test]
    public void GetResolver_UnregisteredKind_ThrowsInvalidOperationException()
    {
        var invalidKind = (PathTerminalKind)99;

        var ex = Assert.Throws<InvalidOperationException>(() => _factory.GetResolver(invalidKind));

        Assert.That(ex!.Message, Is.EqualTo("No terminal resolver registered for '99'."));
    }
}
