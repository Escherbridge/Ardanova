namespace ArdaNova.Infrastructure.Azoa;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Options;

/// <summary>
/// Infrastructure adapter that satisfies the Application-layer port
/// <see cref="IAzoaAllocationNode"/> by delegating to the typed-HttpClient
/// transport <see cref="IAzoaNodeClient"/>. Mirrors <see cref="AzoaAvatarNodeAdapter"/>:
/// the interface lives in Application, the implementation here in Infrastructure,
/// so the Application layer never references Infrastructure wire models.
///
/// Translates the Application-owned <see cref="AzoaAllocationCommand"/> into the
/// node <see cref="AzoaAllocationRequest"/> wire model (supplying the configured
/// default <c>ChainType</c> when the command leaves it null), and re-projects the
/// node's <see cref="AzoaAllocationResult"/> back into the Application-owned
/// <see cref="AzoaAllocationOutcome"/>. The fail-closed KYC error
/// (<c>KYC_FORBIDDEN</c> → <see cref="ResultType.Forbidden"/>) survives verbatim.
/// </summary>
public sealed class AzoaAllocationNodeAdapter : IAzoaAllocationNode
{
    private readonly IAzoaNodeClient _node;
    private readonly AzoaSettings _settings;

    public AzoaAllocationNodeAdapter(IAzoaNodeClient node, IOptions<AzoaSettings> settings)
    {
        _node = node;
        _settings = settings.Value;
    }

    public async Task<Result<AzoaAllocationOutcome>> AllocateAsync(
        Guid avatarId,
        AzoaAllocationCommand command,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        var wire = new AzoaAllocationRequest
        {
            Kind = command.Kind == AzoaAllocationType.Transfer
                ? AzoaAllocationKind.Transfer
                : AzoaAllocationKind.Mint,
            // ChainType default lives in node config, not the Application layer.
            ChainType = string.IsNullOrWhiteSpace(command.ChainType)
                ? _settings.ChainType
                : command.ChainType,
            // Amount is opaque — passed straight through, no economics here (§1, §3).
            Amount = command.Amount,
            Name = command.Name,
            Description = command.Description,
            AssetId = command.AssetId,
            AssetRecordId = command.AssetRecordId,
            Memo = command.Memo,
            Metadata = command.Metadata is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(command.Metadata),
        };

        var result = await _node.AllocateAsync(avatarId, wire, idempotencyKey, ct);

        // Preserve the failure type verbatim (incl. KYC_FORBIDDEN → Forbidden).
        if (result.IsFailure)
            return MapFailure(result);

        var r = result.Value!;
        return Result<AzoaAllocationOutcome>.Success(new AzoaAllocationOutcome(
            AvatarId: r.AvatarId,
            WalletId: r.WalletId,
            WalletAddress: r.WalletAddress,
            WalletProvisioned: r.WalletProvisioned,
            OperationId: r.OperationId,
            Replayed: r.Replayed,
            IdempotencyKey: r.IdempotencyKey));
    }

    /// <summary>Re-wrap a failed Result&lt;AzoaAllocationResult&gt; as Result&lt;AzoaAllocationOutcome&gt;, keeping the ResultType.</summary>
    private static Result<AzoaAllocationOutcome> MapFailure(Result<AzoaAllocationResult> source)
    {
        var error = source.Error ?? "AZOA allocation failed.";
        return source.Type switch
        {
            ResultType.NotFound => Result<AzoaAllocationOutcome>.NotFound(error),
            ResultType.Forbidden => Result<AzoaAllocationOutcome>.Forbidden(error),
            ResultType.Unauthorized => Result<AzoaAllocationOutcome>.Unauthorized(error),
            ResultType.Conflict => Result<AzoaAllocationOutcome>.Conflict(error),
            ResultType.ValidationError => Result<AzoaAllocationOutcome>.ValidationError(error),
            ResultType.BadRequest => Result<AzoaAllocationOutcome>.BadRequest(error),
            _ => Result<AzoaAllocationOutcome>.Failure(error),
        };
    }
}
