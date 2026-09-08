namespace ArdaNova.Application.Services.Implementations;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

public class TaskCommerceService : ITaskCommerceService
{
    /// <summary>
    /// Why a guild bid cannot enter commerce yet. A hold has exactly one beneficiary, and a
    /// guild bid records no split between its members, so accepting one would hold the whole
    /// award for whichever member happened to submit it.
    /// </summary>
    /// <remarks>
    /// STUB, fail-closed. Unblocked by a declared split at bid time: a
    /// <c>guildAwardSplit</c> column on <c>OpportunityBid</c> plus the matching field on the
    /// bid DTOs, both outside this change's file boundary. Until then a guild bids as the
    /// member who will do the work.
    /// </remarks>
    public const string GuildSplitUnsupported =
        "A guild bid does not yet say how the award is split between its members, and an award "
        + "is set aside for exactly one person. Ask the guild to bid as the member who will do "
        + "the work.";

    private readonly IRepository<OpportunityBid> _bidRepository;
    private readonly IRepository<Opportunity> _opportunityRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<ProjectTokenConfig> _tokenConfigRepository;
    private readonly IRepository<ProjectTask> _taskRepository;
    private readonly IRepository<TaskCommerceAgreement> _agreementRepository;
    private readonly IAzoaValueEligibilityService _valueEligibility;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectTokenProvisioning? _awards;
    private readonly IRepository<TokenAllocation>? _allocationRepository;

    /// <param name="awards">
    /// The award holds. Supplied by the container as the same scoped instance that runs funding
    /// setup, and optional only so call sites written before worker awards existed still
    /// construct. A null is not permission to skip the reservation: acceptance stops at
    /// <see cref="TaskCommerceAgreementStatus.ACCEPTED"/> with a named reason, and no
    /// assignment is ever reported funded.
    /// </param>
    /// <param name="allocationRepository">
    /// The award read-model, used only to tell a worker when their own window ends. Optional
    /// for the same construct-compatibility reason as <paramref name="awards"/>; a null simply
    /// means the window cannot be reported, never that the award is unrestricted — the
    /// restricted flag stays true whenever the window date is unknown.
    /// </param>
    public TaskCommerceService(
        IRepository<OpportunityBid> bidRepository,
        IRepository<Opportunity> opportunityRepository,
        IRepository<Project> projectRepository,
        IRepository<ProjectTokenConfig> tokenConfigRepository,
        IRepository<ProjectTask> taskRepository,
        IRepository<TaskCommerceAgreement> agreementRepository,
        IAzoaValueEligibilityService valueEligibility,
        IUnitOfWork unitOfWork,
        IProjectTokenProvisioning? awards = null,
        IRepository<TokenAllocation>? allocationRepository = null)
    {
        _bidRepository = bidRepository;
        _opportunityRepository = opportunityRepository;
        _projectRepository = projectRepository;
        _tokenConfigRepository = tokenConfigRepository;
        _taskRepository = taskRepository;
        _agreementRepository = agreementRepository;
        _valueEligibility = valueEligibility;
        _unitOfWork = unitOfWork;
        _awards = awards;
        _allocationRepository = allocationRepository;
    }

