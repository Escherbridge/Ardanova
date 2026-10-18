namespace ArdaNova.API.Tests.Controllers;

using ArdaNova.API.Controllers;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// /api/projects/{projectId}/proposals/* reads answer with the same viewer policy as /api/Governance/*:
/// a proposal the viewer may not see (DRAFT to a non-author) or one filed under another project is 404.
/// </summary>
public class ProjectProposalVisibilityTests
{
    public static TheoryData<string> ProposalReads =>
    [
        nameof(ProjectsController.GetProposalById),
        nameof(ProjectsController.GetProposalComments),
        nameof(ProjectsController.GetProposalVoteSummary),
        nameof(ProjectsController.GetVotes),
    ];

    [Theory]
    [MemberData(nameof(ProposalReads))]
    public async Task HiddenProposal_IsNotFound(string action)
    {
        var governance = new Mock<IGovernanceService>(MockBehavior.Strict);
        governance.Setup(g => g.GetVisibleProposalByIdAsync(It.IsAny<ProjectViewer>(), "proposal-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProposalDto>.NotFound("Proposal not found"));

        var result = await Invoke(Controller(governance.Object), action, "project-1");

        result.Should().BeAssignableTo<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Theory]
    [MemberData(nameof(ProposalReads))]
    public async Task ProposalFromAnotherProject_IsNotFound(string action)
    {
        var governance = new Mock<IGovernanceService>(MockBehavior.Strict);
        governance.Setup(g => g.GetVisibleProposalByIdAsync(It.IsAny<ProjectViewer>(), "proposal-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProposalDto>.Success(Proposal(projectId: "project-2")));

        var result = await Invoke(Controller(governance.Object), action, "project-1");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Theory]
    [MemberData(nameof(ProposalReads))]
    public async Task VisibleProposalInItsProject_IsServed(string action)
    {
        var governance = new Mock<IGovernanceService>(MockBehavior.Strict);
        governance.Setup(g => g.GetVisibleProposalByIdAsync(It.IsAny<ProjectViewer>(), "proposal-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProposalDto>.Success(Proposal(projectId: "project-1")));
        governance.Setup(g => g.GetProposalCommentsAsync("proposal-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ProposalCommentDto>>.Success([]));
        governance.Setup(g => g.GetVoteSummaryAsync("proposal-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProposalVoteSummaryDto>.Success(new ProposalVoteSummaryDto()));
        governance.Setup(g => g.GetVotesAsync("proposal-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<VoteDto>>.Success([]));

        var result = await Invoke(Controller(governance.Object), action, "project-1");

        result.Should().BeOfType<OkObjectResult>();
    }

    private static Task<IActionResult> Invoke(ProjectsController controller, string action, string projectId) => action switch
    {
        nameof(ProjectsController.GetProposalById) => controller.GetProposalById(projectId, "proposal-1", CancellationToken.None),
        nameof(ProjectsController.GetProposalComments) => controller.GetProposalComments(projectId, "proposal-1", CancellationToken.None),
        nameof(ProjectsController.GetProposalVoteSummary) => controller.GetProposalVoteSummary(projectId, "proposal-1", CancellationToken.None),
        nameof(ProjectsController.GetVotes) => controller.GetVotes(projectId, "proposal-1", CancellationToken.None),
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    private static ProposalDto Proposal(string projectId) => new()
    {
        Id = "proposal-1",
        ProjectId = projectId,
        CreatorId = "author-1",
        Title = "Proposal",
        Description = "",
        Options = "[]",
    };

    private static ProjectsController Controller(IGovernanceService governance)
    {
        var visibility = new Mock<IProjectVisibility>();
        visibility.Setup(v => v.GetVisibleByIdAsync(It.IsAny<ProjectViewer>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectViewer _, string id, CancellationToken _) =>
                Result<ProjectDto>.Success(new ProjectDto { Id = id }));

        var controller = new ProjectsController(
            Mock.Of<IProjectService>(),
            visibility.Object,
            Mock.Of<IProjectResourceService>(),
            Mock.Of<IProjectMilestoneService>(),
            Mock.Of<IProjectMemberService>(),
            Mock.Of<IProjectApplicationService>(),
            Mock.Of<IProjectCommentService>(),
            Mock.Of<IProjectUpdateService>(),
            Mock.Of<IProjectSupportService>(),
            governance,
            Mock.Of<IProposalVoteClosing>(),
            Mock.Of<IProjectInvitationService>(),
            Mock.Of<IHierarchyAuthorizationService>(),
            Mock.Of<IMembershipCredentialService>());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }
}
