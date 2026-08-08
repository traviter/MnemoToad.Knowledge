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
    };

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is OpenApiSchema concrete && Examples.TryGetValue(context.Type, out var example))
        {
            concrete.Example = example();
        }
    }
}
