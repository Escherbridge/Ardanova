namespace ArdaNova.Application.Tests.Feed;

using System.Text;
using ArdaNova.Application.Feed;
using FluentAssertions;

/// <summary>What the feed refuses before it reads anything: cursors it did not issue, and scopes it does not serve.</summary>
public class FeedCursorTests
{
    public static TheoryData<string, string> NotACursor => new()
    {
        { "not base64 at all!", "garbage" },
        { Encode("""{"v":2,"a":638000000000000000,"s":"home"}"""), "a future cursor version" },
        { Encode("""{"v":1,"a":-1,"s":"home"}"""), "ticks out of range" },
        { Encode("""{"v":1,"a":638000000000000000,"s":"home","f":{"c":638000000000000000,"i":""}}"""), "a key with no id" },
        { Encode("""{"v":1,"a":638000000000000000,"s":"home","x":"AAAAAAA="}"""), "a served list that is not whole fingerprints" },
        { Encode($$"""{"v":1,"a":638000000000000000,"s":"home","x":"{{Convert.ToBase64String(new byte[6 * (FeedCursor.MaxServed + 1)])}}"}"""), "more served cards than any window holds" },
    };

    [Theory]
    [MemberData(nameof(NotACursor))]
    public void TryDecode_RefusesWhatThisFeedNeverIssued(string text, string because)
        => FeedCursor.TryDecode(text, out _).Should().BeFalse(because);

    [Theory]
    [InlineData(null, "home")]
    [InlineData("home", "home")]
    [InlineData("project:p-1", "project:p-1")]
    [InlineData("guild:g-1", "guild:g-1")]
    [InlineData("tag:#Carpentry", "tag:carpentry")]
    [InlineData("tag:two words", null)]
    [InlineData("user:u-1", null)]
    [InlineData("project:", null)]
    public void TheScope_IsOneOfFourShapes_WithATagNormalisedLikeAStoredOne(string? text, string? expectedKey)
    {
        var parsed = FeedScope.TryParse(text, out var scope);

        parsed.Should().Be(expectedKey is not null);
        if (expectedKey is not null)
            scope.Key.Should().Be(expectedKey);
    }

    private static string Encode(string json)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
