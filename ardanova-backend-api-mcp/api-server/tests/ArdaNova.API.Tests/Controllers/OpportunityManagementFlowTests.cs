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

/// <summary>
/// The tRPC opportunity router no longer pre-checks ownership (loop-2 projects lane), so these
/// actions are the only guard: a non-manager is refused before any mutation, a manager succeeds.
/// </summary>
public class OpportunityManagementFlowTests
{
    private const string Actor = "actor-1";
    private const string ManagedProject = "project-1";

    public static TheoryData<string> ManageActions =>
    [
        nameof(OpportunitiesController.Close),
        nameof(OpportunitiesController.Delete),
        nameof(OpportunitiesController.GetApplications),
        nameof(OpportunitiesController.UpdateApplicationStatus),
        nameof(OpportunitiesController.DeleteComment),
    ];

    [Theory]
    [MemberData(nameof(ManageActions))]
    public async Task NonManager_IsForbiddenAndNothingChanges(string action)
    {
        var opportunities = ReadableOpportunity(commentAuthor: "someone-else");
        var controller = Controller(opportunities.Object, actorManagesProject: false);

        var result = await Invoke(controller, action);

        // Strict mock: any mutation call would throw before this assertion.
        result.Should().BeOfType<ForbidResult>();
    }

    [Theory]
    [MemberData(nameof(ManageActions))]
    public async Task ProjectManager_Succeeds(string action)
    {
        var opportunities = ReadableOpportunity(commentAuthor: "someone-else");
        opportunities.Setup(s => s.CloseAsync("opportunity-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityDto>.Success(Opportunity()));
        opportunities.Setup(s => s.DeleteAsync("opportunity-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        opportunities.Setup(s => s.GetApplicationsAsync("opportunity-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<OpportunityApplicationDto>>.Success([Application()]));
        opportunities.Setup(s => s.UpdateApplicationStatusAsync(
                "application-1", It.IsAny<UpdateApplicationStatusDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityApplicationDto>.Success(Application()));
        opportunities.Setup(s => s.DeleteCommentAsync("comment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        var controller = Controller(opportunities.Object, actorManagesProject: true);

        var result = await Invoke(controller, action);

        result.Should().Match<IActionResult>(r => r is OkObjectResult || r is NoContentResult);
    }

    [Fact]
    public async Task UpdateApplicationStatus_IsAuthorizedAgainstTheApplicationsOwnOpportunity()
    {
        // The actor manages project-2, but the application belongs to an opportunity on project-1.
        var opportunities = ReadableOpportunity(commentAuthor: "someone-else");
        var hierarchy = new Mock<IHierarchyAuthorizationService>(MockBehavior.Strict);
        hierarchy.Setup(h => h.CanManageProjectAsync(Actor, ManagedProject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        hierarchy.Setup(h => h.CanManageProjectAsync(Actor, "project-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = WithActor(Create(opportunities.Object, hierarchy.Object));

        var result = await controller.UpdateApplicationStatus(
            "application-1",
            new UpdateApplicationStatusDto { Status = ApplicationStatus.ACCEPTED },
            CancellationToken.None);

        // Strict mock: any mutation call would throw before this assertion.
        result.Should().BeOfType<ForbidResult>();
    }

    private static Task<IActionResult> Invoke(OpportunitiesController controller, string action) => action switch
    {
        nameof(OpportunitiesController.Close) => controller.Close("opportunity-1", CancellationToken.None),
        nameof(OpportunitiesController.Delete) => controller.Delete("opportunity-1", CancellationToken.None),
        nameof(OpportunitiesController.GetApplications) => controller.GetApplications("opportunity-1", CancellationToken.None),
        nameof(OpportunitiesController.UpdateApplicationStatus) => controller.UpdateApplicationStatus(
            "application-1",
            new UpdateApplicationStatusDto { Status = ApplicationStatus.ACCEPTED },
            CancellationToken.None),
        nameof(OpportunitiesController.DeleteComment) => controller.DeleteComment("comment-1", CancellationToken.None),
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    /// <summary>Strict mock that answers only the reads an authorization check needs.</summary>
    private static Mock<IOpportunityService> ReadableOpportunity(string commentAuthor)
    {
        var opportunities = new Mock<IOpportunityService>(MockBehavior.Strict);
        opportunities.Setup(s => s.GetByIdAsync("opportunity-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityDto>.Success(Opportunity()));
        opportunities.Setup(s => s.GetApplicationByIdAsync("application-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityApplicationDto>.Success(Application()));
        opportunities.Setup(s => s.GetCommentByIdAsync("comment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityCommentDto>.Success(new OpportunityCommentDto
            {
                Id = "comment-1",
                OpportunityId = "opportunity-1",
                UserId = commentAuthor,
                Content = "A comment by someone else.",
            }));
        return opportunities;
    }

    private static OpportunityDto Opportunity() => new()
    {
        Id = "opportunity-1",
        PosterId = "poster-1",
        ProjectId = ManagedProject,
        Title = "Opportunity",
        Slug = "opportunity",
        Description = "",
    };

    private static OpportunityApplicationDto Application() => new()
    {
        Id = "application-1",
        OpportunityId = "opportunity-1",
        ApplicantId = "applicant-1",
        CoverLetter = "I can help with this work.",
    };

    private static OpportunitiesController Controller(IOpportunityService opportunities, bool actorManagesProject)
    {
        var hierarchy = new Mock<IHierarchyAuthorizationService>(MockBehavior.Strict);
        hierarchy.Setup(h => h.CanManageProjectAsync(Actor, ManagedProject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(actorManagesProject);
        return WithActor(Create(opportunities, hierarchy.Object));
    }

    private static OpportunitiesController Create(IOpportunityService opportunities, IHierarchyAuthorizationService hierarchy)
        => new(
            opportunities,
            hierarchy,
            new Mock<IGuildService>(MockBehavior.Strict).Object,
            new Mock<IGuildMemberService>(MockBehavior.Strict).Object,
            new Mock<IAzoaValueEligibilityService>(MockBehavior.Strict).Object);

    private static OpportunitiesController WithActor(OpportunitiesController controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, Actor)],
                    ActorAssertionMiddleware.AuthenticationType)),
            },
        };
        return controller;
    }
}
