namespace ArdaNova.Application.DTOs;

using System.Collections.Generic;

/// <summary>
/// Result of publishing ArdaNova's canonical scrum-lifecycle quest definitions to
/// the AZOA node (track <c>azoa-quest-authoring</c>).
///
/// ArdaNova AUTHORS quest definitions and publishes them; avatars self-run the
/// resulting quests. Publishing is version-aware: the node may mint a new version
/// id on each (re-)publish, so this DTO simply reports the ids that were created
/// and validated this run rather than asserting a single canonical id.
/// </summary>
public record AzoaQuestPublishResultDto
{
    /// <summary>The definitions published in this run, in publish order.</summary>
    public IReadOnlyList<AzoaPublishedQuestDto> Published { get; init; } =
        new List<AzoaPublishedQuestDto>();
}

/// <summary>
/// Reference to a single quest definition published to the node, with the outcome
/// of its post-publish validation pass.
/// </summary>
public record AzoaPublishedQuestDto
{
    /// <summary>Human-readable quest name (from the ScrumLifecycleQuests factory).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The id the node assigned to the published definition/version.</summary>
    public string QuestId { get; init; } = string.Empty;

    /// <summary>
    /// True when this definition was published as a public template
    /// (<c>POST /api/quest/templates</c>) rather than a plain quest
    /// (<c>POST /api/quest</c>). Mirrors <c>AzoaQuestDefinition.IsPublic</c>.
    /// </summary>
    public bool PublishedAsTemplate { get; init; }

    /// <summary>True when the node's <c>validate</c> pass reported the DAG valid.</summary>
    public bool Validated { get; init; }
}

/// <summary>
/// Lifecycle state of a quest run from AZOA's <c>execution-state</c> contract.
/// </summary>
public enum AzoaRunState
{
    /// <summary>Run created but not yet started.</summary>
    Pending,

    /// <summary>Run is actively executing nodes.</summary>
    Running,

    /// <summary>Run completed successfully.</summary>
    Succeeded,

    /// <summary>Run terminated with an error.</summary>
    Failed,

    /// <summary>Run was replaced by a forked run.</summary>
    Forked,

    /// <summary>Run was cancelled.</summary>
    Cancelled,

    /// <summary>Run is parked until explicitly advanced.</summary>
    Suspended,

    /// <summary>Run is parked at a GateCheck waiting for a signal to advance.</summary>
    AwaitingSignal,

    /// <summary>Run is parked until its timer becomes due.</summary>
    AwaitingTimer,

    /// <summary>Node returned a state ArdaNova does not yet model.</summary>
    Unknown,
}

/// <summary>
/// Snapshot of a quest run's execution state as read from the AZOA node.
///
/// ArdaNova maps domain events to gate SIGNALS to advance runs (it never acts AS
/// an avatar). This read lets ArdaNova observe where a self-running avatar's quest
/// currently sits so it can decide which signal, if any, to send next.
/// </summary>
public record AzoaRunExecutionStateDto
{
    /// <summary>The node-assigned run id this snapshot describes.</summary>
    public string RunId { get; init; } = string.Empty;

    /// <summary>Mapped lifecycle/parking state (see <see cref="AzoaRunState"/>).</summary>
    public AzoaRunState State { get; init; }

    /// <summary>True when the run cannot transition again.</summary>
    public bool IsTerminal =>
        State is AzoaRunState.Succeeded
            or AzoaRunState.Failed
            or AzoaRunState.Forked
            or AzoaRunState.Cancelled;

    /// <summary>
    /// Id of the node the run is currently parked on / executing, if the node
    /// reported one. Null when not applicable.
    /// </summary>
    public string? CurrentNodeId { get; init; }

    /// <summary>
    /// The raw state string the node returned, preserved verbatim for diagnostics
    /// and for forward-compatibility when <see cref="State"/> is
    /// <see cref="AzoaRunState.Unknown"/>.
    /// </summary>
    public string? RawStatus { get; init; }
}
