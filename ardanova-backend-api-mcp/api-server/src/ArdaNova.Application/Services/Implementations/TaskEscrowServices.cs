namespace ArdaNova.Application.Services.Implementations;

using System.Linq;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class TaskEscrowService : ITaskEscrowService
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

    private readonly IRepository<TaskEscrow> _repository;
    private readonly IRepository<ProjectTask> _taskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IRepository<TaskCommerceAgreement>? _agreementRepository;
    private readonly IRepository<TokenAllocation>? _allocationRepository;
    private readonly IAzoaHoldGateway? _holds;
    private readonly IProjectTokenProvisioning? _awards;

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
    public TaskEscrowService(
        IRepository<TaskEscrow> repository,
        IRepository<ProjectTask> taskRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRepository<TaskCommerceAgreement>? agreementRepository = null,
        IRepository<TokenAllocation>? allocationRepository = null,
        IEnumerable<IAzoaHoldGateway>? holds = null,
        IProjectTokenProvisioning? awards = null)
    {
        _repository = repository;
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _agreementRepository = agreementRepository;
        _allocationRepository = allocationRepository;
        _holds = holds?.FirstOrDefault();
        _awards = awards;
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

    public async Task<Result<TaskEscrowDto>> CreateAsync(CreateTaskEscrowDto dto, CancellationToken ct = default)
    {
        var exists = await _repository.ExistsAsync(e => e.taskId == dto.TaskId, ct);
        if (exists)
            return Result<TaskEscrowDto>.ValidationError($"Escrow already exists for task {dto.TaskId}");

        var escrow = new TaskEscrow
        {
            id = Guid.NewGuid().ToString(),
            taskId = dto.TaskId,
            funderId = dto.FunderId,
            shareId = dto.ShareId,
            amount = dto.Amount,
            status = EscrowStatus.NONE,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(escrow, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

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

        var allocation = await ResolveAllocationAsync(escrow, ct);
        if (allocation is null || string.IsNullOrWhiteSpace(allocation.id))
            return Result<TaskEscrowDto>.Conflict(NoLinkedHoldError);

        var approved = await _awards.ApproveWorkCompletionAsync(allocation.id, dto.ReleasedByUserId, ct);
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

        var allocation = await ResolveAllocationAsync(escrow, ct);
        // TokenAllocation.IsNodeHeld is the shared predicate; inlining the null-check here would
        // make this the one payout path that could disagree with the rest about what "held" means.
        if (allocation is null
            || !allocation.IsNodeHeld
            || string.IsNullOrWhiteSpace(allocation.holdChain))
        {
            return Result<TaskEscrowDto>.Conflict(NoLinkedHoldError);
        }

        var refunded = await _holds.RefundAsync(
            new AzoaHoldReference(allocation.holdChain!, allocation.holdId!),
            dto.Reason,
            ct);
        if (refunded.IsFailure)
            return PropagateFailure(refunded);

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
    /// The <see cref="TokenAllocation"/> read-model of the hold this escrow's task was awarded
    /// under, found by walking the escrow's own <c>taskId</c> to its commerce agreement and
    /// from there to the allocation the agreement's award was reserved as. Null when either
    /// join repository is unwired, or when no agreement/allocation exists yet — a caller that
    /// gets null has nothing on the node to release or refund.
    /// </summary>
    private async Task<TokenAllocation?> ResolveAllocationAsync(TaskEscrow escrow, CancellationToken ct)
    {
        if (_agreementRepository is null || _allocationRepository is null)
            return null;

        var agreement = await _agreementRepository.FindOneAsync(a => a.taskId == escrow.taskId, ct);
        if (agreement is null)
            return null;

        return await _allocationRepository.FindOneAsync(a => a.taskCommerceAgreementId == agreement.id, ct);
    }

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
