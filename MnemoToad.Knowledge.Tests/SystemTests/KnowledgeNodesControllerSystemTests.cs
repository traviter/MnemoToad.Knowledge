using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Api.Contracts;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.SystemTests;

[TestFixture]
public class KnowledgeNodesControllerSystemTests
{
    private MockedDbWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new MockedDbWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetById_WhenNotFound_Returns404()
    {
        var response = await _client.GetAsync($"/nodes/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Create_ThenGetById_RoundTripsThroughTheRealStack()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();

        var createResponse = await _client.PostAsJsonAsync("/nodes", new KnowledgeNodeRequest(nodeType.Id, "Mercury", "The planet"));
        var created = await createResponse.Content.ReadFromJsonAsync<KnowledgeNode>();

        var getResponse = await _client.GetAsync($"/nodes/{created!.Id}");

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var fetched = await getResponse.Content.ReadFromJsonAsync<KnowledgeNode>();
        Assert.That(fetched!.CanonicalName, Is.EqualTo("Mercury"));
    }

    [Test]
    public async Task Create_WithAttributes_RoundTripsAttributesThroughTheRealStack()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();
        var attributes = new Dictionary<string, JsonValue>
        {
            ["isoCode"] = JsonValue.Create("FR"),
            ["population"] = JsonValue.Create(68000000),
            ["isEuMember"] = JsonValue.Create(true)
        };

        var createResponse = await _client.PostAsJsonAsync("/nodes", new KnowledgeNodeRequest(nodeType.Id, "France", null, attributes));
        var created = await createResponse.Content.ReadFromJsonAsync<KnowledgeNode>();

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(created!.Attributes!["isoCode"]!.GetValue<string>(), Is.EqualTo("FR"));
        Assert.That(created.Attributes!["population"]!.GetValue<int>(), Is.EqualTo(68000000));
        Assert.That(created.Attributes!["isEuMember"]!.GetValue<bool>(), Is.True);

        var getResponse = await _client.GetAsync($"/nodes/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<KnowledgeNode>();
        Assert.That(fetched!.Attributes!["isoCode"]!.GetValue<string>(), Is.EqualTo("FR"));
    }

    [Test]
    public async Task Update_FullReplace_RemovesOmittedAttributeAndAddsNewOne()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();
        var createResponse = await _client.PostAsJsonAsync("/nodes", new KnowledgeNodeRequest(nodeType.Id, "France", null,
            new Dictionary<string, JsonValue> { ["isoCode"] = JsonValue.Create("FR"), ["population"] = JsonValue.Create(68000000) }));
        var created = await createResponse.Content.ReadFromJsonAsync<KnowledgeNode>();

        var updateResponse = await _client.PutAsJsonAsync($"/nodes/{created!.Id}", new KnowledgeNodeRequest(nodeType.Id, "France", null,
            new Dictionary<string, JsonValue> { ["isoCode"] = JsonValue.Create("FR"), ["isEuMember"] = JsonValue.Create(true) }));
        var updated = await updateResponse.Content.ReadFromJsonAsync<KnowledgeNode>();

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(updated!.Attributes!.Keys, Is.EquivalentTo(new[] { "isoCode", "isEuMember" }));
    }

    [Test]
    public async Task Create_WithMedia_RoundTripsStanzaThroughTheRealStack()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();
        var mediaAssetId = Guid.NewGuid();

        var createResponse = await _client.PostAsJsonAsync("/nodes", new KnowledgeNodeRequest(nodeType.Id, "France", null, null,
            new Dictionary<string, JsonObject> { ["flag"] = new JsonObject { ["id"] = mediaAssetId.ToString(), ["alt_text"] = "Flag of France" } }));
        var created = await createResponse.Content.ReadFromJsonAsync<KnowledgeNode>();

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(created!.Media!["flag"]!["id"]!.GetValue<string>(), Is.EqualTo(mediaAssetId.ToString()));
        Assert.That(created.Media!["flag"]!["alt_text"]!.GetValue<string>(), Is.EqualTo("Flag of France"));

