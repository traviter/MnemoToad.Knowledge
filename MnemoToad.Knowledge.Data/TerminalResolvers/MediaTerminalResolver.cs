using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.DbUtil;
using MnemoToad.Knowledge.Data.Entities;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.TerminalResolvers;

public class MediaTerminalResolver : ITerminalResolver
{
    private readonly IQueryTransform<KnowledgeNode, KnowledgeNodeMedia> _queryTransform;
    private readonly IEntityJsonMapper<KnowledgeNodeMedia> _mediaMapper;

    public MediaTerminalResolver(IQueryTransform<KnowledgeNode, KnowledgeNodeMedia> queryTransform, IEntityJsonMapper<KnowledgeNodeMedia> mediaMapper)
    {
        _queryTransform = queryTransform;
        _mediaMapper = mediaMapper;
    }

    public async Task<Result<JsonNode>> ResolveAsync(IQueryable<KnowledgeNode> targetNode, string terminalName)
    {
        var media = await _queryTransform.Transform(targetNode, terminalName).FirstOrDefaultAsync();
        return media is null
            ? new Error("Path could not be resolved.")
            : _mediaMapper.ToJson(media);
    }
}
