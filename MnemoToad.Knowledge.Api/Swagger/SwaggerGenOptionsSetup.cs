using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MnemoToad.Knowledge.Api.Swagger;

public class SwaggerGenOptionsSetup : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "MnemoToad Knowledge API",
            Version = "v1",
            Description = "A knowledge graph of real-world things",
        });

        // GenerateDocumentationFile in both this project and MnemoToad.Knowledge.Data (entities
        // are returned directly as responses, no separate response DTOs) drives these files.
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "MnemoToad.Knowledge.Api.xml"));
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "MnemoToad.Knowledge.Data.xml"));

        options.SchemaFilter<ExampleSchemaFilter>();
    }
}
