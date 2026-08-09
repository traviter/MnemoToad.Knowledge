using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Knowledge.Api.Contracts;

/// <summary>The body for creating or replacing a RelationshipType.</summary>
/// <param name="Name">The relationship's forward name (e.g. "capitalOf"). Must be unique. Required.</param>
/// <param name="InverseName">The relationship's reverse name (e.g. "hasCapital"), for traversing the Path DSL backward.</param>
/// <param name="Description">Free-text description of what this relationship represents.</param>
public record RelationshipTypeRequest([Required] string Name, string? InverseName, string? Description);
