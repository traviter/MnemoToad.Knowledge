using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.PathResolution;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.Repositories;

public class PathResolutionRepository : IPathResolutionRepository
{
    private static readonly HashSet<string> ValidColumns = ["id", "canonicalName", "description"];

    private readonly IAppDbContext _db;
    private readonly IPathExpressionParser _parser;

    public PathResolutionRepository(IAppDbContext db, IPathExpressionParser parser)
    {
        _db = db;
        _parser = parser;
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
        var (value, error) = expression.TerminalKind switch
        {
            PathTerminalKind.Column => await ResolveColumnAsync(targetNode, expression.TerminalName),
            PathTerminalKind.Attribute => await ResolveAttributeAsync(targetNode, expression.TerminalName),
            PathTerminalKind.Media => await ResolveMediaAsync(targetNode, expression.TerminalName),
            _ => (null, "Unknown terminal kind.")
        };
        return new ResolvedPath(query.NodeId, query.Path, value, error);
    }

    private IQueryable<KnowledgeNode> TraversePathToNode(Guid startingNodeId, IReadOnlyList<string> edges)
    {
        IQueryable<KnowledgeNode> currentNodes = _db.KnowledgeNode.AsNoTracking().Where(n => n.Id == startingNodeId);
        foreach (var edge in edges)
            currentNodes = TraverseEdge(currentNodes, edge);
        return currentNodes;
    }

    private IQueryable<KnowledgeNode> TraverseEdge(IQueryable<KnowledgeNode> currentNode, string edgeName) =>
        from n in currentNode
        join r in _db.KnowledgeRelation on n.Id equals r.SourceNodeId
        join rt in _db.RelationshipType on r.RelationshipTypeId equals rt.Id
        where rt.Name == edgeName
        join target in _db.KnowledgeNode on r.TargetNodeId equals target.Id
        select target;

    private async Task<(JsonNode? Value, string? Error)> ResolveColumnAsync(IQueryable<KnowledgeNode> targetNode, string columnName)
    {
        if (!ValidColumns.Contains(columnName))
            return (null, $"No column named '{columnName}' on KnowledgeNode.");

        var node = await targetNode.FirstOrDefaultAsync();
        if (node is null)
            return (null, "Path could not be resolved.");

        return columnName switch
        {
            "id" => (JsonValue.Create(node.Id.ToString()), null),
            "canonicalName" => (JsonValue.Create(node.CanonicalName), null),
            "description" => (node.Description is null ? null : JsonValue.Create(node.Description), null),
            _ => (null, $"No column named '{columnName}' on KnowledgeNode.")
        };
    }

    private async Task<(JsonNode? Value, string? Error)> ResolveAttributeAsync(IQueryable<KnowledgeNode> targetNode, string key)
    {
        var attribute = await (from n in targetNode
                                join a in _db.KnowledgeNodeAttribute on n.Id equals a.KnowledgeNodeId
                                where a.Key == key
                                select a)
            .FirstOrDefaultAsync();
        return attribute is null
            ? (null, "Path could not be resolved.")
            : (attribute.Value, null);
    }

    private async Task<(JsonNode? Value, string? Error)> ResolveMediaAsync(IQueryable<KnowledgeNode> targetNode, string key)
    {
        var media = await (from n in targetNode
                            join m in _db.KnowledgeNodeMedia on n.Id equals m.KnowledgeNodeId
                            where m.Key == key
                            select m)
            .FirstOrDefaultAsync();
        if (media is null)
            return (null, "Path could not be resolved.");

        var result = new JsonObject
        {
            ["id"] = media.MediaAssetId.ToString(),
            ["alt_text"] = media.AltText
        };
        var extra = ExtractExtraMetadata(media.Metadata);
        if (extra is { Count: > 0 })
            result["metadata"] = extra;

        return (result, null);
    }

    private static JsonObject? ExtractExtraMetadata(JsonObject? stored)
    {
        if (stored is null)
            return null;

        var extra = new JsonObject();
        foreach (var (key, value) in stored)
        {
            if (key is "id" or "alt_text")
                continue;
            extra[key] = value?.DeepClone();
        }
        return extra;
    }
}
