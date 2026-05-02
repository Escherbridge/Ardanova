namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class OpportunityBidService : IOpportunityBidService
{
    private readonly IRepository<OpportunityBid> _repository;
    private readonly IRepository<Opportunity> _opportunityRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Guild> _guildRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public OpportunityBidService(
        IRepository<OpportunityBid> repository,
        IRepository<Opportunity> opportunityRepository,
        IRepository<User> userRepository,
        IRepository<Guild> guildRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _opportunityRepository = opportunityRepository;
        _userRepository = userRepository;
        _guildRepository = guildRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<OpportunityBidDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<OpportunityBidDto>.NotFound($"Opportunity bid with id {id} not found");

        var dto = await EnrichBidDtoAsync(bid, ct);
        return Result<OpportunityBidDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<OpportunityBidDto>>> GetByOpportunityIdAsync(string opportunityId, CancellationToken ct = default)
    {
        var opportunity = await _opportunityRepository.GetByIdAsync(opportunityId, ct);
        if (opportunity is null)
            return Result<IReadOnlyList<OpportunityBidDto>>.NotFound($"Opportunity with id {opportunityId} not found");

        var bids = await _repository.FindAsync(b => b.opportunityId == opportunityId, ct);
        var dtos = await EnrichBidDtosAsync(bids, ct);
        return Result<IReadOnlyList<OpportunityBidDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<OpportunityBidDto>>> GetByBidderIdAsync(string bidderId, CancellationToken ct = default)
    {
        var bids = await _repository.FindAsync(b => b.bidderId == bidderId, ct);
        var dtos = await EnrichBidDtosAsync(bids, ct);
        return Result<IReadOnlyList<OpportunityBidDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<OpportunityBidDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default)
    {
        var bids = await _repository.FindAsync(b => b.guildId == guildId, ct);
        var dtos = await EnrichBidDtosAsync(bids, ct);
        return Result<IReadOnlyList<OpportunityBidDto>>.Success(dtos);
    }

    public async Task<Result<OpportunityBidDto>> CreateAsync(CreateOpportunityBidDto dto, CancellationToken ct = default)
    {
        var opportunity = await _opportunityRepository.GetByIdAsync(dto.OpportunityId, ct);
        if (opportunity is null)
            return Result<OpportunityBidDto>.NotFound($"Opportunity with id {dto.OpportunityId} not found");

        var existingBid = await _repository.FindOneAsync(
            b => b.opportunityId == dto.OpportunityId && b.bidderId == dto.BidderId, ct);
        if (existingBid is not null)
            return Result<OpportunityBidDto>.ValidationError("User has already submitted a bid for this opportunity");

        var bid = new OpportunityBid
        {
            id = Guid.NewGuid().ToString(),
            opportunityId = dto.OpportunityId,
            bidderId = dto.BidderId,
            guildId = dto.GuildId,
            proposedAmount = dto.ProposedAmount,
            proposal = dto.Proposal,
            estimatedHours = dto.EstimatedHours,
            timeline = dto.Timeline,
            deliverables = dto.Deliverables,
            status = BidStatus.SUBMITTED,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichBidDtoAsync(bid, ct);
        return Result<OpportunityBidDto>.Success(resultDto);
    }

    public async Task<Result<OpportunityBidDto>> UpdateAsync(string id, UpdateOpportunityBidDto dto, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<OpportunityBidDto>.NotFound($"Opportunity bid with id {id} not found");

        if (bid.status != BidStatus.SUBMITTED)
            return Result<OpportunityBidDto>.ValidationError("Only bids with SUBMITTED status can be updated");

        if (dto.ProposedAmount.HasValue) bid.proposedAmount = dto.ProposedAmount;
        if (dto.Proposal is not null) bid.proposal = dto.Proposal;
        if (dto.EstimatedHours.HasValue) bid.estimatedHours = dto.EstimatedHours;
        if (dto.Timeline is not null) bid.timeline = dto.Timeline;
        if (dto.Deliverables is not null) bid.deliverables = dto.Deliverables;

        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichBidDtoAsync(bid, ct);
        return Result<OpportunityBidDto>.Success(resultDto);
    }

    public async Task<Result<OpportunityBidDto>> AcceptAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<OpportunityBidDto>.NotFound($"Opportunity bid with id {id} not found");

        if (bid.status != BidStatus.SUBMITTED && bid.status != BidStatus.UNDER_REVIEW)
            return Result<OpportunityBidDto>.ValidationError("Only bids with SUBMITTED or UNDER_REVIEW status can be accepted");

        bid.status = BidStatus.ACCEPTED;
        bid.reviewedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichBidDtoAsync(bid, ct);
        return Result<OpportunityBidDto>.Success(resultDto);
    }

    public async Task<Result<OpportunityBidDto>> RejectAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<OpportunityBidDto>.NotFound($"Opportunity bid with id {id} not found");

        if (bid.status != BidStatus.SUBMITTED && bid.status != BidStatus.UNDER_REVIEW)
            return Result<OpportunityBidDto>.ValidationError("Only bids with SUBMITTED or UNDER_REVIEW status can be rejected");

        bid.status = BidStatus.REJECTED;
        bid.reviewedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichBidDtoAsync(bid, ct);
        return Result<OpportunityBidDto>.Success(resultDto);
    }

    public async Task<Result<OpportunityBidDto>> WithdrawAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<OpportunityBidDto>.NotFound($"Opportunity bid with id {id} not found");

        if (bid.status != BidStatus.SUBMITTED && bid.status != BidStatus.UNDER_REVIEW)
            return Result<OpportunityBidDto>.ValidationError("Only bids with SUBMITTED or UNDER_REVIEW status can be withdrawn");

        bid.status = BidStatus.WITHDRAWN;

        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichBidDtoAsync(bid, ct);
        return Result<OpportunityBidDto>.Success(resultDto);
    }

    public async Task<Result<OpportunityBidDto>> CompleteAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<OpportunityBidDto>.NotFound($"Opportunity bid with id {id} not found");

        if (bid.status != BidStatus.ACCEPTED)
            return Result<OpportunityBidDto>.ValidationError("Only bids with ACCEPTED status can be completed");

        bid.status = BidStatus.COMPLETED;

        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var completedDto = await EnrichBidDtoAsync(bid, ct);
        return Result<OpportunityBidDto>.Success(completedDto);
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<bool>.NotFound($"Opportunity bid with id {id} not found");

        await _repository.DeleteAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    private async Task<OpportunityBidDto> EnrichBidDtoAsync(OpportunityBid bid, CancellationToken ct)
    {
        var dto = _mapper.Map<OpportunityBidDto>(bid);

        var bidder = await _userRepository.GetByIdAsync(bid.bidderId, ct);
        if (bidder is not null)
            dto = dto with { Bidder = _mapper.Map<OpportunityBidBidderDto>(bidder) };

        if (!string.IsNullOrWhiteSpace(bid.guildId))
        {
            var guild = await _guildRepository.GetByIdAsync(bid.guildId, ct);
            if (guild is not null)
                dto = dto with { Guild = _mapper.Map<OpportunityBidGuildDto>(guild) };
        }

        return dto;
    }

    private async Task<IReadOnlyList<OpportunityBidDto>> EnrichBidDtosAsync(IEnumerable<OpportunityBid> bids, CancellationToken ct)
    {
        var dtos = new List<OpportunityBidDto>();
        foreach (var bid in bids)
        {
            dtos.Add(await EnrichBidDtoAsync(bid, ct));
        }
        return dtos;
    }
}