    /// <inheritdoc/>
    public async Task<Result<TaskCommerceAcceptanceDto>> AcceptBidAsync(
        string bidId,
        string actorId,
        CancellationToken ct = default)
    {
        var bid = await _bidRepository.GetByIdAsync(bidId, ct);
        if (bid is null)
            return Result<TaskCommerceAcceptanceDto>.NotFound("Opportunity bid not found");

        var opportunity = await _opportunityRepository.GetByIdAsync(bid.opportunityId, ct);
        if (opportunity is null)
            return Result<TaskCommerceAcceptanceDto>.NotFound("Opportunity not found");

        if (string.IsNullOrWhiteSpace(opportunity.projectId))
        {
            if (!string.Equals(opportunity.posterId, actorId, StringComparison.Ordinal))
                return Result<TaskCommerceAcceptanceDto>.Forbidden("Only the opportunity poster can accept this bid");

            return Result<TaskCommerceAcceptanceDto>.ValidationError(
                "A commerce task requires a project-backed opportunity");
        }

        var project = await _projectRepository.GetByIdAsync(opportunity.projectId, ct);
        if (project is null)
            return Result<TaskCommerceAcceptanceDto>.NotFound("Project not found");

        if (!string.Equals(project.createdById, actorId, StringComparison.Ordinal))
            return Result<TaskCommerceAcceptanceDto>.Forbidden("Only the project owner can accept this bid");

        var existingAgreement = await _agreementRepository.FindOneAsync(
            agreement => agreement.bidId == bid.id,
            ct);
        if (existingAgreement is not null)
            return await ValidateReplayAsync(existingAgreement, bid, opportunity, project, ct);

        if (bid.status is not (BidStatus.SUBMITTED or BidStatus.UNDER_REVIEW or BidStatus.ACCEPTED))
        {
            return Result<TaskCommerceAcceptanceDto>.ValidationError(
                "Only submitted, under-review, or already accepted bids can enter commerce");
        }

        if (bid.proposedAmount is not > 0m)
            return Result<TaskCommerceAcceptanceDto>.ValidationError(
                "An accepted commerce bid requires a positive project-token award");

        // One award, one beneficiary. See GuildSplitUnsupported.
        if (!string.IsNullOrWhiteSpace(bid.guildId))
            return Result<TaskCommerceAcceptanceDto>.ValidationError(GuildSplitUnsupported);

        var ownerReadiness = await _valueEligibility.RequireReadyAsync(
            project.createdById,
            "accept paid project work",
            ct);
        if (!ownerReadiness.IsSuccess)
            return ValueEligibilityFailure.Propagate<AzoaCustodialAccountStatusDto, TaskCommerceAcceptanceDto>(
                ownerReadiness);

        var contributorReadiness = await _valueEligibility.RequireReadyAsync(
            bid.bidderId,
            "receive a paid project assignment",
            ct);
        if (!contributorReadiness.IsSuccess)
            return ValueEligibilityFailure.Propagate<AzoaCustodialAccountStatusDto, TaskCommerceAcceptanceDto>(
                contributorReadiness,
                "The selected contributor must complete account setup before this paid assignment can be accepted.");

        if (!TryResolveAvatar(contributorReadiness.Value, out var contributorAvatarId))
        {
            return Result<TaskCommerceAcceptanceDto>.Conflict(
                ValueEligibilityMessages.SubjectUnknown);
        }

        var tokenConfig = await _tokenConfigRepository.FindOneAsync(
            config => config.projectId == project.id,
            ct);
        if (tokenConfig is null
            || string.IsNullOrWhiteSpace(tokenConfig.unitName)
            || tokenConfig.assetScale is not int assetScale
            || !FixedScaleAmount.IsSupportedScale(assetScale))
        {
            return Result<TaskCommerceAcceptanceDto>.ValidationError(
                "The project needs a token configuration with a valid asset scale before a commerce bid can be accepted");
        }

        // Without the node's three legs there is no stake to set aside and no declared
        // allowance to draw it from, so accepting would promise units nothing backs.
        if (!tokenConfig.IsFundable)
        {
            return Result<TaskCommerceAcceptanceDto>.ValidationError(
                "This project's funding setup is not finished, so no stake can be set aside for a worker yet.");
        }

        var existingTask = await _taskRepository.FindOneAsync(
            task => task.opportunityId == opportunity.id,
            ct);
        if (existingTask is not null
            && (!string.Equals(existingTask.projectId, project.id, StringComparison.Ordinal)
                || !string.Equals(existingTask.assignedToId, bid.bidderId, StringComparison.Ordinal)))
        {
            return Result<TaskCommerceAcceptanceDto>.Conflict(
                "This opportunity is already assigned to a different accepted bid");
        }

        var now = DateTime.UtcNow;
        var task = existingTask ?? new ProjectTask
        {
            id = Guid.NewGuid().ToString(),
            projectId = project.id,
            guildId = bid.guildId,
            title = opportunity.title,
            description = opportunity.description,
            status = TaskStatus.TODO,
            priority = TaskPriority.MEDIUM,
            taskType = TaskType.OTHER,
            estimatedHours = bid.estimatedHours,
            escrowStatus = EscrowStatus.NONE,
            assignedToId = bid.bidderId,
            opportunityId = opportunity.id,
            createdAt = now,
            updatedAt = now,
        };
        var terms = CreateTermsSnapshot(bid, opportunity, project, tokenConfig, assetScale);
        var agreement = new TaskCommerceAgreement
        {
            id = Guid.NewGuid().ToString(),
            semanticKey = $"task-commerce:bid:{bid.id}",
            status = TaskCommerceAgreementStatus.ACCEPTED,
            projectId = project.id,
            taskId = task.id,
            bidId = bid.id,
            contributorUserId = bid.bidderId,
            projectTokenConfigId = tokenConfig.id,
            assetCode = tokenConfig.unitName,
            awardAmount = bid.proposedAmount.Value,
            scale = assetScale,
            acceptedTermsSnapshot = terms,
            termsHash = Hash(terms),
            acceptedAt = now,
            createdAt = now,
            updatedAt = now,
        };

        // The agreement is the award of record. The task's own figure is written from it here
        // and nowhere else, so a board and an agreement can never quote different numbers.
        task.equityReward = agreement.awardAmount;
        task.updatedAt = now;

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            if (existingTask is null)
                await _taskRepository.AddAsync(task, ct);
            else
                await _taskRepository.UpdateAsync(task, ct);

            if (bid.status != BidStatus.ACCEPTED)
            {
                bid.status = BidStatus.ACCEPTED;
                bid.reviewedAt = now;
                await _bidRepository.UpdateAsync(bid, ct);
            }

            await _agreementRepository.AddAsync(agreement, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);
        }
        catch (DbUpdateException)
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            _unitOfWork.ClearTrackedChanges();
            return await NormalizeConcurrentAcceptanceAsync(bid, opportunity, project, ct);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }

        // The escrow leg runs after the agreement exists so a failure here leaves a resumable
        // ACCEPTED agreement rather than a hold nothing in this database points at. Nothing is
        // payable in that state: no allocation exists and the task's escrow stays NONE.
        return await FundEscrowAsync(agreement, task, tokenConfig, contributorAvatarId, ct);
    }

    /// <inheritdoc/>
    public async Task<Result<TaskCommerceViewDto>> GetByTaskIdAsync(
        string taskId,
        string actorId,
        CancellationToken ct = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, ct);
        if (task is null)
            return Result<TaskCommerceViewDto>.NotFound("Task not found");

        var agreement = await _agreementRepository.FindOneAsync(
            item => item.taskId == task.id,
            ct);
        if (agreement is null)
            return Result<TaskCommerceViewDto>.NotFound("Commerce agreement not found for this task");

        var project = await _projectRepository.GetByIdAsync(task.projectId, ct);
        if (project is null)
            return Result<TaskCommerceViewDto>.NotFound("Project not found");

        if (!string.Equals(agreement.contributorUserId, actorId, StringComparison.Ordinal)
            && !string.Equals(project.createdById, actorId, StringComparison.Ordinal))
        {
            return Result<TaskCommerceViewDto>.Forbidden("You cannot view this task's commerce agreement");
        }

        // The worker's own window, read off the award this agreement produced. A missing
        // repository or a missing award reports RESTRICTED with no date rather than
        // "unrestricted": an award nobody can date is one nobody may move yet.
        var award = _allocationRepository is null
            ? null
            : await _allocationRepository.FindOneAsync(
                item => item.taskCommerceAgreementId == agreement.id,
                ct);

        return Result<TaskCommerceViewDto>.Success(new TaskCommerceViewDto
        {
            TaskId = task.id,
            AgreementId = agreement.id,
            Title = task.title,
            Description = task.description,
            AssetCode = agreement.assetCode,
            AwardAmount = agreement.awardAmount,
            Scale = agreement.scale,
            AgreementStatus = agreement.status.ToString(),
            EscrowStatus = task.escrowStatus.ToString(),
            WorkerHoldReleasesAt = award?.workerHoldReleasesAt,
            IsRestricted = award is null
                || award.workerHoldReleasesAt is null
                || award.workerHoldReleasesAt > DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Sets the accepted award aside on the node and advances the agreement to
    /// <see cref="TaskCommerceAgreementStatus.ESCROW_FUNDED"/>. Idempotent: the reservation is
    /// anchored on a hold id derived from the bid, so a redelivered acceptance resumes rather
    /// than drawing the project's contributor allowance a second time.
    /// </summary>
    private async Task<Result<TaskCommerceAcceptanceDto>> FundEscrowAsync(
        TaskCommerceAgreement agreement,
        ProjectTask task,
        ProjectTokenConfig tokenConfig,
        Guid contributorAvatarId,
        CancellationToken ct)
    {
        if (_awards is null)
        {
            return Result<TaskCommerceAcceptanceDto>.Failure(
                "This assignment was accepted, but the stake for it could not be set aside: this deployment "
                + "has no shared-node award gateway. Nothing has been promised to the worker.");
        }

        var holdId = ProjectAwardHold.EscrowIdFor(agreement.bidId);
        if (!ProjectAwardHold.IsAddressable(holdId))
        {
            return Result<TaskCommerceAcceptanceDto>.Failure(
                "This assignment was accepted, but its reference cannot be used to set stake aside on the "
                + "shared node. Nothing has been promised to the worker.");
        }

        var reserved = await _awards.ReserveWorkAwardAsync(
            new ProjectAwardReservationRequest(
                tokenConfig.id,
                agreement.id,
                holdId,
                agreement.awardAmount,
                agreement.contributorUserId,
                contributorAvatarId),
            ct);

        if (reserved.IsFailure)
        {
            // The agreement stays ACCEPTED and the task's escrow stays NONE, which is exactly
            // what is true: the work is assigned and the stake is not yet committed.
            return PropagateReservationFailure(reserved);
        }

        var now = DateTime.UtcNow;
        if (agreement.status is TaskCommerceAgreementStatus.DRAFT or TaskCommerceAgreementStatus.ACCEPTED)
        {
            agreement.status = TaskCommerceAgreementStatus.ESCROW_FUNDED;
            agreement.updatedAt = now;
            await _agreementRepository.UpdateAsync(agreement, ct);
        }

        if (task.escrowStatus == EscrowStatus.NONE)
        {
            task.escrowStatus = EscrowStatus.FUNDED;
            task.updatedAt = now;
            await _taskRepository.UpdateAsync(task, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return Result<TaskCommerceAcceptanceDto>.Success(ToDto(agreement.bidId, task.id, agreement.id));
    }

    private async Task<Result<TaskCommerceAcceptanceDto>> NormalizeConcurrentAcceptanceAsync(
        OpportunityBid bid,
        Opportunity opportunity,
        Project project,
        CancellationToken ct)
    {
        var agreement = await _agreementRepository.FindOneAsync(
            existing => existing.bidId == bid.id,
            ct);
        if (agreement is not null)
            return await ValidateReplayAsync(agreement, bid, opportunity, project, ct);

        var task = await _taskRepository.FindOneAsync(
            existing => existing.opportunityId == opportunity.id,
            ct);
        return task is not null
            && string.Equals(task.projectId, project.id, StringComparison.Ordinal)
            && string.Equals(task.assignedToId, bid.bidderId, StringComparison.Ordinal)
            ? Result<TaskCommerceAcceptanceDto>.Conflict(
                "Commerce acceptance is still being finalized; retry with the same bid")
            : Result<TaskCommerceAcceptanceDto>.Conflict(
                "This opportunity is already assigned to a different accepted bid");
    }

    /// <summary>
    /// Confirms a repeated acceptance addresses the same assignment, then RESUMES it if the
    /// escrow leg never finished. Returning success on an agreement no stake was ever set
    /// aside for is the failure mode this guard exists to prevent: the caller would read
    /// "accepted" and the worker would be owed units nobody committed.
    /// </summary>
    private async Task<Result<TaskCommerceAcceptanceDto>> ValidateReplayAsync(
        TaskCommerceAgreement agreement,
        OpportunityBid bid,
        Opportunity opportunity,
        Project project,
        CancellationToken ct)
    {
        var task = await _taskRepository.GetByIdAsync(agreement.taskId, ct);
        if (task is null
            || !string.Equals(agreement.projectId, project.id, StringComparison.Ordinal)
            || !string.Equals(agreement.contributorUserId, bid.bidderId, StringComparison.Ordinal)
            || !string.Equals(task.opportunityId, opportunity.id, StringComparison.Ordinal)
            || !string.Equals(task.assignedToId, bid.bidderId, StringComparison.Ordinal))
        {
            return Result<TaskCommerceAcceptanceDto>.Conflict(
                "The existing commerce agreement conflicts with this bid's assignment");
        }

        if (agreement.status is not (TaskCommerceAgreementStatus.DRAFT or TaskCommerceAgreementStatus.ACCEPTED))
            return Result<TaskCommerceAcceptanceDto>.Success(ToDto(bid.id, task.id, agreement.id));

        var tokenConfig = string.IsNullOrWhiteSpace(agreement.projectTokenConfigId)
            ? null
            : await _tokenConfigRepository.GetByIdAsync(agreement.projectTokenConfigId!, ct);
        if (tokenConfig is null || !tokenConfig.IsFundable)
        {
            return Result<TaskCommerceAcceptanceDto>.ValidationError(
                "This project's funding setup is not finished, so the stake for this assignment could not be set aside.");
        }

        var contributorReadiness = await _valueEligibility.RequireReadyAsync(
            agreement.contributorUserId,
            "receive a paid project assignment",
            ct);
        if (!contributorReadiness.IsSuccess)
        {
            return ValueEligibilityFailure.Propagate<AzoaCustodialAccountStatusDto, TaskCommerceAcceptanceDto>(
                contributorReadiness,
                "The selected contributor must complete account setup before this paid assignment can be funded.");
        }

        if (!TryResolveAvatar(contributorReadiness.Value, out var contributorAvatarId))
            return Result<TaskCommerceAcceptanceDto>.Conflict(ValueEligibilityMessages.SubjectUnknown);

        return await FundEscrowAsync(agreement, task, tokenConfig, contributorAvatarId, ct);
    }

    /// <summary>
    /// Carries a reservation refusal out at the shape it actually had. Flattening every
    /// failure to Forbidden would tell an owner whose node was briefly unreachable that they
    /// are not allowed to accept the bid.
    /// </summary>
    private static Result<TaskCommerceAcceptanceDto> PropagateReservationFailure(
        Result<ProjectAwardReservation> source)
    {
        var error = source.Error
            ?? "The stake for this assignment could not be set aside on the shared node.";
        return source.Type switch
        {
            ResultType.NotFound => Result<TaskCommerceAcceptanceDto>.NotFound(error),
            ResultType.Forbidden => Result<TaskCommerceAcceptanceDto>.Forbidden(error),
            ResultType.Unauthorized => Result<TaskCommerceAcceptanceDto>.Unauthorized(error),
            ResultType.Conflict => Result<TaskCommerceAcceptanceDto>.Conflict(error),
            ResultType.ValidationError => Result<TaskCommerceAcceptanceDto>.ValidationError(error),
            ResultType.BadRequest => Result<TaskCommerceAcceptanceDto>.BadRequest(error),
            _ => Result<TaskCommerceAcceptanceDto>.Failure(error),
        };
    }

    /// <summary>The worker's account on the node; false when the platform cannot name it.</summary>
    private static bool TryResolveAvatar(AzoaCustodialAccountStatusDto? status, out Guid avatarId)
    {
        avatarId = Guid.Empty;
        return status is not null
            && Guid.TryParse(status.AvatarId, out avatarId)
            && avatarId != Guid.Empty;
    }

    private static TaskCommerceAcceptanceDto ToDto(string bidId, string taskId, string agreementId)
        => new()
        {
            BidId = bidId,
            TaskId = taskId,
            AgreementId = agreementId,
            CommerceUrl = $"/tasks/{Uri.EscapeDataString(taskId)}/commerce",
        };

    private static string CreateTermsSnapshot(
        OpportunityBid bid,
        Opportunity opportunity,
        Project project,
        ProjectTokenConfig tokenConfig,
        int assetScale)
        => JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            bid = new
            {
                id = bid.id,
                bidderId = bid.bidderId,
                guildId = bid.guildId,
                awardAmount = bid.proposedAmount!.Value.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture),
                proposal = bid.proposal,
                estimatedHours = bid.estimatedHours,
                timeline = bid.timeline,
                deliverables = bid.deliverables,
            },
            opportunity = new
            {
                id = opportunity.id,
                projectId = project.id,
                title = opportunity.title,
                description = opportunity.description,
                requirements = opportunity.requirements,
            },
            asset = new
            {
                projectTokenConfigId = tokenConfig.id,
                code = tokenConfig.unitName,
                scale = assetScale,
            },
        });

    private static string Hash(string input)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
}
