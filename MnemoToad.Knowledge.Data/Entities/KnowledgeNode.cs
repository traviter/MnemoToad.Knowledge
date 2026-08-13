using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MnemoToad.Knowledge.Data.Entities;

/// <summary>
/// A single "thing" in the graph (e.g. "France", "Mercury"), classified by a NodeType, with
/// scalar attributes and media links embedded directly rather than exposed as separate resources.
/// </summary>
public class KnowledgeNode
{
    /// <summary>The KnowledgeNode's id.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The NodeType this node is classified under.</summary>
    public Guid NodeTypeId { get; set; }

    /// <summary>The node's display name. Unique within its NodeType.</summary>
    public string CanonicalName { get; set; } = string.Empty;

    /// <summary>Free-text description of the node.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Scalar key/value facts about the node. Omitted entirely (not just empty) on list responses —
    /// only present when fetching a single node.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonValue>? Attributes { get; set; }

    /// <summary>
    /// Media links keyed by name (e.g. <c>"flag"</c>). Omitted entirely (not just empty) on list
    /// responses — only present when fetching a single node.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonObject>? Media { get; set; }
}
