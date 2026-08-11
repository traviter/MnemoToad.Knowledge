using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.PathResolution;

public record ResolvedPath(Guid NodeId, string Path, JsonNode? Value, string? Error);
