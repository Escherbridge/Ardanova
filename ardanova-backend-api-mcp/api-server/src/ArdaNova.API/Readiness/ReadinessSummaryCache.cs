namespace ArdaNova.API.Readiness;

/// <summary>Holds the last /ready verdict briefly so anonymous probes cannot turn into one database round-trip each. See AGENTS.md §Anonymous probe cache.</summary>
public sealed class ReadinessSummaryCache(TimeProvider clock)
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(5);

    private sealed record Entry(bool Ready, DateTimeOffset At);

    // A reference swap is atomic, so concurrent probes never read a torn verdict.
    private volatile Entry? _last;

    /// <summary>The verdict computed within <see cref="Lifetime"/>, if any.</summary>
    public bool TryGet(out bool ready)
    {
        var last = _last;
        ready = last?.Ready ?? false;
        return last is not null && clock.GetUtcNow() - last.At < Lifetime;
    }

    public void Store(bool ready) => _last = new Entry(ready, clock.GetUtcNow());
}
