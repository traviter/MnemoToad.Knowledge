using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.QueryTransforms;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.TerminalResolvers;

public class MediaTerminalResolver : ITerminalResolver
{
    private readonly IQueryTransform<KnowledgeNode, KnowledgeNodeMedia> _queryTransform;

    public MediaTerminalResolver(IQueryTransform<KnowledgeNode, KnowledgeNodeMedia> queryTransform) => _queryTransform = queryTransform;

    public async Task<Result<JsonNode>> ResolveAsync(IQueryable<KnowledgeNode> targetNode, string terminalName)
    {
        var media = await _queryTransform.Transform(targetNode, terminalName).FirstOrDefaultAsync();
        if (media is null)
            return new Error("Path could not be resolved.");

        var result = new JsonObject
        {
            ["id"] = media.MediaAssetId.ToString(),
            ["alt_text"] = media.AltText
        };
        var extra = ExtractExtraMetadata(media.Metadata);
        if (extra is { Count: > 0 })
            result["metadata"] = extra;

        return result;
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
