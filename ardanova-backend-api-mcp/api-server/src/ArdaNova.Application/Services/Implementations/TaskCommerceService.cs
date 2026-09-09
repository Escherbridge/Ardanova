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
    /// Why a guild bid that does not declare its split cannot enter commerce. Each share is
    /// set aside for exactly one person, so an undeclared split could only hold the whole
    /// award for whichever member happened to submit the bid.
    /// </summary>
    /// <remarks>
    /// A declared split IS payable: acceptance opens one hold per beneficiary. This refusal is
    /// re-made here, at the point the units move, because a bid row may predate the rule that
    /// <see cref="OpportunityBidService"/> now applies at bid time.
    /// </remarks>
    public const string GuildSplitUnsupported =
        "This guild bid does not say how the award is split between the guild and the member "
        + "doing the work, and each share is set aside for exactly one person. Ask for a bid "
        + "that names the guild's share.";

    /// <summary>Why a declared guild share cannot be set aside for the guild itself.</summary>
    /// <remarks>
    /// Fail-closed: a share nobody can be named for is never quietly folded into the worker's
    /// leg, because that would pay the wrong person the guild's units.
    /// </remarks>
    public const string GuildSplitUnpayable =
        "This bid promises a share to a guild that has nobody to receive it, so nothing was "
        + "set aside. The guild needs an owner with a finished account before its share can be "
        + "held.";

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
    private readonly IRepository<Guild>? _guildRepository;

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
    /// <param name="guildRepository">
    /// Names the person a guild's share is held for. Optional for the same reason as the two
    /// above; a null refuses a split bid with <see cref="GuildSplitUnpayable"/> rather than
    /// paying the guild's units to the member who submitted the bid.
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
        IRepository<TokenAllocation>? allocationRepository = null,
        IRepository<Guild>? guildRepository = null)
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
        _guildRepository = guildRepository;
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

        // The split is re-checked here, where the units move, and not only where the bid was
        // written: a row saved before this rule existed still has to add up before it is held.
        if (DescribeUnpayableSplit(bid) is { } splitRefusal)
            return Result<TaskCommerceAcceptanceDto>.ValidationError(splitRefusal);

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

        // Who the award is held FOR, resolved before anything is written: a split that cannot
        // name its beneficiaries must refuse before an agreement exists, not after.
        var beneficiaries = await ResolveBeneficiariesAsync(
            bid,
            bid.proposedAmount.Value,
            contributorAvatarId,
            ct);
        if (beneficiaries.IsFailure)
            return PropagateBeneficiaryFailure(beneficiaries);

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
        return await FundEscrowAsync(agreement, task, tokenConfig, beneficiaries.Value!, ct);
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

        // The VIEWER's own window, read off the award this agreement produced. A split award
        // has one share per beneficiary, so reading "the" award would show a guild's window to
        // the worker; the reader is shown their own share, and the project owner falls back to
        // the contributor's. A missing repository or a missing award reports RESTRICTED with no
        // date rather than "unrestricted": an award nobody can date is one nobody may move yet.
        var award = _allocationRepository is null
            ? null
            : await _allocationRepository.FindOneAsync(
                item => item.taskCommerceAgreementId == agreement.id
                    && item.recipientUserId == actorId,
                ct)
                ?? await _allocationRepository.FindOneAsync(
                    item => item.taskCommerceAgreementId == agreement.id
                        && item.recipientUserId == agreement.contributorUserId,
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
    /// Sets the accepted award aside on the node — one hold per beneficiary — and advances the
    /// agreement to <see cref="TaskCommerceAgreementStatus.ESCROW_FUNDED"/> only once every
    /// share is held. Idempotent: each hold id is derived from the bid, so a redelivered
    /// acceptance resumes the same reservations rather than drawing the project's contributor
    /// allowance a second time.
    /// </summary>
    private async Task<Result<TaskCommerceAcceptanceDto>> FundEscrowAsync(
        TaskCommerceAgreement agreement,
        ProjectTask task,
        ProjectTokenConfig tokenConfig,
        IReadOnlyList<AwardBeneficiary> beneficiaries,
        CancellationToken ct)
    {
        if (_awards is null)
        {
            return Result<TaskCommerceAcceptanceDto>.Failure(
                "This assignment was accepted, but the stake for it could not be set aside: this deployment "
                + "has no shared-node award gateway. Nothing has been promised to the worker.");
        }

        // The shares are the award, exactly. Holding a set that does not add up would either
        // promise units the project never committed or quietly keep some of the worker's.
        if (beneficiaries.Count == 0
            || beneficiaries.Sum(beneficiary => beneficiary.Amount) != agreement.awardAmount)
        {
            return Result<TaskCommerceAcceptanceDto>.Failure(
                "This assignment was accepted, but the shares it would be set aside in do not add up to the "
                + "award. Nothing has been promised to anyone.");
        }

        if (beneficiaries.Any(beneficiary => !ProjectAwardHold.IsAddressable(beneficiary.HoldId)))
        {
            return Result<TaskCommerceAcceptanceDto>.Failure(
                "This assignment was accepted, but its reference cannot be used to set stake aside on the "
                + "shared node. Nothing has been promised to the worker.");
        }

        foreach (var beneficiary in beneficiaries)
        {
            var reserved = await _awards.ReserveWorkAwardAsync(
                new ProjectAwardReservationRequest(
                    tokenConfig.id,
                    agreement.id,
                    beneficiary.HoldId,
                    beneficiary.Amount,
                    beneficiary.UserId,
                    beneficiary.AvatarId),
                ct);

            if (reserved.IsFailure)
            {
                // The agreement stays ACCEPTED and the task's escrow stays NONE, which is
                // exactly what is true: the work is assigned and the stake is not fully
                // committed. Any share already held is resumed on the next attempt, because
                // each reservation is idempotent on its own hold id.
                return PropagateReservationFailure(reserved);
            }
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

        // Resumed from the bid, not from the agreement: the agreement records the whole award,
        // and only the bid says how it splits.
        var beneficiaries = await ResolveBeneficiariesAsync(
            bid,
            agreement.awardAmount,
            contributorAvatarId,
            ct);
        if (beneficiaries.IsFailure)
            return PropagateBeneficiaryFailure(beneficiaries);

        return await FundEscrowAsync(agreement, task, tokenConfig, beneficiaries.Value!, ct);
    }

    /// <summary>One share of an accepted award: whose it is, how much, and under which hold.</summary>
    private sealed record AwardBeneficiary(string HoldId, decimal Amount, string UserId, Guid AvatarId);

    /// <summary>The guild's leg of a split award, derived from the same accepted bid.</summary>
    private static string GuildHoldIdFor(string escrowHoldId) => escrowHoldId + "-guild";

    /// <summary>
    /// Null when this bid's split could actually be held; the person-facing refusal otherwise.
    /// Mirrors the rule <see cref="OpportunityBidService"/> applies at bid time, because this
    /// is where the units move and a stored row is not evidence that they add up.
    /// </summary>
    private static string? DescribeUnpayableSplit(OpportunityBid bid)
    {
        if (string.IsNullOrWhiteSpace(bid.guildId))
            return bid.guildAwardSplit is > 0m ? OpportunityBidService.GuildSplitNeedsGuild : null;

        var award = bid.proposedAmount ?? 0m;
        var guildShare = bid.guildAwardSplit ?? 0m;
        if (guildShare <= 0m)
            return GuildSplitUnsupported;

        if (decimal.Truncate(guildShare) != guildShare)
            return OpportunityBidService.PartUnitSplitUnsupported;

        return guildShare >= award ? OpportunityBidService.GuildSplitOutOfRange : null;
    }

    /// <summary>
    /// The shares one accepted award is held in: the worker's own, plus the guild's when the
    /// bid declared one. Every beneficiary is named from the bid and the platform's own
    /// records — never from anything a caller sent — and a guild share that cannot be named
    /// refuses the whole acceptance rather than being folded into the worker's leg.
    /// </summary>
    /// <remarks>
    /// KNOWN GAP, and the reason the guild's share is described as "held" and never as "paid":
    /// approval currently settles the CONTRIBUTOR's leg only.
    /// <c>ProjectTokenService.ApproveWorkCompletionAsync</c> refuses any allocation whose
    /// recipient is not the agreement's contributor (ProjectTokenService.cs:1181-1182), so the
    /// guild's hold stays reserved until that check admits a declared split beneficiary. The
    /// units are committed and refundable in the meantime; nothing is lost and nothing is paid.
    /// </remarks>
    private async Task<Result<IReadOnlyList<AwardBeneficiary>>> ResolveBeneficiariesAsync(
        OpportunityBid bid,
        decimal awardAmount,
        Guid contributorAvatarId,
        CancellationToken ct)
    {
        if (DescribeUnpayableSplit(bid) is { } refusal)
            return Result<IReadOnlyList<AwardBeneficiary>>.ValidationError(refusal);

        var escrowHoldId = ProjectAwardHold.EscrowIdFor(bid.id);
        var guildShare = string.IsNullOrWhiteSpace(bid.guildId) ? 0m : bid.guildAwardSplit ?? 0m;
        var shares = new List<AwardBeneficiary>
        {
            new(escrowHoldId, awardAmount - guildShare, bid.bidderId, contributorAvatarId),
        };

        if (guildShare <= 0m)
            return Result<IReadOnlyList<AwardBeneficiary>>.Success(shares);

        if (_guildRepository is null)
            return Result<IReadOnlyList<AwardBeneficiary>>.Failure(GuildSplitUnpayable);

        var guild = await _guildRepository.GetByIdAsync(bid.guildId!, ct);
        if (guild is null || string.IsNullOrWhiteSpace(guild.ownerId))
            return Result<IReadOnlyList<AwardBeneficiary>>.Conflict(GuildSplitUnpayable);

        var guildReadiness = await _valueEligibility.RequireReadyAsync(
            guild.ownerId,
            "receive a guild's share of paid project work",
            ct);
        if (!guildReadiness.IsSuccess)
        {
            return ValueEligibilityFailure.Propagate<AzoaCustodialAccountStatusDto, IReadOnlyList<AwardBeneficiary>>(
                guildReadiness,
                "The guild must complete its account setup before its share of this assignment can be set aside.");
        }

        if (!TryResolveAvatar(guildReadiness.Value, out var guildAvatarId))
            return Result<IReadOnlyList<AwardBeneficiary>>.Conflict(ValueEligibilityMessages.SubjectUnknown);

        shares.Add(new AwardBeneficiary(GuildHoldIdFor(escrowHoldId), guildShare, guild.ownerId, guildAvatarId));
        return Result<IReadOnlyList<AwardBeneficiary>>.Success(shares);
    }

    /// <summary>Carries a beneficiary refusal out at the shape it actually had.</summary>
    private static Result<TaskCommerceAcceptanceDto> PropagateBeneficiaryFailure(
        Result<IReadOnlyList<AwardBeneficiary>> source)
    {
        var error = source.Error ?? GuildSplitUnpayable;
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
                // The accepted terms have to say who gets what, not just how much in total.
                guildAwardSplit = bid.guildAwardSplit?.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture),
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
