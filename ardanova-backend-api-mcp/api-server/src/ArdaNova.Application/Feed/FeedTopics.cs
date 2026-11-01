namespace ArdaNova.Application.Feed;

using ArdaNova.Domain.Models.Enums;

/// <summary>A TaxonomyTerm as the canonical tag layer reads it; ParentSlug is a SKILL's sector.</summary>
public sealed record FeedTaxonomyTerm(TaxonomyKind Kind, string Slug, string Label, string? ParentSlug);

/// <summary>
/// What a card is about, or what a viewer is into, in one comparable space: canonical TaxonomyTerm topics
/// ("sector:CONSTRUCTION", "skill:CARPENTRY") and free-form tags ("tag:solarbarn"). See Feed/AGENTS.md §Hashtags.
/// </summary>
public readonly record struct FeedTopic(string Key)
{
    public static FeedTopic Sector(string slug) => new("sector:" + slug.ToUpperInvariant());

    public static FeedTopic FreeTag(string tag) => new("tag:" + tag);

    public bool IsSector => Key.StartsWith("sector:", StringComparison.Ordinal);

    /// <summary>The sector slug of a sector topic, else null.</summary>
    public string? SectorSlug => IsSector ? Key["sector:".Length..] : null;
}

/// <summary>
/// The canonical layer over hashtags: a tag that names an existing TaxonomyTerm (by slug or label, in any of
/// the spellings <see cref="SpellingsOf"/> lists) resolves to it, so #construction and a CONSTRUCTION project
/// meet; any other tag stays free-form and still matches itself. No aliases table - see Feed/AGENTS.md.
/// </summary>
public sealed class FeedTopicCanon
{
    private readonly Dictionary<string, FeedTaxonomyTerm> _bySpelling;

    private FeedTopicCanon(Dictionary<string, FeedTaxonomyTerm> bySpelling)
    {
        _bySpelling = bySpelling;
    }

    public static FeedTopicCanon Empty { get; } = new(new Dictionary<string, FeedTaxonomyTerm>(StringComparer.Ordinal));

    /// <summary>Indexes active terms; on a shared spelling SECTOR beats SKILL beats PROJECT_TYPE (RESEARCH is both).</summary>
    public static FeedTopicCanon Build(IEnumerable<FeedTaxonomyTerm> terms)
    {
        var bySpelling = new Dictionary<string, FeedTaxonomyTerm>(StringComparer.Ordinal);
        foreach (var term in terms.OrderBy(term => Precedence(term.Kind)))
        {
            foreach (var spelling in SpellingsOf(term.Slug).Concat(SpellingsOf(term.Label)))
                bySpelling.TryAdd(spelling, term);
        }

        return new FeedTopicCanon(bySpelling);
    }

    /// <summary>The term a stored tag names, or null when it is free-form.</summary>
    public FeedTaxonomyTerm? Resolve(string tag) => _bySpelling.GetValueOrDefault(tag);

    /// <summary>The topics one tag stands for: its term (and a skill's sector), else the free tag itself.</summary>
    public IEnumerable<FeedTopic> TopicsOf(string tag)
    {
        if (Resolve(tag) is not { } term)
        {
            yield return FeedTopic.FreeTag(tag);
            yield break;
        }

        yield return TopicOf(term);
        if (term.Kind == TaxonomyKind.SKILL && !string.IsNullOrWhiteSpace(term.ParentSlug))
            yield return FeedTopic.Sector(term.ParentSlug);
    }

    /// <summary>The topics a free-text name (a UserSkill.skill) stands for when it names a known term; else none.</summary>
    public IEnumerable<FeedTopic> TopicsOfName(string name)
    {
        var spelling = SpellingsOf(name).FirstOrDefault(candidate => Resolve(candidate) is not null);
        return spelling is null ? [] : TopicsOf(spelling);
    }

    /// <summary>The tag spellings that name a term: "FOOD_BEVERAGE" or "Food and drink" -> food_beverage, food-beverage, foodbeverage...</summary>
    public static IEnumerable<string> SpellingsOf(string slugOrLabel)
    {
        var words = slugOrLabel.Trim().ToLowerInvariant()
            .Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            yield break;

        foreach (var joined in new[] { string.Join('_', words), string.Join('-', words), string.Concat(words) }.Distinct())
        {
            if (Hashtags.Normalize(joined) is { } spelling)
                yield return spelling;
        }
    }

    private static FeedTopic TopicOf(FeedTaxonomyTerm term) => term.Kind switch
    {
        TaxonomyKind.SECTOR => FeedTopic.Sector(term.Slug),
        TaxonomyKind.SKILL => new FeedTopic("skill:" + term.Slug.ToUpperInvariant()),
        _ => new FeedTopic("type:" + term.Slug.ToUpperInvariant()),
    };

    private static int Precedence(TaxonomyKind kind) => kind switch
    {
        TaxonomyKind.SECTOR => 0,
        TaxonomyKind.SKILL => 1,
        _ => 2,
    };
}
