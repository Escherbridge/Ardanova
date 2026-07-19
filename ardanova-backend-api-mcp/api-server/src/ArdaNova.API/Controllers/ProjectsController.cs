namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IProjectResourceService _resourceService;
    private readonly IProjectMilestoneService _milestoneService;
    private readonly IProjectMemberService _memberService;
    private readonly IProjectApplicationService _applicationService;
    private readonly IProjectCommentService _commentService;
    private readonly IProjectUpdateService _updateService;
    private readonly IProjectSupportService _supportService;
    private readonly IGovernanceService _governanceService;
    private readonly IProjectInvitationService _invitationService;
    private readonly IHierarchyAuthorizationService _hierarchyAuthorization;
    private readonly IMembershipCredentialService _membershipCredentialService;

    public ProjectsController(
        IProjectService projectService,
        IProjectResourceService resourceService,
        IProjectMilestoneService milestoneService,
        IProjectMemberService memberService,
        IProjectApplicationService applicationService,
        IProjectCommentService commentService,
        IProjectUpdateService updateService,
        IProjectSupportService supportService,
        IGovernanceService governanceService,
        IProjectInvitationService invitationService,
        IHierarchyAuthorizationService hierarchyAuthorization,
        IMembershipCredentialService membershipCredentialService)
    {
        _projectService = projectService;
        _resourceService = resourceService;
        _milestoneService = milestoneService;
        _memberService = memberService;
        _applicationService = applicationService;
        _commentService = commentService;
        _updateService = updateService;
        _supportService = supportService;
        _governanceService = governanceService;
        _invitationService = invitationService;
        _hierarchyAuthorization = hierarchyAuthorization;
        _membershipCredentialService = membershipCredentialService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _projectService.GetAllAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _projectService.GetPagedAsync(page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] ProjectStatus? status,
        [FromQuery] string? category,
        [FromQuery] ProjectType? projectType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _projectService.SearchAsync(searchTerm, status, category, projectType, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _projectService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await _projectService.GetBySlugAsync(slug, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(string userId, CancellationToken ct)
    {
        var result = await _projectService.GetByUserIdAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(ProjectStatus status, CancellationToken ct)
    {
        var result = await _projectService.GetByStatusAsync(status, ct);
        return ToActionResult(result);
    }

    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category, CancellationToken ct)
    {
        var result = await _projectService.GetByCategory(category, ct);
        return ToActionResult(result);
    }

    [HttpGet("type/{projectType}")]
    public async Task<IActionResult> GetByProjectType(ProjectType projectType, CancellationToken ct)
    {
        var result = await _projectService.GetByProjectTypeAsync(projectType, ct);
        return ToActionResult(result);
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured(CancellationToken ct)
    {
        var result = await _projectService.GetFeaturedAsync(ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Create([FromBody] CreateProjectDto dto, CancellationToken ct)
    {
        var result = await _projectService.CreateAsync(dto with { CreatedById = ActorId }, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateProjectDto dto, CancellationToken ct)
    {
        var project = await _projectService.GetByIdAsync(id, ct);
        if (!project.IsSuccess)
            return ToActionResult(project);
        if (!await CanManageProjectAsync(project.Value!.Id, ct))
            return Forbid();

        var result = await _projectService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var project = await _projectService.GetByIdAsync(id, ct);
        if (!project.IsSuccess)
            return ToActionResult(project);
        if (!await CanManageProjectAsync(project.Value!.Id, ct))
            return Forbid();

        var result = await _projectService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id}/publish")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Publish(string id, CancellationToken ct)
    {
        var project = await _projectService.GetByIdAsync(id, ct);
        if (!project.IsSuccess)
            return ToActionResult(project);
        if (!await CanManageProjectAsync(project.Value!.Id, ct))
            return Forbid();

        var result = await _projectService.PublishAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/featured")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> SetFeatured(string id, [FromQuery] bool featured, CancellationToken ct)
    {
        if (!IsAdmin)
            return Forbid();

        var result = await _projectService.SetFeaturedAsync(id, featured, ct);
        return ToActionResult(result);
    }

    // ===== PROJECT RESOURCES =====
    [HttpGet("{projectId}/resources")]
    public async Task<IActionResult> GetResources(string projectId, CancellationToken ct)
    {
        var result = await _resourceService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{projectId}/resources/{resourceId}")]
    public async Task<IActionResult> GetResourceById(string projectId, string resourceId, CancellationToken ct)
    {
        var result = await _resourceService.GetByIdAsync(resourceId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/resources")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CreateResource(string projectId, [FromBody] CreateProjectResourceDto dto, CancellationToken ct)
    {
        if (!await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var dtoWithProject = dto with { ProjectId = projectId };
        var result = await _resourceService.CreateAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetResourceById), new { projectId, resourceId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{projectId}/resources/{resourceId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdateResource(string projectId, string resourceId, [FromBody] UpdateProjectResourceDto dto, CancellationToken ct)
    {
        var resource = await _resourceService.GetByIdAsync(resourceId, ct);
        if (!resource.IsSuccess)
            return ToActionResult(resource);
        if (!string.Equals(resource.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Resource not found in the routed project" });
        if (!await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _resourceService.UpdateAsync(resourceId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{projectId}/resources/{resourceId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> DeleteResource(string projectId, string resourceId, CancellationToken ct)
    {
        var resource = await _resourceService.GetByIdAsync(resourceId, ct);
        if (!resource.IsSuccess)
            return ToActionResult(resource);
        if (!string.Equals(resource.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Resource not found in the routed project" });
        if (!await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _resourceService.DeleteAsync(resourceId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{projectId}/resources/{resourceId}/obtained")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> MarkResourceObtained(string projectId, string resourceId, [FromQuery] bool obtained, CancellationToken ct)
    {
        var resource = await _resourceService.GetByIdAsync(resourceId, ct);
        if (!resource.IsSuccess)
            return ToActionResult(resource);
        if (!string.Equals(resource.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Resource not found in the routed project" });
        if (!await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _resourceService.MarkObtainedAsync(resourceId, obtained, ct);
        return ToActionResult(result);
    }

    // ===== PROJECT MILESTONES =====
    [HttpGet("{projectId}/milestones")]
    public async Task<IActionResult> GetMilestones(string projectId, CancellationToken ct)
    {
        var result = await _milestoneService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{projectId}/milestones/{milestoneId}")]
    public async Task<IActionResult> GetMilestoneById(string projectId, string milestoneId, CancellationToken ct)
    {
        var result = await _milestoneService.GetByIdAsync(milestoneId, ct);
        if (result.IsSuccess &&
            !string.Equals(result.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Milestone not found in the routed project" });
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/milestones")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CreateMilestone(string projectId, [FromBody] CreateProjectMilestoneDto dto, CancellationToken ct)
    {
        if (!await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var dtoWithProject = dto with { ProjectId = projectId };
        var result = await _milestoneService.CreateAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMilestoneById), new { projectId, milestoneId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{projectId}/milestones/{milestoneId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdateMilestone(string projectId, string milestoneId, [FromBody] UpdateProjectMilestoneDto dto, CancellationToken ct)
    {
        if (!await MayManageMilestoneAsync(projectId, milestoneId, ct))
            return Forbid();
        var result = await _milestoneService.UpdateAsync(milestoneId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{projectId}/milestones/{milestoneId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> DeleteMilestone(string projectId, string milestoneId, CancellationToken ct)
    {
        if (!await MayManageMilestoneAsync(projectId, milestoneId, ct))
            return Forbid();
        var result = await _milestoneService.DeleteAsync(milestoneId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{projectId}/milestones/{milestoneId}/complete")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CompleteMilestone(string projectId, string milestoneId, CancellationToken ct)
    {
        if (!await MayManageMilestoneAsync(projectId, milestoneId, ct))
            return Forbid();
        var result = await _milestoneService.CompleteAsync(milestoneId, ct);
        return ToActionResult(result);
    }

    // ===== PROJECT MEMBERS =====
    [HttpGet("{projectId}/members")]
    public async Task<IActionResult> GetMembers(string projectId, CancellationToken ct)
    {
        var result = await _memberService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{projectId}/members/{memberId}")]
    public async Task<IActionResult> GetMemberById(string projectId, string memberId, CancellationToken ct)
    {
        var result = await _memberService.GetByIdAsync(memberId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/members")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> AddMember(string projectId, [FromBody] CreateProjectMemberDto dto, CancellationToken ct)
    {
        if (!await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var dtoWithProject = dto with { ProjectId = projectId };
        var result = await _memberService.CreateAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMemberById), new { projectId, memberId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{projectId}/members/{memberId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdateMember(string projectId, string memberId, [FromBody] UpdateProjectMemberDto dto, CancellationToken ct)
    {
        var member = await _memberService.GetByIdAsync(memberId, ct);
        if (!member.IsSuccess)
            return ToActionResult(member);
        if (!string.Equals(member.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Member not found in the routed project" });
        if (!await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _memberService.UpdateAsync(memberId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{projectId}/members/{memberId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> RemoveMember(string projectId, string memberId, CancellationToken ct)
    {
        var member = await _memberService.GetByIdAsync(memberId, ct);
        if (!member.IsSuccess)
            return ToActionResult(member);
        if (!string.Equals(member.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Member not found in the routed project" });
        if (!await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _memberService.DeleteAsync(memberId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== PROJECT APPLICATIONS =====
    [HttpGet("{projectId}/applications")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetApplications(string projectId, CancellationToken ct)
    {
        if (!await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _applicationService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{projectId}/applications/{applicationId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetApplicationById(string projectId, string applicationId, CancellationToken ct)
    {
        var result = await _applicationService.GetByIdAsync(applicationId, ct);
        if (!result.IsSuccess)
            return ToActionResult(result);
        if (!string.Equals(result.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Application not found in the routed project" });
        if (!IsAdmin &&
            !string.Equals(result.Value.UserId, ActorId, StringComparison.Ordinal) &&
            !await CanManageProjectAsync(projectId, ct))
            return Forbid();

        return ToActionResult(result);
    }

    [HttpPost("{projectId}/applications")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> SubmitApplication(string projectId, [FromBody] CreateProjectApplicationDto dto, CancellationToken ct)
    {
        if (!string.Equals(
                await _hierarchyAuthorization.ResolveCommentTargetProjectAsync(
                    CommentTargetType.PROJECT,
                    projectId,
                    ct),
                projectId,
                StringComparison.Ordinal))
            return NotFound(new { error = "Project not found" });

        var dtoWithProject = dto with { ProjectId = projectId, UserId = ActorId };
        var result = await _applicationService.CreateAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetApplicationById), new { projectId, applicationId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{projectId}/applications/{applicationId}/accept")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> AcceptApplication(string projectId, string applicationId, [FromBody] ReviewProjectApplicationDto? dto, CancellationToken ct)
    {
        var application = await _applicationService.GetByIdAsync(applicationId, ct);
        if (!application.IsSuccess)
            return ToActionResult(application);
        if (!string.Equals(application.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Application not found in the routed project" });
        if (!await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _applicationService.AcceptAsync(applicationId, dto?.ReviewMessage, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/applications/{applicationId}/reject")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> RejectApplication(string projectId, string applicationId, [FromBody] ReviewProjectApplicationDto? dto, CancellationToken ct)
    {
        var application = await _applicationService.GetByIdAsync(applicationId, ct);
        if (!application.IsSuccess)
            return ToActionResult(application);
        if (!string.Equals(application.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Application not found in the routed project" });
        if (!await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _applicationService.RejectAsync(applicationId, dto?.ReviewMessage, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/applications/{applicationId}/withdraw")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> WithdrawApplication(string projectId, string applicationId, CancellationToken ct)
    {
        var application = await _applicationService.GetByIdAsync(applicationId, ct);
        if (!application.IsSuccess)
            return ToActionResult(application);
        if (!string.Equals(application.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Application not found in the routed project" });
        if (!IsAdmin &&
            !string.Equals(application.Value.UserId, ActorId, StringComparison.Ordinal) &&
            !await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _applicationService.WithdrawAsync(applicationId, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{projectId}/applications/{applicationId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> DeleteApplication(string projectId, string applicationId, CancellationToken ct)
    {
        var application = await _applicationService.GetByIdAsync(applicationId, ct);
        if (!application.IsSuccess)
            return ToActionResult(application);
        if (!string.Equals(application.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Application not found in the routed project" });
        if (!IsAdmin &&
            !string.Equals(application.Value.UserId, ActorId, StringComparison.Ordinal) &&
            !await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _applicationService.DeleteAsync(applicationId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== PROJECT COMMENTS =====
    [HttpGet("{projectId}/comments")]
    public async Task<IActionResult> GetComments(string projectId, CancellationToken ct)
    {
        var result = await _commentService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{projectId}/comments/{commentId}")]
    public async Task<IActionResult> GetCommentById(string projectId, string commentId, CancellationToken ct)
    {
        var result = await _commentService.GetByIdAsync(commentId, ct);
        if (result.IsSuccess &&
            !string.Equals(result.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Comment not found in the routed project" });
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/comments")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CreateComment(string projectId, [FromBody] CreateProjectCommentDto dto, CancellationToken ct)
    {
        var targetId = dto.TargetId ?? projectId;
        var targetProjectId = await _hierarchyAuthorization.ResolveCommentTargetProjectAsync(
            dto.TargetType,
            targetId,
            ct);
        if (!string.Equals(targetProjectId, projectId, StringComparison.Ordinal))
            return BadRequest(new { error = "Comment target does not belong to the routed project" });

        if (dto.ParentId is not null)
        {
            var parent = await _commentService.GetByIdAsync(dto.ParentId, ct);
            if (!parent.IsSuccess ||
                !string.Equals(parent.Value!.ProjectId, projectId, StringComparison.Ordinal) ||
                parent.Value.TargetType != dto.TargetType ||
                !string.Equals(parent.Value.TargetId, targetId, StringComparison.Ordinal))
                return BadRequest(new { error = "Reply parent does not match this comment target" });
        }

        var dtoWithProject = dto with
        {
            ProjectId = projectId,
            UserId = ActorId,
            TargetId = targetId
        };
        var result = await _commentService.CreateAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetCommentById), new { projectId, commentId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{projectId}/comments/{commentId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdateComment(string projectId, string commentId, [FromBody] UpdateCommentRequest request, CancellationToken ct)
    {
        var comment = await _commentService.GetByIdAsync(commentId, ct);
        if (!comment.IsSuccess)
            return ToActionResult(comment);
        if (!await MayModifyCommentAsync(comment.Value!, projectId, ct))
            return Forbid();

        var result = await _commentService.UpdateAsync(commentId, request.Content, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{projectId}/comments/{commentId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> DeleteComment(string projectId, string commentId, CancellationToken ct)
    {
        var comment = await _commentService.GetByIdAsync(commentId, ct);
        if (!comment.IsSuccess)
            return ToActionResult(comment);
        if (!await MayModifyCommentAsync(comment.Value!, projectId, ct))
            return Forbid();

        var result = await _commentService.DeleteAsync(commentId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpGet("comments/target/{targetType}/{targetId}")]
    public async Task<IActionResult> GetCommentsByTarget(string targetType, string targetId, CancellationToken ct)
    {
        if (!Enum.TryParse<CommentTargetType>(targetType, true, out var parsedType))
            return BadRequest(new { error = $"Invalid target type: {targetType}" });

        var result = await _commentService.GetByTargetAsync(parsedType, targetId, ct);
        return ToActionResult(result);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private bool IsAdmin => User.IsInRole(UserRole.ADMIN.ToString());

    private Task<bool> CanManageProjectAsync(string projectId, CancellationToken ct)
        => IsAdmin
            ? Task.FromResult(true)
            : _hierarchyAuthorization.CanManageProjectAsync(ActorId, projectId, ct);

    private async Task<bool> MayManageMilestoneAsync(
        string projectId,
        string milestoneId,
        CancellationToken ct)
    {
        var milestone = await _milestoneService.GetByIdAsync(milestoneId, ct);
        return milestone.IsSuccess &&
               string.Equals(milestone.Value!.ProjectId, projectId, StringComparison.Ordinal) &&
               await CanManageProjectAsync(projectId, ct);
    }

    private async Task<bool> MayModifyCommentAsync(
        ProjectCommentDto comment,
        string routedProjectId,
        CancellationToken ct)
    {
        if (!string.Equals(comment.ProjectId, routedProjectId, StringComparison.Ordinal))
            return false;
        if (IsAdmin || string.Equals(comment.UserId, ActorId, StringComparison.Ordinal))
            return true;
        return await _hierarchyAuthorization.CanManageProjectAsync(
            ActorId,
            comment.ProjectId,
            ct);
    }

    // ===== PROJECT UPDATES =====
    [HttpGet("{projectId}/updates")]
    public async Task<IActionResult> GetUpdates(string projectId, CancellationToken ct)
    {
        var result = await _updateService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{projectId}/updates/{updateId}")]
    public async Task<IActionResult> GetUpdateById(string projectId, string updateId, CancellationToken ct)
    {
        var result = await _updateService.GetByIdAsync(updateId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/updates")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CreateUpdate(string projectId, [FromBody] CreateProjectUpdateDto dto, CancellationToken ct)
    {
        if (!IsAdmin &&
            !await _hierarchyAuthorization.IsProjectMemberAsync(ActorId, projectId, ct))
            return Forbid();

        var dtoWithProject = dto with { ProjectId = projectId, UserId = ActorId };
        var result = await _updateService.CreateAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetUpdateById), new { projectId, updateId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpDelete("{projectId}/updates/{updateId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> DeleteUpdate(string projectId, string updateId, CancellationToken ct)
    {
        var update = await _updateService.GetByIdAsync(updateId, ct);
        if (!update.IsSuccess)
            return ToActionResult(update);
        if (!string.Equals(update.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Update not found in the routed project" });
        if (!IsAdmin &&
            !string.Equals(update.Value.UserId, ActorId, StringComparison.Ordinal) &&
            !await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _updateService.DeleteAsync(updateId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== PROJECT SUPPORT =====
    [HttpGet("{projectId}/support")]
    public async Task<IActionResult> GetSupport(string projectId, CancellationToken ct)
    {
        var result = await _supportService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{projectId}/support/{supportId}")]
    public async Task<IActionResult> GetSupportById(string projectId, string supportId, CancellationToken ct)
    {
        var result = await _supportService.GetByIdAsync(supportId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/support")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CreateSupport(string projectId, [FromBody] CreateProjectSupportDto dto, CancellationToken ct)
    {
        if (!string.Equals(
                await _hierarchyAuthorization.ResolveCommentTargetProjectAsync(
                    CommentTargetType.PROJECT,
                    projectId,
                    ct),
                projectId,
                StringComparison.Ordinal))
            return NotFound(new { error = "Project not found" });

        var dtoWithProject = dto with { ProjectId = projectId, UserId = ActorId };
        var result = await _supportService.CreateAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetSupportById), new { projectId, supportId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{projectId}/support/{supportId}/toggle")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> ToggleSupport(string projectId, string supportId, CancellationToken ct)
    {
        var support = await _supportService.GetByIdAsync(supportId, ct);
        if (!support.IsSuccess)
            return ToActionResult(support);
        if (!string.Equals(support.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Support record not found in the routed project" });
        if (!IsAdmin &&
            !string.Equals(support.Value.UserId, ActorId, StringComparison.Ordinal) &&
            !await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _supportService.ToggleActiveAsync(supportId, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{projectId}/support/{supportId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> DeleteSupport(string projectId, string supportId, CancellationToken ct)
    {
        var support = await _supportService.GetByIdAsync(supportId, ct);
        if (!support.IsSuccess)
            return ToActionResult(support);
        if (!string.Equals(support.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Support record not found in the routed project" });
        if (!IsAdmin &&
            !string.Equals(support.Value.UserId, ActorId, StringComparison.Ordinal) &&
            !await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _supportService.DeleteAsync(supportId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== PROJECT PROPOSALS =====
    [HttpGet("{projectId}/proposals")]
    public async Task<IActionResult> GetProposals(string projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _governanceService.SearchProposalsAsync(null, null, null, projectId, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("{projectId}/proposals/{proposalId}")]
    public async Task<IActionResult> GetProposalById(string projectId, string proposalId, CancellationToken ct)
    {
        var result = await _governanceService.GetProposalByIdAsync(proposalId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/proposals")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CreateProposal(string projectId, [FromBody] CreateProposalDto dto, CancellationToken ct)
    {
        if (!IsAdmin &&
            !await _hierarchyAuthorization.IsProjectMemberAsync(ActorId, projectId, ct))
            return Forbid();

        var dtoWithProject = dto with { ProjectId = projectId, CreatorId = ActorId };
        var result = await _governanceService.CreateProposalAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetProposalById), new { projectId, proposalId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{projectId}/proposals/{proposalId}/execute")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> ExecuteProposal(string projectId, string proposalId, CancellationToken ct)
    {
        var proposal = await _governanceService.GetProposalByIdAsync(proposalId, ct);
        if (!proposal.IsSuccess)
            return ToActionResult(proposal);
        if (!string.Equals(proposal.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Proposal not found in the routed project" });
        if (!await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _governanceService.ExecuteProposalAsync(proposalId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/proposals/{proposalId}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CancelProposal(string projectId, string proposalId, CancellationToken ct)
    {
        var proposal = await _governanceService.GetProposalByIdAsync(proposalId, ct);
        if (!proposal.IsSuccess)
            return ToActionResult(proposal);
        if (!string.Equals(proposal.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Proposal not found in the routed project" });
        if (!IsAdmin &&
            !string.Equals(proposal.Value.CreatorId, ActorId, StringComparison.Ordinal) &&
            !await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _governanceService.CancelProposalAsync(proposalId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/proposals/{proposalId}/publish")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> PublishProposal(string projectId, string proposalId, CancellationToken ct)
    {
        var proposal = await _governanceService.GetProposalByIdAsync(proposalId, ct);
        if (!proposal.IsSuccess)
            return ToActionResult(proposal);
        if (!string.Equals(proposal.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Proposal not found in the routed project" });
        if (!IsAdmin &&
            !string.Equals(proposal.Value.CreatorId, ActorId, StringComparison.Ordinal) &&
            !await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _governanceService.PublishProposalAsync(proposalId, ct);
        return ToActionResult(result);
    }

    [HttpPut("{projectId}/proposals/{proposalId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdateProposal(string projectId, string proposalId, [FromBody] UpdateProposalDto dto, CancellationToken ct)
    {
        var proposal = await _governanceService.GetProposalByIdAsync(proposalId, ct);
        if (!proposal.IsSuccess)
            return ToActionResult(proposal);
        if (!string.Equals(proposal.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Proposal not found in the routed project" });
        if (!IsAdmin &&
            !string.Equals(proposal.Value.CreatorId, ActorId, StringComparison.Ordinal) &&
            !await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _governanceService.UpdateProposalAsync(proposalId, dto, ct);
        return ToActionResult(result);
    }

    [HttpGet("{projectId}/proposals/{proposalId}/comments")]
    public async Task<IActionResult> GetProposalComments(string projectId, string proposalId, CancellationToken ct)
    {
        var result = await _governanceService.GetProposalCommentsAsync(proposalId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/proposals/{proposalId}/comments")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CreateProposalComment(string projectId, string proposalId, [FromBody] CreateProposalCommentDto dto, CancellationToken ct)
    {
        var proposal = await _governanceService.GetProposalByIdAsync(proposalId, ct);
        if (!proposal.IsSuccess)
            return ToActionResult(proposal);
        if (!string.Equals(proposal.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Proposal not found in the routed project" });

        var dtoWithProposal = dto with { ProposalId = proposalId, UserId = ActorId };
        var result = await _governanceService.CreateProposalCommentAsync(dtoWithProposal, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : ToActionResult(result);
    }

    [HttpGet("{projectId}/proposals/{proposalId}/summary")]
    public async Task<IActionResult> GetProposalVoteSummary(string projectId, string proposalId, CancellationToken ct)
    {
        var result = await _governanceService.GetVoteSummaryAsync(proposalId, ct);
        return ToActionResult(result);
    }

    // ===== PROJECT VOTES =====
    [HttpGet("{projectId}/proposals/{proposalId}/votes")]
    public async Task<IActionResult> GetVotes(string projectId, string proposalId, CancellationToken ct)
    {
        var result = await _governanceService.GetVotesAsync(proposalId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/proposals/{proposalId}/votes")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CastVote(string projectId, string proposalId, [FromBody] CastVoteDto dto, CancellationToken ct)
    {
        var proposal = await _governanceService.GetProposalByIdAsync(proposalId, ct);
        if (!proposal.IsSuccess)
            return ToActionResult(proposal);
        if (!string.Equals(proposal.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Proposal not found in the routed project" });
        if (!IsAdmin &&
            !await _hierarchyAuthorization.IsProjectMemberAsync(ActorId, projectId, ct))
            return Forbid();

        var credential = await _membershipCredentialService.GetByProjectAndUserAsync(
            projectId,
            ActorId,
            ct);
        if (!IsAdmin &&
            (!credential.IsSuccess ||
             !string.Equals(credential.Value!.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase)))
            return Forbid();

        var result = await _governanceService.CastVoteAsync(
            proposalId,
            dto with { VoterId = ActorId, Weight = null },
            ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : ToActionResult(result);
    }

    // ===== PROJECT INVITATIONS =====
    [HttpGet("{projectId}/invitations")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetInvitations(string projectId, CancellationToken ct)
    {
        if (!await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _invitationService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/invitations")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CreateInvitation(string projectId, [FromBody] CreateProjectInvitationDto dto, CancellationToken ct)
    {
        if (!await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var dtoWithProject = dto with { ProjectId = projectId, InvitedById = ActorId };
        var result = await _invitationService.CreateAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetInvitationById), new { projectId, invitationId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpGet("{projectId}/invitations/{invitationId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetInvitationById(string projectId, string invitationId, CancellationToken ct)
    {
        var result = await _invitationService.GetByIdAsync(invitationId, ct);
        if (!result.IsSuccess)
            return ToActionResult(result);
        if (!string.Equals(result.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Invitation not found in the routed project" });
        if (!IsAdmin &&
            !string.Equals(result.Value.InvitedUserId, ActorId, StringComparison.Ordinal) &&
            !string.Equals(result.Value.InvitedById, ActorId, StringComparison.Ordinal) &&
            !await CanManageProjectAsync(projectId, ct))
            return Forbid();

        return ToActionResult(result);
    }

    [HttpPost("{projectId}/invitations/{invitationId}/accept")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> AcceptInvitation(string projectId, string invitationId, CancellationToken ct)
    {
        var invitation = await _invitationService.GetByIdAsync(invitationId, ct);
        if (!invitation.IsSuccess)
            return ToActionResult(invitation);
        if (!string.Equals(invitation.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Invitation not found in the routed project" });
        if (!IsAdmin &&
            !string.Equals(invitation.Value.InvitedUserId, ActorId, StringComparison.Ordinal))
            return Forbid();

        var result = await _invitationService.AcceptAsync(invitationId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/invitations/{invitationId}/reject")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> RejectInvitation(string projectId, string invitationId, CancellationToken ct)
    {
        var invitation = await _invitationService.GetByIdAsync(invitationId, ct);
        if (!invitation.IsSuccess)
            return ToActionResult(invitation);
        if (!string.Equals(invitation.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Invitation not found in the routed project" });
        if (!IsAdmin &&
            !string.Equals(invitation.Value.InvitedUserId, ActorId, StringComparison.Ordinal))
            return Forbid();

        var result = await _invitationService.RejectAsync(invitationId, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{projectId}/invitations/{invitationId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> DeleteInvitation(string projectId, string invitationId, CancellationToken ct)
    {
        var invitation = await _invitationService.GetByIdAsync(invitationId, ct);
        if (!invitation.IsSuccess)
            return ToActionResult(invitation);
        if (!string.Equals(invitation.Value!.ProjectId, projectId, StringComparison.Ordinal))
            return NotFound(new { error = "Invitation not found in the routed project" });
        if (!IsAdmin &&
            !string.Equals(invitation.Value.InvitedById, ActorId, StringComparison.Ordinal) &&
            !await CanManageProjectAsync(projectId, ct))
            return Forbid();

        var result = await _invitationService.DeleteAsync(invitationId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== USER INVITATIONS (cross-project) =====
    [HttpGet("/api/project-invitations/user/{userId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetUserInvitations(string userId, CancellationToken ct)
    {
        if (!IsAdmin && !string.Equals(userId, ActorId, StringComparison.Ordinal))
            return Forbid();

        var result = await _invitationService.GetByUserIdAsync(userId, ct);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => Forbid(),
            _ => BadRequest(new { error = result.Error })
        };
    }
}

public record UpdateCommentRequest
{
    public required string Content { get; init; }
}
