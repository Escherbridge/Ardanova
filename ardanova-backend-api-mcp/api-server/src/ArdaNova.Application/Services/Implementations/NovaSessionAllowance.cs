namespace ArdaNova.Application.Services.Implementations;

using System.Collections.Concurrent;

/// <summary>Per-member daily cap on new Nova AI refinement sessions (fixed UTC day, in memory) and one turn in flight per member; see Infrastructure/Ogai/AGENTS.md.</summary>
public sealed class NovaSessionAllowance
{
    public const int DefaultDailySessionsPerActor = 10;

    private readonly TimeProvider _time;
    private readonly int _cap;
    private readonly ConcurrentDictionary<string, (DateOnly Day, int Used)> _used = new();
    private readonly ConcurrentDictionary<string, byte> _inFlight = new();
    private DateOnly _prunedOn;

    public NovaSessionAllowance(TimeProvider time, int dailySessionsPerActor = DefaultDailySessionsPerActor)
    {
        _time = time;
        _cap = dailySessionsPerActor > 0 ? dailySessionsPerActor : DefaultDailySessionsPerActor;
    }

    /// <summary>Claims one new session for the actor today; false at the cap.</summary>
    public bool TryStart(string actorId)
    {
        var today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
        PruneBefore(today);
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

    /// <summary>New sessions each member may start per UTC day.</summary>
    public int DailyLimit => _cap;

    /// <summary>New sessions the actor can still start today.</summary>
    public int Remaining(string actorId)
    {
        var today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
        return _used.TryGetValue(actorId, out var entry) && entry.Day == today ? Math.Max(0, _cap - entry.Used) : _cap;
    }

    /// <summary>Claims the actor's single turn slot; false while another of their turns is still being answered.</summary>
    public bool TryBeginTurn(string actorId) => _inFlight.TryAdd(actorId, 0);

    /// <summary>Releases the slot <see cref="TryBeginTurn"/> claimed.</summary>
    public void EndTurn(string actorId) => _inFlight.TryRemove(actorId, out _);

    /// <summary>Gives back a session whose first turn failed upstream.</summary>
    public void Refund(string actorId)
        => _used.AddOrUpdate(actorId, _ => (default, 0), (_, entry) => (entry.Day, Math.Max(0, entry.Used - 1)));

    /// <summary>Drops earlier days' counters once per UTC day, so the map holds only today's members.</summary>
    private void PruneBefore(DateOnly today)
    {
        if (_prunedOn == today)
            return;
        _prunedOn = today;
        foreach (var stale in _used.Where(entry => entry.Value.Day != today).Select(entry => entry.Key).ToArray())
            _used.TryRemove(stale, out _);
    }
}
