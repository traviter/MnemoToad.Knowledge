using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MnemoToad.Knowledge.Api.Contracts;

/// <summary>
/// One resolved combination of an entry's paths against its KnowledgeNode. For
/// <c>POST /nodes/resolve</c>, exactly one of these per request entry. For
/// <c>POST /nodes/resolve/type</c>, a <see cref="NodeId"/> can repeat — one entry per combination
/// its requested edges actually matched (the cartesian product of however many relations each edge
/// traversal found), and a node whose edge traversals matched nothing contributes no entries at all
/// rather than one with missing values.
/// </summary>
/// <param name="NodeId">The KnowledgeNode this entry was resolved against, echoed back from the request.</param>
/// <param name="Properties">
/// The paths that resolved successfully in this entry, keyed by the exact path string from the
/// request. A column/attribute path's value is a scalar; a media path's value is the stored media
/// stanza (<c>id</c>, <c>alt_text</c>, and any other client-supplied fields, all flat).
/// </param>
/// <param name="Errors">
/// The paths that reached this entry's node but couldn't resolve their terminal (an unknown
/// column, or a missing attribute/media key), keyed the same way, each with a message describing
/// why. A path in this entry appears in exactly one of <paramref name="Properties"/> or
/// <paramref name="Errors"/>. Omitted entirely when every path in this entry resolved.
/// </param>
public record ResolvedNodePaths(
    Guid NodeId,
    Dictionary<string, JsonNode?> Properties,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Dictionary<string, string>? Errors);
