using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MnemoToad.Knowledge.Api.Contracts;

/// <summary>The outcome of resolving one request entry's paths against its KnowledgeNode.</summary>
/// <param name="NodeId">The KnowledgeNode the paths were resolved against, echoed back from the request.</param>
/// <param name="Properties">
/// The paths that resolved successfully, keyed by the exact path string from the request. A
/// column/attribute path's value is a scalar; a media path's value is an
/// <c>{ id, alt_text, metadata }</c> object.
/// </param>
/// <param name="Errors">
/// The paths that couldn't be resolved (node not found, a hop's relation doesn't exist, or the
/// terminal's attribute/media key is missing), keyed the same way, each with a message describing
/// why. A path appears in exactly one of <paramref name="Properties"/> or <paramref name="Errors"/>.
/// Omitted entirely when every path resolved.
/// </param>
public record ResolvedNodePaths(
    Guid NodeId,
    Dictionary<string, JsonNode?> Properties,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Dictionary<string, string>? Errors);
