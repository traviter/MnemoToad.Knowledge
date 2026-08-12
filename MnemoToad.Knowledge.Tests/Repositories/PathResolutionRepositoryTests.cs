using Moq;
using MnemoToad.Knowledge.Data.PathResolution;
using MnemoToad.Knowledge.Data.Repositories;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.Repositories;

[TestFixture]
public class PathResolutionRepositoryTests
{
    private MockableAppDbContext _db = null!;
    private Mock<IPathExpressionParser> _parser = null!;
    private PathResolutionRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _parser = new Mock<IPathExpressionParser>();
        _repository = new PathResolutionRepository(_db, _parser.Object);
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

    [Test]
    public async Task ResolveAsync_UnparseablePath_ReturnsError()
    {
        SetupParseFailure("not-a-valid-path");

        var result = await _repository.ResolveAsync(new PathResolutionQuery(Guid.NewGuid(), "not-a-valid-path"));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo("Invalid Path DSL syntax."));
    }

    [Test]
    public async Task ResolveAsync_NodeNotFound_ReturnsError()
    {
        SetupParse("_canonicalName", new PathExpression([], PathTerminalKind.Column, "canonicalName"));

        var result = await _repository.ResolveAsync(new PathResolutionQuery(Guid.NewGuid(), "_canonicalName"));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo("No KnowledgeNode exists with that id."));
    }

    [Test]
    public async Task ResolveAsync_ColumnCanonicalName_ReturnsValue()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "France");
        SetupParse("_canonicalName", new PathExpression([], PathTerminalKind.Column, "canonicalName"));

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, "_canonicalName"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value!.GetValue<string>(), Is.EqualTo("France"));
    }

    [Test]
    public async Task ResolveAsync_ColumnId_ReturnsNodeIdAsString()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        SetupParse("_id", new PathExpression([], PathTerminalKind.Column, "id"));

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, "_id"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value!.GetValue<string>(), Is.EqualTo(node.Id.ToString()));
    }

    [Test]
    public async Task ResolveAsync_ColumnDescriptionWhenNull_ReturnsNullValueNotError()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id, description: null);
        SetupParse("_description", new PathExpression([], PathTerminalKind.Column, "description"));

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, "_description"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value, Is.Null);
    }

    [Test]
    public async Task ResolveAsync_UnknownColumn_ReturnsError()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        SetupParse("_bogus", new PathExpression([], PathTerminalKind.Column, "bogus"));

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, "_bogus"));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo("No column named 'bogus' on KnowledgeNode."));
    }

    [Test]
    public async Task ResolveAsync_Attribute_ReturnsStoredValue()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        await _db.CreateKnowledgeNodeAttributeAsync(node.Id, "population", JsonValue.Create(68000000));
        SetupParse(".population", new PathExpression([], PathTerminalKind.Attribute, "population"));

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, ".population"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value!.GetValue<int>(), Is.EqualTo(68000000));
    }

    [Test]
    public async Task ResolveAsync_MissingAttribute_ReturnsError()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        SetupParse(".gdp", new PathExpression([], PathTerminalKind.Attribute, "gdp"));

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, ".gdp"));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo("No attribute named 'gdp' on this node."));
    }

    [Test]
    public async Task ResolveAsync_MediaWithNoExtraMetadata_ReturnsIdAndAltTextOnly()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var mediaAsset = await _db.CreateMediaAssetAsync();
        await _db.CreateKnowledgeNodeMediaAsync(node.Id, "flag", mediaAsset.Id, "A flag");
        SetupParse("#flag", new PathExpression([], PathTerminalKind.Media, "flag"));

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, "#flag"));

        Assert.That(result.Error, Is.Null);
        var value = result.Value!.AsObject();
        Assert.That(value["id"]!.GetValue<string>(), Is.EqualTo(mediaAsset.Id.ToString()));
        Assert.That(value["alt_text"]!.GetValue<string>(), Is.EqualTo("A flag"));
        Assert.That(value.ContainsKey("metadata"), Is.False);
    }

    [Test]
    public async Task ResolveAsync_MediaWithExtraMetadata_ReturnsMetadataWithRedundantFieldsStripped()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var mediaAsset = await _db.CreateMediaAssetAsync();
        await _db.CreateKnowledgeNodeMediaAsync(node.Id, "coatOfArms", mediaAsset.Id, "A coat of arms",
            new JsonObject { ["id"] = mediaAsset.Id.ToString(), ["alt_text"] = "A coat of arms", ["credit"] = "Wikimedia Commons" });
        SetupParse("#coatOfArms", new PathExpression([], PathTerminalKind.Media, "coatOfArms"));

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, "#coatOfArms"));

        Assert.That(result.Error, Is.Null);
        var value = result.Value!.AsObject();
        Assert.That(value["id"]!.GetValue<string>(), Is.EqualTo(mediaAsset.Id.ToString()));
        Assert.That(value["alt_text"]!.GetValue<string>(), Is.EqualTo("A coat of arms"));
        Assert.That(value["metadata"]!["credit"]!.GetValue<string>(), Is.EqualTo("Wikimedia Commons"));
        Assert.That(value["metadata"]!.AsObject().ContainsKey("id"), Is.False);
        Assert.That(value["metadata"]!.AsObject().ContainsKey("alt_text"), Is.False);
    }

    [Test]
    public async Task ResolveAsync_MissingMedia_ReturnsError()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        SetupParse("#flag", new PathExpression([], PathTerminalKind.Media, "flag"));

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, "#flag"));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo("No media named 'flag' on this node."));
    }

    [Test]
    public async Task ResolveAsync_OneHopForwardViaName_FollowsToTargetNode()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var source = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var target = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Paris");
        var relationshipType = await _db.CreateRelationshipTypeAsync(name: "capital", inverseName: "capitalOf");
        await _db.CreateKnowledgeRelationAsync(source.Id, relationshipType.Id, target.Id);
        SetupParse(">capital_canonicalName", new PathExpression(["capital"], PathTerminalKind.Column, "canonicalName"));

        var result = await _repository.ResolveAsync(new PathResolutionQuery(source.Id, ">capital_canonicalName"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value!.GetValue<string>(), Is.EqualTo("Paris"));
    }

    [Test]
    public async Task ResolveAsync_OneHopReverseViaInverseName_FollowsToSourceNode()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var source = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "France");
        var target = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var relationshipType = await _db.CreateRelationshipTypeAsync(name: "capital", inverseName: "capitalOf");
        await _db.CreateKnowledgeRelationAsync(source.Id, relationshipType.Id, target.Id);
        SetupParse(">capitalOf_canonicalName", new PathExpression(["capitalOf"], PathTerminalKind.Column, "canonicalName"));

        var result = await _repository.ResolveAsync(new PathResolutionQuery(target.Id, ">capitalOf_canonicalName"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value!.GetValue<string>(), Is.EqualTo("France"));
    }

    [Test]
    public async Task ResolveAsync_MissingRelation_ReturnsError()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        SetupParse(">doesNotExist_canonicalName", new PathExpression(["doesNotExist"], PathTerminalKind.Column, "canonicalName"));

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, ">doesNotExist_canonicalName"));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo("No relation named 'doesNotExist' from this node."));
    }

    [Test]
    public async Task ResolveAsync_TwoHopChain_ResolvesAcrossBothRelations()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var city = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var region = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var country = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "France");
        var partOf = await _db.CreateRelationshipTypeAsync(name: "partOf");
        await _db.CreateKnowledgeRelationAsync(city.Id, partOf.Id, region.Id);
        await _db.CreateKnowledgeRelationAsync(region.Id, partOf.Id, country.Id);
        SetupParse(">partOf>partOf_canonicalName", new PathExpression(["partOf", "partOf"], PathTerminalKind.Column, "canonicalName"));

        var result = await _repository.ResolveAsync(new PathResolutionQuery(city.Id, ">partOf>partOf_canonicalName"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value!.GetValue<string>(), Is.EqualTo("France"));
    }

    [Test]
    public async Task ResolveAsync_Batch_ResolvesEachQueryIndependentlyInOrder()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "France");
        await _db.CreateKnowledgeNodeAttributeAsync(node.Id, "population", JsonValue.Create(68000000));
        SetupParse("_canonicalName", new PathExpression([], PathTerminalKind.Column, "canonicalName"));
        SetupParse(".population", new PathExpression([], PathTerminalKind.Attribute, "population"));
        SetupParse(".gdp", new PathExpression([], PathTerminalKind.Attribute, "gdp"));

        var results = await _repository.ResolveAsync(
        [
            new PathResolutionQuery(node.Id, "_canonicalName"),
            new PathResolutionQuery(node.Id, ".population"),
            new PathResolutionQuery(node.Id, ".gdp")
        ]);

        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0].Value!.GetValue<string>(), Is.EqualTo("France"));
        Assert.That(results[1].Value!.GetValue<int>(), Is.EqualTo(68000000));
        Assert.That(results[2].Error, Is.EqualTo("No attribute named 'gdp' on this node."));
    }
}
