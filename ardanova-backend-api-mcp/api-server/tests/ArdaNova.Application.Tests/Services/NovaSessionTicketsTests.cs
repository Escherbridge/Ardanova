namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Services.Implementations;
using FluentAssertions;

/// <summary>The ticket envelope is fixed-size, so the client's state cap must be envelope + upstream max (Infrastructure/Ogai/AGENTS.md §Session tickets).</summary>
public class NovaSessionTicketsTests
{
    private static readonly byte[] Key = Enumerable.Repeat((byte)7, NovaSessionTickets.MinKeyBytes).ToArray();

    [Fact]
    public void TicketAroundTheLargestUpstreamState_IsExactlyTheMaximumAndStillOpens()
    {
        var tickets = new NovaSessionTickets(Key, TimeProvider.System);
        var upstream = new string('s', NovaSessionTickets.MaxUpstreamStateLength);

        var ticket = tickets.Issue("actor-1", "session-1", upstream, tickets.NewExpiry());

        ticket.Length.Should().Be(NovaSessionTickets.MaxTicketLength);
        ticket.Length.Should().Be(80 + 512, "the client cap NOVA_STATE_MAX_CHARS is computed the same way");
        tickets.TryOpen(ticket, "actor-1", "session-1", out var opened, out _).Should().BeTrue();
        opened.Should().Be(upstream);
    }

    [Fact]
    public void TicketOneCharacterOverTheMaximum_IsRefusedBeforeAnyHmacWork()
    {
        var tickets = new NovaSessionTickets(Key, TimeProvider.System);
        var upstream = new string('s', NovaSessionTickets.MaxUpstreamStateLength + 1);
        var ticket = tickets.Issue("actor-1", "session-1", upstream, tickets.NewExpiry());

        tickets.TryOpen(ticket, "actor-1", "session-1", out _, out _).Should().BeFalse();
    }
}
