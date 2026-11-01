namespace ArdaNova.Application.Services.Implementations;

using System.Data.Common;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>See <see cref="IProjectFollowService"/> for the hardening this applies over GuildFollowService's pattern.</summary>
public class ProjectFollowService : IProjectFollowService
{
    private readonly IRepository<ProjectFollow> _repository;
    private readonly IProjectVisibility _projectVisibility;
    private readonly IUnitOfWork _unitOfWork;

    public ProjectFollowService(
        IRepository<ProjectFollow> repository,
        IProjectVisibility projectVisibility,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _projectVisibility = projectVisibility;
        _unitOfWork = unitOfWork;
    }

    private static ProjectFollowDto ToDto(ProjectFollow follow) => new()
    {
        Id = follow.id,
        ProjectId = follow.projectId,
        UserId = follow.userId,
        CreatedAt = follow.createdAt
    };

    /// <summary>The (userId, projectId) unique index refused a second row: a concurrent follow stored it first.</summary>
    private static bool IsUniqueViolation(DbUpdateException failure) =>
        failure.InnerException is DbException { SqlState: "23505" };

    public async Task<Result<ProjectFollowDto>> FollowAsync(string projectId, ProjectViewer viewer, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(viewer.UserId))
            return Result<ProjectFollowDto>.Unauthorized("Sign in to follow a project");

        // Visibility FIRST: a hidden/draft project answers the same 404 a missing one would, so
        // its existence stays private to non-members too (ProjectVisibility's own invariant).
        var visible = await _projectVisibility.GetVisibleByIdAsync(viewer, projectId, ct);
        if (!visible.IsSuccess)
            return Result<ProjectFollowDto>.NotFound($"Project with id {projectId} not found");

        var userId = viewer.UserId;
        var existing = await _repository.FindOneAsync(f => f.projectId == projectId && f.userId == userId, ct);
        if (existing is not null)
            return Result<ProjectFollowDto>.Success(ToDto(existing));

        var follow = new ProjectFollow
        {
            id = Guid.NewGuid().ToString(),
            userId = userId,
            projectId = projectId,
            notifyUpdates = true,
            notifyMilestones = true,
            notifyProposals = true,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(follow, ct);
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException failure) when (IsUniqueViolation(failure))
        {
            // The exists-then-insert race GuildFollowService has (§8-6): the pre-check above is
            // only the common-case shortcut, not the correctness guard. A concurrent follow by the
            // same viewer stored its row first, so the double tap is idempotent, not a conflict.
            _unitOfWork.ClearTrackedChanges();
            var raced = await _repository.FindOneAsync(f => f.projectId == projectId && f.userId == userId, ct);
            return raced is null
                ? Result<ProjectFollowDto>.Conflict("Already following this project")
                : Result<ProjectFollowDto>.Success(ToDto(raced));
        }

        return Result<ProjectFollowDto>.Success(ToDto(follow));
    }

    public async Task<Result<bool>> UnfollowAsync(string projectId, ProjectViewer viewer, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(viewer.UserId))
            return Result<bool>.Unauthorized("Sign in to unfollow a project");

        var visible = await _projectVisibility.GetVisibleByIdAsync(viewer, projectId, ct);
        if (!visible.IsSuccess)
            return Result<bool>.NotFound($"Project with id {projectId} not found");

        var userId = viewer.UserId;
        // One DELETE, no find-then-delete: a double tap's second delete finds no row, and "not following" is still success.
        await _repository.Query()
            .Where(f => f.projectId == projectId && f.userId == userId)
            .ExecuteDeleteAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProjectFollowStatusDto>> GetMyFollowStatusAsync(string projectId, ProjectViewer viewer, CancellationToken ct = default)
    {
        var visible = await _projectVisibility.GetVisibleByIdAsync(viewer, projectId, ct);
        if (!visible.IsSuccess)
            return Result<ProjectFollowStatusDto>.NotFound($"Project with id {projectId} not found");

        if (string.IsNullOrWhiteSpace(viewer.UserId))
            return Result<ProjectFollowStatusDto>.Success(new ProjectFollowStatusDto { Following = false });

        var userId = viewer.UserId;
        var following = await _repository.ExistsAsync(f => f.projectId == projectId && f.userId == userId, ct);
        return Result<ProjectFollowStatusDto>.Success(new ProjectFollowStatusDto { Following = following });
    }
}
