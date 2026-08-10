using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using MnemoToad.Knowledge.Api.Contracts;
using MnemoToad.Knowledge.Data.Entities;

namespace MnemoToad.Knowledge.Api.Swagger;

/// <summary>
/// Attaches a sample JSON payload to specific request/response types in the generated Swagger
/// schema. Add an entry here (not a per-type attribute/class) whenever a new type needs one.
/// </summary>
public class ExampleSchemaFilter : ISchemaFilter
{
    private static readonly Dictionary<Type, Func<JsonNode>> Examples = new()
    {
        [typeof(NodeTypeRequest)] = () => new JsonObject
        {
            ["name"] = "Planet",
            ["description"] = "A celestial body orbiting a star.",
        },
        [typeof(NodeType)] = () => new JsonObject
        {
            ["id"] = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            ["name"] = "Planet",
            ["description"] = "A celestial body orbiting a star.",
        },
        [typeof(RelationshipTypeRequest)] = () => new JsonObject
        {
            ["name"] = "capitalOf",
            ["inverseName"] = "hasCapital",
            ["description"] = "Links a capital city to the country it governs.",
        },
        [typeof(RelationshipType)] = () => new JsonObject
        {
            ["id"] = "6c9b2f2e-2a3d-4e33-9b0a-2f1a6d4c8e10",
            ["name"] = "capitalOf",
            ["inverseName"] = "hasCapital",
            ["description"] = "Links a capital city to the country it governs.",
        },
        [typeof(KnowledgeNodeRequest)] = () => new JsonObject
        {
            ["nodeTypeId"] = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            ["canonicalName"] = "France",
            ["description"] = "A country in Western Europe.",
            ["attributes"] = new JsonObject
            {
                ["isoCode"] = "FR",
                ["population"] = 68000000,
            },
            ["media"] = new JsonObject
            {
                ["flag"] = new JsonObject
                {
                    ["id"] = "a1b2c3d4-e5f6-4789-9abc-def012345678",
                    ["alt_text"] = "The flag of France",
                },
            },
        },
        [typeof(KnowledgeNode)] = () => new JsonObject
        {
            ["id"] = "b2c3d4e5-f6a7-4890-9abc-def012345679",
            ["nodeTypeId"] = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            ["canonicalName"] = "France",
            ["description"] = "A country in Western Europe.",
            ["attributes"] = new JsonObject
            {
                ["isoCode"] = "FR",
                ["population"] = 68000000,
            },
            ["media"] = new JsonObject
            {
                ["flag"] = new JsonObject
                {
                    ["id"] = "a1b2c3d4-e5f6-4789-9abc-def012345678",
                    ["alt_text"] = "The flag of France",
                },
            },
        },
        [typeof(KnowledgeRelationRequest)] = () => new JsonObject
        {
            ["sourceNodeId"] = "b2c3d4e5-f6a7-4890-9abc-def012345679",
            ["relationshipTypeId"] = "6c9b2f2e-2a3d-4e33-9b0a-2f1a6d4c8e10",
            ["targetNodeId"] = "c3d4e5f6-a7b8-4901-9abc-def01234567a",
        },
        [typeof(KnowledgeRelation)] = () => new JsonObject
        {
            ["id"] = "d4e5f6a7-b8c9-4012-9abc-def01234567b",
            ["sourceNodeId"] = "b2c3d4e5-f6a7-4890-9abc-def012345679",
            ["relationshipTypeId"] = "6c9b2f2e-2a3d-4e33-9b0a-2f1a6d4c8e10",
            ["targetNodeId"] = "c3d4e5f6-a7b8-4901-9abc-def01234567a",
        },
        [typeof(MediaAssetRequest)] = () => new JsonObject
        {
            ["url"] = "https://flagcdn.com/fr.svg",
        },
        [typeof(MediaAsset)] = () => new JsonObject
        {
            ["id"] = "a1b2c3d4-e5f6-4789-9abc-def012345678",
            ["url"] = "https://flagcdn.com/fr.svg",
        },
        [typeof(List<ResolvePathsRequestItem>)] = () => new JsonArray
        {
            new JsonObject
            {
                ["nodeId"] = "b2c3d4e5-f6a7-4890-9abc-def012345679",
                ["paths"] = new JsonArray { "_canonicalName", ".population", "#flag" },
            },
            new JsonObject
            {
                ["nodeId"] = "c3d4e5f6-a7b8-4901-9abc-def01234567a",
                ["paths"] = new JsonArray { "#coatOfArms", ".gdp" },
            },
            new JsonObject
            {
                ["nodeId"] = "00000000-0000-0000-0000-000000000000",
                ["paths"] = new JsonArray { "_canonicalName" },
            },
        },
        [typeof(IEnumerable<ResolvedNodePaths>)] = () => new JsonArray
        {
            new JsonObject
            {
                ["nodeId"] = "b2c3d4e5-f6a7-4890-9abc-def012345679",
                ["properties"] = new JsonObject
                {
                    ["_canonicalName"] = "France",
                    [".population"] = 68000000,
                    ["#flag"] = new JsonObject
                    {
                        ["id"] = "a1b2c3d4-e5f6-4789-9abc-def012345678",
                        ["alt_text"] = "The flag of France",
                    },
                },
            },
            new JsonObject
            {
                ["nodeId"] = "c3d4e5f6-a7b8-4901-9abc-def01234567a",
                ["properties"] = new JsonObject
                {
                    ["#coatOfArms"] = new JsonObject
                    {
                        ["id"] = "d4e5f6a7-b8c9-4012-9abc-def01234567b",
                        ["alt_text"] = "The coat of arms of Paris",
                        ["metadata"] = new JsonObject { ["credit"] = "Wikimedia Commons" },
                    },
                },
                ["errors"] = new JsonObject
                {
                    [".gdp"] = "No attribute named 'gdp' on this node.",
                },
            },
            new JsonObject
            {
                ["nodeId"] = "00000000-0000-0000-0000-000000000000",
                ["properties"] = new JsonObject(),
                ["errors"] = new JsonObject
                {
                    ["_canonicalName"] = "No KnowledgeNode exists with that id.",
                },
            },
        },
    };

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is OpenApiSchema concrete && Examples.TryGetValue(context.Type, out var example))
        {
            concrete.Example = example();
        }
    }
}
