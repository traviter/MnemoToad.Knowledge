using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.PathResolution;

public record ResolvedNodeRow(Guid NodeId, Dictionary<string, JsonNode?> Properties, Dictionary<string, string>? Errors);
