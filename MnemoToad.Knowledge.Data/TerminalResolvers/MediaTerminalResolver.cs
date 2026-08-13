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
        return media is null
            ? new Error("Path could not be resolved.")
            : media.ToJson();
    }
}
