using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Knowledge.Api.Contracts;

/// <summary>The body for creating or replacing a RelationshipType.</summary>
/// <param name="Name">The relationship's name (e.g. "capitalOf"). Must be unique. Required.</param>
/// <param name="Description">Free-text description of what this relationship represents.</param>
public record RelationshipTypeRequest([Required] string Name, string? Description);
