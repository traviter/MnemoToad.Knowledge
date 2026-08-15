using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Api.Json;
using MnemoToad.Knowledge.Api.Swagger;
using MnemoToad.Knowledge.Data;
using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.EntityMappers;
using MnemoToad.Knowledge.Data.PathResolution;
using MnemoToad.Knowledge.Data.QueryTransforms;
using MnemoToad.Knowledge.Data.TerminalResolvers;
using MnemoToad.Knowledge.Data.Repositories;

namespace MnemoToad.Knowledge.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Wires up routing/model-binding/action-invocation for [ApiController] classes.
        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new ScalarJsonValueConverter()));

        // Translates a ValidationException escaping any action into a 400 ProblemDetails response.
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddProblemDetails();

        // Lets Swashbuckle discover our controllers' routes/parameters/response types.
        services.AddEndpointsApiExplorer();
        // Registers the OpenAPI document generator (built from the explorer data above).
        // Nothing is written to disk here — the JSON is generated in memory per-request by
        // app.UseSwagger() below. Configuration lives in SwaggerGenOptionsSetup.
        services.ConfigureOptions<SwaggerGenOptionsSetup>();
        services.AddSwaggerGen();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")).UseSnakeCaseNamingConvention());
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IEntityJsonMapper<KnowledgeNodeMedia>, KnowledgeNodeMediaJsonMapper>();

        services.AddScoped<INodeTypeRepository, NodeTypeRepository>();
        services.AddScoped<IKnowledgeNodeRepository, KnowledgeNodeRepository>();
        services.AddScoped<IRelationshipTypeRepository, RelationshipTypeRepository>();
        services.AddScoped<IKnowledgeRelationRepository, KnowledgeRelationRepository>();
        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
        services.AddScoped<IPathExpressionParser, PathExpressionParser>();
        services.AddScoped<ITerminalResolverFactory, TerminalResolverFactory>();
        services.AddScoped<IQueryTransform<KnowledgeNode, KnowledgeNode>, NodeRelationshipQueryTransform>();
        services.AddScoped<IPathResolutionRepository, PathResolutionRepository>();

        return services;
    }
}
