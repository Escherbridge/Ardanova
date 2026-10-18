namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs.Nova;

/// <summary>Nova AI use cases; the only caller of the upstream validator. See Infrastructure/Ogai/AGENTS.md.</summary>
public interface INovaOrchestrationService
{
    /// <summary>Runs one project-refinement turn for the actor: the next question, or the feasibility verdict.</summary>
    Task<Result<NovaRefineResponseDto>> RefineProjectIdeaAsync(
        NovaRefineRequestDto request,
        string actorId,
        CancellationToken ct = default);

    /// <summary>Which Nova AI capabilities this deployment serves.</summary>
    NovaStatusDto GetStatus();
}

/// <summary>Port onto the OVERSEASGENAI partner validate API (Infrastructure/Ogai); never exposed to members.</summary>
public interface IOgaiClient
{
    /// <summary>A <see cref="NovaAvailability"/> value: what this client really answers with.</summary>
    string Availability { get; }

    /// <summary>Sends one validate turn. ValidationError = input refused, NotFound = session expired upstream, Failure = unavailable (error <see cref="NovaErrors.Capacity"/> when the partner's day is spent).</summary>
    Task<Result<OgaiTurn>> ValidateAsync(
        string idea,
        IReadOnlyList<NovaTurnDto> turns,
        string? state,
        string? sessionId,
        CancellationToken ct = default);
}
