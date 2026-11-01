namespace ArdaNova.Application.Tests.Feed;

using ArdaNova.Application.Feed;
using ArdaNova.Domain.Models.Enums;
using FluentAssertions;

/// <summary>#tag extraction and the canonical layer: what a person's copy turns into, and what it means.</summary>
public class HashtagsTests
{
    public static TheoryData<string, string[]> Copy => new()
    {
        { "Raised the frame #Construction #construction #Carpentry", ["construction", "carpentry"] },
        { "Done for today #carpentry.", ["carpentry"] },
        { "Finished (#roofing), next #plumbing!", ["roofing", "plumbing"] },
        { "Photos at example.com/#anchor and https://x.org/page#frag", [] },
        { "# Weekly notes\n## Budget", [] },
        { "&#39;quoted&#39; and ##double", [] },
        { "#a is too short, #" + new string('x', 41) + " is too long", [] },
        { "Café day #CAFÉ #café", ["café"] },
        { "Ｆｕｌｌ ＃Ｗｉｄｔｈ tag", ["width"] },
        { "#नमस्ते from the kitchen", ["नमस्ते"] },
        { "#solar-barn and #solar_barn are two tags; #roofing- trims", ["solar-barn", "solar_barn", "roofing"] },
        { "mid#word is not a tag but #word is", ["word"] },
    };

    [Theory]
    [MemberData(nameof(Copy))]
    public void Extract_ReadsTagsTheWayAPersonMeantThem(string copy, string[] expected)
        => Hashtags.Extract(copy).Should().Equal(expected);

    [Fact]
    public void Extract_KeepsTheFirstTenDistinctTags_InTheOrderTheyWereWritten()
    {
        var copy = string.Join(' ', Enumerable.Range(0, 12).Select(index => $"#tag{index}")) + " #TAG0";

        Hashtags.Extract(copy).Should().Equal(Enumerable.Range(0, 10).Select(index => $"tag{index}"));
    }

    [Theory]
    [InlineData("#Carpentry", "carpentry")]
    [InlineData("carpentry", "carpentry")]
    [InlineData("  #Solar-Barn ", "solar-barn")]
    [InlineData("two words", null)]
    [InlineData("#x", null)]
    [InlineData("", null)]
    public void Normalize_TurnsWhatACallerTypedIntoTheStoredForm(string input, string? expected)
        => Hashtags.Normalize(input).Should().Be(expected);

    [Theory]
    [InlineData("construction", "sector:CONSTRUCTION")]
    [InlineData("carpentry", "skill:CARPENTRY")]
    [InlineData("food-and-drink", "sector:FOOD_BEVERAGE")]
    [InlineData("foodbeverage", "sector:FOOD_BEVERAGE")]
    [InlineData("research", "sector:RESEARCH")]
    [InlineData("solarbarn", "tag:solarbarn")]
    public void TheCanon_ResolvesATagToItsTaxonomyTerm_OrLeavesItFreeForm(string tag, string expectedTopic)
        => Canon().TopicsOf(tag).First().Key.Should().Be(expectedTopic);

    [Fact]
    public void ASkillTag_AlsoStandsForItsSector_SoACarpentryCardMeetsAConstructionReader()
        => Canon().TopicsOf("carpentry").Should().Contain(FeedTopic.Sector("CONSTRUCTION"));

    private static FeedTopicCanon Canon() => FeedTopicCanon.Build(
    [
        new FeedTaxonomyTerm(TaxonomyKind.PROJECT_TYPE, "RESEARCH", "Research", null),
        new FeedTaxonomyTerm(TaxonomyKind.SECTOR, "CONSTRUCTION", "Construction", null),
        new FeedTaxonomyTerm(TaxonomyKind.SECTOR, "FOOD_BEVERAGE", "Food and drink", null),
        new FeedTaxonomyTerm(TaxonomyKind.SECTOR, "RESEARCH", "Research", null),
        new FeedTaxonomyTerm(TaxonomyKind.SKILL, "CARPENTRY", "Carpentry", "CONSTRUCTION"),
    ]);
}
