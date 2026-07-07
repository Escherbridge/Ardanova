namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Azoa.Quests;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

/// <summary>
/// Authors ArdaNova's canonical scrum-lifecycle quest definitions and publishes
/// them to the AZOA node (track <c>azoa-quest-authoring</c>; contract §5).
///
/// Division of responsibility: ArdaNova AUTHORS quest definitions (the DAGs in
/// <see cref="ScrumLifecycleQuests"/>) and publishes them; avatars then SELF-RUN
/// the published quests. ArdaNova never instantiates or starts a run AS an avatar
/// (no acting-as) — it only authors definitions and, later, signals gates
/// (<see cref="IAzoaQuestSignalService"/>).
///
/// Economics are decided in ArdaNova FIRST and baked into the definition before
/// publish; the AZOA node computes no economics of its own (contract §1, §3).
///
/// Layering: this service talks to the node through the Application-owned
/// <see cref="IAzoaQuestNode"/> port, never the Infrastructure transport directly
/// — the same dependency-inversion seam used by <c>IAzoaAvatarNode</c> /
/// <c>IAlgorandService</c>.
/// </summary>
public interface IAzoaQuestAuthoringService
{
    /// <summary>
    /// Publish the three canonical scrum-lifecycle quest definitions
    /// (<see cref="ScrumLifecycleQuests.CreateProjectLifecycleDefinition"/>,
    /// <see cref="ScrumLifecycleQuests.CreateTaskBountyDefinition"/>,
    /// <see cref="ScrumLifecycleQuests.CreateMembershipCredentialDefinition"/>)
    /// to the node, then validate each one.
    ///
    /// Public definitions (<c>IsPublic == true</c>) are published as templates
    /// (<c>POST /api/quest/templates</c>) so any avatar may instantiate them;
    /// restricted ones are published as plain quests (<c>POST /api/quest</c>).
    ///
    /// Version-aware / idempotent-friendly: re-publishing may mint a new version id
    /// on the node. This method does not attempt to dedupe — it returns the ids the
    /// node created this run so the caller can record them.
    /// </summary>
    Task<Result<AzoaQuestPublishResultDto>> PublishLifecycleDefinitionsAsync(
        CancellationToken ct = default);
}

// ────────────────────────────────────────────────────────────────────────────
//  PORT: IAzoaQuestNode
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Application-layer PORT onto the AZOA node's quest surface, used by
/// <c>AzoaQuestAuthoringService</c> and <c>AzoaQuestSignalService</c>.
///
/// Why this exists: the typed-HttpClient transport (<c>IAzoaNodeClient</c>) lives
/// in <c>ArdaNova.Infrastructure.Azoa</c>, and the Application layer must not (and
/// at compile time cannot) reference Infrastructure. This port is the seam — the
/// same dependency-inversion pattern used by <c>IAzoaAvatarNode</c> and
/// <c>IAlgorandService</c>. An Infrastructure adapter
/// (<c>AzoaQuestNodeAdapter</c>) maps these calls onto
/// <c>IAzoaNodeClient.PostAsync&lt;T&gt;</c>/<c>GetAsync&lt;T&gt;</c> and the real
/// quest endpoints, and registers itself against this port.
///
/// The port speaks only in Application-owned records so no Infrastructure wire
/// type leaks into the services. The quest DEFINITION shape
/// (<see cref="AzoaQuestDefinition"/>) is already Application-owned, so it is
/// passed through directly.
/// </summary>
public interface IAzoaQuestNode
{
    /// <summary>
    /// Publish a quest definition as a plain (non-public) quest.
    /// Maps to <c>POST /api/quest</c>. Returns the node-assigned quest id.
    /// </summary>
    Task<Result<AzoaQuestRef>> CreateQuestAsync(
        AzoaQuestDefinition definition, CancellationToken ct = default);

    /// <summary>
    /// Publish a quest definition as a public template that any avatar may
    /// instantiate. Maps to <c>POST /api/quest/templates</c>. Returns the
    /// node-assigned template id.
    /// </summary>
    Task<Result<AzoaQuestRef>> CreateTemplateAsync(
        AzoaQuestDefinition definition, CancellationToken ct = default);

    /// <summary>
    /// Validate a published quest/template DAG.
    /// Maps to <c>POST /api/quest/{id}/validate</c>.
    /// </summary>
    Task<Result<AzoaQuestValidation>> ValidateQuestAsync(
        string questId, CancellationToken ct = default);

    /// <summary>
    /// Send a gate signal to a self-running quest run to advance it past a
    /// GateCheck. Maps to <c>POST /api/quest/runs/{runId}/signal</c>.
    /// </summary>
    Task<Result<AzoaSignalAck>> SignalRunAsync(
        string runId, AzoaRunSignal signal, CancellationToken ct = default);

    /// <summary>
    /// Read a run's execution state.
    /// Maps to <c>GET /api/quest/runs/{runId}/execution-state</c>.
    /// </summary>
    Task<Result<AzoaRunExecutionState>> GetExecutionStateAsync(
        string runId, CancellationToken ct = default);
}

// ────────────────────────────────────────────────────────────────────────────
//  Application-owned port records
// ────────────────────────────────────────────────────────────────────────────

/// <summary>Reference to a published quest/template on the node.</summary>
public sealed record AzoaQuestRef(string QuestId);

/// <summary>Outcome of a node-side quest DAG validation pass.</summary>
public sealed record AzoaQuestValidation(bool IsValid, string? Message = null);

/// <summary>
/// A gate signal sent to a run. <paramref name="SignalName"/> identifies the gate
/// being satisfied (e.g. <c>"fundingGoalMet"</c>, <c>"submissionAccepted"</c>);
/// <paramref name="Payload"/> is the opaque context the runner merges before
/// re-evaluating the gate predicate.
/// </summary>
public sealed record AzoaRunSignal(string SignalName, object? Payload = null);

/// <summary>Acknowledgement of a delivered run signal.</summary>
public sealed record AzoaSignalAck(string RunId, bool Accepted);

/// <summary>
/// Raw execution-state read from the node port. The adapter populates
/// <paramref name="Status"/> verbatim from the node; the service maps it onto the
/// Application <see cref="AzoaRunState"/> enum (so the parking-state mapping —
/// notably <c>AwaitingReconciliation</c> as non-error — lives in the Application
/// layer, not Infrastructure).
/// </summary>
public sealed record AzoaRunExecutionState(
    string RunId,
    string Status,
    string? CurrentNodeId = null);
