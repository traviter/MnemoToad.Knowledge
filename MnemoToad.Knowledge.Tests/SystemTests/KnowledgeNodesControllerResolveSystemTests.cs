using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.SystemTests;

[TestFixture]
public class KnowledgeNodesControllerResolveSystemTests
{
    private static readonly string TestDataDir =
        Path.Combine(AppContext.BaseDirectory, "SystemTests", "TestData", "Resolve");

    private MockedDbWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        _factory = new MockedDbWebApplicationFactory();
        _client = _factory.CreateClient();

        var graphJson = await File.ReadAllTextAsync(Path.Combine(TestDataDir, "graph.json"));
        var seedResponse = await _client.PostAsync("/testdata/seed",
            new StringContent(graphJson, Encoding.UTF8, "application/json"));
        seedResponse.EnsureSuccessStatusCode();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
        // No /testdata/cleanup call -- _factory.Dispose() destroys the whole SQLite :memory: DB.
    }

    private static JsonNode ReadCase(string name) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(TestDataDir, "cases", $"{name}.json")))!;

    private static readonly string[] SuccessCases =
    [
        "mixed-terminals", "media-with-metadata", "one-edge-relation", "duplicate-node-id",
        "missing-attribute-error", "missing-relation-error", "node-not-found"
    ];

    [TestCaseSource(nameof(SuccessCases))]
    public async Task Resolve_ReturnsExpectedProperties(string caseName)
    {
        var testCase = ReadCase(caseName);
        var response = await _client.PostAsync("/nodes/resolve",
            new StringContent(testCase["request"]!.ToJsonString(), Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var actual = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        var expected = testCase["response"];
        Assert.That(JsonNode.DeepEquals(actual, expected), Is.True,
            $"Expected:\n{expected}\nActual:\n{actual}");
    }

    private static readonly string[] BadRequestCases =
    [
        "empty-batch", "entry-empty-paths", "entry-missing-node-id",
        "entry-missing-paths", "invalid-path-syntax"
    ];

    [TestCaseSource(nameof(BadRequestCases))]
    public async Task Resolve_RejectsInvalidRequest(string caseName)
    {
        var testCase = ReadCase(caseName);
        var response = await _client.PostAsync("/nodes/resolve",
            new StringContent(testCase["request"]!.ToJsonString(), Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private static readonly string[] ResolveByTypeSuccessCases =
    [
        "resolve-by-type", "resolve-by-type-multi-hop", "resolve-by-type-partial-results", "resolve-by-type-empty-type"
    ];

    [TestCaseSource(nameof(ResolveByTypeSuccessCases))]
    public async Task ResolveByType_ReturnsExpectedProperties(string caseName)
    {
        var testCase = ReadCase(caseName);
        var response = await _client.PostAsync("/nodes/resolve/type",
            new StringContent(testCase["request"]!.ToJsonString(), Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var actual = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        var expected = testCase["response"];
        // GetAllAsync no longer orders its results, so compare by nodeId regardless of array order.
        Assert.That(JsonNode.DeepEquals(SortByNodeId(actual), SortByNodeId(expected)), Is.True,
            $"Expected:\n{expected}\nActual:\n{actual}");
    }

    private static JsonNode SortByNodeId(JsonNode? node)
    {
        var sorted = new JsonArray();
        foreach (var item in node!.AsArray().OrderBy(item => item!["nodeId"]!.GetValue<string>()))
            sorted.Add(item!.DeepClone());
        return sorted;
    }

    [Test]
    public async Task ResolveByType_UnknownNodeTypeName_ReturnsNotFound()
    {
        var testCase = ReadCase("resolve-by-type-unknown-name");
        var response = await _client.PostAsync("/nodes/resolve/type",
            new StringContent(testCase["request"]!.ToJsonString(), Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ResolveByType_MissingNodeTypeName_ReturnsBadRequest()
    {
        var testCase = ReadCase("resolve-by-type-missing-name");
        var response = await _client.PostAsync("/nodes/resolve/type",
            new StringContent(testCase["request"]!.ToJsonString(), Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
