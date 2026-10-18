namespace ArdaNova.Application.Services.Implementations;

using System.Data.Common;
using System.Text.Json;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// On whose authority an open vote is ended before its window runs out. Required by
/// <see cref="IProposalVoteClosing.CloseVoteAsync"/>, so a new closing seam cannot be written
/// without answering the question — the compiler refuses the call otherwise.
/// </summary>
/// <remarks>
/// <para>There is no "unknown" authority and no default that permits anything. The seam that
/// carries no caller identity — the generic proposal update — supplies
/// <see cref="Unattributed"/>, which refuses, so an early close is impossible except through a
/// door that positively names its closer.</para>
/// <para><b>The author is never independent of their own proposal.</b> OI-46: an author who may
/// publish a proposal, vote on it and close it has manufactured a decision of record without
/// anybody else seeing the vote open.</para>
/// </remarks>
public sealed record ProposalCloseAuthority
{
    private ProposalCloseAuthority(string? closerId, bool runsTheProject)
    {
        CloserId = closerId;
        RunsTheProject = runsTheProject;
    }

    /// <summary>The member ending the vote, or null when no caller identity reached this service.</summary>
    public string? CloserId { get; }

    /// <summary>True only where the caller proved the closer runs this project or administers the platform.</summary>
    public bool RunsTheProject { get; }

    /// <summary>No identity reached the service, so nothing may be closed early. The value every identity-less seam supplies.</summary>
    public static ProposalCloseAuthority Unattributed { get; } = new(null, false);

    /// <summary>A member the calling controller authenticated AND checked runs this project. A blank id resolves to <see cref="Unattributed"/>, never to a pass.</summary>
    public static ProposalCloseAuthority ProjectAuthority(string? closerId) =>
        string.IsNullOrWhiteSpace(closerId) ? Unattributed : new ProposalCloseAuthority(closerId, true);

    /// <summary>Whether this authority may end a vote early on a proposal raised by <paramref name="proposalCreatorId"/>.</summary>
    public bool MayEndEarly(string? proposalCreatorId) =>
        RunsTheProject &&
        !string.IsNullOrWhiteSpace(CloserId) &&
        !string.Equals(CloserId, proposalCreatorId, StringComparison.Ordinal);
}

/// <summary>Ending an open vote early — a different authority from editing a proposal, so callers depend on it explicitly rather than reaching it through the update seam.</summary>
/// <remarks>Declared beside its only implementation so the authority type and the seam that honours it cannot drift apart; registered on the same scoped <see cref="GovernanceService"/> instance.</remarks>
public interface IProposalVoteClosing
{
    /// <summary>Shuts an open voting window now. The outcome is NOT resolved in this request — see <see cref="GovernanceService.CloseVoteAsync"/>.</summary>
    Task<Result<ProposalDto>> CloseVoteAsync(string proposalId, ProposalCloseAuthority authority, CancellationToken ct = default);
}

/// <summary>Project decisions as a vote of record: one member one vote, quorum in members, outcomes resolved from the tally.</summary>
/// <remarks>
/// <para>Rationale and the close-vs-cancel rule: see <c>ArdaNova.Application/Services/AGENTS.md</c> §Governance.</para>
/// <para><b>Who may vote is asked here, not at the edge.</b> Both routes into a ballot —
/// <c>GovernanceController</c> and <c>ProjectsController</c> — funnel through
/// <see cref="CastVoteAsync"/>, so the membership test cannot be reached around. The controllers
/// supply the voter id from the authenticated actor; nothing a caller writes in the body selects a
/// voter or a weight.</para>
/// <para><b>Fail closed by construction.</b> The member register is an optional constructor
/// collaborator, mirroring <c>MembershipCredentialsController</c>. Without it this service cannot
/// answer "who may vote here", so it refuses every ballot and reports an empty electorate — which
/// can only ever resolve to EXPIRED, never to PASSED.</para>
/// </remarks>
public class GovernanceService : IGovernanceService, IProposalVoteClosing
{
    /// <summary>Every eligible member carries exactly one vote; a ballot never carries a caller-supplied weight.</summary>
    private const decimal OneMemberOneVote = 1m;

    /// <summary>The fewest ballots that can carry a decision, whatever percentage the proposal declared.</summary>
    private const int MinimumBallotsForQuorum = 2;

    /// <summary>The shortest time a vote may stay open, so the members a decision binds can see it before it closes.</summary>
    public static readonly TimeSpan MinimumVotingWindow = TimeSpan.FromHours(12);

    /// <summary>SQLSTATE for a unique-constraint violation: the (proposalId, voterId) index refusing a second ballot.</summary>
    private const string UniqueViolationSqlState = "23505";

