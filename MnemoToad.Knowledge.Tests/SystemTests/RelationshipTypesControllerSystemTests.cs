using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Api.Contracts;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;

namespace MnemoToad.Knowledge.Tests.SystemTests;

[TestFixture]
public class RelationshipTypesControllerSystemTests
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
        var response = await _client.GetAsync($"/relationshipTypes/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Create_ThenGetById_RoundTripsThroughTheRealStack()
    {
        var createResponse = await _client.PostAsJsonAsync("/relationshipTypes", new RelationshipTypeRequest("parentOf", "A parent-child relation"));
        var created = await createResponse.Content.ReadFromJsonAsync<RelationshipType>();

        var getResponse = await _client.GetAsync($"/relationshipTypes/{created!.Id}");

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var fetched = await getResponse.Content.ReadFromJsonAsync<RelationshipType>();
        Assert.That(fetched!.Name, Is.EqualTo("parentOf"));
    }

    [Test]
    public async Task Create_WithBlankName_Returns400WithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync("/relationshipTypes", new RelationshipTypeRequest("", null));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problem!.Errors, Contains.Key("Name"));
    }

    [Test]
    public async Task Create_WhenRepositoryHitsUniqueViolation_Returns400()
    {
        _factory.Db.ThrowOnSaveChanges(PostgresExceptionFactory.UniqueViolation());

        var response = await _client.PostAsJsonAsync("/relationshipTypes", new RelationshipTypeRequest("parentOf", null));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problem!.Detail, Is.EqualTo("A RelationshipType with that name already exists."));
    }

    [Test]
    public async Task Update_WhenNotFound_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/relationshipTypes/{Guid.NewGuid()}", new RelationshipTypeRequest("parentOf", null));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Update_WhenRepositoryHitsUniqueViolation_Returns400()
    {
        var relationshipType = await _factory.Db.CreateRelationshipTypeAsync("parentOf");
        _factory.Db.ThrowOnSaveChanges(PostgresExceptionFactory.UniqueViolation());

        var response = await _client.PutAsJsonAsync($"/relationshipTypes/{relationshipType.Id}", new RelationshipTypeRequest("hasCapital", null));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Delete_WhenExists_Returns204AndRemovesIt()
    {
        var relationshipType = await _factory.Db.CreateRelationshipTypeAsync();

        var deleteResponse = await _client.DeleteAsync($"/relationshipTypes/{relationshipType.Id}");

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(await _factory.Db.RelationshipType.AsNoTracking().FirstOrDefaultAsync(r => r.Id == relationshipType.Id), Is.Null);
    }

    [Test]
    public async Task Delete_WhenRepositoryHitsForeignKeyViolation_Returns400()
    {
        var relationshipType = await _factory.Db.CreateRelationshipTypeAsync();
        _factory.Db.ThrowOnExecuteDelete<RelationshipType>(PostgresExceptionFactory.ForeignKeyViolation(tableName: "knowledge_relation"));

        var response = await _client.DeleteAsync($"/relationshipTypes/{relationshipType.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
