using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.Entities;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.TerminalResolvers;

public class ColumnTerminalResolver : ITerminalResolver
{
    private static readonly HashSet<string> ValidColumns = ["id", "canonicalName", "description"];

    public async Task<Result<JsonNode>> ResolveAsync(IQueryable<KnowledgeNode> targetNode, string terminalName)
    {
        if (!ValidColumns.Contains(terminalName))
            return new Error($"No column named '{terminalName}' on KnowledgeNode.");

        var node = await targetNode.FirstOrDefaultAsync();
        if (node is null)
            return new Error("Path could not be resolved.");

        return terminalName switch
        {
            "id" => JsonValue.Create(node.Id.ToString())!,
            "canonicalName" => JsonValue.Create(node.CanonicalName)!,
            "description" => node.Description is null
                ? new Error("Path could not be resolved.")
                : JsonValue.Create(node.Description)!,
            _ => new Error($"No column named '{terminalName}' on KnowledgeNode.")
        };
    }
}
