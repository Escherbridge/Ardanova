namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IOpportunityBidService
{
    Task<Result<OpportunityBidDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OpportunityBidDto>>> GetByOpportunityIdAsync(string opportunityId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OpportunityBidDto>>> GetByBidderIdAsync(string bidderId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OpportunityBidDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default);
    Task<Result<OpportunityBidDto>> CreateAsync(CreateOpportunityBidDto dto, CancellationToken ct = default);
    Task<Result<OpportunityBidDto>> UpdateAsync(string id, UpdateOpportunityBidDto dto, CancellationToken ct = default);
    Task<Result<OpportunityBidDto>> AcceptAsync(string id, CancellationToken ct = default);
    Task<Result<OpportunityBidDto>> RejectAsync(string id, CancellationToken ct = default);
    Task<Result<OpportunityBidDto>> WithdrawAsync(string id, CancellationToken ct = default);
    Task<Result<OpportunityBidDto>> CompleteAsync(string id, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}
