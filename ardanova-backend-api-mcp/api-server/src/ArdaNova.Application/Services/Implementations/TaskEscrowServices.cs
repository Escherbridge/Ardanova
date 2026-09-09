namespace ArdaNova.Application.Services.Implementations;

using System.Linq;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class TaskEscrowService : ITaskEscrowService, ITaskEscrowFunding
{
    private static readonly HashSet<string> AllowedDisputeReasons =
    [
        "SCOPE_DISPUTE",
        "QUALITY_ISSUE",
        "NON_DELIVERY",
        "OTHER"
    ];

    private static readonly HashSet<string> AllowedResolutionOutcomes = ["REFUND", "REJECT"];

    private const string AwardGatewayUnwiredError =
        "Releasing this escrow is unavailable in this deployment: the shared-node award gateway "
        + "is not wired. Nothing has been released.";

    private const string HoldGatewayUnwiredError =
        "Refunding this escrow is unavailable in this deployment: the shared-node hold gateway "
        + "is not wired. Nothing has been refunded.";

    private const string NoLinkedHoldError =
        "This escrow is not linked to a stake reservation on the shared node, so there is "
        + "nothing to move there yet.";

    private const string CreateUnwiredError =
        "Setting funds aside for a task is unavailable in this deployment: the joins that verify "
        + "the assignment, its award and the project it belongs to are not wired. Nothing has "
        + "been set aside.";

    private const string FundGatewayUnwiredError =
        "Confirming that funds are set aside is unavailable in this deployment: the shared-node "
        + "hold gateway is not wired. Nothing has been set aside.";

    /// <summary>
    /// The one seam of this lifecycle that cannot be satisfied on a fresh deployment: the escrow
    /// row carries a NOT NULL foreign key to <c>ProjectShare</c> and nothing in either repository
    /// has ever written a <c>ProjectShare</c> row. It refuses by name rather than inventing a
    /// share of record. Unblocks when <c>TaskEscrow.shareId</c> becomes nullable in
    /// <c>ardanova-client/prisma/schema.prisma</c> and its migration.
    /// </summary>
    private const string NoShareOfRecordError =
        "This project has no share of record yet, so funds cannot be set aside against it. "
        + "Nothing has been set aside.";

    private readonly IRepository<TaskEscrow> _repository;
    private readonly IRepository<ProjectTask> _taskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IRepository<TaskCommerceAgreement>? _agreementRepository;
    private readonly IRepository<TokenAllocation>? _allocationRepository;
    private readonly IAzoaHoldGateway? _holds;
    private readonly IProjectTokenProvisioning? _awards;
    private readonly IRepository<Project>? _projectRepository;
    private readonly IRepository<ProjectShare>? _shareRepository;

    /// <param name="agreementRepository">
    /// Read-only join to the commerce agreement a fund escrow row belongs to (by
    /// <c>taskId</c>): optional so a deployment written before commerce agreements existed
    /// still constructs, but release and refund refuse by name without it rather than moving
    /// units against a reservation this service cannot verify.
    /// </param>
    /// <param name="allocationRepository">
    /// Read-only join to the <see cref="TokenAllocation"/> read-model of the actual hold the
    /// agreement opened. Release and refund address the SAME hold the worker's award was
    /// reserved under; this service never opens a second one.
    /// </param>
    /// <param name="holds">
    /// The node's hold surface. Optional: zero implementations is a supported state, and every
    /// caller that needs it refuses by name rather than treating an unheld escrow as funded.
    /// </param>
    /// <param name="awards">
    /// The award-completion port. Release delegates to the SAME
    /// <c>ApproveWorkCompletionAsync</c> the worker's own award approval uses, so a funder's
    /// "Authorize Release" and a project owner's "approve completed work" are one operation
    /// with one authorization rule, never two paths that could disagree.
    /// </param>
    /// <param name="projectRepository">
    /// Read-only join to the project a task belongs to. <see cref="CreateAsync"/> takes the
    /// funder of record from <c>Project.createdById</c> — the SAME identity
    /// <c>ApproveWorkCompletionAsync</c> demands at release — so an escrow can never be opened
    /// that nobody is permitted to release.
    /// </param>
    /// <param name="shareRepository">
    /// Read-only join to the project's share of record, which the escrow row's NOT NULL
    /// foreign key requires. See <see cref="NoShareOfRecordError"/>.
    /// </param>
    public TaskEscrowService(
        IRepository<TaskEscrow> repository,
        IRepository<ProjectTask> taskRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRepository<TaskCommerceAgreement>? agreementRepository = null,
        IRepository<TokenAllocation>? allocationRepository = null,
        IEnumerable<IAzoaHoldGateway>? holds = null,
        IProjectTokenProvisioning? awards = null,
        IRepository<Project>? projectRepository = null,
        IRepository<ProjectShare>? shareRepository = null)
    {
        _repository = repository;
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _agreementRepository = agreementRepository;
        _allocationRepository = allocationRepository;
        _holds = holds?.FirstOrDefault();
        _awards = awards;
        _projectRepository = projectRepository;
        _shareRepository = shareRepository;
    }

    public async Task<Result<TaskEscrowDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var escrow = await _repository.GetByIdAsync(id, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow with id {id} not found");
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    public async Task<Result<TaskEscrowDto>> GetByTaskIdAsync(string taskId, CancellationToken ct = default)
    {
        var escrow = await _repository.FindOneAsync(e => e.taskId == taskId, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow for task {taskId} not found");
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    public async Task<Result<IReadOnlyList<TaskEscrowDto>>> GetByFunderIdAsync(string funderId, CancellationToken ct = default)
    {
        var escrows = await _repository.FindAsync(e => e.funderId == funderId, ct);
        return Result<IReadOnlyList<TaskEscrowDto>>.Success(_mapper.Map<IReadOnlyList<TaskEscrowDto>>(escrows));
    }

    public async Task<Result<IReadOnlyList<TaskEscrowDto>>> GetDisputedAsync(CancellationToken ct = default)
    {
        var escrows = await _repository.FindAsync(e => e.status == EscrowStatus.DISPUTED, ct);
        return Result<IReadOnlyList<TaskEscrowDto>>.Success(_mapper.Map<IReadOnlyList<TaskEscrowDto>>(escrows));
    }

    /// <summary>
    /// Opens the funder-facing record of the stake an accepted assignment already set aside on
    /// the shared node. Verifies server-side every one of the four things the previous 501
    /// refusal named: the task exists, the funding instrument is a real accepted agreement, the
    /// amount is the agreement's own award figure, and the node is holding units against it.
    /// </summary>
    /// <remarks>
    /// <paramref name="dto"/>'s <c>ShareId</c> and <c>Amount</c> are IGNORED. Both are derived
    /// from the project and the agreement, because a caller-supplied amount on an owned resource
    /// is the shape this codebase refuses everywhere else. The row opens at
    /// <see cref="EscrowStatus.NONE"/>: it becomes <see cref="EscrowStatus.FUNDED"/> only through
    /// <see cref="FundAsync"/>, which asks the node whether the units are actually held.
    /// </remarks>
    public async Task<Result<TaskEscrowDto>> CreateAsync(CreateTaskEscrowDto dto, CancellationToken ct = default)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.TaskId) || string.IsNullOrWhiteSpace(dto.FunderId))
            return Result<TaskEscrowDto>.ValidationError("A task and the funder's own identity are required.");

        var exists = await _repository.ExistsAsync(e => e.taskId == dto.TaskId, ct);
        if (exists)
            return Result<TaskEscrowDto>.ValidationError($"Escrow already exists for task {dto.TaskId}");

        if (_projectRepository is null
            || _agreementRepository is null
            || _allocationRepository is null
            || _shareRepository is null)
        {
            return Result<TaskEscrowDto>.Failure(CreateUnwiredError);
        }

        var task = await _taskRepository.GetByIdAsync(dto.TaskId, ct);
        if (task is null)
            return Result<TaskEscrowDto>.NotFound($"Task with id {dto.TaskId} not found");

        var project = await _projectRepository.GetByIdAsync(task.projectId, ct);
        if (project is null)
            return Result<TaskEscrowDto>.NotFound($"Project with id {task.projectId} not found for task {task.id}");

        // Setting a project's stake aside spends the project's own allowance, so it is the
        // project owner's decision - and the owner is the identity release will later demand,
        // so this check is what stops an escrow being opened that nobody may release.
        if (!string.Equals(project.createdById, dto.FunderId, StringComparison.Ordinal))
        {
            return Result<TaskEscrowDto>.Forbidden(
                "Only the person who created this project can set funds aside for its work.");
        }

        var agreement = await _agreementRepository.FindOneAsync(a => a.taskId == task.id, ct);
        if (agreement is null)
        {
            return Result<TaskEscrowDto>.Conflict(
                "This task has no accepted assignment, so there is no agreed figure to set aside.");
        }

        // Every leg, for the same reason funding and refunding read every leg: an accepted bid
        // that declared a guild share is held in more than one, and an escrow opened over a
        // single arbitrary leg would report the whole award while only one share was committed.
        var openingLegs = await ResolveAgreementLegsAsync(agreement.id, ct);
        if (DescribeUnusableLegs(openingLegs) is { } uncommitted)
            return Result<TaskEscrowDto>.Conflict(uncommitted);

        var share = await _shareRepository.FindOneAsync(s => s.projectId == project.id, ct);
        if (share is null)
            return Result<TaskEscrowDto>.Conflict(NoShareOfRecordError);

        var escrow = new TaskEscrow
        {
            id = Guid.NewGuid().ToString(),
            taskId = task.id,
            // The owner of record, never the request field: they were proven equal above, and
            // reading the project keeps the stored funder correct if that ever stops being true.
            funderId = project.createdById,
            shareId = share.id,
            // The agreement is the award of record, so a board and an escrow can never quote
            // different numbers.
            amount = agreement.awardAmount,
            status = EscrowStatus.NONE,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(escrow, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    /// <summary>
    /// The only entry to <see cref="EscrowStatus.FUNDED"/>: the funder confirms the stake is
    /// actually set aside, and the escrow advances only because the shared node says the units
    /// are held. It never opens a second hold - it addresses the SAME reservation the worker's
    /// award was accepted under.
    /// </summary>
    public async Task<Result<TaskEscrowDto>> FundAsync(string id, string fundedByUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fundedByUserId))
            return Result<TaskEscrowDto>.ValidationError("The funding actor is required.");

        if (_holds is null)
            return Result<TaskEscrowDto>.Failure(FundGatewayUnwiredError);

        var escrow = await _repository.GetByIdAsync(id, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow with id {id} not found");

        // The funder of record is the only person who may confirm their own commitment.
        if (!string.Equals(escrow.funderId, fundedByUserId, StringComparison.Ordinal))
        {
            return Result<TaskEscrowDto>.Forbidden(
                "Only this escrow's funder can confirm that its funds are set aside.");
        }

        // Idempotent: a redelivered confirmation reports the escrow it already funded rather
        // than refusing, so a retried request never looks like a second commitment.
        if (escrow.status == EscrowStatus.FUNDED)
            return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));

        if (escrow.status != EscrowStatus.NONE)
            return Result<TaskEscrowDto>.ValidationError($"Cannot fund escrow in status {escrow.status}");

        var legs = await ResolveAllocationsAsync(escrow, ct);
        if (DescribeUnusableLegs(legs) is { } unusable)
            return Result<TaskEscrowDto>.Conflict(unusable);

        // FUNDED is a statement about the node, not about this database. Only units the node
        // reports as held back it; a claimed-but-undrawn row, or one already spent or given
        // back, must not open a lifecycle whose every later step promises the money is there.
        // EVERY leg, because an award held in shares is only funded when all of them are.
        foreach (var leg in legs)
        {
            var hold = await _holds.GetAsync(new AzoaHoldReference(leg.holdChain!, leg.holdId!), ct);
            if (hold.IsFailure)
                return PropagateFailure(hold);

            if (!string.Equals(hold.Value!.State, AzoaHoldStates.Held, StringComparison.OrdinalIgnoreCase))
                return Result<TaskEscrowDto>.Conflict(DescribeUnfundableHold(hold.Value!.State));
        }

        var task = await GetTaskAsync(escrow, ct);
        var now = DateTime.UtcNow;
        escrow.status = EscrowStatus.FUNDED;
        escrow.fundedAt = now;
        await _repository.UpdateAsync(escrow, ct);

        if (task is not null)
        {
            task.escrowStatus = EscrowStatus.FUNDED;
            task.updatedAt = now;
            await _taskRepository.UpdateAsync(task, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    /// <summary>Says why a reservation the node is not holding cannot open the escrow lifecycle.</summary>
    private static string DescribeUnfundableHold(string? state) => state switch
    {
        AzoaHoldStates.Pending =>
            "The shared node has not committed these units yet, so there is nothing set aside to confirm.",
        AzoaHoldStates.Released or AzoaHoldStates.ReleasedIntoHold =>
            "These units have already been handed over, so they cannot be set aside again.",
        AzoaHoldStates.Refunded =>
            "These units were given back, so there is nothing set aside for this work.",
        _ =>
            "The shared node does not report these units as set aside, so this escrow cannot be funded.",
    };

    /// <summary>
    /// The funder authorizes release: the SAME node hold their stake was reserved under is
    /// continued into the worker's own window through <see cref="IProjectTokenProvisioning"/>,
    /// the read-model the frontend calls "release evidence" is that hold's own confirmed state,
    /// never a hash the funder had to find and paste in.
    /// </summary>
    public async Task<Result<TaskEscrowDto>> ReleaseAsync(string id, ReleaseEscrowDto dto, CancellationToken ct = default)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.ReleasedByUserId))
            return Result<TaskEscrowDto>.ValidationError("The release actor is required.");

        if (_awards is null)
            return Result<TaskEscrowDto>.Failure(AwardGatewayUnwiredError);

        var escrow = await _repository.GetByIdAsync(id, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow with id {id} not found");

        if (escrow.status != EscrowStatus.FUNDED)
            return Result<TaskEscrowDto>.ValidationError($"Cannot release escrow in status {escrow.status}");

        var legs = await ResolveAllocationsAsync(escrow, ct);
        if (legs.Count == 0 || string.IsNullOrWhiteSpace(legs[0].id))
            return Result<TaskEscrowDto>.Conflict(NoLinkedHoldError);

        // One call settles the whole agreement - every beneficiary's leg, not just this one -
        // so the escrow only reaches RELEASED when nothing is left held for anybody.
        var approved = await _awards.ApproveWorkCompletionAsync(legs[0].id, dto.ReleasedByUserId, ct);
        if (approved.IsFailure)
            return PropagateFailure(approved);

        escrow.status = EscrowStatus.RELEASED;
        escrow.releasedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(escrow, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    public async Task<Result<TaskEscrowDto>> DisputeAsync(string id, DisputeEscrowDto dto, CancellationToken ct = default)
    {
        var reason = dto.Reason.Trim().ToUpperInvariant();
        var description = dto.Description.Trim();
        if (!AllowedDisputeReasons.Contains(reason))
            return Result<TaskEscrowDto>.ValidationError("A recognized dispute reason is required");
        if (description.Length is < 20 or > 4000)
            return Result<TaskEscrowDto>.ValidationError("Dispute description must contain between 20 and 4000 characters");
        if (string.IsNullOrWhiteSpace(dto.DisputedByUserId))
            return Result<TaskEscrowDto>.ValidationError("The dispute actor is required");

        var escrow = await _repository.GetByIdAsync(id, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow with id {id} not found");

        if (escrow.status != EscrowStatus.FUNDED)
            return Result<TaskEscrowDto>.ValidationError($"Cannot dispute escrow in status {escrow.status}");

        var task = await GetTaskAsync(escrow, ct);
        if (task is null)
            return Result<TaskEscrowDto>.NotFound($"Task with id {escrow.taskId} not found for escrow {escrow.id}");

        escrow.status = EscrowStatus.DISPUTED;
        escrow.disputeReason = reason;
        escrow.disputeDescription = description;
        escrow.disputedByUserId = dto.DisputedByUserId;
        escrow.disputedAt = DateTime.UtcNow;
        task.escrowStatus = EscrowStatus.DISPUTED;
        task.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(escrow, ct);
        await _taskRepository.UpdateAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    /// <summary>
    /// Returns the held units to the reserve partition they were drawn from. This is the ONE
    /// place that ever calls <see cref="IAzoaHoldGateway.RefundAsync"/> for a task escrow; both
    /// the funder's own refund request and an administrator's REFUND resolution go through it,
    /// so there is exactly one code path that can move a disputed escrow's stake backward.
    /// </summary>
    public async Task<Result<TaskEscrowDto>> RefundAsync(string id, RefundEscrowDto dto, CancellationToken ct = default)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.RefundedByUserId))
            return Result<TaskEscrowDto>.ValidationError("The refund actor is required.");

        if (_holds is null)
            return Result<TaskEscrowDto>.Failure(HoldGatewayUnwiredError);

        var escrow = await _repository.GetByIdAsync(id, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow with id {id} not found");

        if (escrow.status is not (EscrowStatus.FUNDED or EscrowStatus.DISPUTED))
            return Result<TaskEscrowDto>.ValidationError($"Cannot refund escrow in status {escrow.status}");

        var legs = await ResolveAllocationsAsync(escrow, ct);
        // TokenAllocation.IsNodeHeld is the shared predicate; inlining the null-check here would
        // make this the one payout path that could disagree with the rest about what "held" means.
        if (DescribeUnusableLegs(legs) is { } unrefundable)
            return Result<TaskEscrowDto>.Conflict(unrefundable);

        // Every beneficiary's leg goes back, and the escrow is only stamped REFUNDED below once
        // they all have. Giving back one leg and calling the escrow refunded is how a guild's
        // share stayed drawn from the project with nothing left that could return it.
        foreach (var leg in legs)
        {
            var refunded = await _holds.RefundAsync(
                new AzoaHoldReference(leg.holdChain!, leg.holdId!),
                dto.Reason,
                ct);
            if (refunded.IsFailure)
                return PropagateFailure(refunded);
        }

        var task = await GetTaskAsync(escrow, ct);
        var now = DateTime.UtcNow;
        escrow.status = EscrowStatus.REFUNDED;
        escrow.refundedAt = now;
        await _repository.UpdateAsync(escrow, ct);

        if (task is not null)
        {
            task.escrowStatus = EscrowStatus.REFUNDED;
            task.updatedAt = now;
            await _taskRepository.UpdateAsync(task, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    /// <summary>
    /// An administrator adjudicates a disputed escrow. REFUND delegates to
    /// <see cref="RefundAsync"/> so there remains exactly one refund path. REJECT dismisses the
    /// dispute and restores FUNDED; nothing moved on the node while disputed, so nothing needs
    /// undoing there — the funder's own "Authorize Release" is how the worker is paid next.
    /// </summary>
    public async Task<Result<TaskEscrowDto>> ResolveAsync(string id, ResolveDisputeDto dto, CancellationToken ct = default)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.ResolvedByUserId))
            return Result<TaskEscrowDto>.ValidationError("The resolving administrator is required.");

        var outcome = dto.Outcome.Trim().ToUpperInvariant();
        if (!AllowedResolutionOutcomes.Contains(outcome))
        {
            return Result<TaskEscrowDto>.ValidationError(
                "A dispute can only be resolved by refunding the funder (REFUND) or dismissing the dispute (REJECT).");
        }

        var escrow = await _repository.GetByIdAsync(id, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow with id {id} not found");

        if (escrow.status != EscrowStatus.DISPUTED)
            return Result<TaskEscrowDto>.ValidationError($"Only a disputed escrow can be resolved. This one is {escrow.status}.");

        if (outcome == "REFUND")
        {
            return await RefundAsync(
                id,
                new RefundEscrowDto
                {
                    RefundedByUserId = dto.ResolvedByUserId,
                    Reason = "Dispute resolved in the funder's favor by an administrator.",
                },
                ct);
        }

        var task = await GetTaskAsync(escrow, ct);
        var now = DateTime.UtcNow;
        escrow.status = EscrowStatus.FUNDED;
        await _repository.UpdateAsync(escrow, ct);

        if (task is not null)
        {
            task.escrowStatus = EscrowStatus.FUNDED;
            task.updatedAt = now;
            await _taskRepository.UpdateAsync(task, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    private Task<ProjectTask?> GetTaskAsync(TaskEscrow escrow, CancellationToken ct)
        => _taskRepository.GetByIdAsync(escrow.taskId, ct);

    /// <summary>
    /// EVERY <see cref="TokenAllocation"/> the hold behind this escrow was reserved as, found by
    /// walking the escrow's own <c>taskId</c> to its commerce agreement and from there to the
    /// legs that agreement's award was held in. Empty when either join repository is unwired, or
    /// when no agreement/allocation exists yet — a caller that gets an empty list has nothing on
    /// the node to fund, release or refund.
    /// </summary>
    /// <remarks>
    /// This used to answer <c>FindOneAsync</c>, which returns ONE arbitrary row. An accepted bid
    /// that declared a guild share is held in TWO legs against the same agreement, so funding
    /// checked a non-deterministic leg, refunding gave back a non-deterministic leg and then
    /// stamped the whole escrow REFUNDED, and the other beneficiary's units stayed drawn from
    /// the project with nothing that could reach them.
    /// </remarks>
    private async Task<IReadOnlyList<TokenAllocation>> ResolveAllocationsAsync(
        TaskEscrow escrow,
        CancellationToken ct)
    {
        if (_agreementRepository is null || _allocationRepository is null)
            return Array.Empty<TokenAllocation>();

        var agreement = await _agreementRepository.FindOneAsync(a => a.taskId == escrow.taskId, ct);
        if (agreement is null)
            return Array.Empty<TokenAllocation>();

        return await ResolveAgreementLegsAsync(agreement.id, ct);
    }

    /// <summary>Every leg one agreement's award was held in, in a stable order.</summary>
    private async Task<IReadOnlyList<TokenAllocation>> ResolveAgreementLegsAsync(
        string agreementId,
        CancellationToken ct)
    {
        if (_allocationRepository is null)
            return Array.Empty<TokenAllocation>();

        var legs = await _allocationRepository.FindAsync(
            a => a.taskCommerceAgreementId == agreementId, ct);

        return legs.OrderBy(leg => leg.id, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The legs, or the refusal that says none of them can be acted on. Every leg must be one the
    /// node actually holds: acting on a set where only some are held would move part of an award
    /// and report the whole of it.
    /// </summary>
    private static string? DescribeUnusableLegs(IReadOnlyList<TokenAllocation> legs) =>
        legs.Count == 0 || legs.Any(leg => !leg.IsNodeHeld || string.IsNullOrWhiteSpace(leg.holdChain))
            ? NoLinkedHoldError
            : null;

    /// <summary>Re-wraps a failure from a downstream gateway result, keeping its result type.</summary>
    private static Result<TaskEscrowDto> PropagateFailure<TSource>(Result<TSource> source)
    {
        var error = source.Error ?? "The shared-node call failed.";
        return source.Type switch
        {
            ResultType.NotFound => Result<TaskEscrowDto>.NotFound(error),
            ResultType.Forbidden => Result<TaskEscrowDto>.Forbidden(error),
            ResultType.Unauthorized => Result<TaskEscrowDto>.Unauthorized(error),
            ResultType.Conflict => Result<TaskEscrowDto>.Conflict(error),
            ResultType.ValidationError => Result<TaskEscrowDto>.ValidationError(error),
            ResultType.BadRequest => Result<TaskEscrowDto>.BadRequest(error),
            _ => Result<TaskEscrowDto>.Failure(error),
        };
    }
}

/// <summary>
/// The entry to the escrow lifecycle, exposed under its own interface so a caller depends on it
/// explicitly - the <c>IProjectTokenProvisioning</c> precedent beside <c>ProjectTokenService</c>.
/// <see cref="TaskEscrowService"/> implements both this and <c>ITaskEscrowService</c> on one
/// scoped instance, so the container resolves a single object for both surfaces.
/// </summary>
public interface ITaskEscrowFunding
{
    /// <summary>
    /// Confirms the stake behind one escrow is actually held on the shared node and moves the
    /// escrow to <c>FUNDED</c>. <paramref name="fundedByUserId"/> is the caller's verified
    /// identity and must be the escrow's own funder; nothing about the money comes from the
    /// request.
    /// </summary>
    Task<Result<TaskEscrowDto>> FundAsync(
        string id,
        string fundedByUserId,
        CancellationToken ct = default);
}
