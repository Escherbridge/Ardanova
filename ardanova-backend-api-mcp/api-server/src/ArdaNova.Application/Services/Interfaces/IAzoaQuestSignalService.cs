namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

/// <summary>
/// Maps ArdaNova domain events to gate SIGNALS that advance self-running AZOA
/// quest runs, and reads run execution-state (track <c>azoa-quest-authoring</c>;
/// contract §5).
///
/// ArdaNova does not act AS an avatar and does not start runs — avatars self-run
/// the published quests. ArdaNova's role at runtime is to translate things it
/// observes in its own domain (a funding goal being met, a task submission being
/// accepted or rejected) into the corresponding gate signals so the avatar's run
/// can progress past its <c>GateCheck</c> nodes.
///
/// Layering: talks to the node only through the Application-owned
/// <see cref="IAzoaQuestNode"/> port.
/// </summary>
public interface IAzoaQuestSignalService
{
    /// <summary>
    /// Signal that a project's funding goal has been met — advances the Project
    /// Lifecycle quest past its <c>gate-funding-goal</c> GateCheck.
    /// (Predicate: <c>fundingGoalMet == true</c>.)
    /// </summary>
    Task<Result<AzoaSignalAck>> SignalFundingGoalMetAsync(
        string runId, object? payload = null, CancellationToken ct = default);

    /// <summary>
    /// Signal that a task submission was ACCEPTED — drives the Task Bounty quest
    /// down its happy path (reward transfer). Sets the gate's
    /// <c>submissionAccepted</c> binding to true.
    /// </summary>
    Task<Result<AzoaSignalAck>> SignalTaskAcceptedAsync(
        string runId, object? payload = null, CancellationToken ct = default);

    /// <summary>
    /// Signal that a task submission was REJECTED — drives the Task Bounty quest
    /// down its refund branch. Sets the gate's <c>submissionAccepted</c> binding
    /// to false.
    /// </summary>
    Task<Result<AzoaSignalAck>> SignalTaskRejectedAsync(
        string runId, object? payload = null, CancellationToken ct = default);

    /// <summary>
    /// Read a run's current execution state and map the canonical node status
    /// onto <see cref="AzoaRunState"/>.
    /// </summary>
    Task<Result<AzoaRunExecutionStateDto>> GetRunExecutionStateAsync(
        string runId, CancellationToken ct = default);
}
