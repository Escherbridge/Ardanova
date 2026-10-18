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

/// <summary>
/// What /api/projects tells a viewer about a team and a hidden project: member emails only to managers,
/// a member read bound to its routed project, and refused writes on a hidden project worded as a missing one.
/// </summary>
public sealed class ProjectMemberDisclosureFlowTests
{
    private const string PublicProject = "project-public";
    private const string HiddenProject = "project-hidden";
    private const string Founder = "founder-1";
    private const string Outsider = "outsider-1";

    private readonly ListRepository<ProjectMember> _members = new();
    private readonly ListRepository<User> _users = new();
    private readonly ProjectMemberService _memberService;

    public ProjectMemberDisclosureFlowTests()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
            MappingSecurityPolicy.Apply(cfg);
        }).CreateMapper();

        _users.Items.AddRange(
        [
            new User { id = Founder, email = "founder@example.test", name = "Founder" },
            new User { id = "hidden-member", email = "hidden@example.test", name = "Hidden" },
        ]);
        _members.Items.AddRange(
        [
            new ProjectMember { id = "public-row", projectId = PublicProject, userId = Founder, role = ProjectRole.FOUNDER, joinedAt = DateTime.UtcNow },
            new ProjectMember { id = "hidden-row", projectId = HiddenProject, userId = "hidden-member", role = ProjectRole.CONTRIBUTOR, joinedAt = DateTime.UtcNow },
        ]);
        _memberService = new ProjectMemberService(
            _members, _users, new ListRepository<ProjectTask>(), new Mock<IUnitOfWork>().Object, mapper);
    }

    [Fact]
    public async Task TheMembersList_ShowsEmailsToManagersOnly()
    {
        var anonymous = await Controller(actorId: null).GetMembers(PublicProject, CancellationToken.None);
        var outsider = await Controller(Outsider).GetMembers(PublicProject, CancellationToken.None);
        var founder = await Controller(Founder).GetMembers(PublicProject, CancellationToken.None);

        Emails(anonymous).Should().Equal((string?)null);
        Emails(outsider).Should().Equal((string?)null);
        Emails(founder).Should().Equal("founder@example.test");
    }

    [Fact]
    public async Task AMemberOfAHiddenProject_CannotBeReadThroughAPublicProjectsRoute()
    {
        var crossProject = await Controller(actorId: null).GetMemberById(PublicProject, "hidden-row", CancellationToken.None);

        // Worded exactly as the member service words a missing id, with nothing about the row itself.
        crossProject.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new { error = "Member with id hidden-row not found" });
    }

    public static TheoryData<string> ProjectWrites => ["update", "publish", "delete", "addMember"];

    [Theory]
    [MemberData(nameof(ProjectWrites))]
    public async Task AnOutsidersWriteToAHiddenProject_ReadsExactlyLikeAMissingProject(string write)
    {
        var hidden = await InvokeWrite(write, HiddenProject);
        var missing = await InvokeWrite(write, HiddenProject, hiddenProjectExists: false);

        var hiddenBody = hidden.Should().BeOfType<NotFoundObjectResult>().Which.Value;
        missing.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().BeEquivalentTo(hiddenBody);
    }

    [Theory]
    [MemberData(nameof(ProjectWrites))]
    public async Task AnOutsidersWriteToAPublicProject_IsStillRefusedWith403(string write)
    {
        var result = await InvokeWrite(write, PublicProject);

        result.Should().Match<IActionResult>(r =>
            r is ForbidResult || (r is ObjectResult && ((ObjectResult)r).StatusCode == StatusCodes.Status403Forbidden));
    }

    private Task<IActionResult> InvokeWrite(string write, string projectId, bool hiddenProjectExists = true)
    {
        var controller = Controller(Outsider, hiddenProjectExists);
        var ct = CancellationToken.None;
        return write switch
        {
            "update" => controller.Update(projectId, new UpdateProjectDto { Title = "x" }, ct),
            "publish" => controller.Publish(projectId, ct),
            "delete" => controller.Delete(projectId, ct),
            "addMember" => controller.AddMember(projectId, new CreateProjectMemberDto { UserId = Outsider, Role = ProjectRole.CONTRIBUTOR }, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(write)),
        };
    }

    private static IEnumerable<string?> Emails(IActionResult result)
        => result.Should().BeOfType<OkObjectResult>().Which.Value
            .Should().BeAssignableTo<IEnumerable<ProjectMemberDto>>().Which
            .Select(member => member.User?.Email);

    private ProjectsController Controller(string? actorId, bool hiddenProjectExists = true)
    {
        // Strict: no project or member mutation may run on any of these paths.
        var projects = new Mock<IProjectService>(MockBehavior.Strict);
        projects.Setup(p => p.GetByIdAsync(PublicProject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectDto>.Success(new ProjectDto { Id = PublicProject, CreatedById = Founder }));
        projects.Setup(p => p.GetByIdAsync(HiddenProject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hiddenProjectExists
                ? Result<ProjectDto>.Success(new ProjectDto { Id = HiddenProject, CreatedById = Founder })
                : Result<ProjectDto>.NotFound($"Project with id {HiddenProject} not found"));

        // The hidden project is a draft only its founder sees.
        var visibility = new Mock<IProjectVisibility>(MockBehavior.Strict);
        visibility.Setup(v => v.GetVisibleByIdAsync(It.IsAny<ProjectViewer>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectViewer viewer, string id, CancellationToken _) =>
                id == PublicProject || (id == HiddenProject && hiddenProjectExists && viewer.UserId == Founder)
                    ? Result<ProjectDto>.Success(new ProjectDto { Id = id, CreatedById = Founder })
                    : Result<ProjectDto>.NotFound($"Project with id {id} not found"));

        var hierarchy = new Mock<IHierarchyAuthorizationService>(MockBehavior.Strict);
        hierarchy.Setup(h => h.CanManageProjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string actor, string _, CancellationToken _) => actor == Founder);

        var controller = new ProjectsController(
            projects.Object,
            visibility.Object,
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
                User = actorId is null
                    ? new ClaimsPrincipal(new ClaimsIdentity())
                    : new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, actorId), new Claim(ActorAssertionMiddleware.ClaimType, "v2")],
                        ActorAssertionMiddleware.AuthenticationType)),
            },
        };
        return controller;
    }
}
