namespace ArdaNova.Application.Tests.Policies;

using ArdaNova.Application.Common.Security;
using FluentAssertions;

public class ActorAssertionReplayRetentionPolicyTests
{
    [Fact]
    public void RetentionWindow_ExtendsBeyondTheAcceptedClockSkew()
    {
        ActorAssertionReplayRetentionPolicy.RetentionAfterExpirySeconds
            .Should().BeGreaterThan(ActorAssertionReplayRetentionPolicy.AllowedClockSkewSeconds);
    }

    [Fact]
    public void PurgeBefore_KeepsTheFullPostExpiryRetentionWindow()
    {
        var now = new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero);

        var cutoff = ActorAssertionReplayRetentionPolicy.PurgeBefore(now);

        cutoff.Should().Be(now.UtcDateTime.AddMinutes(-5));
    }
}
