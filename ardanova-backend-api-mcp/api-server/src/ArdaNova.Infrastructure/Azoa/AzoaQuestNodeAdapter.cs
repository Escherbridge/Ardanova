namespace ArdaNova.Infrastructure.Azoa;

using System.Text.Json.Serialization;
using ArdaNova.Application.Azoa.Quests;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;

/// <summary>
/// Infrastructure adapter that satisfies the Application-layer port
/// <see cref="IAzoaQuestNode"/> by delegating to the typed-HttpClient transport
/// <see cref="IAzoaQuestNodeClient"/> (its generic
/// <see cref="IAzoaQuestNodeClient.PostAsync{T}"/> / <see cref="IAzoaQuestNodeClient.GetAsync{T}"/>
/// helpers). Mirrors <see cref="AzoaAvatarNodeAdapter"/>: the interface lives in
/// Application, the implementation here in Infrastructure, so the Application layer
/// never references Infrastructure wire models.
///
/// Endpoint map (track <c>azoa-quest-authoring</c>; verified):
///   CreateQuest        → POST /api/quest
///   CreateTemplate     → POST /api/quest/templates
///   ValidateQuest      → POST /api/quest/{id}/validate
///   SignalRun          → POST /api/quest/runs/{runId}/signal
///   GetExecutionState  → GET  /api/quest/runs/{runId}/execution-state
///
/// The quest DEFINITION (<see cref="AzoaQuestDefinition"/>) is already an
/// Application-owned, [JsonPropertyName]-annotated record, so it is posted as the
/// body verbatim. Responses are deserialised into the small wire records below and
/// projected onto the Application-owned port records — no node type leaks upward.
/// </summary>
public sealed class AzoaQuestNodeAdapter : IAzoaQuestNode
{
    private readonly IAzoaQuestNodeClient _node;

    public AzoaQuestNodeAdapter(IAzoaQuestNodeClient node)
    {
        _node = node;
    }

    public async Task<Result<AzoaQuestRef>> CreateQuestAsync(
        AzoaQuestDefinition definition, CancellationToken ct = default)
    {
        var result = await _node.PostAsync<AzoaQuestWire>("/api/quest", definition, ct);
        return MapQuestRef(result);
    }

    public async Task<Result<AzoaQuestRef>> CreateTemplateAsync(
        AzoaQuestDefinition definition, CancellationToken ct = default)
    {
        var result = await _node.PostAsync<AzoaQuestWire>("/api/quest/templates", definition, ct);
        return MapQuestRef(result);
    }

    public async Task<Result<AzoaQuestValidation>> ValidateQuestAsync(
        string questId, CancellationToken ct = default)
    {
        var result = await _node.PostAsync<AzoaQuestValidationWire>(
            $"/api/quest/{questId}/validate", null, ct);

        if (result.IsFailure)
            return MapFailure<AzoaQuestValidationWire, AzoaQuestValidation>(result);

        var wire = result.Value!;
        return Result<AzoaQuestValidation>.Success(
            new AzoaQuestValidation(wire.IsValid, wire.Message));
    }

    public async Task<Result<AzoaSignalAck>> SignalRunAsync(
        string runId, AzoaRunSignal signal, CancellationToken ct = default)
    {
        var body = new AzoaSignalRequestWire
        {
            Signal = signal.SignalName,
            Payload = signal.Payload,
        };

        var result = await _node.PostAsync<AzoaSignalAckWire>(
            $"/api/quest/runs/{runId}/signal", body, ct);

        if (result.IsFailure)
            return MapFailure<AzoaSignalAckWire, AzoaSignalAck>(result);

        var wire = result.Value!;
        return Result<AzoaSignalAck>.Success(
            new AzoaSignalAck(
                string.IsNullOrWhiteSpace(wire.RunId) ? runId : wire.RunId,
                wire.Accepted));
    }

    public async Task<Result<AzoaRunExecutionState>> GetExecutionStateAsync(
        string runId, CancellationToken ct = default)
    {
        var result = await _node.GetAsync<AzoaRunExecutionStateWire>(
            $"/api/quest/runs/{runId}/execution-state", ct);

        if (result.IsFailure)
            return MapFailure<AzoaRunExecutionStateWire, AzoaRunExecutionState>(result);

        var wire = result.Value!;
        return Result<AzoaRunExecutionState>.Success(
            new AzoaRunExecutionState(
                string.IsNullOrWhiteSpace(wire.RunId) ? runId : wire.RunId,
                wire.Status ?? string.Empty,
                wire.CurrentNodeId));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Result<AzoaQuestRef> MapQuestRef(Result<AzoaQuestWire> result)
    {
        if (result.IsFailure)
            return MapFailure<AzoaQuestWire, AzoaQuestRef>(result);

        var wire = result.Value!;
        return Result<AzoaQuestRef>.Success(new AzoaQuestRef(wire.Id ?? string.Empty));
    }

    /// <summary>Re-wrap a failed Result&lt;TIn&gt; as Result&lt;TOut&gt;, keeping the ResultType.</summary>
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

    // ── Wire models (Infrastructure-only; never surface to Application) ─────────

    private sealed class AzoaQuestWire
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    private sealed class AzoaQuestValidationWire
    {
        [JsonPropertyName("isValid")]
        public bool IsValid { get; init; }

        [JsonPropertyName("message")]
        public string? Message { get; init; }
    }

    private sealed class AzoaSignalRequestWire
    {
        [JsonPropertyName("signal")]
        public string Signal { get; init; } = string.Empty;

        [JsonPropertyName("payload")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Payload { get; init; }
    }

    private sealed class AzoaSignalAckWire
    {
        [JsonPropertyName("runId")]
        public string? RunId { get; init; }

        [JsonPropertyName("accepted")]
        public bool Accepted { get; init; }
    }

    private sealed class AzoaRunExecutionStateWire
    {
        [JsonPropertyName("runId")]
        public string? RunId { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("currentNodeId")]
        public string? CurrentNodeId { get; init; }
    }
}
