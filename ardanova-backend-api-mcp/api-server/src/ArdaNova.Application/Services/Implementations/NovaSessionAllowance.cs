namespace ArdaNova.Application.Services.Implementations;

using System.Collections.Concurrent;

/// <summary>Per-member daily cap on new Nova AI refinement sessions (fixed UTC day); see Infrastructure/Ogai/AGENTS.md.</summary>
public sealed class NovaSessionAllowance
{
    public const int DefaultDailySessionsPerActor = 10;

    private readonly TimeProvider _time;
    private readonly int _cap;
    /// <summary>How long a started refinement may be continued by its actor.</summary>
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(6);

    private readonly ConcurrentDictionary<string, (DateOnly Day, int Used)> _used = new();
    private readonly ConcurrentDictionary<(string ActorId, string SessionId), DateTimeOffset> _owned = new();

    public NovaSessionAllowance(TimeProvider time, int dailySessionsPerActor = DefaultDailySessionsPerActor)
    {
        _time = time;
        _cap = dailySessionsPerActor > 0 ? dailySessionsPerActor : DefaultDailySessionsPerActor;
    }

    /// <summary>Claims one new session for the actor today; false at the cap.</summary>
    public bool TryStart(string actorId)
    {
        var today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
        var granted = false;
        _used.AddOrUpdate(
            actorId,
            _ => { granted = true; return (today, 1); },
            (_, entry) =>
            {
                var used = entry.Day == today ? entry.Used : 0;
                granted = used < _cap;
                return (today, granted ? used + 1 : used);
            });
        return granted;
    }

    /// <summary>Records that the upstream issued this session to this actor; keyed by the pair, so a shared preview id never transfers ownership.</summary>
    public void BindSession(string actorId, string sessionId)
    {
        var now = _time.GetUtcNow();
        foreach (var stale in _owned.Where(entry => entry.Value <= now).Select(entry => entry.Key).ToArray())
            _owned.TryRemove(stale, out _);
        _owned[(actorId, sessionId)] = now + SessionLifetime;
    }

    /// <summary>True only when this actor started (and still holds) the session.</summary>
    public bool OwnsSession(string actorId, string sessionId)
        => _owned.TryGetValue((actorId, sessionId), out var expiresAt) && expiresAt > _time.GetUtcNow();

    /// <summary>Gives back a session whose first turn failed upstream.</summary>
    public void Refund(string actorId)
        => _used.AddOrUpdate(actorId, _ => (default, 0), (_, entry) => (entry.Day, Math.Max(0, entry.Used - 1)));
}
