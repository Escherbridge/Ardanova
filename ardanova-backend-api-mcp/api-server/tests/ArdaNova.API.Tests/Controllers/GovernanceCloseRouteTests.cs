namespace ArdaNova.API.Tests.Controllers;

using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>PATCH /api/Governance/proposals/{id}/close: a manager who did not raise the proposal can end the vote; its author never can (OI-46).</summary>
public class GovernanceCloseRouteTests
{
    private const string Author = "author-1";

    /// <summary>The closing seam answers exactly as GovernanceService does: only an authority that may end this author's vote early succeeds.</summary>
    private static IProposalVoteClosing ClosingSeam()
    {
        var closing = new Mock<IProposalVoteClosing>();
        closing
            .Setup(c => c.CloseVoteAsync("proposal-1", It.IsAny<ProposalCloseAuthority>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, ProposalCloseAuthority authority, CancellationToken _) =>
                authority.MayEndEarly(Author)
                    ? Result<ProposalDto>.Success(new ProposalDto { Id = id, ProjectId = "project-1", CreatorId = Author })
                    : Result<ProposalDto>.Forbidden("Only someone who runs this project, and who did not raise this proposal, can end the vote before it closes on its own"));
        return closing.Object;
    }

    private static GovernanceController Controller(string actorId, bool runsProject)
    {
        var governance = new Mock<IGovernanceService>();
        governance
            .Setup(g => g.GetProposalByIdAsync("proposal-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProposalDto>.Success(new ProposalDto { Id = "proposal-1", ProjectId = "project-1", CreatorId = Author }));
        var hierarchy = new Mock<IHierarchyAuthorizationService>();
        hierarchy
            .Setup(h => h.CanManageProjectAsync(actorId, "project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(runsProject);

        var controller = new GovernanceController(governance.Object, hierarchy.Object, ClosingSeam());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, actorId)],
                    ActorAssertionMiddleware.AuthenticationType))
            }
        };
        return controller;
    }

    [Fact]
    public async Task A_manager_who_did_not_raise_the_proposal_can_end_the_vote()
    {
        var result = await Controller("founder-1", runsProject: true).CloseProposal("proposal-1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task The_author_is_refused_with_the_reason_whether_or_not_they_run_the_project(bool authorRunsProject)
    {
        var result = await Controller(Author, authorRunsProject).CloseProposal("proposal-1", CancellationToken.None);

        var refusal = result.Should().BeOfType<ObjectResult>().Subject;
        refusal.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        refusal.Value!.ToString().Should().Contain("did not raise this proposal");
    }
}
