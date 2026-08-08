using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using MnemoToad.Knowledge.Api.Json;
using MnemoToad.Knowledge.Api.Swagger;
using MnemoToad.Knowledge.Data;
using MnemoToad.Knowledge.Data.Repositories;

namespace MnemoToad.Knowledge.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Wires up routing/model-binding/action-invocation for [ApiController] classes.
        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new ScalarJsonValueConverter()));

        // Lets Swashbuckle discover our controllers' routes/parameters/response types.
        services.AddEndpointsApiExplorer();
        // Registers the OpenAPI document generator (built from the explorer data above).
        // Nothing is written to disk here — the JSON is generated in memory per-request by
        // app.UseSwagger() below.
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "MnemoToad Knowledge API",
                Version = "v1",
                Description = "A ConceptNet-style graph of nodes, relationships, attributes, and media.",
            });

            // GenerateDocumentationFile in both this project and MnemoToad.Knowledge.Data (entities
            // are returned directly as responses, no separate response DTOs) drives these files.
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "MnemoToad.Knowledge.Api.xml"));
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "MnemoToad.Knowledge.Data.xml"));

            options.SchemaFilter<ExampleSchemaFilter>();
        });

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")).UseSnakeCaseNamingConvention());
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<INodeTypeRepository, NodeTypeRepository>();
        services.AddScoped<IKnowledgeNodeRepository, KnowledgeNodeRepository>();
        services.AddScoped<IRelationshipTypeRepository, RelationshipTypeRepository>();
        services.AddScoped<IKnowledgeRelationRepository, KnowledgeRelationRepository>();
        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();

        return services;
    }
}