        var getResponse = await _client.GetAsync($"/nodes/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<KnowledgeNode>();
        Assert.That(fetched!.Media!["flag"]!["id"]!.GetValue<string>(), Is.EqualTo(mediaAssetId.ToString()));
    }

    [Test]
    public async Task Create_WithMediaExtraFields_RoundTripsArbitraryMetadataThroughTheRealStack()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();
        var mediaAssetId = Guid.NewGuid();

        var createResponse = await _client.PostAsJsonAsync("/nodes", new KnowledgeNodeRequest(nodeType.Id, "France", null, null,
            new Dictionary<string, JsonObject>
            {
                ["flag"] = new JsonObject { ["id"] = mediaAssetId.ToString(), ["alt_text"] = "Flag of France", ["other_metadata"] = 2323 }
            }));
        var created = await createResponse.Content.ReadFromJsonAsync<KnowledgeNode>();

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(created!.Media!["flag"]!["other_metadata"]!.GetValue<int>(), Is.EqualTo(2323));

        var getResponse = await _client.GetAsync($"/nodes/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<KnowledgeNode>();
        Assert.That(fetched!.Media!["flag"]!["other_metadata"]!.GetValue<int>(), Is.EqualTo(2323));
    }

    [Test]
    public async Task Update_MediaFullReplace_RemovesOmittedKeyAndAddsNewOne()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();
        var createResponse = await _client.PostAsJsonAsync("/nodes", new KnowledgeNodeRequest(nodeType.Id, "France", null, null,
            new Dictionary<string, JsonObject> { ["flag"] = new JsonObject { ["id"] = Guid.NewGuid().ToString(), ["alt_text"] = "Flag of France" } }));
        var created = await createResponse.Content.ReadFromJsonAsync<KnowledgeNode>();

