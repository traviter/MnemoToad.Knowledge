using MnemoToad.Knowledge.Data.PathResolution;
using MnemoToad.Knowledge.Data.QueryTransforms;
using MnemoToad.Knowledge.Data.TerminalResolvers;
using MnemoToad.Knowledge.Data.Repositories;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.Components;

[TestFixture]
public class PathResolutionRepositoryTests
{
    private MockableAppDbContext _db = null!;
    private PathResolutionRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _repository = new PathResolutionRepository(_db, new PathExpressionParser(), new TerminalResolverFactory(_db),
            new ForwardNodeRelationshipQueryTransform(_db), new BackwardNodeRelationshipQueryTransform(_db));
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task ResolveAsync_UnparseablePath_ReturnsError()
    {
        var result = await _repository.ResolveAsync(new PathResolutionQuery(Guid.NewGuid(), "not-a-valid-path"));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo("Invalid Path DSL syntax."));
    }

    [Test]
    public async Task ResolveAsync_EdgeWithNoTerminal_ReturnsError()
    {
        var result = await _repository.ResolveAsync(new PathResolutionQuery(Guid.NewGuid(), ">capital"));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo("Invalid Path DSL syntax."));
    }

    [Test]
    public async Task ResolveAsync_EdgeNameContainingReservedCharacter_ReturnsError()
    {
        var result = await _repository.ResolveAsync(new PathResolutionQuery(Guid.NewGuid(), ">a_b.value"));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo("Invalid Path DSL syntax."));
    }

    [Test]
    public async Task ResolveAsync_NodeNotFound_ReturnsError()
    {
        var result = await _repository.ResolveAsync(new PathResolutionQuery(Guid.NewGuid(), "_canonicalName"));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo("Path could not be resolved."));
    }

    [Test]
    public async Task ResolveAsync_ColumnCanonicalName_ReturnsValue()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "France");

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, "_canonicalName"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value!.GetValue<string>(), Is.EqualTo("France"));
    }

    [Test]
    public async Task ResolveAsync_ColumnId_ReturnsNodeIdAsString()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, "_id"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value!.GetValue<string>(), Is.EqualTo(node.Id.ToString()));
    }

    [Test]
    public async Task ResolveAsync_ColumnDescriptionWhenNull_ReturnsError()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id, description: null);

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, "_description"));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo("Path could not be resolved."));
    }

    [Test]
    public async Task ResolveAsync_UnknownColumn_ReturnsError()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);

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

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, ".population"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value!.GetValue<int>(), Is.EqualTo(68000000));
    }

    [Test]
    public async Task ResolveAsync_MissingAttribute_ReturnsError()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, ".gdp"));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo("Path could not be resolved."));
    }

    [Test]
    public async Task ResolveAsync_MediaWithNoExtraMetadata_ReturnsIdAndAltTextOnly()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var mediaAsset = await _db.CreateMediaAssetAsync();
        await _db.CreateKnowledgeNodeMediaAsync(node.Id, "flag", mediaAsset.Id, "A flag");

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, "#flag"));

        Assert.That(result.Error, Is.Null);
        var value = result.Value!.AsObject();
        Assert.That(value["id"]!.GetValue<string>(), Is.EqualTo(mediaAsset.Id.ToString()));
        Assert.That(value["alt_text"]!.GetValue<string>(), Is.EqualTo("A flag"));
        Assert.That(value.ContainsKey("metadata"), Is.False);
    }

    [Test]
    public async Task ResolveAsync_MediaWithExtraFields_ReturnsAllFieldsFlat()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var mediaAsset = await _db.CreateMediaAssetAsync();
        await _db.CreateKnowledgeNodeMediaAsync(node.Id, "coatOfArms", mediaAsset.Id, "A coat of arms",
            new JsonObject { ["id"] = mediaAsset.Id.ToString(), ["alt_text"] = "A coat of arms", ["credit"] = "Wikimedia Commons" });

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, "#coatOfArms"));

        Assert.That(result.Error, Is.Null);
        var value = result.Value!.AsObject();
        Assert.That(value["id"]!.GetValue<string>(), Is.EqualTo(mediaAsset.Id.ToString()));
        Assert.That(value["alt_text"]!.GetValue<string>(), Is.EqualTo("A coat of arms"));
        Assert.That(value["credit"]!.GetValue<string>(), Is.EqualTo("Wikimedia Commons"));
        Assert.That(value.ContainsKey("metadata"), Is.False);
    }

    [Test]
    public async Task ResolveAsync_MissingMedia_ReturnsError()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, "#flag"));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo("Path could not be resolved."));
    }

    [Test]
    public async Task ResolveAsync_OneEdgeForwardViaName_FollowsToTargetNode()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var source = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var target = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Paris");
        var relationshipType = await _db.CreateRelationshipTypeAsync(name: "capital");
        await _db.CreateKnowledgeRelationAsync(source.Id, relationshipType.Id, target.Id);

        var result = await _repository.ResolveAsync(new PathResolutionQuery(source.Id, ">capital_canonicalName"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value!.GetValue<string>(), Is.EqualTo("Paris"));
    }

    [Test]
    public async Task ResolveAsync_MissingRelation_ReturnsError()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);

        var result = await _repository.ResolveAsync(new PathResolutionQuery(node.Id, ">doesNotExist_canonicalName"));

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo("Path could not be resolved."));
    }

    [Test]
    public async Task ResolveAsync_OneEdgeBackwardViaName_FollowsToSourceNode()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var source = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "France");
        var target = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Paris");
        var relationshipType = await _db.CreateRelationshipTypeAsync(name: "capital");
        await _db.CreateKnowledgeRelationAsync(source.Id, relationshipType.Id, target.Id);

        var result = await _repository.ResolveAsync(new PathResolutionQuery(target.Id, "<capital_canonicalName"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value!.GetValue<string>(), Is.EqualTo("France"));
    }

    [Test]
    public async Task ResolveAsync_TwoEdgeChain_ResolvesAcrossBothRelations()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var city = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var region = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var country = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "France");
        var partOf = await _db.CreateRelationshipTypeAsync(name: "partOf");
        await _db.CreateKnowledgeRelationAsync(city.Id, partOf.Id, region.Id);
        await _db.CreateKnowledgeRelationAsync(region.Id, partOf.Id, country.Id);

        var result = await _repository.ResolveAsync(new PathResolutionQuery(city.Id, ">partOf>partOf_canonicalName"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value!.GetValue<string>(), Is.EqualTo("France"));
    }

    [Test]
    public async Task ResolveAsync_MixedDirectionTwoEdgeChain_ResolvesAcrossBothRelations()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var country = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var capital = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var landmark = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Eiffel Tower");
        var capitalOf = await _db.CreateRelationshipTypeAsync(name: "capital");
        var locatedIn = await _db.CreateRelationshipTypeAsync(name: "locatedIn");
        await _db.CreateKnowledgeRelationAsync(country.Id, capitalOf.Id, capital.Id);
        await _db.CreateKnowledgeRelationAsync(landmark.Id, locatedIn.Id, capital.Id);

        var result = await _repository.ResolveAsync(new PathResolutionQuery(country.Id, ">capital<locatedIn_canonicalName"));

        Assert.That(result.Error, Is.Null);
        Assert.That(result.Value!.GetValue<string>(), Is.EqualTo("Eiffel Tower"));
    }

    [Test]
    public async Task ResolveAsync_Batch_ResolvesEachQueryIndependentlyInOrder()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "France");
        await _db.CreateKnowledgeNodeAttributeAsync(node.Id, "population", JsonValue.Create(68000000));

        var results = await _repository.ResolveAsync(
        [
            new PathResolutionQuery(node.Id, "_canonicalName"),
            new PathResolutionQuery(node.Id, ".population"),
            new PathResolutionQuery(node.Id, ".gdp")
        ]);

        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0].Value!.GetValue<string>(), Is.EqualTo("France"));
        Assert.That(results[1].Value!.GetValue<int>(), Is.EqualTo(68000000));
        Assert.That(results[2].Error, Is.EqualTo("Path could not be resolved."));
    }
}
