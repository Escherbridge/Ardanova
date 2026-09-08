namespace ArdaNova.API.Tests.Controllers;

using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
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
        var hierarchy = new Mock<IHierarchyAuthorizationService>();
        hierarchy.Setup(service => service.CanManageProjectAsync(
                "manager-1", "project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = WithActor(CreateController(members.Object, hierarchy.Object), "manager-1");

        var result = await controller.UpdateMember(
            "project-1",
            "member-1",
            new UpdateProjectMemberDto { Role = ProjectRole.LEADER },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        members.VerifyAll();
        hierarchy.VerifyAll();
    }

    public static TheoryData<UpdateProjectMemberDto> DirectAllocationChanges =>
    [
        new UpdateProjectMemberDto { ShareBalance = 1m },
        new UpdateProjectMemberDto { VotingPower = 1m },
    ];

    private static ProjectsController CreateController(
        IProjectMemberService members,
        IHierarchyAuthorizationService? hierarchy = null)
        => new(
            new Mock<IProjectService>(MockBehavior.Strict).Object,
            new Mock<IProjectResourceService>(MockBehavior.Strict).Object,
            new Mock<IProjectMilestoneService>(MockBehavior.Strict).Object,
            members,
            new Mock<IProjectApplicationService>(MockBehavior.Strict).Object,
            new Mock<IProjectCommentService>(MockBehavior.Strict).Object,
            new Mock<IProjectUpdateService>(MockBehavior.Strict).Object,
            new Mock<IProjectSupportService>(MockBehavior.Strict).Object,
            new Mock<IGovernanceService>(MockBehavior.Strict).Object,
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