    private readonly IRepository<Proposal> _proposalRepository;
    private readonly IRepository<Vote> _voteRepository;
    private readonly IRepository<ProposalExecution> _executionRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<ProposalComment> _proposalCommentRepository;
    private readonly IRepository<MembershipCredential>? _membershipCredentialRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GovernanceService(
        IRepository<Proposal> proposalRepository,
        IRepository<Vote> voteRepository,
        IRepository<ProposalExecution> executionRepository,
        IRepository<User> userRepository,
        IRepository<Project> projectRepository,
        IRepository<ProposalComment> proposalCommentRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRepository<MembershipCredential>? membershipCredentialRepository = null)
    {
        _proposalRepository = proposalRepository;
        _voteRepository = voteRepository;
        _executionRepository = executionRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _proposalCommentRepository = proposalCommentRepository;
        _membershipCredentialRepository = membershipCredentialRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProposalDto>> GetProposalByIdAsync(string id, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(id, ct);
        if (proposal is null)
            return Result<ProposalDto>.NotFound($"Proposal with id {id} not found");

        await ResolveDueProposalsAsync(new[] { proposal }, ct);

        var dto = await EnrichProposalDtoAsync(proposal, ct);
        return Result<ProposalDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<ProposalDto>>> GetAllProposalsAsync(CancellationToken ct = default)
    {
        var proposals = await _proposalRepository.GetAllAsync(ct);
        await ResolveDueProposalsAsync(proposals, ct);
        var dtos = await EnrichProposalDtosAsync(proposals, ct);
        return Result<IReadOnlyList<ProposalDto>>.Success(dtos);
    }

    public async Task<Result<PagedResult<ProposalDto>>> GetProposalsPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _proposalRepository.GetPagedAsync(page, pageSize, null, ct);
        await ResolveDueProposalsAsync(result.Items, ct);
        var dtos = await EnrichProposalDtosAsync(result.Items, ct);
        return Result<PagedResult<ProposalDto>>.Success(new PagedResult<ProposalDto>(dtos.ToList(), result.TotalCount, result.Page, result.PageSize));
    }

    public Task<Result<PagedResult<ProposalDto>>> SearchProposalsAsync(
        string? searchTerm,
        ProposalType? type,
        ProposalStatus? status,
        string? projectId,
        int page,
        int pageSize,
        CancellationToken ct = default)
        => SearchWithinAsync(_proposalRepository.Query(), searchTerm, type, status, projectId, page, pageSize, ct);

    /// <summary>The source decides what may be seen; the caller's filters only narrow it.</summary>
    private async Task<Result<PagedResult<ProposalDto>>> SearchWithinAsync(
        IQueryable<Proposal> source,
        string? searchTerm,
        ProposalType? type,
        ProposalStatus? status,
        string? projectId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = source;

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
        await ResolveDueProposalsAsync(items, ct);

        // A proposal that resolved while this page was being read no longer belongs under a status filter it has left.
        if (status.HasValue)
            items = items.Where(p => p.status == status.Value).ToList();

        var dtos = await EnrichProposalDtosAsync(items, ct);

        return Result<PagedResult<ProposalDto>>.Success(new PagedResult<ProposalDto>(dtos.ToList(), totalCount, page, pageSize));
    }

    public async Task<Result<IReadOnlyList<ProposalDto>>> GetActiveProposalsAsync(CancellationToken ct = default)
    {
        var proposals = await _proposalRepository.FindAsync(p => p.status == ProposalStatus.ACTIVE, ct);
        await ResolveDueProposalsAsync(proposals, ct);
        var stillActive = proposals.Where(p => p.status == ProposalStatus.ACTIVE).ToList();
        var dtos = await EnrichProposalDtosAsync(stillActive, ct);
        return Result<IReadOnlyList<ProposalDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<ProposalDto>>> GetByProposerIdAsync(string proposerId, CancellationToken ct = default)
    {
        var proposals = await _proposalRepository.FindAsync(p => p.creatorId == proposerId, ct);
        await ResolveDueProposalsAsync(proposals, ct);
        var dtos = await EnrichProposalDtosAsync(proposals, ct);
        return Result<IReadOnlyList<ProposalDto>>.Success(dtos);
    }

    // ===== Viewer-scoped reads: see Services/AGENTS.md section Governance visibility =====

    public async Task<Result<ProposalDto>> GetVisibleProposalByIdAsync(ProjectViewer viewer, string id, CancellationToken ct = default)
    {
        var proposal = VisibleProposals(viewer).FirstOrDefault(p => p.id == id);
        if (proposal is null)
            return Result<ProposalDto>.NotFound($"Proposal with id {id} not found");

        await ResolveDueProposalsAsync(new[] { proposal }, ct);
        return Result<ProposalDto>.Success(await EnrichProposalDtoAsync(proposal, ct));
    }

    public async Task<Result<IReadOnlyList<ProposalDto>>> GetAllVisibleProposalsAsync(ProjectViewer viewer, CancellationToken ct = default)
    {
        var proposals = VisibleProposals(viewer).ToList();
        await ResolveDueProposalsAsync(proposals, ct);
        return Result<IReadOnlyList<ProposalDto>>.Success(await EnrichProposalDtosAsync(proposals, ct));
    }

    public Task<Result<PagedResult<ProposalDto>>> GetVisibleProposalsPagedAsync(ProjectViewer viewer, int page, int pageSize, CancellationToken ct = default)
        => SearchWithinAsync(VisibleProposals(viewer), null, null, null, null, page, pageSize, ct);

    public Task<Result<PagedResult<ProposalDto>>> SearchVisibleProposalsAsync(
        ProjectViewer viewer,
        string? searchTerm,
        ProposalType? type,
        ProposalStatus? status,
        string? projectId,
        int page,
        int pageSize,
        CancellationToken ct = default)
        => SearchWithinAsync(VisibleProposals(viewer), searchTerm, type, status, projectId, page, pageSize, ct);

    public async Task<Result<IReadOnlyList<ProposalDto>>> GetVisibleActiveProposalsAsync(ProjectViewer viewer, CancellationToken ct = default)
    {
        var proposals = VisibleProposals(viewer).Where(p => p.status == ProposalStatus.ACTIVE).ToList();
        await ResolveDueProposalsAsync(proposals, ct);
        var stillActive = proposals.Where(p => p.status == ProposalStatus.ACTIVE).ToList();
        return Result<IReadOnlyList<ProposalDto>>.Success(await EnrichProposalDtosAsync(stillActive, ct));
    }

    public async Task<Result<IReadOnlyList<ProposalDto>>> GetVisibleByProposerIdAsync(ProjectViewer viewer, string proposerId, CancellationToken ct = default)
    {
        var proposals = VisibleProposals(viewer).Where(p => p.creatorId == proposerId).ToList();
        await ResolveDueProposalsAsync(proposals, ct);
        return Result<IReadOnlyList<ProposalDto>>.Success(await EnrichProposalDtosAsync(proposals, ct));
    }

    /// <summary>Proposals this viewer may see: the project must be visible to them, and a DRAFT proposal only to its author and the people who run the project.</summary>
    private IQueryable<Proposal> VisibleProposals(ProjectViewer viewer)
    {
        if (viewer.IsAdmin)
            return _proposalRepository.Query();

        var visibleProjectIds = _projectRepository.Query()
            .Where(ProjectQueryFilters.VisibleTo(viewer))
            .Select(project => project.id);

        var viewerId = viewer.UserId;
        if (string.IsNullOrEmpty(viewerId))
        {
            return _proposalRepository.Query().Where(p =>
                p.status != ProposalStatus.DRAFT && visibleProjectIds.Contains(p.projectId));
        }

        // Mirrors HierarchyAuthorizationService.CanManageProjectAsync: the creator, or a FOUNDER/LEADER/CORE_CONTRIBUTOR member.
        var managedProjectIds = _projectRepository.Query()
            .Where(project => project.createdById == viewerId
                || project.ProjectMembers.Any(member => member.userId == viewerId
                    && (member.role == ProjectRole.FOUNDER
                        || member.role == ProjectRole.LEADER
                        || member.role == ProjectRole.CORE_CONTRIBUTOR)))
            .Select(project => project.id);

        return _proposalRepository.Query().Where(p =>
            visibleProjectIds.Contains(p.projectId)
            && (p.status != ProposalStatus.DRAFT
                || p.creatorId == viewerId
                || managedProjectIds.Contains(p.projectId)));
    }

    public async Task<Result<ProposalDto>> CreateProposalAsync(CreateProposalDto dto, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetByIdAsync(dto.ProjectId, ct);
        if (project is null)
            return Result<ProposalDto>.NotFound($"Project with id {dto.ProjectId} not found");

        if (string.IsNullOrWhiteSpace(dto.CreatorId))
            return Result<ProposalDto>.ValidationError("A proposal must name the member who raised it");

        var shapeError = ValidateProposalShape(dto.Options, dto.Quorum, dto.Threshold);
        if (shapeError is not null)
            return Result<ProposalDto>.ValidationError(shapeError);

        var windowError = ValidateVotingEnd(dto.VotingEnd);
        if (windowError is not null)
            return Result<ProposalDto>.ValidationError(windowError);

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

    /// <summary>A draft is fully editable; once voting is open only the closing time may move, and only earlier.</summary>
    public async Task<Result<ProposalDto>> UpdateProposalAsync(string id, UpdateProposalDto dto, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(id, ct);
        if (proposal is null)
            return Result<ProposalDto>.NotFound($"Proposal with id {id} not found");

        // This seam carries no caller identity, so it cannot end an open vote: closing is a
        // separate act with its own authority (IProposalVoteClosing).
        if (proposal.status == ProposalStatus.ACTIVE)
            return await CloseActiveProposalAsync(proposal, dto, ProposalCloseAuthority.Unattributed, ct);

        if (proposal.status != ProposalStatus.DRAFT)
            return Result<ProposalDto>.ValidationError("Only draft proposals can be edited");

        var quorum = dto.Quorum ?? proposal.quorum;
        var threshold = dto.Threshold ?? proposal.threshold;
        var options = dto.Options ?? proposal.options;
        var shapeError = ValidateProposalShape(options, quorum, threshold);
        if (shapeError is not null)
            return Result<ProposalDto>.ValidationError(shapeError);

        var windowError = ValidateVotingEnd(dto.VotingEnd);
        if (windowError is not null)
            return Result<ProposalDto>.ValidationError(windowError);

        if (dto.Title is not null) proposal.title = dto.Title;
        if (dto.Description is not null) proposal.description = dto.Description;
        proposal.options = options;
        proposal.quorum = quorum;
        proposal.threshold = threshold;
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

    /// <summary>The ballot is the caller's own: the voter is the id the controller resolved from the authenticated actor, and the weight is always one.</summary>
    public async Task<Result<VoteDto>> CastVoteAsync(string proposalId, CastVoteDto dto, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId, ct);
        if (proposal is null)
            return Result<VoteDto>.NotFound($"Proposal with id {proposalId} not found");

        await ResolveDueProposalsAsync(new[] { proposal }, ct);

        if (proposal.status != ProposalStatus.ACTIVE)
            return Result<VoteDto>.ValidationError("Voting is not open for this proposal");

        if (proposal.votingEnd.HasValue && DateTime.UtcNow > proposal.votingEnd.Value)
            return Result<VoteDto>.ValidationError("Voting has closed for this proposal");

        if (string.IsNullOrWhiteSpace(dto.VoterId))
            return Result<VoteDto>.Unauthorized("A vote must be cast by a signed-in member");

        if (!TryReadChoices(proposal.options, out var declaredChoices))
            return Result<VoteDto>.ValidationError("This proposal's options cannot be read, so no vote can be recorded against it");

        if (!declaredChoices.Contains(dto.Choice))
            return Result<VoteDto>.ValidationError("That option is not on this proposal");

        // The membership gate lives here, not only in the controllers, so both routes into a ballot enforce it.
        if (_membershipCredentialRepository is null)
            return Result<VoteDto>.Forbidden(
                "Voting is unavailable here until this project's member register is connected");

        var voterId = dto.VoterId;
        var projectId = proposal.projectId;
        var isEligible = await _membershipCredentialRepository.ExistsAsync(
            credential => credential.projectId == projectId &&
                          credential.userId == voterId &&
                          credential.status == MembershipCredentialStatus.ACTIVE,
            ct);
        if (!isEligible)
            return Result<VoteDto>.Forbidden("Only confirmed members of this project can vote on it");

        var existingVote = await _voteRepository.FindOneAsync(
            v => v.proposalId == proposalId && v.voterId == voterId, ct);
        if (existingVote is not null)
            return Result<VoteDto>.ValidationError("You have already voted on this proposal");

        var vote = new Vote
        {
            id = Guid.NewGuid().ToString(),
            proposalId = proposalId,
            voterId = voterId,
            choice = dto.Choice,
            weight = OneMemberOneVote,
            reason = dto.Reason,
            createdAt = DateTime.UtcNow
        };

        await _voteRepository.AddAsync(vote, ct);
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException failure) when (failure.InnerException is DbException { SqlState: UniqueViolationSqlState })
        {
            // A concurrent request recorded this member's ballot between the check above and this insert.
            _unitOfWork.ClearTrackedChanges();
            return Result<VoteDto>.ValidationError("You have already voted on this proposal");
        }

        var voteDto = _mapper.Map<VoteDto>(vote);
        var voter = await _userRepository.GetByIdAsync(voterId, ct);
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

        await ResolveDueProposalsAsync(new[] { proposal }, ct);

        var tally = await TallyAsync(proposal, ct);

        var optionSummaries = tally.VotesByChoice.ToDictionary(
            entry => entry.Key,
            entry => new VoteOptionSummary
            {
                Choice = entry.Key,
                VoteCount = entry.Value,
                VotingPower = entry.Value,
                Percentage = tally.VotesCast > 0
                    ? Math.Round(entry.Value * 100m / tally.VotesCast, 2)
                    : 0m
            });

        var summary = new ProposalVoteSummaryDto
        {
            ProposalId = proposalId,
            TotalVotes = tally.VotesCast,
            TotalVotingPower = tally.VotesCast,
            OptionSummaries = optionSummaries,
            QuorumReached = tally.QuorumReached,
            WinningOption = tally.PassingChoice,
            EligibleMembers = tally.EligibleMembers,
            VotesNeededForQuorum = tally.VotesNeededForQuorum
        };

        return Result<ProposalVoteSummaryDto>.Success(summary);
    }

    /// <summary>Carrying out a decision the members already made; the tally that produced it is written onto the execution row.</summary>
    public async Task<Result<ProposalDto>> ExecuteProposalAsync(string id, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(id, ct);
        if (proposal is null)
            return Result<ProposalDto>.NotFound($"Proposal with id {id} not found");

        await ResolveDueProposalsAsync(new[] { proposal }, ct);

        if (proposal.status == ProposalStatus.EXECUTED)
            return Result<ProposalDto>.Success(await EnrichProposalDtoAsync(proposal, ct));

        if (proposal.status != ProposalStatus.PASSED)
            return Result<ProposalDto>.ValidationError(
                "Only a decision the members have passed can be carried out");

        var readyAt = EarliestExecutionTime(proposal);
        if (readyAt > DateTime.UtcNow)
            return Result<ProposalDto>.ValidationError(
                $"This decision can be carried out from {readyAt:yyyy-MM-dd HH:mm} UTC");

        var tally = await TallyAsync(proposal, ct);

        proposal.status = ProposalStatus.EXECUTED;
        proposal.updatedAt = DateTime.UtcNow;

        var execution = new ProposalExecution
        {
            id = Guid.NewGuid().ToString(),
            proposalId = id,
            executedAt = DateTime.UtcNow,
            result = JsonSerializer.Serialize(new
            {
                outcome = nameof(ProposalStatus.PASSED),
                winningOption = tally.PassingChoice,
                votesCast = tally.VotesCast,
                eligibleMembers = tally.EligibleMembers,
                votesNeededForQuorum = tally.VotesNeededForQuorum,
                quorumPercent = proposal.quorum,
                thresholdPercent = proposal.threshold,
                votesByOption = tally.VotesByChoice
            })
        };

        await _proposalRepository.UpdateAsync(proposal, ct);
        await _executionRepository.AddAsync(execution, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichProposalDtoAsync(proposal, ct);
        return Result<ProposalDto>.Success(resultDto);
    }

    /// <summary>Withdrawing a proposal, which is not the same act as closing a vote: once a ballot exists the vote must be closed and resolved instead.</summary>
    public async Task<Result<ProposalDto>> CancelProposalAsync(string id, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(id, ct);
        if (proposal is null)
            return Result<ProposalDto>.NotFound($"Proposal with id {id} not found");

        if (proposal.status is not (ProposalStatus.DRAFT or ProposalStatus.ACTIVE))
            return Result<ProposalDto>.ValidationError("Only a draft or an open proposal can be withdrawn");

        var votesCast = await _voteRepository.CountAsync(v => v.proposalId == id, ct);
        if (votesCast > 0)
            return Result<ProposalDto>.ValidationError(
                "Members have already voted, so this proposal cannot be withdrawn — close the vote to record its outcome");

        proposal.status = ProposalStatus.CANCELLED;
        proposal.updatedAt = DateTime.UtcNow;

        await _proposalRepository.UpdateAsync(proposal, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichProposalDtoAsync(proposal, ct);
        return Result<ProposalDto>.Success(resultDto);
    }

    public async Task<Result<ProposalDto>> PublishProposalAsync(string id, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(id, ct);
        if (proposal is null)
            return Result<ProposalDto>.NotFound($"Proposal with id {id} not found");

        if (proposal.status != ProposalStatus.DRAFT)
            return Result<ProposalDto>.Failure("Only DRAFT proposals can be published");

        var shapeError = ValidateProposalShape(proposal.options, proposal.quorum, proposal.threshold);
        if (shapeError is not null)
            return Result<ProposalDto>.ValidationError(shapeError);

        // A stated closing time the members could not see in time is refused, never silently moved.
        var windowError = ValidateVotingEnd(proposal.votingEnd);
        if (windowError is not null)
            return Result<ProposalDto>.ValidationError(windowError);

        proposal.status = ProposalStatus.ACTIVE;
        proposal.votingStart = DateTime.UtcNow;
        proposal.votingEnd ??= DateTime.UtcNow.AddDays(7);
        proposal.updatedAt = DateTime.UtcNow;

        await _proposalRepository.UpdateAsync(proposal, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichProposalDtoAsync(proposal, ct);
        return Result<ProposalDto>.Success(resultDto);
    }

    // ===== VOTE OF RECORD =====

    /// <summary>What the members decided, computed from the ballots on file.</summary>
    private sealed record ProposalTally(
        int VotesCast,
        int EligibleMembers,
        int VotesNeededForQuorum,
        bool QuorumReached,
        int? PassingChoice,
        IReadOnlyDictionary<int, int> VotesByChoice);

    private async Task<ProposalTally> TallyAsync(Proposal proposal, CancellationToken ct)
    {
        var votes = await _voteRepository.FindAsync(v => v.proposalId == proposal.id, ct);
        var votesByChoice = votes
            .GroupBy(v => v.choice)
            .ToDictionary(g => g.Key, g => g.Count());
        var votesCast = votes.Count;

        var projectId = proposal.projectId;
        var eligibleMembers = _membershipCredentialRepository is null
            ? 0
            : await _membershipCredentialRepository.CountAsync(
                credential => credential.projectId == projectId &&
                              credential.status == MembershipCredentialStatus.ACTIVE,
                ct);

        var votesNeededForQuorum = VotesNeededForQuorum(eligibleMembers, proposal.quorum);
        var quorumReached = eligibleMembers > 0 && votesCast >= votesNeededForQuorum;

        int? passingChoice = null;
        if (votesCast > 0)
        {
            var leader = votesByChoice.OrderByDescending(entry => entry.Value).ThenBy(entry => entry.Key).First();

            // The threshold is a share of the votes actually cast, so it is checked in whole votes, never in percentages of a percentage.
            var isTied = votesByChoice.Count(entry => entry.Value == leader.Value) > 1;
            if (!isTied && leader.Value * 100 >= proposal.threshold * votesCast)
                passingChoice = leader.Key;
        }

        return new ProposalTally(
            votesCast,
            eligibleMembers,
            votesNeededForQuorum,
            quorumReached,
            quorumReached ? passingChoice : null,
            votesByChoice);
    }

    /// <summary>Quorum is a share of the members who may vote, rounded up, and never fewer ballots than one member acting alone can produce.</summary>
    /// <remarks>
    /// The declared percentage is not a floor. One percent of forty members rounds up to a single
    /// ballot, and the single ballot an attacker can produce is their own — so
    /// <see cref="MinimumBallotsForQuorum"/> is applied ON TOP of the percentage. Only an
    /// electorate too small to reach it falls back below it, and there the whole register has
    /// voted, which is not the same act as one member manufacturing a decision.
    /// </remarks>
    private static int VotesNeededForQuorum(int eligibleMembers, int quorumPercent)
    {
        if (eligibleMembers <= 0)
            return MinimumBallotsForQuorum;

        var clamped = Math.Clamp(quorumPercent, 0, 100);
        var byDeclaredPercentage = (int)Math.Ceiling(eligibleMembers * clamped / 100.0);
        var needed = Math.Max(byDeclaredPercentage, MinimumBallotsForQuorum);
        return Math.Clamp(needed, 1, eligibleMembers);
    }

    private static DateTime EarliestExecutionTime(Proposal proposal)
    {
        var closedAt = proposal.votingEnd ?? proposal.updatedAt;
        // `executionDelay` is expressed in hours, matching the governance overview surface that displays it.
        return proposal.executionDelay.HasValue
            ? closedAt.AddHours(proposal.executionDelay.Value)
            : closedAt;
    }

    /// <summary>Moves every open proposal whose window has run out to PASSED, REJECTED or EXPIRED, and saves once.</summary>
    private async Task ResolveDueProposalsAsync(IEnumerable<Proposal> proposals, CancellationToken ct)
    {
        var due = proposals
            .Where(p => p.status == ProposalStatus.ACTIVE &&
                        p.votingEnd.HasValue &&
                        p.votingEnd.Value <= DateTime.UtcNow)
            .ToList();

        if (due.Count == 0)
            return;

        foreach (var proposal in due)
        {
            var tally = await TallyAsync(proposal, ct);
            proposal.status = ResolvedStatus(tally);
            proposal.updatedAt = DateTime.UtcNow;
            await _proposalRepository.UpdateAsync(proposal, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>Too few members took part: EXPIRED. Enough took part but no option cleared the bar: REJECTED. Otherwise PASSED.</summary>
    private static ProposalStatus ResolvedStatus(ProposalTally tally)
    {
        if (!tally.QuorumReached)
            return ProposalStatus.EXPIRED;

        return tally.PassingChoice.HasValue
            ? ProposalStatus.PASSED
            : ProposalStatus.REJECTED;
    }

    /// <summary>Shuts an open voting window now, on a named authority. The outcome is left to the ordinary due-resolution path, so no single request both ends a vote and declares its result.</summary>
    public async Task<Result<ProposalDto>> CloseVoteAsync(
        string proposalId,
        ProposalCloseAuthority authority,
        CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId, ct);
        if (proposal is null)
            return Result<ProposalDto>.NotFound($"Proposal with id {proposalId} not found");

        if (proposal.status != ProposalStatus.ACTIVE)
            return Result<ProposalDto>.ValidationError("Only an open vote can be closed");

        return await CloseActiveProposalAsync(
            proposal,
            new UpdateProposalDto { VotingEnd = DateTime.UtcNow },
            authority,
            ct);
    }

    /// <summary>Closing an open vote early: the window is pulled in to now, and nothing else about the proposal may move. This is the ONLY place a voting window moves once ballots can exist, so the authority question is answered here rather than at each door that reaches it.</summary>
    private async Task<Result<ProposalDto>> CloseActiveProposalAsync(
        Proposal proposal,
        UpdateProposalDto dto,
        ProposalCloseAuthority authority,
        CancellationToken ct)
    {
        var rewritesRecord =
            dto.Title is not null ||
            dto.Description is not null ||
            dto.Options is not null ||
            dto.Quorum.HasValue ||
            dto.Threshold.HasValue ||
            dto.VotingStart.HasValue ||
            dto.ExecutionDelay.HasValue;
        if (rewritesRecord)
            return Result<ProposalDto>.ValidationError(
                "Voting is open, so this proposal can no longer be edited — only its closing time can be brought forward");

        if (!dto.VotingEnd.HasValue)
            return Result<ProposalDto>.ValidationError(
                "Voting is open, so this proposal can no longer be edited");

        var currentEnd = proposal.votingEnd ?? DateTime.MaxValue;

        // The window has already run out: closing it is simply recording the outcome that is already due.
        if (currentEnd <= DateTime.UtcNow)
        {
            await ResolveDueProposalsAsync(new[] { proposal }, ct);
            return Result<ProposalDto>.Success(await EnrichProposalDtoAsync(proposal, ct));
        }

        // Ending a vote the members can still take part in is an act of authority, and the
        // proposal's own author never holds it — OI-46.
        if (!authority.MayEndEarly(proposal.creatorId))
            return Result<ProposalDto>.Forbidden(
                "Only someone who runs this project, and who did not raise this proposal, can end the vote before it closes on its own");

        if (dto.VotingEnd.Value > currentEnd)
            return Result<ProposalDto>.ValidationError("A voting window can be shortened but never extended");

        proposal.votingEnd = dto.VotingEnd.Value;
        proposal.updatedAt = DateTime.UtcNow;
        await _proposalRepository.UpdateAsync(proposal, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Deliberately NOT resolved here: closing shuts the window, and the outcome is read off
        // the ballots on file by the ordinary due-resolution path. One request cannot both pull
        // the closing time in and stamp the decision it produced.
        var resultDto = await EnrichProposalDtoAsync(proposal, ct);
        return Result<ProposalDto>.Success(resultDto);
    }

    /// <summary>A stated closing time must leave the vote open for at least <see cref="MinimumVotingWindow"/> from now.</summary>
    private static string? ValidateVotingEnd(DateTime? votingEnd)
    {
        if (!votingEnd.HasValue)
            return null;

        return votingEnd.Value < DateTime.UtcNow.Add(MinimumVotingWindow)
            ? $"Voting must stay open for at least {MinimumVotingWindow.TotalHours:0} hours so every member can see it - choose a later closing time"
            : null;
    }

    /// <summary>A proposal must offer at least two real options and state quorum and threshold as percentages.</summary>
    private static string? ValidateProposalShape(string options, int quorumPercent, int thresholdPercent)
    {
        if (!TryReadChoices(options, out var choices) || choices.Count < 2)
            return "A proposal must offer at least two options";

        if (quorumPercent is < 1 or > 100)
            return "Quorum is the share of members who must vote, between 1 and 100 percent";

        if (thresholdPercent is < 1 or > 100)
            return "The threshold is the share of votes needed to pass, between 1 and 100 percent";

        return null;
    }

    /// <summary>Reads the choice numbers a proposal actually offers, so a ballot cannot name an option that does not exist.</summary>
    private static bool TryReadChoices(string options, out HashSet<int> choices)
    {
        choices = new HashSet<int>();
        if (string.IsNullOrWhiteSpace(options))
            return false;

        try
        {
            using var document = JsonDocument.Parse(options);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return false;

            var index = 0;
            foreach (var element in document.RootElement.EnumerateArray())
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        if (string.IsNullOrWhiteSpace(element.GetString()))
                            return false;
                        choices.Add(index);
                        break;
                    case JsonValueKind.Object:
                        if (element.TryGetProperty("choice", out var choice) &&
                            choice.ValueKind == JsonValueKind.Number &&
                            choice.TryGetInt32(out var choiceValue))
                            choices.Add(choiceValue);
                        else
                            choices.Add(index);
                        break;
                    default:
                        return false;
                }

                index++;
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return choices.Count > 0;
    }

    private async Task<ProposalDto> EnrichProposalDtoAsync(Proposal proposal, CancellationToken ct)
    {
        var tally = await TallyAsync(proposal, ct);

        var dto = _mapper.Map<ProposalDto>(proposal) with
        {
            VotesCount = tally.VotesCast,
            TotalVotingPower = tally.VotesCast,
            EligibleMembers = tally.EligibleMembers,
            VotesNeededForQuorum = tally.VotesNeededForQuorum,
            QuorumReached = tally.QuorumReached,
            VotesByChoice = tally.VotesByChoice.ToDictionary(entry => entry.Key, entry => entry.Value)
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

    public async Task<Result<IReadOnlyList<ProposalCommentDto>>> GetProposalCommentsAsync(string proposalId, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId, ct);
        if (proposal is null)
            return Result<IReadOnlyList<ProposalCommentDto>>.NotFound($"Proposal with id {proposalId} not found");

        var allComments = await _proposalCommentRepository.FindAsync(
            c => c.proposalId == proposalId, ct);
        var commentsList = allComments.OrderBy(c => c.createdAt).ToList();

        // Build enriched DTOs with user info
        var userIds = commentsList.Select(c => c.userId).Distinct().ToList();
        var users = new Dictionary<string, User>();
        foreach (var uid in userIds)
        {
            var user = await _userRepository.GetByIdAsync(uid, ct);
            if (user is not null) users[uid] = user;
        }

        var topLevel = commentsList.Where(c => c.parentId == null).ToList();
        var repliesByParent = commentsList.Where(c => c.parentId != null)
            .GroupBy(c => c.parentId!)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = topLevel.Select(c => MapCommentDto(c, users, repliesByParent)).ToList();
        return Result<IReadOnlyList<ProposalCommentDto>>.Success(result);
    }

    /// <summary>The author may post only where they may read: a hidden proposal answers NotFound, exactly as its GET does. See Services/AGENTS.md §Governance visibility.</summary>
    public async Task<Result<ProposalCommentDto>> CreateProposalCommentAsync(CreateProposalCommentDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.UserId))
            return Result<ProposalCommentDto>.Unauthorized("A comment must be written by a signed-in member");

        var author = await _userRepository.GetByIdAsync(dto.UserId, ct);
        var viewer = new ProjectViewer(dto.UserId, author?.role == UserRole.ADMIN);
        var proposalId = dto.ProposalId;
        if (!VisibleProposals(viewer).Any(p => p.id == proposalId))
            return Result<ProposalCommentDto>.NotFound($"Proposal with id {proposalId} not found");

        var contentError = ValidateCommentContent(dto.Content);
        if (contentError is not null)
            return Result<ProposalCommentDto>.ValidationError(contentError);

        if (!string.IsNullOrEmpty(dto.ParentId))
        {
            var parent = await _proposalCommentRepository.GetByIdAsync(dto.ParentId, ct);
            if (parent is null || parent.proposalId != proposalId)
                return Result<ProposalCommentDto>.ValidationError("That reply target is not a comment on this proposal");
        }

        var comment = new ProposalComment
        {
            id = Guid.NewGuid().ToString(),
            proposalId = proposalId,
            userId = dto.UserId,
            content = dto.Content,
            parentId = string.IsNullOrEmpty(dto.ParentId) ? null : dto.ParentId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _proposalCommentRepository.AddAsync(comment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var commentDto = _mapper.Map<ProposalCommentDto>(comment);
        commentDto = commentDto with
        {
            User = author is not null ? _mapper.Map<ProposalCommentUserDto>(author) : null
        };

        return Result<ProposalCommentDto>.Success(commentDto);
    }

    /// <summary>The longest comment a proposal discussion stores; the column itself is unbounded text.</summary>
    public const int MaxCommentLength = 10_000;

    private static string? ValidateCommentContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "Write something before posting a comment";
        return content.Length > MaxCommentLength
            ? $"A comment can be at most {MaxCommentLength:N0} characters"
            : null;
    }

    public async Task<Result<ProposalCommentDto>> UpdateProposalCommentAsync(
        string proposalId,
        string commentId,
        string userId,
        UpdateProposalCommentDto dto,
        CancellationToken ct = default)
    {
        var comment = await _proposalCommentRepository.GetByIdAsync(commentId, ct);
        if (comment is null || comment.proposalId != proposalId)
            return Result<ProposalCommentDto>.NotFound("Comment not found");
        if (comment.userId != userId)
            return Result<ProposalCommentDto>.Forbidden("Only the author can update this comment");

        var contentError = ValidateCommentContent(dto.Content);
        if (contentError is not null)
            return Result<ProposalCommentDto>.ValidationError(contentError);

        comment.content = dto.Content;
        comment.updatedAt = DateTime.UtcNow;
        await _proposalCommentRepository.UpdateAsync(comment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var user = await _userRepository.GetByIdAsync(userId, ct);
        var commentDto = _mapper.Map<ProposalCommentDto>(comment);
        commentDto = commentDto with
        {
            User = user is not null ? _mapper.Map<ProposalCommentUserDto>(user) : null
        };
        return Result<ProposalCommentDto>.Success(commentDto);
    }

    public async Task<Result<bool>> DeleteProposalCommentAsync(
        string proposalId,
        string commentId,
        string userId,
        CancellationToken ct = default)
    {
        var comment = await _proposalCommentRepository.GetByIdAsync(commentId, ct);
        if (comment is null || comment.proposalId != proposalId)
            return Result<bool>.NotFound("Comment not found");
        if (comment.userId != userId)
            return Result<bool>.Forbidden("Only the author can delete this comment");

        await _proposalCommentRepository.DeleteAsync(comment, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    private ProposalCommentDto MapCommentDto(
        ProposalComment comment,
        Dictionary<string, User> users,
        Dictionary<string, List<ProposalComment>> repliesByParent)
    {
        var dto = _mapper.Map<ProposalCommentDto>(comment);
        dto = dto with
        {
            User = users.TryGetValue(comment.userId, out var user)
                ? _mapper.Map<ProposalCommentUserDto>(user)
                : null,
            Replies = repliesByParent.TryGetValue(comment.id, out var replies)
                ? replies.Select(r => MapCommentDto(r, users, repliesByParent)).ToList()
                : null
        };
        return dto;
    }
}
