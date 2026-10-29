namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;

/// <summary>The shared vocabulary surface. See Services/Implementations/AGENTS.md §Taxonomy.</summary>
public class TaxonomyService : ITaxonomyService
{
    private readonly IRepository<TaxonomyTerm> _repository;

    public TaxonomyService(IRepository<TaxonomyTerm> repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<TaxonomyTermDto>>> GetActiveTermsAsync(
        string? kind,
        CancellationToken ct = default)
    {
        TaxonomyKind? requestedKind = null;
        if (!string.IsNullOrWhiteSpace(kind))
        {
            if (!Enum.TryParse<TaxonomyKind>(kind, ignoreCase: true, out var parsed))
                return Result<IReadOnlyList<TaxonomyTermDto>>.ValidationError(
                    $"Unknown taxonomy kind '{kind}'. Expected SECTOR, SKILL or PROJECT_TYPE.");
            requestedKind = parsed;
        }

        // FindProjectedAsync (not Query()+ToListAsync) so this runs the same way over the real EF
        // repository and over InMemoryRepository in tests - the latter's Query() is plain
        // LINQ-to-Objects and never implements IAsyncEnumerable. Parent is a navigation, not an
        // Include target: EF Core translates Parent.slug into a LEFT JOIN inside the projection,
        // so a SKILL's sector comes back as a slug, never an id. Ordering happens after
        // materializing because FindProjectedAsync has no OrderBy of its own.
        var terms = await _repository.FindProjectedAsync(
            term => term.isActive && (requestedKind == null || term.kind == requestedKind),
            term => new TaxonomyTermDto
            {
                Slug = term.slug,
                Kind = term.kind,
                Label = term.label,
                Description = term.description,
                ParentSlug = term.Parent != null ? term.Parent.slug : null,
                SortOrder = term.sortOrder
            },
            ct);

        var ordered = terms
            .OrderBy(term => term.Kind)
            .ThenBy(term => term.SortOrder)
            .ThenBy(term => term.Label, StringComparer.Ordinal)
            .ToList();

        return Result<IReadOnlyList<TaxonomyTermDto>>.Success(ordered);
    }
}
