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
    /// <summary>
    /// Why a PAID guild bid that does not say how the award splits is refused. A share is set
    /// aside per beneficiary, so a bid naming a guild but not the guild's share could only be
    /// held for whichever member happened to submit it. The same refusal
    /// <see cref="TaskCommerceService.GuildSplitUnsupported"/> makes at acceptance.
    /// </summary>
    /// <remarks>An unpaid guild bid commits no stake and is unaffected.</remarks>
    public const string GuildStakeBidUnsupported =
        "A guild bidding for a share of a project's stake has to say how much of the bid is the "
        + "guild's. The rest stays with the member who does the work.";

    /// <summary>Why a declared guild share that names no guild is refused.</summary>
    public const string GuildSplitNeedsGuild =
        "This bid sets a share aside for a guild without naming one. Choose the guild you are "
        + "bidding for, or remove the guild's share.";

    /// <summary>
    /// Why a guild share that leaves nothing for the worker — or nothing for the guild — is
    /// refused.
    /// </summary>
    public const string GuildSplitOutOfRange =
        "The guild's share has to be part of a paid bid, and it has to leave the member doing "
        + "the work a share of their own.";

    /// <summary>Why a part-unit guild share is refused.</summary>
    public const string PartUnitSplitUnsupported =
        "Split the bid in whole units of the project's stake: each share is set aside in whole "
        + "units, so a part unit cannot be committed.";

    /// <summary>Why a part-unit bid is refused before anyone can accept it.</summary>
    public const string PartUnitBidUnsupported =
        "Bid in whole units of the project's stake: a project sets its worker and sponsor "
        + "allowances aside in whole units, so a part unit cannot be committed.";

    /// <summary>
    /// Why a proposal is refused once the opportunity has stopped taking them. The client hides
    /// the form; this refusal is what a direct call meets, and it is the only one that counts.
    /// </summary>
    public const string OpportunityNotOpen =
        "This opportunity is not taking proposals any more.";

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

        // The opportunity's own state decides whether a proposal may be posted. Hiding the form
        // stops nobody who calls the route directly, so the gate lives where the bid is written.
        if (opportunity.status != OpportunityStatus.OPEN)
            return Result<OpportunityBidDto>.ValidationError(OpportunityNotOpen);

        var existingBid = await _repository.FindOneAsync(
            b => b.opportunityId == dto.OpportunityId && b.bidderId == dto.BidderId, ct);
        if (existingBid is not null)
            return Result<OpportunityBidDto>.ValidationError("User has already submitted a bid for this opportunity");

        if (DescribeUnacceptableStakeBid(opportunity, dto.ProposedAmount, dto.GuildId, dto.GuildAwardSplit) is { } refusal)
            return Result<OpportunityBidDto>.ValidationError(refusal);

        var bid = new OpportunityBid
        {
            id = Guid.NewGuid().ToString(),
            opportunityId = dto.OpportunityId,
            bidderId = dto.BidderId,
            guildId = dto.GuildId,
            proposedAmount = dto.ProposedAmount,
            guildAwardSplit = dto.GuildAwardSplit,
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

        var opportunity = await _opportunityRepository.GetByIdAsync(bid.opportunityId, ct);
        if (opportunity is null)
            return Result<OpportunityBidDto>.NotFound($"Opportunity with id {bid.opportunityId} not found");

        // An edit is a fresh proposal in everything but name, so it meets the same gate: a
        // closed opportunity cannot be re-bid by revising a proposal that is already in.
        if (opportunity.status != OpportunityStatus.OPEN)
            return Result<OpportunityBidDto>.ValidationError(OpportunityNotOpen);

        // The same rule the bid was created under, checked against the values the edit would
        // leave behind. Without it an edit is the way around it.
        var revisedAmount = dto.ProposedAmount ?? bid.proposedAmount;
        var revisedSplit = dto.GuildAwardSplit ?? bid.guildAwardSplit;
        if (DescribeUnacceptableStakeBid(opportunity, revisedAmount, bid.guildId, revisedSplit) is { } refusal)
            return Result<OpportunityBidDto>.ValidationError(refusal);

        if (dto.ProposedAmount.HasValue) bid.proposedAmount = dto.ProposedAmount;
        if (dto.GuildAwardSplit.HasValue) bid.guildAwardSplit = dto.GuildAwardSplit;
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

    /// <summary>
    /// Null when this bid could actually be accepted; the person-facing refusal otherwise.
    /// Only a paid bid on a project-backed opportunity is constrained — an unpaid bid, or a bid
    /// on an opportunity with no project behind it, commits no stake and is left alone.
    /// </summary>
    /// <remarks>
    /// A guild bid is acceptable only when it declares the guild's share, because acceptance
    /// opens one hold per beneficiary and each of those has to be a positive whole number of
    /// units. The same arithmetic is re-checked at acceptance, where the units actually move.
    /// </remarks>
    private static string? DescribeUnacceptableStakeBid(
        Opportunity opportunity,
        decimal? proposedAmount,
        string? guildId,
        decimal? guildAwardSplit)
    {
        if (string.IsNullOrWhiteSpace(opportunity.projectId) || proposedAmount is not > 0m)
        {
            // A share can only be honoured out of a paid, project-backed bid, so a declared
            // split on anything else is named rather than quietly kept.
            return guildAwardSplit is > 0m ? GuildSplitOutOfRange : null;
        }

        if (decimal.Truncate(proposedAmount.Value) != proposedAmount.Value)
            return PartUnitBidUnsupported;

        var namesGuild = !string.IsNullOrWhiteSpace(guildId);
        if (guildAwardSplit is null or <= 0m)
            return namesGuild ? GuildStakeBidUnsupported : null;

        if (!namesGuild)
            return GuildSplitNeedsGuild;

        if (decimal.Truncate(guildAwardSplit.Value) != guildAwardSplit.Value)
            return PartUnitSplitUnsupported;

        // Both legs are held separately, so both have to be worth holding.
        return guildAwardSplit.Value >= proposedAmount.Value ? GuildSplitOutOfRange : null;
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
