namespace ArdaNova.API.Tests.Controllers;

using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public sealed class ProjectMemberValueBoundaryTests
{
    [Theory]
    [MemberData(nameof(DirectAllocationChanges))]
    public async Task UpdateMember_DirectShareOrVotingAllocationFailsClosedBeforeLookup(
        UpdateProjectMemberDto dto)
    {
        var members = new Mock<IProjectMemberService>(MockBehavior.Strict);
        var controller = WithActor(CreateController(members.Object), "manager-1");

        var result = await controller.UpdateMember(
            "project-1",
            "member-1",
            dto,
            CancellationToken.None);

        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(StatusCodes.Status501NotImplemented);
        response.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Contain("Provider-verified account eligibility");
        members.VerifyNoOtherCalls();
    }

    /// <summary>Voting power is not a settable allocation any more: one confirmed member is one vote, so the request is simply wrong rather than deferred.</summary>
    [Fact]
    public async Task UpdateMember_VotingPowerIsRefusedAsMeaninglessBeforeLookup()
    {
        var members = new Mock<IProjectMemberService>(MockBehavior.Strict);
        var controller = WithActor(CreateController(members.Object), "manager-1");

        var result = await controller.UpdateMember(
            "project-1",
            "member-1",
            new UpdateProjectMemberDto { VotingPower = 1m },
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeEquivalentTo(
                new { error = "Every confirmed member of a project gets one vote, so there is nothing to set here." });
        members.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateMember_RoleOnlyUpdateRemainsAvailable()
    {
        var members = new Mock<IProjectMemberService>();
        members.Setup(service => service.GetByIdAsync("member-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectMemberDto>.Success(new ProjectMemberDto
            {
                Id = "member-1",
                ProjectId = "project-1",
                UserId = "member-user-1",
                Role = ProjectRole.CONTRIBUTOR,
            }));
        members.Setup(service => service.UpdateAsync(
                "member-1",
                It.Is<UpdateProjectMemberDto>(dto => dto.Role == ProjectRole.LEADER &&
                                                   !dto.ShareBalance.HasValue &&
                                                   !dto.VotingPower.HasValue),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectMemberDto>.Success(new ProjectMemberDto
            {
                Id = "member-1",
                ProjectId = "project-1",
                UserId = "member-user-1",
                Role = ProjectRole.LEADER,
            }));
        members.Setup(service => service.GetByProjectIdAsync("project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ProjectMemberDto>>.Success([]));
        var hierarchy = new Mock<IHierarchyAuthorizationService>();
        hierarchy.Setup(service => service.CanManageProjectAsync(
                "manager-1", "project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = WithActor(
            CreateController(members.Object, hierarchy.Object, ProjectCreatedBy("manager-1")),
            "manager-1");

        var result = await controller.UpdateMember(
            "project-1",
            "member-1",
            new UpdateProjectMemberDto { Role = ProjectRole.LEADER },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        members.VerifyAll();
        hierarchy.VerifyAll();
    }

    /// <summary>A CORE_CONTRIBUTOR passes the manager gate but may not raise themself, crown anyone, or delete the project.</summary>
    [Fact]
    public async Task ACoreContributorManager_CannotEscalateRolesOrDeleteTheFoundersProject()
    {
        var members = new Mock<IProjectMemberService>(MockBehavior.Strict);
        members.Setup(service => service.GetByIdAsync("own-row", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectMemberDto>.Success(Member("own-row", "core-1", ProjectRole.CORE_CONTRIBUTOR)));
        members.Setup(service => service.GetByIdAsync("peer-row", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectMemberDto>.Success(Member("peer-row", "peer-1", ProjectRole.CONTRIBUTOR)));
        members.Setup(service => service.GetByProjectIdAsync("project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ProjectMemberDto>>.Success(
                [Member("own-row", "core-1", ProjectRole.CORE_CONTRIBUTOR), Member("peer-row", "peer-1", ProjectRole.CONTRIBUTOR)]));
        var hierarchy = new Mock<IHierarchyAuthorizationService>();
        hierarchy.Setup(service => service.CanManageProjectAsync("core-1", "project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = WithActor(
            CreateController(members.Object, hierarchy.Object, ProjectCreatedBy("founder-1")),
            "core-1");

        var raiseSelf = await controller.UpdateMember(
            "project-1", "own-row", new UpdateProjectMemberDto { Role = ProjectRole.LEADER }, CancellationToken.None);
        var crownPeer = await controller.UpdateMember(
            "project-1", "peer-row", new UpdateProjectMemberDto { Role = ProjectRole.FOUNDER }, CancellationToken.None);
        var promotePeerToOwnRank = await controller.UpdateMember(
            "project-1", "peer-row", new UpdateProjectMemberDto { Role = ProjectRole.CORE_CONTRIBUTOR }, CancellationToken.None);
        var seatStranger = await controller.AddMember(
            "project-1", new CreateProjectMemberDto { UserId = "stranger-1", Role = ProjectRole.CONTRIBUTOR }, CancellationToken.None);
        var deleteProject = await controller.Delete("project-1", CancellationToken.None);
        // Invitations are another way to seat a role; the strict invitation mock throws if one is created.
        var inviteLeader = await controller.CreateInvitation(
            "project-1",
            new CreateProjectInvitationDto { ProjectId = "", InvitedById = "", Role = "LEADER", InvitedUserId = "sock-puppet" },
            CancellationToken.None);
        var inviteFounder = await controller.CreateInvitation(
            "project-1",
            new CreateProjectInvitationDto { ProjectId = "", InvitedById = "", Role = "FOUNDER", InvitedUserId = "sock-puppet" },
            CancellationToken.None);

        RefusalOf(inviteLeader).Should().Be(ProjectMemberRolePolicy.OutranksActor);
        RefusalOf(inviteFounder).Should().Be(ProjectMemberRolePolicy.FounderIsNotGrantable);
        RefusalOf(raiseSelf).Should().Be(ProjectMemberRolePolicy.CannotChangeOwnRole);
        RefusalOf(crownPeer).Should().Be(ProjectMemberRolePolicy.FounderIsNotGrantable);
        RefusalOf(promotePeerToOwnRank).Should().Be(ProjectMemberRolePolicy.OutranksActor);
        RefusalOf(seatStranger).Should().Be(ProjectMemberRolePolicy.FounderSeatOnly);
        RefusalOf(deleteProject).Should().Be(ProjectMemberRolePolicy.FounderOnlyDelete);
    }

    /// <summary>Project creation's step 4: the creator seats their own founder row, attributed to themself.</summary>
    [Fact]
    public async Task TheCreator_SeatsTheirOwnFounderRow()
    {
        var members = new Mock<IProjectMemberService>(MockBehavior.Strict);
        members.Setup(service => service.CreateAsync(
                It.Is<CreateProjectMemberDto>(dto => dto.ProjectId == "project-1" && dto.UserId == "founder-1" &&
                                                     dto.Role == ProjectRole.FOUNDER && dto.InvitedById == "founder-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectMemberDto>.Success(Member("founder-row", "founder-1", ProjectRole.FOUNDER)));
        var controller = WithActor(
            CreateController(members.Object, projects: ProjectCreatedBy("founder-1")),
            "founder-1");

        var result = await controller.AddMember(
            "project-1",
            new CreateProjectMemberDto { UserId = "founder-1", Role = ProjectRole.FOUNDER, InvitedById = "spoofed" },
            CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>()
            .Which.Value.Should().BeOfType<ProjectMemberDto>().Which.Role.Should().Be(ProjectRole.FOUNDER);
    }

    private static string? RefusalOf(IActionResult result)
    {
        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        return response.Value!.GetType().GetProperty("error")!.GetValue(response.Value) as string;
    }

    private static ProjectMemberDto Member(string id, string userId, ProjectRole role)
        => new() { Id = id, ProjectId = "project-1", UserId = userId, Role = role };

    private static IProjectService ProjectCreatedBy(string creatorId)
    {
        var projects = new Mock<IProjectService>(MockBehavior.Strict);
        projects.Setup(service => service.GetByIdAsync("project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectDto>.Success(new ProjectDto { Id = "project-1", CreatedById = creatorId }));
        return projects.Object;
    }

    public static TheoryData<UpdateProjectMemberDto> DirectAllocationChanges =>
    [
        new UpdateProjectMemberDto { ShareBalance = 1m },
    ];

    private static ProjectsController CreateController(
        IProjectMemberService members,
        IHierarchyAuthorizationService? hierarchy = null,
        IProjectService? projects = null)
        => new(
            projects ?? new Mock<IProjectService>(MockBehavior.Strict).Object,
            new Mock<IProjectVisibility>(MockBehavior.Strict).Object,
            new Mock<IProjectResourceService>(MockBehavior.Strict).Object,
            new Mock<IProjectMilestoneService>(MockBehavior.Strict).Object,
            members,
            new Mock<IProjectApplicationService>(MockBehavior.Strict).Object,
            new Mock<IProjectCommentService>(MockBehavior.Strict).Object,
            new Mock<IProjectUpdateService>(MockBehavior.Strict).Object,
            new Mock<IProjectSupportService>(MockBehavior.Strict).Object,
            new Mock<IGovernanceService>(MockBehavior.Strict).Object,
            new Mock<IProposalVoteClosing>(MockBehavior.Strict).Object,
            new Mock<IProjectInvitationService>(MockBehavior.Strict).Object,
            hierarchy ?? new Mock<IHierarchyAuthorizationService>(MockBehavior.Strict).Object,
            new Mock<IMembershipCredentialService>(MockBehavior.Strict).Object);

    private static T WithActor<T>(T controller, string actorId)
        where T : ControllerBase
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, actorId)],
                ActorAssertionMiddleware.AuthenticationType))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }
}
