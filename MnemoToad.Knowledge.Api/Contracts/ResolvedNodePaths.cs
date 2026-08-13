using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MnemoToad.Knowledge.Api.Contracts;

/// <summary>The outcome of resolving one request entry's paths against its KnowledgeNode.</summary>
/// <param name="NodeId">The KnowledgeNode the paths were resolved against, echoed back from the request.</param>
/// <param name="Properties">
/// The paths that resolved successfully, keyed by the exact path string from the request. A
/// column/attribute path's value is a scalar; a media path's value is the stored media stanza
/// (<c>id</c>, <c>alt_text</c>, and any other client-supplied fields, all flat).
/// </param>
/// <param name="Errors">
/// The paths that couldn't be resolved (node not found, an edge in the path doesn't match any
/// relation, or the terminal's attribute/media key is missing), keyed the same way, each with a
/// message describing why. A path appears in exactly one of <paramref name="Properties"/> or
/// <paramref name="Errors"/>.
/// Omitted entirely when every path resolved.
/// </param>
public record ResolvedNodePaths(
    Guid NodeId,
    Dictionary<string, JsonNode?> Properties,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Dictionary<string, string>? Errors);
