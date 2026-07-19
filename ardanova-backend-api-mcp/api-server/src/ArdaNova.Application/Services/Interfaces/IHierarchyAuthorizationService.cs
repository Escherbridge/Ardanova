namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Domain.Models.Enums;

public interface IHierarchyAuthorizationService
{
    Task<bool> CanManageProjectAsync(string actorId, string projectId, CancellationToken ct = default);
    Task<bool> CanWorkOnItemAsync(string actorId, string projectId, string? assigneeId, CancellationToken ct = default);
    Task<bool> IsProjectMemberAsync(string userId, string projectId, CancellationToken ct = default);
    Task<string?> ResolveCommentTargetProjectAsync(CommentTargetType targetType, string targetId, CancellationToken ct = default);
}
