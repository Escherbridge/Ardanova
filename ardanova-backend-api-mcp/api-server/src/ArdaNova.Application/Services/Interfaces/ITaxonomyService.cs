namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface ITaxonomyService
{
    /// <summary>Every ACTIVE term, optionally narrowed to one kind; refused if <paramref name="kind"/> names none.</summary>
    Task<Result<IReadOnlyList<TaxonomyTermDto>>> GetActiveTermsAsync(
        string? kind,
        CancellationToken ct = default);

    /// <summary>
    /// Classifies already-normalized tags (<see cref="ArdaNova.Application.Feed.Hashtags.Normalize"/>)
    /// against the active taxonomy, by any spelling the canonical tag layer accepts (slug or label -
    /// see <see cref="ArdaNova.Application.Feed.FeedTopicCanon.SpellingsOf"/>). Used by Lane L's
    /// project hashtag resolution (SR-03 Phase 3 §3); see Services/Implementations/AGENTS.md
    /// §Project hashtags.
    /// </summary>
    Task<ResolvedTaxonomyTags> ResolveTagsAsync(
        IEnumerable<string> normalizedTags,
        CancellationToken ct = default);
}

/// <summary>
/// One pass of <see cref="ITaxonomyService.ResolveTagsAsync"/>: every SECTOR hit (deduped, first-seen
/// order), the first PROJECT_TYPE hit (a later one in the same list is ignored - the caller's
/// submission order decides "first"), and every tag that named neither.
/// </summary>
public sealed record ResolvedTaxonomyTags(
    IReadOnlyList<string> SectorSlugs,
    string? ProjectTypeSlug,
    IReadOnlyList<string> UnresolvedTags);
