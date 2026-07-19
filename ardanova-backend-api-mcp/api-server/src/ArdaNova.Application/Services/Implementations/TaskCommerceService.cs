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
    private readonly IRepository<OpportunityBid> _bidRepository;
    private readonly IRepository<Opportunity> _opportunityRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<ProjectTokenConfig> _tokenConfigRepository;
    private readonly IRepository<ProjectTask> _taskRepository;
    private readonly IRepository<TaskCommerceAgreement> _agreementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TaskCommerceService(
        IRepository<OpportunityBid> bidRepository,
        IRepository<Opportunity> opportunityRepository,
        IRepository<Project> projectRepository,
        IRepository<ProjectTokenConfig> tokenConfigRepository,
        IRepository<ProjectTask> taskRepository,
        IRepository<TaskCommerceAgreement> agreementRepository,
        IUnitOfWork unitOfWork)
    {
        _bidRepository = bidRepository;
        _opportunityRepository = opportunityRepository;
        _projectRepository = projectRepository;
        _tokenConfigRepository = tokenConfigRepository;
        _taskRepository = taskRepository;
        _agreementRepository = agreementRepository;
        _unitOfWork = unitOfWork;
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

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            if (existingTask is null)
                await _taskRepository.AddAsync(task, ct);

            if (bid.status != BidStatus.ACCEPTED)
            {
                bid.status = BidStatus.ACCEPTED;
                bid.reviewedAt = now;
                await _bidRepository.UpdateAsync(bid, ct);
            }

            await _agreementRepository.AddAsync(agreement, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);
            return Result<TaskCommerceAcceptanceDto>.Success(ToDto(bid.id, task.id, agreement.id));
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
        });
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

        return Result<TaskCommerceAcceptanceDto>.Success(ToDto(bid.id, task.id, agreement.id));
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
