namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class GovernanceService : IGovernanceService
{
    private readonly IRepository<Proposal> _proposalRepository;
    private readonly IRepository<Vote> _voteRepository;
    private readonly IRepository<ProposalExecution> _executionRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GovernanceService(
        IRepository<Proposal> proposalRepository,
        IRepository<Vote> voteRepository,
        IRepository<ProposalExecution> executionRepository,
        IRepository<User> userRepository,
        IRepository<Project> projectRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _proposalRepository = proposalRepository;
        _voteRepository = voteRepository;
        _executionRepository = executionRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProposalDto>> GetProposalByIdAsync(string id, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(id, ct);
        if (proposal is null)
            return Result<ProposalDto>.NotFound($"Proposal with id {id} not found");

        var dto = await EnrichProposalDtoAsync(proposal, ct);
        return Result<ProposalDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<ProposalDto>>> GetAllProposalsAsync(CancellationToken ct = default)
    {
        var proposals = await _proposalRepository.GetAllAsync(ct);
        var dtos = await EnrichProposalDtosAsync(proposals, ct);
        return Result<IReadOnlyList<ProposalDto>>.Success(dtos);
    }

    public async Task<Result<PagedResult<ProposalDto>>> GetProposalsPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _proposalRepository.GetPagedAsync(page, pageSize, null, ct);
        var dtos = await EnrichProposalDtosAsync(result.Items, ct);
        return Result<PagedResult<ProposalDto>>.Success(new PagedResult<ProposalDto>(dtos.ToList(), result.TotalCount, result.Page, result.PageSize));
    }

    public async Task<Result<PagedResult<ProposalDto>>> SearchProposalsAsync(
        string? searchTerm,
        ProposalType? type,
        ProposalStatus? status,
        string? projectId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _proposalRepository.Query();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p => p.title.ToLower().Contains(term) ||
                p.description.ToLower().Contains(term));
        }

        if (type.HasValue)
            query = query.Where(p => p.type == type.Value);

        if (status.HasValue)
            query = query.Where(p => p.status == status.Value);

        if (!string.IsNullOrEmpty(projectId))
            query = query.Where(p => p.projectId == projectId);

        query = query.OrderByDescending(p => p.createdAt);

        var totalCount = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var dtos = await EnrichProposalDtosAsync(items, ct);

        return Result<PagedResult<ProposalDto>>.Success(new PagedResult<ProposalDto>(dtos.ToList(), totalCount, page, pageSize));
    }

    public async Task<Result<IReadOnlyList<ProposalDto>>> GetActiveProposalsAsync(CancellationToken ct = default)
    {
        var proposals = await _proposalRepository.FindAsync(p => p.status == ProposalStatus.ACTIVE, ct);
        var dtos = await EnrichProposalDtosAsync(proposals, ct);
        return Result<IReadOnlyList<ProposalDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<ProposalDto>>> GetByProposerIdAsync(string proposerId, CancellationToken ct = default)
    {
        var proposals = await _proposalRepository.FindAsync(p => p.creatorId == proposerId, ct);
        var dtos = await EnrichProposalDtosAsync(proposals, ct);
        return Result<IReadOnlyList<ProposalDto>>.Success(dtos);
    }

    public async Task<Result<ProposalDto>> CreateProposalAsync(CreateProposalDto dto, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetByIdAsync(dto.ProjectId, ct);
        if (project is null)
            return Result<ProposalDto>.NotFound($"Project with id {dto.ProjectId} not found");

        var proposal = new Proposal
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            creatorId = dto.CreatorId,
            type = dto.Type,
            title = dto.Title,
            description = dto.Description,
            options = dto.Options,
            quorum = dto.Quorum,
            threshold = dto.Threshold,
            status = ProposalStatus.DRAFT,
            votingStart = dto.VotingStart,
            votingEnd = dto.VotingEnd,
            executionDelay = dto.ExecutionDelay,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _proposalRepository.AddAsync(proposal, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichProposalDtoAsync(proposal, ct);
        return Result<ProposalDto>.Success(resultDto);
    }

    public async Task<Result<ProposalDto>> UpdateProposalAsync(string id, UpdateProposalDto dto, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(id, ct);
        if (proposal is null)
            return Result<ProposalDto>.NotFound($"Proposal with id {id} not found");

        if (proposal.status != ProposalStatus.DRAFT)
            return Result<ProposalDto>.ValidationError("Only draft proposals can be updated");

        if (dto.Title is not null) proposal.title = dto.Title;
        if (dto.Description is not null) proposal.description = dto.Description;
        if (dto.Options is not null) proposal.options = dto.Options;
        if (dto.Quorum.HasValue) proposal.quorum = dto.Quorum.Value;
        if (dto.Threshold.HasValue) proposal.threshold = dto.Threshold.Value;
        if (dto.VotingStart.HasValue) proposal.votingStart = dto.VotingStart;
        if (dto.VotingEnd.HasValue) proposal.votingEnd = dto.VotingEnd;
        if (dto.ExecutionDelay.HasValue) proposal.executionDelay = dto.ExecutionDelay;
        proposal.updatedAt = DateTime.UtcNow;

        await _proposalRepository.UpdateAsync(proposal, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichProposalDtoAsync(proposal, ct);
        return Result<ProposalDto>.Success(resultDto);
    }

    public async Task<Result<bool>> DeleteProposalAsync(string id, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(id, ct);
        if (proposal is null)
            return Result<bool>.NotFound($"Proposal with id {id} not found");

        if (proposal.status != ProposalStatus.DRAFT)
            return Result<bool>.ValidationError("Only draft proposals can be deleted");

        await _proposalRepository.DeleteAsync(proposal, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<VoteDto>> CastVoteAsync(string proposalId, CastVoteDto dto, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId, ct);
        if (proposal is null)
            return Result<VoteDto>.NotFound($"Proposal with id {proposalId} not found");

        if (proposal.status != ProposalStatus.ACTIVE)
            return Result<VoteDto>.ValidationError("Voting is not active for this proposal");

        if (proposal.votingEnd.HasValue && DateTime.UtcNow > proposal.votingEnd.Value)
            return Result<VoteDto>.ValidationError("Voting period has ended");

        var existingVote = await _voteRepository.FindOneAsync(
            v => v.proposalId == proposalId && v.voterId == dto.VoterId, ct);
        if (existingVote is not null)
            return Result<VoteDto>.ValidationError("User has already voted on this proposal");

        var vote = new Vote
        {
            id = Guid.NewGuid().ToString(),
            proposalId = proposalId,
            voterId = dto.VoterId,
            choice = dto.Choice,
            weight = dto.Weight ?? 1,
            reason = dto.Reason,
            createdAt = DateTime.UtcNow
        };

        await _voteRepository.AddAsync(vote, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var voteDto = _mapper.Map<VoteDto>(vote);
        var voter = await _userRepository.GetByIdAsync(dto.VoterId, ct);
        if (voter is not null)
            voteDto = voteDto with { Voter = _mapper.Map<VoteUserDto>(voter) };

        return Result<VoteDto>.Success(voteDto);
    }

    public async Task<Result<IReadOnlyList<VoteDto>>> GetVotesAsync(string proposalId, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId, ct);
        if (proposal is null)
            return Result<IReadOnlyList<VoteDto>>.NotFound($"Proposal with id {proposalId} not found");

        var votes = await _voteRepository.FindAsync(v => v.proposalId == proposalId, ct);
        var dtos = new List<VoteDto>();

        foreach (var vote in votes)
        {
            var dto = _mapper.Map<VoteDto>(vote);
            var voter = await _userRepository.GetByIdAsync(vote.voterId, ct);
            if (voter is not null)
                dto = dto with { Voter = _mapper.Map<VoteUserDto>(voter) };
            dtos.Add(dto);
        }

        return Result<IReadOnlyList<VoteDto>>.Success(dtos);
    }

    public async Task<Result<VoteDto>> GetUserVoteAsync(string proposalId, string userId, CancellationToken ct = default)
    {
        var vote = await _voteRepository.FindOneAsync(
            v => v.proposalId == proposalId && v.voterId == userId, ct);
        if (vote is null)
            return Result<VoteDto>.NotFound("User has not voted on this proposal");

        var dto = _mapper.Map<VoteDto>(vote);
        var voter = await _userRepository.GetByIdAsync(userId, ct);
        if (voter is not null)
            dto = dto with { Voter = _mapper.Map<VoteUserDto>(voter) };

        return Result<VoteDto>.Success(dto);
    }

    public async Task<Result<ProposalVoteSummaryDto>> GetVoteSummaryAsync(string proposalId, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId, ct);
        if (proposal is null)
            return Result<ProposalVoteSummaryDto>.NotFound($"Proposal with id {proposalId} not found");

        var votes = await _voteRepository.FindAsync(v => v.proposalId == proposalId, ct);
        var votesList = votes.ToList();

        var totalVotes = votesList.Count;
        var totalVotingPower = votesList.Sum(v => v.weight);

        var optionSummaries = votesList
            .GroupBy(v => v.choice)
            .ToDictionary(
                g => g.Key,
                g => new VoteOptionSummary
                {
                    Choice = g.Key,
                    VoteCount = g.Count(),
                    VotingPower = g.Sum(v => v.weight),
                    Percentage = totalVotingPower > 0 ? (g.Sum(v => v.weight) / totalVotingPower) * 100 : 0
                });

        var quorumReached = totalVotingPower >= proposal.quorum;
        int? winningOption = null;

        if (optionSummaries.Any())
        {
            var maxVotingPower = optionSummaries.Values.Max(s => s.VotingPower);
            var winner = optionSummaries.Values.FirstOrDefault(s => s.VotingPower == maxVotingPower);
            if (winner is not null && (winner.Percentage >= proposal.threshold))
                winningOption = winner.Choice;
        }

        var summary = new ProposalVoteSummaryDto
        {
            ProposalId = proposalId,
            TotalVotes = totalVotes,
            TotalVotingPower = totalVotingPower,
            OptionSummaries = optionSummaries,
            QuorumReached = quorumReached,
            WinningOption = winningOption
        };

        return Result<ProposalVoteSummaryDto>.Success(summary);
    }

    public async Task<Result<ProposalDto>> ExecuteProposalAsync(string id, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(id, ct);
        if (proposal is null)
            return Result<ProposalDto>.NotFound($"Proposal with id {id} not found");

        if (proposal.status != ProposalStatus.PASSED)
            return Result<ProposalDto>.ValidationError("Only passed proposals can be executed");

        proposal.status = ProposalStatus.EXECUTED;
        proposal.updatedAt = DateTime.UtcNow;

        var execution = new ProposalExecution
        {
            id = Guid.NewGuid().ToString(),
            proposalId = id,
            executedAt = DateTime.UtcNow
        };

        await _proposalRepository.UpdateAsync(proposal, ct);
        await _executionRepository.AddAsync(execution, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichProposalDtoAsync(proposal, ct);
        return Result<ProposalDto>.Success(resultDto);
    }

    public async Task<Result<ProposalDto>> CancelProposalAsync(string id, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(id, ct);
        if (proposal is null)
            return Result<ProposalDto>.NotFound($"Proposal with id {id} not found");

        if (proposal.status == ProposalStatus.EXECUTED)
            return Result<ProposalDto>.ValidationError("Executed proposals cannot be cancelled");

        proposal.status = ProposalStatus.CANCELLED;
        proposal.updatedAt = DateTime.UtcNow;

        await _proposalRepository.UpdateAsync(proposal, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichProposalDtoAsync(proposal, ct);
        return Result<ProposalDto>.Success(resultDto);
    }

    private async Task<ProposalDto> EnrichProposalDtoAsync(Proposal proposal, CancellationToken ct)
    {
        var votes = await _voteRepository.FindAsync(v => v.proposalId == proposal.id, ct);
        var votesList = votes.ToList();

        var dto = _mapper.Map<ProposalDto>(proposal) with
        {
            VotesCount = votesList.Count,
            TotalVotingPower = votesList.Sum(v => v.weight)
        };

        var creator = await _userRepository.GetByIdAsync(proposal.creatorId, ct);
        if (creator is not null)
            dto = dto with { Creator = _mapper.Map<ProposalCreatorDto>(creator) };

        var project = await _projectRepository.GetByIdAsync(proposal.projectId, ct);
        if (project is not null)
            dto = dto with { Project = _mapper.Map<ProposalProjectDto>(project) };

        return dto;
    }

    private async Task<IReadOnlyList<ProposalDto>> EnrichProposalDtosAsync(IEnumerable<Proposal> proposals, CancellationToken ct)
    {
        var dtos = new List<ProposalDto>();
        foreach (var proposal in proposals)
        {
            dtos.Add(await EnrichProposalDtoAsync(proposal, ct));
        }
        return dtos;
    }
}
