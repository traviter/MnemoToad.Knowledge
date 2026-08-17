using Microsoft.Extensions.DependencyInjection;
using MnemoToad.Knowledge.Data.DbUtil;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.Entities.Operations;
using MnemoToad.Knowledge.Data.PathResolution;
using MnemoToad.Knowledge.Data.QueryTransforms;
using MnemoToad.Knowledge.Data.Repositories;
using MnemoToad.Knowledge.Data.TerminalResolvers;

namespace MnemoToad.Knowledge.Data.Configuration;

public static class DataServiceRegistration
{
    public static IServiceCollection AddDataServices(IServiceCollection services)
    {
        services.AddSingleton<IEntityJsonMapper<KnowledgeNodeMedia>, KnowledgeNodeMediaJsonMapper>();
        services.AddScoped<INodeTypeRepository, NodeTypeRepository>();
        services.AddScoped<IKnowledgeNodeRepository, KnowledgeNodeRepository>();
        services.AddScoped<IRelationshipTypeRepository, RelationshipTypeRepository>();
        services.AddScoped<IKnowledgeRelationRepository, KnowledgeRelationRepository>();
        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
        services.AddSingleton<IPathExpressionParser, PathExpressionParser>();
        services.AddScoped<ITerminalResolverFactory, TerminalResolverFactory>();
        services.AddScoped<IQueryTransform<KnowledgeNode, KnowledgeNode>, NodeRelationshipQueryTransform>();
        services.AddScoped<IPathResolutionRepository, PathResolutionRepository>();
        return services;
    }
}
