namespace ArdaNova.Infrastructure.Azoa;

using System.Text.Json.Serialization;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;

/// <summary>Tenant-scoped transport for AZOA-owned identity, custody, and KYC.</summary>
public sealed class AzoaCustodialAccountGateway : IAzoaCustodialAccountGateway
{
    private readonly IAzoaCustodialNodeClient _node;

    public AzoaCustodialAccountGateway(IAzoaCustodialNodeClient node)
    {
        _node = node;
    }

    public async Task<Result<AzoaCustodialAccountCapabilities>> GetCapabilitiesAsync(
        CancellationToken ct = default)
    {
        var result = await _node.GetAsync<CapabilitiesWire>(
            "/api/tenant/custodial-accounts/capabilities",
            ct);
        if (result.IsFailure)
            return MapFailure<CapabilitiesWire, AzoaCustodialAccountCapabilities>(result);

        var value = result.Value!;
        return Result<AzoaCustodialAccountCapabilities>.Success(new(
            value.Enabled,
            value.WalletChain ?? string.Empty,
            value.CustodyMode ?? string.Empty,
            value.CustodyAvailable,
            value.BlockchainProviderAvailable,
            value.KycProvider ?? string.Empty,
            value.KycAvailable,
            value.HostedVerification,
            value.AcceptsDocumentReferences,
            value.IdentityReady,
            value.KycReady,
            value.WalletProvisioningReady,
            value.Ready,
            value.DevelopmentSimulation,
            value.UnavailableReason));
    }

    public async Task<Result<AzoaCustodialAccountStatus>> EnsureAsync(
        AzoaCustodialAccountBinding binding,
        CancellationToken ct = default)
    {
        var result = await _node.PutAsync<StatusWire>(
            AccountPath(binding.ArdaNovaUserId),
            body: null,
            binding.IdempotencyKey,
            ct);
        return MapStatus(result);
    }

    public async Task<Result<AzoaCustodialAccountStatus>> GetStatusAsync(
        AzoaCustodialAccountBinding binding,
        CancellationToken ct = default)
    {
        var result = await _node.GetAsync<StatusWire>(AccountPath(binding.ArdaNovaUserId), ct);
        return MapStatus(result);
    }

    public async Task<Result<AzoaKycSession>> BeginKycAsync(
        AzoaCustodialAccountBinding binding,
        CancellationToken ct = default)
    {
        var result = await _node.PostAsync<KycSessionWire>(
            $"{AccountPath(binding.ArdaNovaUserId)}/kyc/session",
            body: null,
            binding.KycSessionIdempotencyKey,
            ct);
        if (result.IsFailure)
            return MapFailure<KycSessionWire, AzoaKycSession>(result);

        var value = result.Value!;
        return Result<AzoaKycSession>.Success(new(
            value.Provider ?? string.Empty,
            value.HostedVerification,
            value.AcceptsDocumentReferences,
            value.VerificationUrl,
            value.ExpiresAt,
            value.Instructions,
            value.DevelopmentSimulation));
    }

    private static Result<AzoaCustodialAccountStatus> MapStatus(Result<StatusWire> result)
    {
        if (result.IsFailure)
            return MapFailure<StatusWire, AzoaCustodialAccountStatus>(result);

        var value = result.Value!;
        var externalSubject = value.ExternalSubject ?? value.ArdaNovaUserId;
        if (!TryParseKycStatus(value.KycStatus, out var status)
            || string.IsNullOrWhiteSpace(value.TenantId)
            || string.IsNullOrWhiteSpace(externalSubject))
        {
            return Result<AzoaCustodialAccountStatus>.Conflict(
                "AZOA returned an invalid custodial account binding or KYC state.");
        }

        return Result<AzoaCustodialAccountStatus>.Success(new(
            value.TenantId,
            externalSubject,
            value.AvatarId,
            value.WalletId,
            value.WalletAddress,
            status,
            value.IdentityReady,
            value.KycReady,
            value.WalletReady,
            value.Ready,
            value.UnavailableReason));
    }

    private static bool TryParseKycStatus(string? value, out AzoaKycStatus status)
        => Enum.TryParse(value, ignoreCase: false, out status)
            && Enum.IsDefined(status);

    private static string AccountPath(string externalSubject)
        => $"/api/tenant/custodial-accounts/{Uri.EscapeDataString(externalSubject)}";

    private static Result<TOut> MapFailure<TIn, TOut>(Result<TIn> source)
    {
        var error = source.Error ?? "AZOA custodial account operation failed.";
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

    private sealed record CapabilitiesWire
    {
        public bool Enabled { get; init; }
        public string? WalletChain { get; init; }
        public string? CustodyMode { get; init; }
        public bool CustodyAvailable { get; init; }
        public bool BlockchainProviderAvailable { get; init; }
        public string? KycProvider { get; init; }
        public bool KycAvailable { get; init; }
        public bool HostedVerification { get; init; }
        public bool AcceptsDocumentReferences { get; init; }
        public bool DevelopmentSimulation { get; init; }
        public bool IdentityReady { get; init; }
        public bool KycReady { get; init; }
        public bool WalletProvisioningReady { get; init; }
        public bool Ready { get; init; }
        public string? UnavailableReason { get; init; }
    }

    private sealed record StatusWire
    {
        public string? TenantId { get; init; }
        public string? ExternalSubject { get; init; }
        [JsonPropertyName("ardanovaUserId")]
        public string? ArdaNovaUserId { get; init; }
        public string? AvatarId { get; init; }
        public string? WalletId { get; init; }
        public string? WalletAddress { get; init; }
        public string? KycStatus { get; init; }
        public bool IdentityReady { get; init; }
        public bool KycReady { get; init; }
        public bool WalletReady { get; init; }
        public bool Ready { get; init; }
        public string? UnavailableReason { get; init; }
    }

    private sealed record KycSessionWire
    {
        public string? Provider { get; init; }
        public bool HostedVerification { get; init; }
        public bool AcceptsDocumentReferences { get; init; }
        public bool DevelopmentSimulation { get; init; }
        public string? VerificationUrl { get; init; }
        public DateTime? ExpiresAt { get; init; }
        public string? Instructions { get; init; }
    }

}
