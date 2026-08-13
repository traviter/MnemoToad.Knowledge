using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.PathResolution;
using MnemoToad.Knowledge.Data.QueryTransforms;
using MnemoToad.Knowledge.Data.TerminalResolvers;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.Repositories;

public class PathResolutionRepository : IPathResolutionRepository
{
    private readonly IAppDbContext _db;
    private readonly IPathExpressionParser _parser;
    private readonly ITerminalResolverFactory _terminalResolverFactory;
    private readonly IQueryTransform<KnowledgeNode, KnowledgeNode> _edgeQueryTransform;

    public PathResolutionRepository(IAppDbContext db, IPathExpressionParser parser, ITerminalResolverFactory terminalResolverFactory,
        IQueryTransform<KnowledgeNode, KnowledgeNode> edgeQueryTransform)
    {
        _db = db;
        _parser = parser;
        _terminalResolverFactory = terminalResolverFactory;
        _edgeQueryTransform = edgeQueryTransform;
    }

    public async Task<List<ResolvedPath>> ResolveAsync(IReadOnlyList<PathResolutionQuery> queries)
    {
        var results = new List<ResolvedPath>(queries.Count);
        foreach (var query in queries)
            results.Add(await ResolveAsync(query));
        return results;
    }

    public async Task<ResolvedPath> ResolveAsync(PathResolutionQuery query)
    {
        if (!_parser.TryParse(query.Path, out var expression))
            return new ResolvedPath(query.NodeId, query.Path, null, "Invalid Path DSL syntax.");

        var targetNode = TraversePathToNode(query.NodeId, expression!.Edges);
        var resolver = _terminalResolverFactory.GetResolver(expression.TerminalKind);
        var result = await resolver.ResolveAsync(targetNode, expression.TerminalName);
        return result switch
        {
            Result<JsonNode>.Success success => new ResolvedPath(query.NodeId, query.Path, success.Value, null),
            Result<JsonNode>.Failure failure => new ResolvedPath(query.NodeId, query.Path, null, failure.Message),
            _ => throw new UnreachableException()
        };
    }

    private IQueryable<KnowledgeNode> TraversePathToNode(Guid startingNodeId, IReadOnlyList<string> edges)
    {
        IQueryable<KnowledgeNode> currentNode = _db.KnowledgeNode.AsNoTracking().Where(n => n.Id == startingNodeId);
        foreach (var edge in edges)
            currentNode = _edgeQueryTransform.Transform(currentNode, edge);
        return currentNode;
    }
}
