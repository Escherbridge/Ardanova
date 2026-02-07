namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
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

    public ProjectsController(
        IProjectService projectService,
        IProjectResourceService resourceService,
        IProjectMilestoneService milestoneService,
        IProjectMemberService memberService,
        IProjectApplicationService applicationService,
        IProjectCommentService commentService,
        IProjectUpdateService updateService,
        IProjectSupportService supportService,
        IGovernanceService governanceService)
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
    public async Task<IActionResult> Create([FromBody] CreateProjectDto dto, CancellationToken ct)
    {
        var result = await _projectService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateProjectDto dto, CancellationToken ct)
    {
        var result = await _projectService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _projectService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id}/publish")]
    public async Task<IActionResult> Publish(string id, CancellationToken ct)
    {
        var result = await _projectService.PublishAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/featured")]
    public async Task<IActionResult> SetFeatured(string id, [FromQuery] bool featured, CancellationToken ct)
    {
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
    public async Task<IActionResult> CreateResource(string projectId, [FromBody] CreateProjectResourceDto dto, CancellationToken ct)
    {
        var dtoWithProject = dto with { ProjectId = projectId };
        var result = await _resourceService.CreateAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetResourceById), new { projectId, resourceId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{projectId}/resources/{resourceId}")]
    public async Task<IActionResult> UpdateResource(string projectId, string resourceId, [FromBody] UpdateProjectResourceDto dto, CancellationToken ct)
    {
        var result = await _resourceService.UpdateAsync(resourceId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{projectId}/resources/{resourceId}")]
    public async Task<IActionResult> DeleteResource(string projectId, string resourceId, CancellationToken ct)
    {
        var result = await _resourceService.DeleteAsync(resourceId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{projectId}/resources/{resourceId}/obtained")]
    public async Task<IActionResult> MarkResourceObtained(string projectId, string resourceId, [FromQuery] bool obtained, CancellationToken ct)
    {
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
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/milestones")]
    public async Task<IActionResult> CreateMilestone(string projectId, [FromBody] CreateProjectMilestoneDto dto, CancellationToken ct)
    {
        var dtoWithProject = dto with { ProjectId = projectId };
        var result = await _milestoneService.CreateAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMilestoneById), new { projectId, milestoneId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{projectId}/milestones/{milestoneId}")]
    public async Task<IActionResult> UpdateMilestone(string projectId, string milestoneId, [FromBody] UpdateProjectMilestoneDto dto, CancellationToken ct)
    {
        var result = await _milestoneService.UpdateAsync(milestoneId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{projectId}/milestones/{milestoneId}")]
    public async Task<IActionResult> DeleteMilestone(string projectId, string milestoneId, CancellationToken ct)
    {
        var result = await _milestoneService.DeleteAsync(milestoneId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{projectId}/milestones/{milestoneId}/complete")]
    public async Task<IActionResult> CompleteMilestone(string projectId, string milestoneId, CancellationToken ct)
    {
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
    public async Task<IActionResult> AddMember(string projectId, [FromBody] CreateProjectMemberDto dto, CancellationToken ct)
    {
        var dtoWithProject = dto with { ProjectId = projectId };
        var result = await _memberService.CreateAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMemberById), new { projectId, memberId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{projectId}/members/{memberId}")]
    public async Task<IActionResult> UpdateMember(string projectId, string memberId, [FromBody] UpdateProjectMemberDto dto, CancellationToken ct)
    {
        var result = await _memberService.UpdateAsync(memberId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{projectId}/members/{memberId}")]
    public async Task<IActionResult> RemoveMember(string projectId, string memberId, CancellationToken ct)
    {
        var result = await _memberService.DeleteAsync(memberId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== PROJECT APPLICATIONS =====
    [HttpGet("{projectId}/applications")]
    public async Task<IActionResult> GetApplications(string projectId, CancellationToken ct)
    {
        var result = await _applicationService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{projectId}/applications/{applicationId}")]
    public async Task<IActionResult> GetApplicationById(string projectId, string applicationId, CancellationToken ct)
    {
        var result = await _applicationService.GetByIdAsync(applicationId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/applications")]
    public async Task<IActionResult> SubmitApplication(string projectId, [FromBody] CreateProjectApplicationDto dto, CancellationToken ct)
    {
        var dtoWithProject = dto with { ProjectId = projectId };
        var result = await _applicationService.CreateAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetApplicationById), new { projectId, applicationId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{projectId}/applications/{applicationId}/accept")]
    public async Task<IActionResult> AcceptApplication(string projectId, string applicationId, [FromBody] ReviewProjectApplicationDto? dto, CancellationToken ct)
    {
        var result = await _applicationService.AcceptAsync(applicationId, dto?.ReviewMessage, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/applications/{applicationId}/reject")]
    public async Task<IActionResult> RejectApplication(string projectId, string applicationId, [FromBody] ReviewProjectApplicationDto? dto, CancellationToken ct)
    {
        var result = await _applicationService.RejectAsync(applicationId, dto?.ReviewMessage, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/applications/{applicationId}/withdraw")]
    public async Task<IActionResult> WithdrawApplication(string projectId, string applicationId, CancellationToken ct)
    {
        var result = await _applicationService.WithdrawAsync(applicationId, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{projectId}/applications/{applicationId}")]
    public async Task<IActionResult> DeleteApplication(string projectId, string applicationId, CancellationToken ct)
    {
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
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/comments")]
    public async Task<IActionResult> CreateComment(string projectId, [FromBody] CreateProjectCommentDto dto, CancellationToken ct)
    {
        var dtoWithProject = dto with { ProjectId = projectId };
        var result = await _commentService.CreateAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetCommentById), new { projectId, commentId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{projectId}/comments/{commentId}")]
    public async Task<IActionResult> UpdateComment(string projectId, string commentId, [FromBody] UpdateCommentRequest request, CancellationToken ct)
    {
        var result = await _commentService.UpdateAsync(commentId, request.Content, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{projectId}/comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(string projectId, string commentId, CancellationToken ct)
    {
        var result = await _commentService.DeleteAsync(commentId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
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
    public async Task<IActionResult> CreateUpdate(string projectId, [FromBody] CreateProjectUpdateDto dto, CancellationToken ct)
    {
        var dtoWithProject = dto with { ProjectId = projectId };
        var result = await _updateService.CreateAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetUpdateById), new { projectId, updateId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpDelete("{projectId}/updates/{updateId}")]
    public async Task<IActionResult> DeleteUpdate(string projectId, string updateId, CancellationToken ct)
    {
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
    public async Task<IActionResult> CreateSupport(string projectId, [FromBody] CreateProjectSupportDto dto, CancellationToken ct)
    {
        var dtoWithProject = dto with { ProjectId = projectId };
        var result = await _supportService.CreateAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetSupportById), new { projectId, supportId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{projectId}/support/{supportId}/toggle")]
    public async Task<IActionResult> ToggleSupport(string projectId, string supportId, CancellationToken ct)
    {
        var result = await _supportService.ToggleActiveAsync(supportId, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{projectId}/support/{supportId}")]
    public async Task<IActionResult> DeleteSupport(string projectId, string supportId, CancellationToken ct)
    {
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
    public async Task<IActionResult> CreateProposal(string projectId, [FromBody] CreateProposalDto dto, CancellationToken ct)
    {
        var dtoWithProject = dto with { ProjectId = projectId };
        var result = await _governanceService.CreateProposalAsync(dtoWithProject, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetProposalById), new { projectId, proposalId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{projectId}/proposals/{proposalId}/execute")]
    public async Task<IActionResult> ExecuteProposal(string projectId, string proposalId, CancellationToken ct)
    {
        var result = await _governanceService.ExecuteProposalAsync(proposalId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/proposals/{proposalId}/cancel")]
    public async Task<IActionResult> CancelProposal(string projectId, string proposalId, CancellationToken ct)
    {
        var result = await _governanceService.CancelProposalAsync(proposalId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{projectId}/proposals/{proposalId}/publish")]
    public async Task<IActionResult> PublishProposal(string projectId, string proposalId, CancellationToken ct)
    {
        var result = await _governanceService.PublishProposalAsync(proposalId, ct);
        return ToActionResult(result);
    }

    [HttpPut("{projectId}/proposals/{proposalId}")]
    public async Task<IActionResult> UpdateProposal(string projectId, string proposalId, [FromBody] UpdateProposalDto dto, CancellationToken ct)
    {
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
    public async Task<IActionResult> CreateProposalComment(string projectId, string proposalId, [FromBody] CreateProposalCommentDto dto, CancellationToken ct)
    {
        var dtoWithProposal = dto with { ProposalId = proposalId };
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
    public async Task<IActionResult> CastVote(string projectId, string proposalId, [FromBody] CastVoteDto dto, CancellationToken ct)
    {
        var result = await _governanceService.CastVoteAsync(proposalId, dto, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : ToActionResult(result);
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
