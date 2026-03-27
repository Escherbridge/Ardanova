namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface IGovernanceService
{
    // Proposal operations
    Task<Result<ProposalDto>> GetProposalByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProposalDto>>> GetAllProposalsAsync(CancellationToken ct = default);
    Task<Result<PagedResult<ProposalDto>>> GetProposalsPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<PagedResult<ProposalDto>>> SearchProposalsAsync(
        string? searchTerm,
        ProposalType? type,
        ProposalStatus? status,
        string? projectId,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProposalDto>>> GetActiveProposalsAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProposalDto>>> GetByProposerIdAsync(string proposerId, CancellationToken ct = default);
    Task<Result<ProposalDto>> CreateProposalAsync(CreateProposalDto dto, CancellationToken ct = default);
    Task<Result<ProposalDto>> UpdateProposalAsync(string id, UpdateProposalDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteProposalAsync(string id, CancellationToken ct = default);

    // Voting operations
    Task<Result<VoteDto>> CastVoteAsync(string proposalId, CastVoteDto dto, CancellationToken ct = default);
    Task<Result<IReadOnlyList<VoteDto>>> GetVotesAsync(string proposalId, CancellationToken ct = default);
    Task<Result<VoteDto>> GetUserVoteAsync(string proposalId, string userId, CancellationToken ct = default);
    Task<Result<ProposalVoteSummaryDto>> GetVoteSummaryAsync(string proposalId, CancellationToken ct = default);

    // Proposal lifecycle
    Task<Result<ProposalDto>> ExecuteProposalAsync(string id, CancellationToken ct = default);
    Task<Result<ProposalDto>> CancelProposalAsync(string id, CancellationToken ct = default);
    Task<Result<ProposalDto>> PublishProposalAsync(string id, CancellationToken ct = default);

    // Proposal comments
    Task<Result<IReadOnlyList<ProposalCommentDto>>> GetProposalCommentsAsync(string proposalId, CancellationToken ct = default);
    Task<Result<ProposalCommentDto>> CreateProposalCommentAsync(CreateProposalCommentDto dto, CancellationToken ct = default);
}
