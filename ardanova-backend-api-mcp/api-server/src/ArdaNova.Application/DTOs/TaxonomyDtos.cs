namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

/// <summary>
/// One entry in the shared vocabulary surface: a suggestion/filter facet, never a stored
/// constraint. See Services/Implementations/AGENTS.md §Taxonomy.
/// </summary>
public record TaxonomyTermDto
{
    public string Slug { get; init; } = null!;
    public TaxonomyKind Kind { get; init; }
    public string Label { get; init; } = null!;
    public string? Description { get; init; }
    /// <summary>The parent term's slug (a SKILL's sector), never an id - the id is an internal key.</summary>
    public string? ParentSlug { get; init; }
    public int SortOrder { get; init; }
}
