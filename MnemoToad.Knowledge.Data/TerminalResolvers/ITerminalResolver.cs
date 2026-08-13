using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.Entities;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.TerminalResolvers;

public interface ITerminalResolver
{
    Task<Result<JsonNode>> ResolveAsync(IQueryable<KnowledgeNode> targetNode, string terminalName);
}
