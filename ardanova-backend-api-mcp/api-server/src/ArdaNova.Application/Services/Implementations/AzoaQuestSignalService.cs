namespace ArdaNova.Application.Services.Implementations;

using System;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// Maps ArdaNova domain events to gate SIGNALS that advance self-running AZOA
/// quest runs, and reads run execution-state (track <c>azoa-quest-authoring</c>;
/// contract §5).
///
/// ArdaNova never starts or runs a quest as an avatar — avatars self-run. This
/// service only translates observed domain facts (funding goal met, task
/// accepted/rejected) into the matching gate signal, and reads back where a run
/// currently sits. The parking-state mapping — including treating
/// <c>AwaitingReconciliation</c> as a non-error pending-settlement state — lives
/// here in the Application layer.
///
/// Layering: depends only on the Application-owned <see cref="IAzoaQuestNode"/>
/// port.
/// </summary>
public class AzoaQuestSignalService : IAzoaQuestSignalService
{
    // Gate signal names — must match the GateCheck bindings in
    // ScrumLifecycleQuests. Project Lifecycle gate-funding-goal reads
    // "fundingGoalMet"; Task Bounty gate-submission-accepted reads
    // "submissionAccepted".
    private const string FundingGoalMetSignal = "fundingGoalMet";
    private const string SubmissionAcceptedSignal = "submissionAccepted";

    private readonly IAzoaQuestNode _node;
    private readonly ILogger<AzoaQuestSignalService> _logger;

    public AzoaQuestSignalService(
        IAzoaQuestNode node,
        ILogger<AzoaQuestSignalService> logger)
    {
        _node = node;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<Result<AzoaSignalAck>> SignalFundingGoalMetAsync(
        string runId, object? payload = null, CancellationToken ct = default) =>
        SendSignalAsync(runId, FundingGoalMetSignal, payload ?? new { fundingGoalMet = true }, ct);

    /// <inheritdoc/>
    public Task<Result<AzoaSignalAck>> SignalTaskAcceptedAsync(
        string runId, object? payload = null, CancellationToken ct = default) =>
        SendSignalAsync(runId, SubmissionAcceptedSignal, payload ?? new { submissionAccepted = true }, ct);

    /// <inheritdoc/>
    public Task<Result<AzoaSignalAck>> SignalTaskRejectedAsync(
        string runId, object? payload = null, CancellationToken ct = default) =>
        SendSignalAsync(runId, SubmissionAcceptedSignal, payload ?? new { submissionAccepted = false }, ct);

    /// <inheritdoc/>
    public async Task<Result<AzoaRunExecutionStateDto>> GetRunExecutionStateAsync(
        string runId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return Result<AzoaRunExecutionStateDto>.BadRequest("runId is required.");

        var stateResult = await _node.GetExecutionStateAsync(runId, ct);
        if (stateResult.IsFailure)
        {
            _logger.LogError(
                "Failed to read AZOA run execution-state for run {RunId}: {Error}",
                runId, stateResult.Error);
            return MapFailure<AzoaRunExecutionState, AzoaRunExecutionStateDto>(stateResult);
        }

        var raw = stateResult.Value!;
        var mappedState = MapRunState(raw.Status);

        return Result<AzoaRunExecutionStateDto>.Success(new AzoaRunExecutionStateDto
        {
            RunId = raw.RunId,
            State = mappedState,
            CurrentNodeId = raw.CurrentNodeId,
            RawStatus = raw.Status,
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Result<AzoaSignalAck>> SendSignalAsync(
        string runId, string signalName, object? payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return Result<AzoaSignalAck>.BadRequest("runId is required.");

        var signal = new AzoaRunSignal(signalName, payload);
        var result = await _node.SignalRunAsync(runId, signal, ct);

        if (result.IsFailure)
        {
            _logger.LogError(
                "Failed to deliver signal '{Signal}' to AZOA run {RunId}: {Error}",
                signalName, runId, result.Error);
            return result;
        }

        _logger.LogInformation(
            "Delivered signal '{Signal}' to AZOA run {RunId}.", signalName, runId);
        return result;
    }

    /// <summary>
    /// Maps the node's raw status string onto <see cref="AzoaRunState"/>.
    ///
    /// <c>AwaitingReconciliation</c> is a NON-error pending-settlement parking
    /// state and is mapped as such (not to <see cref="AzoaRunState.Failed"/>).
    /// Unrecognised statuses map to <see cref="AzoaRunState.Unknown"/> with the
    /// raw value preserved on the DTO for diagnostics.
    /// </summary>
    private static AzoaRunState MapRunState(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return AzoaRunState.Unknown;

        // Normalise: case-insensitive, ignore separators the node might use
        // (snake_case / kebab-case / PascalCase all collapse to the same key).
        var normalized = status
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);

        return normalized.ToLowerInvariant() switch
        {
            "pending" => AzoaRunState.Pending,
            "created" => AzoaRunState.Pending,
            "running" => AzoaRunState.Running,
            "inprogress" => AzoaRunState.Running,
            "active" => AzoaRunState.Running,
            "awaitingsignal" => AzoaRunState.AwaitingSignal,
            "waitingforsignal" => AzoaRunState.AwaitingSignal,
            "gated" => AzoaRunState.AwaitingSignal,
            "awaitingreconciliation" => AzoaRunState.AwaitingReconciliation,
            "pendingsettlement" => AzoaRunState.AwaitingReconciliation,
            "reconciling" => AzoaRunState.AwaitingReconciliation,
            "completed" => AzoaRunState.Completed,
            "complete" => AzoaRunState.Completed,
            "succeeded" => AzoaRunState.Completed,
            "done" => AzoaRunState.Completed,
            "failed" => AzoaRunState.Failed,
            "error" => AzoaRunState.Failed,
            "errored" => AzoaRunState.Failed,
            _ => AzoaRunState.Unknown,
        };
    }

    /// <summary>
    /// Re-projects a failed <c>Result&lt;TIn&gt;</c> onto <c>Result&lt;TOut&gt;</c>
    /// preserving the original <see cref="ResultType"/>.
    /// </summary>
    private static Result<TOut> MapFailure<TIn, TOut>(Result<TIn> source)
    {
        var error = source.Error ?? "AZOA quest node call failed.";
        return source.Type switch
        {
            ResultType.NotFound => Result<TOut>.NotFound(error),
            ResultType.Forbidden => Result<TOut>.Forbidden(error),
            ResultType.Unauthorized => Result<TOut>.Unauthorized(error),
            ResultType.Conflict => Result<TOut>.Conflict(error),
            ResultType.ValidationError => Result<TOut>.ValidationError(error),
            ResultType.BadRequest => Result<TOut>.BadRequest(error),
            _ => Result<TOut>.Failure(error),
        };
    }
}
