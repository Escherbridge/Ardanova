namespace ArdaNova.API.Tests.Controllers;

using ArdaNova.API.Controllers;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Domain.Models.Entities;
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

    [Fact]
    public void GovernanceMutations_FailClosedBeforeServiceInvocation()
    {
        var governance = new Mock<IGovernanceService>(MockBehavior.Strict);
        var controller = new GovernanceController(governance.Object);

        var results = new IActionResult[]
        {
            controller.CreateProposal(null!, CancellationToken.None),
            controller.UpdateProposal("proposal-1", null!, CancellationToken.None),
            controller.DeleteProposal("proposal-1", CancellationToken.None),
            controller.CastVote("proposal-1", null!, CancellationToken.None),
            controller.ExecuteProposal("proposal-1", CancellationToken.None),
            controller.CancelProposal("proposal-1", CancellationToken.None),
            controller.PublishProposal("proposal-1", CancellationToken.None),
            controller.CreateProposalComment("proposal-1", null!, CancellationToken.None),
            controller.UpdateProposalComment("proposal-1", "comment-1", "caller-selected-user", null!, CancellationToken.None),
            controller.DeleteProposalComment("proposal-1", "comment-1", "caller-selected-user", CancellationToken.None),
        };

        results.Should().AllSatisfy(result => AssertPaused(result, "Actor-bound proposal"));
        governance.VerifyNoOtherCalls();
    }

    [Fact]
    public void DelegatedVoteMutations_FailClosedBeforeServiceInvocation()
    {
        var delegations = new Mock<IDelegatedVoteService>(MockBehavior.Strict);
        var controller = new DelegatedVotesController(delegations.Object);

        var results = new IActionResult[]
        {
            controller.Create(null!, CancellationToken.None),
            controller.Update("delegation-1", null!, CancellationToken.None),
            controller.Revoke("delegation-1", CancellationToken.None),
        };

        results.Should().AllSatisfy(result => AssertPaused(result, "authenticated delegator"));
        delegations.VerifyNoOtherCalls();
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

    private static void AssertPaused(IActionResult result, string expectedDetail)
    {
        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(StatusCodes.Status501NotImplemented);
        response.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Contain(expectedDetail);
    }
}
