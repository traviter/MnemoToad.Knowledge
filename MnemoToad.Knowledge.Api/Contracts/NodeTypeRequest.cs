using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Knowledge.Api.Contracts;

/// <summary>The body for creating or replacing a NodeType.</summary>
/// <param name="Name">The NodeType's display name. Must be unique. Required.</param>
/// <param name="Description">Free-text description of what this NodeType represents.</param>
public record NodeTypeRequest([Required] string Name, string? Description);