        var updateResponse = await _client.PutAsJsonAsync($"/nodes/{created!.Id}", new KnowledgeNodeRequest(nodeType.Id, "France", null, null,
            new Dictionary<string, JsonObject> { ["photo"] = new JsonObject { ["id"] = Guid.NewGuid().ToString(), ["alt_text"] = "Eiffel Tower" } }));
        var updated = await updateResponse.Content.ReadFromJsonAsync<KnowledgeNode>();

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(updated!.Media!.Keys, Is.EquivalentTo(new[] { "photo" }));
    }

    [Test]
    public async Task Create_WithMediaEntryMissingId_Returns400()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();

        var response = await _client.PostAsJsonAsync("/nodes", new KnowledgeNodeRequest(nodeType.Id, "France", null, null,
            new Dictionary<string, JsonObject> { ["flag"] = new JsonObject { ["alt_text"] = "Flag of France" } }));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problem!.Detail, Is.EqualTo("The media entry 'flag' must include a valid 'id'."));
    }

    [Test]
    public async Task Create_WithMediaEntryMissingAltText_Returns400()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();

        var response = await _client.PostAsJsonAsync("/nodes", new KnowledgeNodeRequest(nodeType.Id, "France", null, null,
            new Dictionary<string, JsonObject> { ["flag"] = new JsonObject { ["id"] = Guid.NewGuid().ToString() } }));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problem!.Detail, Is.EqualTo("The media entry 'flag' must include a valid 'alt_text'."));
    }

    [Test]
    public async Task Create_WhenRepositoryHitsMediaAssetUniqueViolation_Returns400()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();
        _factory.Db.ThrowOnSaveChanges(PostgresExceptionFactory.UniqueViolation(constraintName: "uq_knowledge_node_media_media_asset_id"));

        var response = await _client.PostAsJsonAsync("/nodes", new KnowledgeNodeRequest(nodeType.Id, "France", null, null,
            new Dictionary<string, JsonObject> { ["flag"] = new JsonObject { ["id"] = Guid.NewGuid().ToString(), ["alt_text"] = "x" } }));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problem!.Detail, Is.EqualTo("The specified MediaAsset is already linked to another KnowledgeNode."));
    }

    [Test]
    public async Task Update_WhenRepositoryHitsMediaAssetUniqueViolation_Returns400()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();
        var knowledgeNode = await _factory.Db.CreateKnowledgeNodeAsync(nodeType.Id);
        _factory.Db.ThrowOnSaveChanges(PostgresExceptionFactory.UniqueViolation(constraintName: "uq_knowledge_node_media_media_asset_id"));

        var response = await _client.PutAsJsonAsync($"/nodes/{knowledgeNode.Id}", new KnowledgeNodeRequest(nodeType.Id, "France", null, null,
            new Dictionary<string, JsonObject> { ["flag"] = new JsonObject { ["id"] = Guid.NewGuid().ToString(), ["alt_text"] = "x" } }));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problem!.Detail, Is.EqualTo("The specified MediaAsset is already linked to another KnowledgeNode."));
    }

    [Test]
    public async Task Create_WithArrayAttributeValue_Returns400()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();
        var json = "{\"nodeTypeId\":\"" + nodeType.Id + "\",\"canonicalName\":\"France\",\"attributes\":{\"tags\":[\"a\",\"b\"]}}";

        var response = await _client.PostAsync("/nodes", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Create_WithObjectAttributeValue_Returns400()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();
        var json = "{\"nodeTypeId\":\"" + nodeType.Id + "\",\"canonicalName\":\"France\",\"attributes\":{\"nested\":{\"a\":1}}}";

        var response = await _client.PostAsync("/nodes", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Create_WithNullAttributeValue_Returns400()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();
        var json = "{\"nodeTypeId\":\"" + nodeType.Id + "\",\"canonicalName\":\"France\",\"attributes\":{\"population\":null}}";

        var response = await _client.PostAsync("/nodes", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Create_WithBlankCanonicalName_Returns400WithValidationErrors()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();

        var response = await _client.PostAsJsonAsync("/nodes", new KnowledgeNodeRequest(nodeType.Id, "", null));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problem!.Errors, Contains.Key("CanonicalName"));
    }

    [Test]
    public async Task Create_WithInvalidNodeTypeId_Returns400WithValidationErrors()
    {
        var json = "{\"nodeTypeId\":\"not-a-guid\",\"canonicalName\":\"Mercury\"}";

        var response = await _client.PostAsync("/nodes", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problem!.Errors, Contains.Key("$.nodeTypeId"));
    }

    [Test]
    public async Task Create_WithMissingNodeTypeId_Returns400WithValidationErrors()
    {
        var json = "{\"canonicalName\":\"Mercury\"}";

        var response = await _client.PostAsync("/nodes", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problem!.Errors, Contains.Key("NodeTypeId"));
    }

    [Test]
    public async Task Create_WhenRepositoryHitsUniqueViolation_Returns400()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();
        _factory.Db.ThrowOnSaveChanges(PostgresExceptionFactory.UniqueViolation(constraintName: "uq_knowledge_node_node_type_id_canonical_name"));

        var response = await _client.PostAsJsonAsync("/nodes", new KnowledgeNodeRequest(nodeType.Id, "Mercury", null));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problem!.Detail, Is.EqualTo("A KnowledgeNode with the same NodeType and CanonicalName already exists."));
    }

    [Test]
    public async Task Create_WhenRepositoryHitsNodeTypeForeignKeyViolation_Returns400()
    {
        _factory.Db.ThrowOnSaveChanges(PostgresExceptionFactory.ForeignKeyViolation(tableName: "knowledge_node"));

        var response = await _client.PostAsJsonAsync("/nodes", new KnowledgeNodeRequest(Guid.NewGuid(), "Mercury", null));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problem!.Detail, Is.EqualTo("The specified NodeType does not exist."));
    }

    [Test]
    public async Task GetAll_WithNodeTypeNameQueryParam_ReturnsOnlyMatchingNodes()
    {
        var nodeType1 = await _factory.Db.CreateNodeTypeAsync();
        var nodeType2 = await _factory.Db.CreateNodeTypeAsync();
        await _factory.Db.CreateKnowledgeNodeAsync(nodeType1.Id, "Mercury");
        await _factory.Db.CreateKnowledgeNodeAsync(nodeType2.Id, "Venus");

        var response = await _client.GetAsync($"/nodes?nodeTypeName={Uri.EscapeDataString(nodeType1.Name)}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var nodes = await response.Content.ReadFromJsonAsync<List<KnowledgeNode>>();
        Assert.That(nodes!.Select(n => n.CanonicalName), Is.EqualTo(new[] { "Mercury" }));
    }

    [Test]
    public async Task GetAll_ResponseOmitsAttributesKeyEntirely()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();
        await _factory.Db.CreateKnowledgeNodeAsync(nodeType.Id);

        var response = await _client.GetAsync($"/nodes?nodeTypeName={Uri.EscapeDataString(nodeType.Name)}");

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.That(document.RootElement.GetArrayLength(), Is.GreaterThan(0));
        Assert.That(document.RootElement[0].TryGetProperty("attributes", out _), Is.False);
    }

    [Test]
    public async Task GetAll_WithKnownNodeTypeNameAndNoMatchingNodes_ReturnsOkWithEmptyList()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();

        var response = await _client.GetAsync($"/nodes?nodeTypeName={Uri.EscapeDataString(nodeType.Name)}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var nodes = await response.Content.ReadFromJsonAsync<List<KnowledgeNode>>();
        Assert.That(nodes, Is.Empty);
    }

    [Test]
    public async Task GetAll_WithUnknownNodeTypeName_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/nodes?nodeTypeName=NoSuchNodeType");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetAll_WithMissingNodeTypeName_Returns400WithValidationErrors()
    {
        var response = await _client.GetAsync("/nodes");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problem!.Errors, Contains.Key("nodeTypeName"));
    }

    [Test]
    public async Task GetAll_WithEmptyNodeTypeName_Returns400WithValidationErrors()
    {
        var response = await _client.GetAsync("/nodes?nodeTypeName=");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problem!.Errors, Contains.Key("nodeTypeName"));
    }

    [Test]
    public async Task Update_WhenNotFound_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/nodes/{Guid.NewGuid()}", new KnowledgeNodeRequest(Guid.NewGuid(), "Mercury", null));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Update_WhenRepositoryHitsUniqueViolation_Returns400()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();
        var knowledgeNode = await _factory.Db.CreateKnowledgeNodeAsync(nodeType.Id, "Mercury");
        _factory.Db.ThrowOnSaveChanges(PostgresExceptionFactory.UniqueViolation(constraintName: "uq_knowledge_node_node_type_id_canonical_name"));

        var response = await _client.PutAsJsonAsync($"/nodes/{knowledgeNode.Id}", new KnowledgeNodeRequest(nodeType.Id, "Venus", null));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Update_WhenRepositoryHitsNodeTypeForeignKeyViolation_Returns400()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();
        var knowledgeNode = await _factory.Db.CreateKnowledgeNodeAsync(nodeType.Id, "Mercury");
        _factory.Db.ThrowOnSaveChanges(PostgresExceptionFactory.ForeignKeyViolation(tableName: "knowledge_node"));

        var response = await _client.PutAsJsonAsync($"/nodes/{knowledgeNode.Id}", new KnowledgeNodeRequest(Guid.NewGuid(), "Mercury", null));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Delete_WhenExists_Returns204AndRemovesIt()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();
        var knowledgeNode = await _factory.Db.CreateKnowledgeNodeAsync(nodeType.Id);

        var deleteResponse = await _client.DeleteAsync($"/nodes/{knowledgeNode.Id}");

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(await _factory.Db.KnowledgeNode.AsNoTracking().FirstOrDefaultAsync(n => n.Id == knowledgeNode.Id), Is.Null);
    }

    [Test]
    public async Task Delete_WhenRepositoryHitsKnowledgeRelationForeignKeyViolation_Returns400()
    {
        var nodeType = await _factory.Db.CreateNodeTypeAsync();
        var knowledgeNode = await _factory.Db.CreateKnowledgeNodeAsync(nodeType.Id);
        _factory.Db.ThrowOnExecuteDelete<KnowledgeNode>(PostgresExceptionFactory.ForeignKeyViolation(tableName: "knowledge_relation"));

        var response = await _client.DeleteAsync($"/nodes/{knowledgeNode.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
