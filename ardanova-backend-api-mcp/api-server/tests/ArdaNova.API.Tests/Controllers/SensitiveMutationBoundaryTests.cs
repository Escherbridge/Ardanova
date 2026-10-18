namespace ArdaNova.API.Tests.Controllers;

using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public sealed class SensitiveMutationBoundaryTests
{
    [Fact]
    public async Task ProjectTokenAwardApproval_RefusesARedirectedRecipientBeforeServiceInvocation()
    {
        var tokens = new Mock<IProjectTokenService>(MockBehavior.Strict);
        var provisioning = new Mock<IProjectTokenProvisioning>(MockBehavior.Strict);
        var allocations = new Mock<IRepository<TokenAllocation>>();
        allocations
            .Setup(repository => repository.GetByIdAsync("allocation-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenAllocation { id = "allocation-1", recipientUserId = "worker-1" });
        var controller = CreateController(tokens, provisioning, allocations: allocations);

        var result = await controller.ApproveWorkCompletion("allocation-1", "recipient-1", CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        provisioning.VerifyNoOtherCalls();
        tokens.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProjectTokenBacklogAllocation_ResolvesTheProjectBeforeTouchingTheService()
    {
        var tokens = new Mock<IProjectTokenService>(MockBehavior.Strict);
        var configs = new Mock<IRepository<ProjectTokenConfig>>();
        configs
            .Setup(repository => repository.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTokenConfig?)null);
        var controller = CreateController(tokens, configs: configs);

        var result = await controller.AllocateToPbi(
            "config-1",
            new CreateTokenAllocationDto { PbiId = "task-1", EquityPercentage = 1 },
            CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        tokens.VerifyNoOtherCalls();
    }

    [Fact]
    public void ProjectTokenSponsorAndFounderAllocation_FailsClosedBeforeServiceInvocation()
    {
        var tokens = new Mock<IProjectTokenService>(MockBehavior.Strict);
        var controller = CreateController(tokens);

        var results = new IActionResult[]
        {
            controller.AllocateToInvestor("config-1", new CreateInvestorAllocationDto { UserId = "user-1", UsdAmount = 10, TokenAmount = 1 }, CancellationToken.None),
            controller.AllocateToFounder("config-1", new CreateFounderAllocationDto { UserId = "user-1", EquityPercentage = 1 }, CancellationToken.None),
        };

        results.Should().AllSatisfy(result => AssertPaused(result, "Provider-verified account eligibility"));
        tokens.VerifyNoOtherCalls();
    }

    private static ProjectTokensController CreateController(
        Mock<IProjectTokenService> tokens,
        Mock<IProjectTokenProvisioning>? provisioning = null,
        Mock<IRepository<ProjectTokenConfig>>? configs = null,
        Mock<IRepository<TokenAllocation>>? allocations = null)
        => new(
            tokens.Object,
            new Mock<IProjectGateService>(MockBehavior.Strict).Object,
            (provisioning ?? new Mock<IProjectTokenProvisioning>(MockBehavior.Strict)).Object,
            (configs ?? new Mock<IRepository<ProjectTokenConfig>>(MockBehavior.Strict)).Object,
            (allocations ?? new Mock<IRepository<TokenAllocation>>(MockBehavior.Strict)).Object,
            new Mock<IRepository<ProjectInvestment>>(MockBehavior.Strict).Object,
            new Mock<IRepository<User>>(MockBehavior.Strict).Object,
            new Mock<IHierarchyAuthorizationService>(MockBehavior.Strict).Object);

    [Fact]
    public void CredentialUtilityMutations_FailClosedBeforeServiceInvocation()
    {
        var credentials = new Mock<ICredentialUtilityService>(MockBehavior.Strict);
        var controller = new CredentialUtilityController(credentials.Object);

        var results = new IActionResult[]
        {
            controller.GrantAndMint(null!, CancellationToken.None),
            controller.RevokeAndBurn("credential-1", CancellationToken.None),
            controller.UpgradeTier("credential-1", null!, CancellationToken.None),
            controller.CheckAutoGrant(null!, CancellationToken.None),
            controller.RetryMint("credential-1", CancellationToken.None),
        };

        results.Should().AllSatisfy(result => AssertPaused(result, "Actor-bound scope"));
        credentials.VerifyNoOtherCalls();
    }

    [Fact]
    public void MembershipCredentialMutations_FailClosedBeforeServiceInvocation()
    {
        var credentials = new Mock<IMembershipCredentialService>(MockBehavior.Strict);
        var controller = new MembershipCredentialsController(credentials.Object);

        var results = new IActionResult[]
        {
            controller.Grant(null!, CancellationToken.None),
            controller.Revoke("credential-1", null, CancellationToken.None),
            controller.Suspend("credential-1", CancellationToken.None),
            controller.Reactivate("credential-1", CancellationToken.None),
            controller.UpdateMintInfo("credential-1", null!, CancellationToken.None),
            controller.UpdateTier("credential-1", null!, CancellationToken.None),
        };

        results.Should().AllSatisfy(result => AssertPaused(result, "server-derived grant authority"));
        credentials.VerifyNoOtherCalls();
    }

    /// <summary>The ballot names the signed-in member: a voter id or weight supplied by the caller is discarded before the service ever sees it.</summary>
    [Fact]
    public async Task GovernanceVote_BindsTheBallotToTheActorAndDiscardsCallerSuppliedWeight()
    {
        var governance = new Mock<IGovernanceService>(MockBehavior.Strict);
        governance
            .Setup(service => service.CastVoteAsync(
                "proposal-1",
                It.Is<CastVoteDto>(dto => dto.VoterId == "actor-1" && dto.Weight == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VoteDto>.Success(new VoteDto { Id = "vote-1", ProposalId = "proposal-1", VoterId = "actor-1" }));
        var controller = WithActor(
            new GovernanceController(governance.Object, new Mock<IHierarchyAuthorizationService>(MockBehavior.Strict).Object, new Mock<IProposalVoteClosing>(MockBehavior.Strict).Object),
            "actor-1");

        var result = await controller.CastVote(
            "proposal-1",
            new CastVoteDto { VoterId = "someone-else", Choice = 0, Weight = 999m },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        governance.VerifyAll();
        governance.VerifyNoOtherCalls();
    }

    /// <summary>A comment is authored by the signed-in member; the body user id is overwritten, not trusted.</summary>
    [Fact]
    public async Task GovernanceComment_BindsAuthorshipToTheActor()
    {
        var governance = new Mock<IGovernanceService>(MockBehavior.Strict);
        governance
            .Setup(service => service.CreateProposalCommentAsync(
                It.Is<CreateProposalCommentDto>(dto => dto.UserId == "actor-1" && dto.ProposalId == "proposal-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProposalCommentDto>.Success(new ProposalCommentDto { Id = "comment-1", ProposalId = "proposal-1", UserId = "actor-1", Content = "hi" }));
        var controller = WithActor(
            new GovernanceController(governance.Object, new Mock<IHierarchyAuthorizationService>(MockBehavior.Strict).Object, new Mock<IProposalVoteClosing>(MockBehavior.Strict).Object),
            "actor-1");

        var result = await controller.CreateProposalComment(
            "proposal-1",
            new CreateProposalCommentDto { ProposalId = "someone-elses-proposal", UserId = "someone-else", Content = "hi" },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        governance.VerifyAll();
        governance.VerifyNoOtherCalls();
    }

    /// <summary>Raising a proposal on a project you are not in is refused before the service is reached.</summary>
    [Fact]
    public async Task GovernanceProposalCreation_RefusesANonMemberBeforeServiceInvocation()
    {
        var governance = new Mock<IGovernanceService>(MockBehavior.Strict);
        var hierarchy = new Mock<IHierarchyAuthorizationService>(MockBehavior.Strict);
        hierarchy
            .Setup(service => service.IsProjectMemberAsync("actor-1", "project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = WithActor(new GovernanceController(governance.Object, hierarchy.Object, new Mock<IProposalVoteClosing>(MockBehavior.Strict).Object), "actor-1");

        var result = await controller.CreateProposal(
            new CreateProposalDto
            {
                ProjectId = "project-1",
                CreatorId = "someone-else",
                Type = ProposalType.OPERATIONAL,
                Title = "t",
                Description = "d",
                Options = OptionsJson,
            },
            CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        governance.VerifyNoOtherCalls();
    }

    /// <summary>Lifecycle actions belong to the people who run the project (or, where allowed, the member who raised it) - a bystander is refused before anything mutates.</summary>
    [Fact]
    public async Task GovernanceLifecycleActions_RefuseABystanderBeforeMutating()
    {
        var governance = new Mock<IGovernanceService>(MockBehavior.Strict);
        governance
            .Setup(service => service.GetProposalByIdAsync("proposal-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProposalDto>.Success(new ProposalDto
            {
                Id = "proposal-1",
                ProjectId = "project-1",
                CreatorId = "creator-1",
            }));
        var hierarchy = new Mock<IHierarchyAuthorizationService>(MockBehavior.Strict);
        hierarchy
            .Setup(service => service.CanManageProjectAsync("bystander-1", "project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = WithActor(new GovernanceController(governance.Object, hierarchy.Object, new Mock<IProposalVoteClosing>(MockBehavior.Strict).Object), "bystander-1");

        var results = new IActionResult[]
        {
            await controller.UpdateProposal("proposal-1", new UpdateProposalDto(), CancellationToken.None),
            await controller.DeleteProposal("proposal-1", CancellationToken.None),
            await controller.ExecuteProposal("proposal-1", CancellationToken.None),
            await controller.CloseProposal("proposal-1", CancellationToken.None),
            await controller.CancelProposal("proposal-1", CancellationToken.None),
            await controller.PublishProposal("proposal-1", CancellationToken.None),
        };

        results.Should().AllSatisfy(result =>
            result.Should().BeOfType<ObjectResult>()
                .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden));
        governance.Verify(
            service => service.GetProposalByIdAsync("proposal-1", It.IsAny<CancellationToken>()),
            Times.Exactly(6));
        governance.VerifyNoOtherCalls();
    }

    [Fact]
    public void ReferralRewardClaim_FailsClosedBeforeServiceInvocation()
    {
        var referrals = new Mock<IReferralService>(MockBehavior.Strict);
        var controller = new ReferralsController(referrals.Object);

        var result = controller.ClaimReward("referral-1", null!, CancellationToken.None);

        AssertPaused(result, "server-derived");
        referrals.VerifyNoOtherCalls();
    }

    private const string OptionsJson = "[\"Yes\",\"No\"]";

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

    private static void AssertPaused(IActionResult result, string expectedDetail)
    {
        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(StatusCodes.Status501NotImplemented);
        response.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Contain(expectedDetail);
    }
}
