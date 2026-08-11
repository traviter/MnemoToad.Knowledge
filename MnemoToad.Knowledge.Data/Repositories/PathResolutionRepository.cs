using Microsoft.EntityFrameworkCore;
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

        var exists = await _db.KnowledgeNode.AsNoTracking().AnyAsync(n => n.Id == query.NodeId);
        if (!exists)
            return new ResolvedPath(query.NodeId, query.Path, null, "No KnowledgeNode exists with that id.");

        var currentNodeId = query.NodeId;
        foreach (var hop in expression!.Hops)
        {
            var next = await FindHopTargetAsync(currentNodeId, hop);
            if (next is null)
                return new ResolvedPath(query.NodeId, query.Path, null, $"No relation named '{hop}' from this node.");
            currentNodeId = next.Value;
        }

        var (value, error) = expression.TerminalKind switch
        {
            PathTerminalKind.Column => await ResolveColumnAsync(currentNodeId, expression.TerminalName),
            PathTerminalKind.Attribute => await ResolveAttributeAsync(currentNodeId, expression.TerminalName),
            PathTerminalKind.Media => await ResolveMediaAsync(currentNodeId, expression.TerminalName),
            _ => (null, "Unknown terminal kind.")
        };
        return new ResolvedPath(query.NodeId, query.Path, value, error);
    }

    private async Task<Guid?> FindHopTargetAsync(Guid nodeId, string hopName)
    {
        var forward = await _db.KnowledgeRelation
            .Where(r => r.SourceNodeId == nodeId)
            .Join(_db.RelationshipType, r => r.RelationshipTypeId, rt => rt.Id, (r, rt) => new { r.TargetNodeId, rt.Name })
            .Where(x => x.Name == hopName)
            .Select(x => (Guid?)x.TargetNodeId)
            .FirstOrDefaultAsync();
        if (forward is not null)
            return forward;

        return await _db.KnowledgeRelation
            .Where(r => r.TargetNodeId == nodeId)
            .Join(_db.RelationshipType, r => r.RelationshipTypeId, rt => rt.Id, (r, rt) => new { r.SourceNodeId, rt.InverseName })
            .Where(x => x.InverseName == hopName)
            .Select(x => (Guid?)x.SourceNodeId)
            .FirstOrDefaultAsync();
    }

    private async Task<(JsonNode? Value, string? Error)> ResolveColumnAsync(Guid nodeId, string columnName)
    {
        if (!ValidColumns.Contains(columnName))
            return (null, $"No column named '{columnName}' on KnowledgeNode.");

        var node = await _db.KnowledgeNode.AsNoTracking().FirstAsync(n => n.Id == nodeId);
        return columnName switch
        {
            "id" => (JsonValue.Create(node.Id.ToString()), null),
            "canonicalName" => (JsonValue.Create(node.CanonicalName), null),
            "description" => (node.Description is null ? null : JsonValue.Create(node.Description), null),
            _ => (null, $"No column named '{columnName}' on KnowledgeNode.")
        };
    }

    private async Task<(JsonNode? Value, string? Error)> ResolveAttributeAsync(Guid nodeId, string key)
    {
        var attribute = await _db.KnowledgeNodeAttribute.AsNoTracking()
            .FirstOrDefaultAsync(a => a.KnowledgeNodeId == nodeId && a.Key == key);
        return attribute is null
            ? (null, $"No attribute named '{key}' on this node.")
            : (attribute.Value, null);
    }

    private async Task<(JsonNode? Value, string? Error)> ResolveMediaAsync(Guid nodeId, string key)
    {
        var media = await _db.KnowledgeNodeMedia.AsNoTracking()
            .FirstOrDefaultAsync(m => m.KnowledgeNodeId == nodeId && m.Key == key);
        if (media is null)
            return (null, $"No media named '{key}' on this node.");

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
