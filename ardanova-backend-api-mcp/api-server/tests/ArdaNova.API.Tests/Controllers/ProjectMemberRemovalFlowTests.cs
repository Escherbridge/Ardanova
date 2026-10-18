namespace ArdaNova.API.Tests.Controllers;

using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Mappings;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskStatus = ArdaNova.Domain.Models.Enums.TaskStatus;

/// <summary>
/// DELETE /projects/{projectId}/members/{memberId} is the only guard on member removal: the tRPC
/// router is a thin proxy. Drives the real ProjectMemberService over list-backed rows.
/// </summary>
public sealed class ProjectMemberRemovalFlowTests
{
    private const string ProjectId = "project-1";
    private const string OtherProjectId = "project-2";
    private const string Founder = "founder-1";
    private const string Leader = "leader-1";
    private const string Core = "core-1";
    private const string Contributor = "contributor-1";
    private const string Stranger = "stranger-1";

    private static readonly HashSet<string> Managers = [Founder, Leader, Core];

    private readonly ListRepository<ProjectMember> _members = new();
    private readonly ListRepository<ProjectTask> _tasks = new();
    private readonly ProjectMemberService _memberService;

    public ProjectMemberRemovalFlowTests()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
            MappingSecurityPolicy.Apply(cfg);
        }).CreateMapper();

        _members.Items.AddRange(
        [
            Member("founder-row", ProjectId, Founder, ProjectRole.FOUNDER),
            Member("leader-row", ProjectId, Leader, ProjectRole.LEADER),
            Member("core-row", ProjectId, Core, ProjectRole.CORE_CONTRIBUTOR),
            Member("contributor-row", ProjectId, Contributor, ProjectRole.CONTRIBUTOR),
            Member("elsewhere-row", OtherProjectId, Contributor, ProjectRole.CONTRIBUTOR),
        ]);
        _tasks.Items.AddRange(
        [
            AssignedTask("open-task", Contributor, TaskStatus.IN_PROGRESS),
            AssignedTask("done-task", Contributor, TaskStatus.COMPLETED),
        ]);

        _memberService = new ProjectMemberService(
            _members, new ListRepository<User>(), _tasks, new Mock<IUnitOfWork>().Object, mapper);
    }

    [Fact]
    public async Task AMember_CanLeave_AndTheirUnfinishedWorkReturnsToThePool()
    {
        var result = await RemoveAs(Contributor, "contributor-row");

        result.Should().BeOfType<NoContentResult>();
        RowIds().Should().NotContain("contributor-row");
        _tasks.Items.Single(t => t.id == "open-task").assignedToId.Should().BeNull();
        _tasks.Items.Single(t => t.id == "done-task").assignedToId.Should().Be(Contributor);
    }

    [Fact]
    public async Task AManager_RemovesAMemberRankedBelowThem()
    {
        var result = await RemoveAs(Leader, "core-row");

        result.Should().BeOfType<NoContentResult>();
        RowIds().Should().NotContain("core-row");
    }

    [Fact]
    public async Task ALowerRankedManager_CannotRemoveAHigherRankedMember()
    {
        var result = await RemoveAs(Core, "leader-row");

        var refusal = result.Should().BeOfType<ObjectResult>().Subject;
        refusal.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        refusal.Value.Should().BeEquivalentTo(new { error = ProjectMemberRolePolicy.OutranksActor });
        RowIds().Should().Contain("leader-row");
    }

    [Fact]
    public async Task AMemberRowOfAnotherProject_IsNotFoundUnderThisProjectsRoute()
    {
        // The founder manages project-1, but elsewhere-row belongs to project-2.
        var result = await RemoveAs(Founder, "elsewhere-row");

        result.Should().BeOfType<NotFoundObjectResult>();
        RowIds().Should().Contain("elsewhere-row");
    }

    [Fact]
    public async Task ANonMember_CannotRemoveAnyone()
    {
        var result = await RemoveAs(Stranger, "contributor-row");

        result.Should().BeOfType<ForbidResult>();
        RowIds().Should().Contain("contributor-row");
        _tasks.Items.Single(t => t.id == "open-task").assignedToId.Should().Be(Contributor);
    }

    private Task<IActionResult> RemoveAs(string actorId, string memberId)
        => Controller(actorId).RemoveMember(ProjectId, memberId, CancellationToken.None);

    private IEnumerable<string> RowIds() => _members.Items.Select(m => m.id);

    private ProjectsController Controller(string actorId)
    {
        var hierarchy = new Mock<IHierarchyAuthorizationService>(MockBehavior.Strict);
        hierarchy.Setup(h => h.CanManageProjectAsync(It.IsAny<string>(), ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string actor, string _, CancellationToken _) => Managers.Contains(actor));
        var projects = new Mock<IProjectService>(MockBehavior.Strict);
        projects.Setup(p => p.GetByIdAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectDto>.Success(new ProjectDto { Id = ProjectId, CreatedById = Founder }));

        var controller = new ProjectsController(
            projects.Object,
            VisibleProject(),
            new Mock<IProjectResourceService>(MockBehavior.Strict).Object,
            new Mock<IProjectMilestoneService>(MockBehavior.Strict).Object,
            _memberService,
            new Mock<IProjectApplicationService>(MockBehavior.Strict).Object,
            new Mock<IProjectCommentService>(MockBehavior.Strict).Object,
            new Mock<IProjectUpdateService>(MockBehavior.Strict).Object,
            new Mock<IProjectSupportService>(MockBehavior.Strict).Object,
            new Mock<IGovernanceService>(MockBehavior.Strict).Object,
            new Mock<IProposalVoteClosing>(MockBehavior.Strict).Object,
            new Mock<IProjectInvitationService>(MockBehavior.Strict).Object,
            hierarchy.Object,
            new Mock<IMembershipCredentialService>(MockBehavior.Strict).Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, actorId)],
                    ActorAssertionMiddleware.AuthenticationType)),
            },
        };
        return controller;
    }

    /// <summary>project-1 is published, so a refusal there is a plain 403 rather than a hidden-project 404.</summary>
    private static IProjectVisibility VisibleProject()
    {
        var visibility = new Mock<IProjectVisibility>(MockBehavior.Strict);
        visibility.Setup(v => v.GetVisibleByIdAsync(It.IsAny<ProjectViewer>(), ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectDto>.Success(new ProjectDto { Id = ProjectId, CreatedById = Founder }));
        return visibility.Object;
    }

    private static ProjectMember Member(string id, string projectId, string userId, ProjectRole role) => new()
    {
        id = id,
        projectId = projectId,
        userId = userId,
        role = role,
        joinedAt = DateTime.UtcNow,
    };

    private static ProjectTask AssignedTask(string id, string assigneeId, TaskStatus status) => new()
    {
        id = id,
        projectId = ProjectId,
        title = id,
        assignedToId = assigneeId,
        status = status,
        priority = TaskPriority.MEDIUM,
        taskType = TaskType.FEATURE,
        escrowStatus = EscrowStatus.NONE,
        createdAt = DateTime.UtcNow,
        updatedAt = DateTime.UtcNow,
    };
}
