using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.QueryTransforms;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.TerminalResolvers;

public class AttributeTerminalResolver : ITerminalResolver
{
    private readonly IQueryTransform<KnowledgeNode, KnowledgeNodeAttribute> _queryTransform;

    public AttributeTerminalResolver(IQueryTransform<KnowledgeNode, KnowledgeNodeAttribute> queryTransform) => _queryTransform = queryTransform;

    public async Task<Result<JsonNode>> ResolveAsync(IQueryable<KnowledgeNode> targetNode, string terminalName)
    {
        var attribute = await _queryTransform.Transform(targetNode, terminalName).FirstOrDefaultAsync();
        return attribute is { Value: not null }
            ? attribute.Value
            : new Error("Path could not be resolved.");
    }
}
