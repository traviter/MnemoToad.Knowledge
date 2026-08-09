using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Api.Contracts;

/// <summary>The body for creating or replacing a KnowledgeNode.</summary>
/// <param name="NodeTypeId">The NodeType this node is classified under. Must reference an existing NodeType. Required.</param>
/// <param name="CanonicalName">The node's display name. Must be unique within its NodeType. Required.</param>
/// <param name="Description">Free-text description of the node.</param>
/// <param name="Attributes">
/// Scalar key/value facts about the node (e.g. <c>{ "isoCode": "FR", "population": 68000000 }</c>).
/// Values must be scalars — arrays and nested objects aren't supported. On update this is a full
/// replace: an omitted key is removed from the node.
/// </param>
/// <param name="Media">
/// Media links keyed by name (e.g. <c>"flag"</c>). Each entry must include a valid <c>id</c> (an
/// existing MediaAsset's id) and <c>alt_text</c>; the entire submitted object is stored and
/// returned verbatim. On update this is a full replace, same as <paramref name="Attributes"/>.
/// </param>
public record KnowledgeNodeRequest(
    [Required] Guid? NodeTypeId,
    [Required] string CanonicalName,
    string? Description,
    Dictionary<string, JsonValue?>? Attributes = null,
    Dictionary<string, JsonObject?>? Media = null);
