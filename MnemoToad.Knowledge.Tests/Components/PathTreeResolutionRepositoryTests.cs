using MnemoToad.Knowledge.Data.PathResolution;
using MnemoToad.Knowledge.Data.QueryTransforms;
using MnemoToad.Knowledge.Data.TerminalResolvers;
using MnemoToad.Knowledge.Data.Repositories;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;

namespace MnemoToad.Knowledge.Tests.Components;

[TestFixture]
public class PathTreeResolutionRepositoryTests
{
    private MockableAppDbContext _db = null!;
    private PathTreeResolutionRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _repository = new PathTreeResolutionRepository(_db, new PathExpressionParser(), new TerminalResolverFactory(_db),
            new ForwardNodeRelationshipQueryTransform(_db), new BackwardNodeRelationshipQueryTransform(_db));
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task ResolveTreeAsync_SingleNodeSinglePath_MatchesBaselineBehavior()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "France");

        var rows = await _repository.ResolveTreeAsync([node.Id], ["_canonicalName"]);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].NodeId, Is.EqualTo(node.Id));
        Assert.That(rows[0].Properties["_canonicalName"]!.GetValue<string>(), Is.EqualTo("France"));
        Assert.That(rows[0].Errors, Is.Null);
    }

    [Test]
    public async Task ResolveTreeAsync_EdgeMatchingMultipleRelations_ProducesMultipleCorrectlyScopedRows()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var country = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var paris = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Paris");
        var lyon = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Lyon");
        var cityInCountry = await _db.CreateRelationshipTypeAsync(name: "cityInCountry");
        await _db.CreateKnowledgeRelationAsync(paris.Id, cityInCountry.Id, country.Id);
        await _db.CreateKnowledgeRelationAsync(lyon.Id, cityInCountry.Id, country.Id);

        var rows = await _repository.ResolveTreeAsync([country.Id], ["<cityInCountry_canonicalName"]);

        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows.Select(r => r.Properties["<cityInCountry_canonicalName"]!.GetValue<string>()),
            Is.EquivalentTo(new[] { "Paris", "Lyon" }));
        Assert.That(rows.Select(r => r.NodeId), Is.All.EqualTo(country.Id));
    }

    [Test]
    public async Task ResolveTreeAsync_EdgeMatchingNoRelations_ProducesZeroRows()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var country = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Germany");

        var rows = await _repository.ResolveTreeAsync([country.Id], ["_canonicalName", "<cityInCountry_canonicalName"]);

        Assert.That(rows, Is.Empty);
    }

    [Test]
    public async Task ResolveTreeAsync_TerminalMissingAfterSuccessfulEdgeTraversal_ProducesRowWithPerRowError()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var country = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "France");
        var city = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Paris");
        var cityInCountry = await _db.CreateRelationshipTypeAsync(name: "cityInCountry");
        await _db.CreateKnowledgeRelationAsync(city.Id, cityInCountry.Id, country.Id);

        var rows = await _repository.ResolveTreeAsync([country.Id], ["_canonicalName", "<cityInCountry.gdp"]);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].Properties["_canonicalName"]!.GetValue<string>(), Is.EqualTo("France"));
        Assert.That(rows[0].Errors, Is.Not.Null);
        Assert.That(rows[0].Errors!["<cityInCountry.gdp"], Is.EqualTo("Path could not be resolved."));
    }

    [Test]
    public async Task ResolveTreeAsync_TerminalMissingAtRootWithZeroEdges_ProducesSingleErrorsOnlyRow()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);

        var rows = await _repository.ResolveTreeAsync([node.Id], [".gdp"]);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].Properties, Is.Empty);
        Assert.That(rows[0].Errors![".gdp"], Is.EqualTo("Path could not be resolved."));
    }

    [Test]
    public async Task ResolveTreeAsync_TwoIndependentEdges_RowCountEqualsProductOfMatchCounts()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var node = await _db.CreateKnowledgeNodeAsync(nodeType.Id);
        var target1 = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "A1");
        var target2 = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "A2");
        var target3 = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "B1");
        var edgeA = await _db.CreateRelationshipTypeAsync(name: "edgeA");
        var edgeB = await _db.CreateRelationshipTypeAsync(name: "edgeB");
        await _db.CreateKnowledgeRelationAsync(node.Id, edgeA.Id, target1.Id);
        await _db.CreateKnowledgeRelationAsync(node.Id, edgeA.Id, target2.Id);
        await _db.CreateKnowledgeRelationAsync(node.Id, edgeB.Id, target3.Id);

        var rows = await _repository.ResolveTreeAsync([node.Id], [">edgeA_canonicalName", ">edgeB_canonicalName"]);

        Assert.That(rows, Has.Count.EqualTo(2)); // 2 edgeA matches x 1 edgeB match
    }

    [Test]
    public async Task ResolveTreeAsync_CountryCityStateExample_CorrelatesSharedPrefixWithoutCartesianExplosion()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var france = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "France");
        var germany = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Germany"); // no cities

        var paris = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Paris");
        var lyon = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Lyon");
        var marseille = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Marseille"); // no state

        var ileDeFrance = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Île-de-France");
        var auvergneRhoneAlpes = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Auvergne-Rhône-Alpes");

        var cityInCountry = await _db.CreateRelationshipTypeAsync(name: "cityInCountry");
        var cityInState = await _db.CreateRelationshipTypeAsync(name: "cityInState");

        await _db.CreateKnowledgeRelationAsync(paris.Id, cityInCountry.Id, france.Id);
        await _db.CreateKnowledgeRelationAsync(lyon.Id, cityInCountry.Id, france.Id);
        await _db.CreateKnowledgeRelationAsync(marseille.Id, cityInCountry.Id, france.Id);

        await _db.CreateKnowledgeRelationAsync(paris.Id, cityInState.Id, ileDeFrance.Id);
        await _db.CreateKnowledgeRelationAsync(lyon.Id, cityInState.Id, auvergneRhoneAlpes.Id);
        // marseille has no cityInState relation - its whole branch, and thus its slot in France's
        // <cityInCountry cartesian factor, drops out entirely.

        var rows = await _repository.ResolveTreeAsync([france.Id, germany.Id],
        [
            "<cityInCountry_canonicalName",
            "_canonicalName",
            "<cityInCountry>cityInState_canonicalName"
        ]);

        // Germany has zero cities, so traversing <cityInCountry kills its entire contribution -
        // even its own otherwise-resolvable _canonicalName is discarded.
        Assert.That(rows.Any(r => r.NodeId == germany.Id), Is.False);

        var franceRows = rows.Where(r => r.NodeId == france.Id).ToList();
        Assert.That(franceRows, Has.Count.EqualTo(2)); // Paris and Lyon only - Marseille has no state
        Assert.That(franceRows.Select(r => r.Properties["<cityInCountry_canonicalName"]!.GetValue<string>()),
            Is.EquivalentTo(new[] { "Paris", "Lyon" }));
        foreach (var row in franceRows)
            Assert.That(row.Properties["_canonicalName"]!.GetValue<string>(), Is.EqualTo("France"));

        var parisRow = franceRows.Single(r => r.Properties["<cityInCountry_canonicalName"]!.GetValue<string>() == "Paris");
        Assert.That(parisRow.Properties["<cityInCountry>cityInState_canonicalName"]!.GetValue<string>(), Is.EqualTo("Île-de-France"));
        var lyonRow = franceRows.Single(r => r.Properties["<cityInCountry_canonicalName"]!.GetValue<string>() == "Lyon");
        Assert.That(lyonRow.Properties["<cityInCountry>cityInState_canonicalName"]!.GetValue<string>(), Is.EqualTo("Auvergne-Rhône-Alpes"));
    }

    [Test]
    public async Task ResolveTreeAsync_ForwardAndBackwardTraversalOfSameRelation_ProduceSameResultSet()
    {
        var nodeType = await _db.CreateNodeTypeAsync();
        var france = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "France");
        var paris = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Paris");
        var lyon = await _db.CreateKnowledgeNodeAsync(nodeType.Id, canonicalName: "Lyon");
        var cityInCountry = await _db.CreateRelationshipTypeAsync(name: "cityInCountry");
        await _db.CreateKnowledgeRelationAsync(paris.Id, cityInCountry.Id, france.Id);
        await _db.CreateKnowledgeRelationAsync(lyon.Id, cityInCountry.Id, france.Id);

        var backwardRows = await _repository.ResolveTreeAsync([france.Id], ["_canonicalName", "<cityInCountry_canonicalName"]);
        var forwardRows = await _repository.ResolveTreeAsync([paris.Id, lyon.Id], ["_canonicalName", ">cityInCountry_canonicalName"]);

        var backwardPairs = backwardRows.Select(r =>
            (Country: r.Properties["_canonicalName"]!.GetValue<string>(), City: r.Properties["<cityInCountry_canonicalName"]!.GetValue<string>()));
        var forwardPairs = forwardRows.Select(r =>
            (Country: r.Properties[">cityInCountry_canonicalName"]!.GetValue<string>(), City: r.Properties["_canonicalName"]!.GetValue<string>()));

        Assert.That(backwardPairs, Is.EquivalentTo(forwardPairs));
    }
}
