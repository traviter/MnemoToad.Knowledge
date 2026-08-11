using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Knowledge.Api.Contracts;

/// <summary>A KnowledgeNode and the Property Path DSL expressions to resolve against it.</summary>
/// <param name="NodeId">The KnowledgeNode to resolve the paths against. Required.</param>
/// <param name="Paths">The Property Path DSL expressions to resolve. Must contain at least one. Required.</param>
public record ResolvePathsRequestItem(
    [Required] Guid? NodeId,
    [Required, MinLength(1), ValidPathExpression] List<string>? Paths);
