using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Knowledge.Api.Contracts;

/// <summary>A NodeType name and the Property Path DSL expressions to resolve against every KnowledgeNode of that type.</summary>
/// <param name="NodeTypeName">The NodeType's name. Required.</param>
/// <param name="Paths">The Property Path DSL expressions to resolve against each matching node. Must contain at least one. Required.</param>
public record ResolveByNodeTypeRequest(
    [Required] string? NodeTypeName,
    [Required, MinLength(1), ValidPathExpression] List<string>? Paths);
