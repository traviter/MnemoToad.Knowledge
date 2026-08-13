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

        IQueryable<Guid> currentIds = _db.KnowledgeNode.AsNoTracking()
            .Where(n => n.Id == query.NodeId)
            .Select(n => n.Id);
        foreach (var hop in expression!.Hops)
            currentIds = ExtendPath(currentIds, hop);

        var (value, error) = expression.TerminalKind switch
        {
            PathTerminalKind.Column => await ResolveColumnAsync(currentIds, expression.TerminalName),
            PathTerminalKind.Attribute => await ResolveAttributeAsync(currentIds, expression.TerminalName),
            PathTerminalKind.Media => await ResolveMediaAsync(currentIds, expression.TerminalName),
            _ => (null, "Unknown terminal kind.")
        };
        return new ResolvedPath(query.NodeId, query.Path, value, error);
    }

    private IQueryable<Guid> ExtendPath(IQueryable<Guid> currentIds, string hopName) =>
        from id in currentIds
        join r in _db.KnowledgeRelation on id equals r.SourceNodeId
        join rt in _db.RelationshipType on r.RelationshipTypeId equals rt.Id
        where rt.Name == hopName
        select r.TargetNodeId;

    private async Task<(JsonNode? Value, string? Error)> ResolveColumnAsync(IQueryable<Guid> currentIds, string columnName)
    {
        if (!ValidColumns.Contains(columnName))
            return (null, $"No column named '{columnName}' on KnowledgeNode.");

        var node = await (from id in currentIds join n in _db.KnowledgeNode on id equals n.Id select n)
            .AsNoTracking()
            .FirstOrDefaultAsync();
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

    private async Task<(JsonNode? Value, string? Error)> ResolveAttributeAsync(IQueryable<Guid> currentIds, string key)
    {
        var attribute = await (from id in currentIds
                                join a in _db.KnowledgeNodeAttribute on id equals a.KnowledgeNodeId
                                where a.Key == key
                                select a)
            .AsNoTracking()
            .FirstOrDefaultAsync();
        return attribute is null
            ? (null, "Path could not be resolved.")
            : (attribute.Value, null);
    }

    private async Task<(JsonNode? Value, string? Error)> ResolveMediaAsync(IQueryable<Guid> currentIds, string key)
    {
        var media = await (from id in currentIds
                            join m in _db.KnowledgeNodeMedia on id equals m.KnowledgeNodeId
                            where m.Key == key
                            select m)
            .AsNoTracking()
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
